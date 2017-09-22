/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
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
using System.Threading.Tasks;
using System.Windows;

namespace EC2Connect.Code
{
    public static class Utility
    {
        public static async Task<List<InstanceModel>> GetInstanceListAsync(AmazonEC2Client EC2Client)
        {
            return await FetchInstanceListAsync(EC2Client).ConfigureAwait(continueOnCapturedContext: false);
        }
        private static async Task<List<InstanceModel>> FetchInstanceListAsync(AmazonEC2Client EC2Client)
        {
            DateTime t1 = DateTime.Now;
            Dictionary<string, string> staticIPs = await GetStaticIps(EC2Client);
            List<InstanceModel> allInstances = new List<InstanceModel>();
            DateTime tt1 = DateTime.Now;
            DescribeInstancesResponse instancesResponse = await EC2Client.DescribeInstancesAsync();
            DateTime tt2 = DateTime.Now;
            Logger.Log("Time taken by DescribeInstancesAsync = " + (tt2 - tt1).TotalMilliseconds + " ms");
            List<Reservation> reservations = instancesResponse.Reservations;
            if (reservations != null && reservations.Count > 0)
            {
                foreach (Reservation reservation in reservations)
                {
                    var instances = reservation.Instances;
                    if (instances != null && instances.Count > 0)
                    {
                        foreach (Instance instance in instances)
                        {
                            if (!string.IsNullOrWhiteSpace(instance.PublicIpAddress))
                            {
                                allInstances.Add(new InstanceModel(instance));
                            }
                            else if (staticIPs.ContainsKey(instance.InstanceId))
                            {
                                instance.PublicIpAddress = staticIPs[instance.InstanceId];
                                allInstances.Add(new InstanceModel(instance));
                            }
                        }
                    }
                }
            }
            DateTime t2 = DateTime.Now;
            Logger.Log("Time taken by GetInstanceList = " + (t2 - t1).TotalMilliseconds + " ms");
            return allInstances;
        }

        private static async Task<Dictionary<string, string>> GetStaticIps(AmazonEC2Client eC2Client)
        {
            DateTime t1 = DateTime.Now;
            DescribeAddressesResponse addressesResponse = await eC2Client.DescribeAddressesAsync();
            HashSet<Address> addresses = new HashSet<Address>(addressesResponse.Addresses);
            Dictionary<string, string> staticIPs = new Dictionary<string, string>();
            if (addresses != null && addresses.Count > 0)
            {
                foreach (Address address in addresses)
                {
                    if (!string.IsNullOrWhiteSpace(address.PublicIp) && !string.IsNullOrWhiteSpace(address.InstanceId))
                    {
                        staticIPs.Add(address.InstanceId, address.PublicIp);
                    }
                }
            }
            DateTime t2 = DateTime.Now;
            Logger.Log("Time taken by GetStaticIps = " + (t2 - t1).TotalMilliseconds + " ms");
            return staticIPs;
        }

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

        internal static async Task AllowPorts(IAmazonEC2 ec2Client, Instance instance, string[] publicIPs, string proto, int port)
        {
            if (publicIPs != null && publicIPs.Length > 0)
            {
                foreach (string publicIP in publicIPs)
                {
                    AuthorizeSecurityGroupIngressRequest ingressRequest = new AuthorizeSecurityGroupIngressRequest();
                    ingressRequest.GroupId = instance.SecurityGroups[0].GroupId;
                    ingressRequest.IpPermissions.Add(new IpPermission()
                    {
                        IpProtocol = proto,
                        FromPort = port,
                        ToPort = port,
                        IpRanges = new List<string>() { publicIP.Trim() + "/32" }
                    });

                    try
                    {
                        AuthorizeSecurityGroupIngressResponse res = await ec2Client.AuthorizeSecurityGroupIngressAsync(ingressRequest);
                        Logger.Log("Allowing Port " + port + " for IP Address " + publicIP.Trim() + "/32");
                    }
                    catch
                    {
                        // Ignore //
                    }
                }
            }
        }

        /// <summary>
        /// White lists the current public IP and initiates RDP on the instance
        /// </summary>
        /// <returns>Error message if any, null otherwise</returns>
        public static async Task<string> RdpAsync(AmazonEC2Client ec2Client, InstanceModel item)
        {
            try
            {
                if (item != null && ec2Client != null)
                {
                    await Utility.AllowPorts(ec2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 3389);
                    return await Utility.DoRDP(ec2Client, item.AmazonInstance);
                }
                else
                {
                    return "Invalid Configuration";
                }
            }
            catch (Exception ex)
            {
                Logger.Log("RdpAsync error", ex);
                return ex.Message;
            }
        }

