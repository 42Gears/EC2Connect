/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2;
using Amazon.EC2.Model;
using EC2Connect.Popup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EC2Connect.Code
{
    public static class Utility
    {
        public static string GetInstnaceName(Instance instance)
        {
            try
            {
                return instance.Tags.Where(x => x.Key == "Name").Select(s => s.Value).FirstOrDefault();
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static void AllowPort(IAmazonEC2 ec2Client, Instance instance, string[] publicIPs, string proto, int port)
        {
            if (publicIPs != null && publicIPs.Length > 0)
            {
                AuthorizeSecurityGroupIngressRequest ingressRequest = new AuthorizeSecurityGroupIngressRequest();
                ingressRequest.GroupId = instance.SecurityGroups[0].GroupId;

                publicIPs.ToList().ForEach(x =>
                {
                    ingressRequest.IpPermissions.Add(new IpPermission()
                    {
                        IpProtocol = proto,
                        FromPort = port,
                        ToPort = port,
                        IpRanges = new List<string>() { x.Trim() + "/32" }
                    });
                });

                try
                {
                    var res = ec2Client.AuthorizeSecurityGroupIngress(ingressRequest);
                    Console.WriteLine("Allowing Port " + port + " for IP Address " + string.Join(",", publicIPs));
                }
                catch
                {
                    // Ignore //
                }
            }
        }

        public static void DoRDP(AmazonEC2Client ec2Client, Instance instance)
        {
            string publicIP = instance.PublicIpAddress;
            // remove existing key from credential manager
            ExecuteCommandSync("cmd", string.Format("/c cmdkey /delete:TERMSRV/{0}", publicIP), false);

            string pwd = GetPassword(ec2Client, instance, ".pem");
            if (string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show("Empty Password");
            }
            else
            {
                ExecuteCommandSync("cmd", string.Format("/c cmdkey /generic:TERMSRV/{0} /user: Administrator /pass: \"{1}\"", publicIP, pwd), false);

                // Create mstsc file
                string tempRdpFile = GetTempFilePathWithExtension(".rdp");
                File.WriteAllText(tempRdpFile, "auto connect:i:1" + Environment.NewLine +
                    "full address:s:" + publicIP + Environment.NewLine +
                    "username:s:Administrator" + Environment.NewLine + "authentication level:i:0");
                ExecuteCommandSync("mstsc", tempRdpFile, true);
            }
        }

        private static void ExecuteCommandSync(string command, string arguments, bool useBackground)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(command, arguments);
                if (useBackground)
                {
                    procStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                procStartInfo.RedirectStandardOutput = false;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static string GetPasswordFromUserOrStore(string instanceId, string dialogMessage)
        {
            string password = KeyStore.GetPassword(instanceId);
            if (string.IsNullOrWhiteSpace(password))
            {
                AskPassword dialogue = new AskPassword();
                dialogue.ShowInTaskbar = false;
                dialogue.SizeToContent = SizeToContent.WidthAndHeight;
                dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
                dialogue.ResizeMode = ResizeMode.NoResize;

                dialogue.DisplayMessage = dialogMessage;
                bool? dialogResult = dialogue.ShowDialog();
                if (dialogResult != null && (bool)dialogResult)
                {
                    string userPassword = dialogue.ManualPassword;
                    if (!string.IsNullOrWhiteSpace(userPassword))
                    {
                        KeyStore.SetPassword(instanceId, userPassword);
                        return userPassword;
                    }
                }
            }
            else
            {
                return password;
            }
            return null;
        }

        private static string GetPassword(AmazonEC2Client ec2Client, Instance instance, string extension)
        {
            try
            {
                GetPasswordDataRequest req = new GetPasswordDataRequest(instance.InstanceId);
                GetPasswordDataResponse resp = ec2Client.GetPasswordData(req);
                if (!string.IsNullOrWhiteSpace(resp.PasswordData))
                {
                    if (!File.Exists(instance.KeyName + extension))
                    {
                        return GetPasswordFromUserOrStore(instance.InstanceId, "The File " + instance.KeyName + extension + " does not exist.");
                    }
                    return resp.GetDecryptedPassword(File.ReadAllText(instance.KeyName + ".pem").Trim());
                }
                else
                {
                    return GetPasswordFromUserOrStore(instance.InstanceId, "Password not available with Amazon");
                }
            }
            catch (Exception ex)
            {
                GetPasswordFromUserOrStore(instance.InstanceId, "Cannot fetch password: " + ex.Message);
            }
            return string.Empty;
        }

        private static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + extension;
            return Path.Combine(path, fileName);
        }

        private static readonly string[] IpProviders = new string[] {
                 "http://ipinfo.io/ip", "http://canihazip.com/s",
                 "http://bot.whatismyipaddress.com" ,"http://icanhazip.com" };

        private static string[] _publicIPs = null;
        public static string[] PublicIPs
        {
            get
            {
                return _publicIPs;
            }
        }

        private static string[] GetAllPublicIp(string[] urls)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = urls.Length * 2;
            SortedSet<string> allIps = new SortedSet<string>();
            Parallel.ForEach(urls, x =>
            {
                string externalip = FetchPublicIp(x);
                if (!string.IsNullOrWhiteSpace(externalip))
                {
                    allIps.Add(externalip);
                }
            });
            return allIps.ToArray();
        }

        internal static void PublicIpWatcher()
        {
            while(true)
            {
                try
                {
                    string[] newIPs = GetAllPublicIp(IpProviders);
                    if (newIPs != null && newIPs.Length > 0)
                    {
                        if (_publicIPs == null || _publicIPs.Length == 0)
                        {
                            OnPublicIPsUpdated(newIPs);
                        }
                        else
                        {
                            SortedSet<string> newSet = new SortedSet<string>(_publicIPs);
                            newSet.UnionWith(newIPs);
                            if (newSet.Count > _publicIPs.Length)
                            {
                                OnPublicIPsUpdated(newSet.ToArray());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Thread.Sleep(1024 * 64 * 4);    // ~ 4 minutes
            }
        }

        public delegate void OnPublicIPChanged();
        public static event OnPublicIPChanged PublicIPChangedListeners;

        private static void OnPublicIPsUpdated(string[] newPublicIps)
        {
            if(newPublicIps!=null && newPublicIps.Length > 0)
            {
                _publicIPs = newPublicIps;
                if(PublicIPChangedListeners!=null)
                {
                    PublicIPChangedListeners();
                }
            }
        }

        private static string FetchPublicIp(string providerUrl)
        {
            string externalip;
            try
            {
                using (WebClient httpClient = new WebClient())
                {
                    httpClient.Proxy = null;
                    externalip = httpClient.DownloadStringTaskAsync(providerUrl).Result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                externalip = null;
            }

            if (!string.IsNullOrWhiteSpace(externalip))
            {
                System.Net.IPAddress ip;
                if (System.Net.IPAddress.TryParse(externalip.Trim(), out ip))
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        internal static void DoSSH(Instance instance)
        {
            DoSSHorSCP(instance, "SSH");
        }

        internal static void DoSCP(Instance instance)
        {
            DoSSHorSCP(instance, "SCP");
        }

        private static void DoSSHorSCP(Instance instance, string command)
        {
            string PrivateKey = instance.KeyName + ".ppk";
            if (!File.Exists(PrivateKey))
            {
                MessageBox.Show("The File " + PrivateKey + " does not exist.\nPlease create this file and try again.");
                return;
            }

            string userName = LinuxUser.GetUser(instance.InstanceId);
            if (string.IsNullOrWhiteSpace(userName))
            {
                GetUserDialogue dialogue = new GetUserDialogue();
                dialogue.ShowInTaskbar = false;
                dialogue.SizeToContent = SizeToContent.WidthAndHeight;
                dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
                dialogue.ResizeMode = ResizeMode.NoResize;

                bool? dialogResult = dialogue.ShowDialog();
                if (dialogResult != null && (bool)dialogResult)
                {
                    userName = dialogue.UserName;
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        LinuxUser.SetUser(instance.InstanceId, userName);
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(userName))
            {
                string login = userName + "@" + instance.PublicDnsName;
                if (command.Equals("SSH"))
                {
                    ExecuteCommandSync("putty.exe", "-ssh " + login + " -i " + PrivateKey, true);
                }
                else if (command.Equals("SCP"))
                {
                    ExecuteCommandSync("WinSCP.exe", "scp://" + login + " /privatekey=" + PrivateKey, true);
                }
            }
        }
    }
}