        public static async Task<string> DoRDP(AmazonEC2Client ec2Client, Instance instance)
        {
            string publicIP = instance.PublicIpAddress;
            if (!string.IsNullOrWhiteSpace(publicIP))
            {
                // remove existing key from credential manager
                await Task.Run(() =>
                    ExecuteCommandSync("cmd", string.Format("/c cmdkey /delete:TERMSRV/{0}", publicIP), false));

                string pwd = await GetPassword(ec2Client, instance, ".pem");
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    return "Empty Password";
                }
                else
                {
                    await Task.Run(() =>
                        ExecuteCommandSync("cmd", string.Format("/c cmdkey /generic:TERMSRV/{0} /user: Administrator /pass: \"{1}\"", publicIP, pwd), false))
                        .ConfigureAwait(continueOnCapturedContext: false);

                    // Create mstsc file
                    string tempRdpFile = GetTempFilePathWithExtension(".rdp");
                    File.WriteAllText(tempRdpFile, "auto connect:i:1" + Environment.NewLine +
                        "full address:s:" + publicIP + Environment.NewLine +
                        "username:s:Administrator" + Environment.NewLine + "authentication level:i:0");

                    await Task.Run(() =>
                        ExecuteCommandSync("mstsc", tempRdpFile, true))
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            else
            {
                return "Cannot fetch public IP for this instance";
            }
            return null;
        }

        private static void ExecuteCommandSync(string command, string arguments, bool useBackground)
        {
            try
            {
                ProcessStartInfo procStartInfo = new ProcessStartInfo(command, arguments);
                if (useBackground)
                {
                    procStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                procStartInfo.RedirectStandardOutput = false;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
            }
            catch (Exception ex)
            {
                Logger.Log("Error in command line execution", ex);
            }
        }

        private static string GetPasswordFromUserOrStore(string instanceId, string dialogMessage)
        {
            try
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
            }
            catch (Exception ex)
            {
                Logger.Log("Error in GetPasswordFromUserOrStore", ex);
            }
            return null;
        }

        private static async Task<string> GetPassword(AmazonEC2Client ec2Client, Instance instance, string extension)
        {
            try
            {
                string storePassword = KeyStore.GetPassword(instance.InstanceId);
                if (string.IsNullOrWhiteSpace(storePassword))
                {
                    // Password is not present in store //
                    // Get it from AWS //

                    GetPasswordDataRequest req = new GetPasswordDataRequest(instance.InstanceId);
                    GetPasswordDataResponse resp = await ec2Client.GetPasswordDataAsync(req);
                    if (!string.IsNullOrWhiteSpace(resp.PasswordData))
                    {
                        if (!File.Exists(instance.KeyName + extension))
                        {
                            return GetPasswordFromUserOrStore(instance.InstanceId, "The File " + instance.KeyName + extension + " does not exist.");
                        }
                        else
                        {
                            return resp.GetDecryptedPassword(File.ReadAllText(instance.KeyName + ".pem").Trim());
                        }
                    }
                    else
                    {
                        return GetPasswordFromUserOrStore(instance.InstanceId, "Password not available with Amazon");
                    }
                }
                else
                {
                    return storePassword;
                }
            }
            catch (Exception ex)
            {
                return GetPasswordFromUserOrStore(instance.InstanceId, "Cannot fetch password: " + ex.Message);
            }
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

        private static async Task<string[]> GetAllPublicIp(string[] urls)
        {
            ServicePointManager.DefaultConnectionLimit = urls.Length * 2;
            SortedSet<string> allIps = new SortedSet<string>();
            List<Task<string>> tasks = new List<Task<string>>();
            foreach (string url in urls)
            {
                tasks.Add(FetchPublicIp(url));
            }
            string[] ips = await Task.WhenAll(tasks);

            foreach (string ip in ips)
            {
                if(!string.IsNullOrWhiteSpace(ip))
                {
                    allIps.Add(ip);
                }
            }
            return allIps.ToArray();
        }

        internal static async Task PublicIpWatcher()
        {
            while (true)
            {
                try
                {
                    string[] newIPs = await GetAllPublicIp(IpProviders);
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
                    Logger.Log("PublicIpWatcher error", ex);
                }
                await Task.Delay(1024);    // ~ 1 minutes
            }
        }

        private static void OnPublicIPsUpdated(string[] newPublicIps)
        {
            if (newPublicIps != null && newPublicIps.Length > 0)
            {
                _publicIPs = newPublicIps;
                ((MainWindow)Application.Current.MainWindow).OnIPChanged();
            }
        }

        private static async Task<string> FetchPublicIp(string providerUrl)
        {
            string externalip;
            try
            {
                using (WebClient httpClient = new WebClient())
                {
                    httpClient.Proxy = null;
                    externalip = await httpClient.DownloadStringTaskAsync(providerUrl);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("FetchPublicIp error", ex);
                externalip = null;
            }

            if (!string.IsNullOrWhiteSpace(externalip))
            {
                IPAddress ip;
                if (IPAddress.TryParse(externalip.Trim(), out ip))
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        internal static async Task<string> DoSSH(Instance instance)
        {
            return await DoSSHorSCP(instance, "SSH");
        }

        internal static async Task<string> DoSCP(Instance instance)
        {
            return await DoSSHorSCP(instance, "SCP");
        }

        private static async Task<string> DoSSHorSCP(Instance instance, string command)
        {
            string PrivateKey = instance.KeyName + ".ppk";
            if (!File.Exists(PrivateKey))
            {
                return "The File " + PrivateKey + " does not exist.\nPlease create this file and try again.";
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
                string login = userName + "@" + instance.PublicIpAddress;
                if (command.Equals("SSH"))
                {
                    await Task.Run(() =>
                    ExecuteCommandSync("putty.exe", "-ssh " + login + " -i " + PrivateKey, true))
                    .ConfigureAwait(continueOnCapturedContext: false);
                }
                else if (command.Equals("SCP"))
                {
                    await Task.Run(() =>
                    ExecuteCommandSync("WinSCP.exe", "scp://" + login + " /privatekey=" + PrivateKey, true))
                    .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            return null;
        }
    }
}