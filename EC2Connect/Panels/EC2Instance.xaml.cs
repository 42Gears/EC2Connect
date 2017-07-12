/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using Amazon.Util;
using EC2Connect.Code;
using EC2Connect.Popup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EC2Connect.Panels
{
    /// <summary>
    /// Interaction logic for EC2Instance.xaml
    /// </summary>
    public partial class EC2Instance : Grid
    {
        private AmazonEC2Client EC2Client;
        public EC2Instance()
        {
            InitializeComponent();
        }

        public void LoadProfile(string profileName)
        {
            new Thread(LoadProfileAsync).Start(profileName);
        }

        private void LoadProfileAsync(object pName)
        {
            string profileName = pName as string;
            if (!string.IsNullOrWhiteSpace(profileName))
            {
                try
                {
                    AWSCredentials credentails = ProfileManager.GetAWSCredentials(profileName);
                    EC2Client = new AmazonEC2Client(credentails, RegionEndpoint.USEast1);


                    Dictionary<string, string> staticIPs = GetStaticIps(EC2Client);

                    List<InstanceModel> allInstances = new List<InstanceModel>();

                    var reservations = EC2Client.DescribeInstances().Reservations;
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
                                    else if(staticIPs.ContainsKey(instance.InstanceId))
                                    {
                                        instance.PublicIpAddress = staticIPs[instance.InstanceId];
                                        allInstances.Add(new InstanceModel(instance));
                                    }
                                }
                            }
                        }
                    }

                    if (allInstances.Count > 0)
                    {
                        Dispatcher.BeginInvoke(new Action(() => LoadGrid(allInstances)));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(ex.Message, ex.GetType().FullName)));
                }
            }
        }

        private Dictionary<string, string> GetStaticIps(AmazonEC2Client eC2Client)
        {
            List <Address> addresses =  eC2Client.DescribeAddresses().Addresses;
            Dictionary<string, string> staticIPs = new Dictionary<string, string>();
            if (addresses!=null && addresses.Count>0)
            {
                foreach(Address address in addresses)
                {
                    if(!string.IsNullOrWhiteSpace(address.PublicIp) && !string.IsNullOrWhiteSpace(address.InstanceId))
                    {
                        staticIPs.Add(address.InstanceId, address.PublicIp);
                    }
                }
            }
            return staticIPs;
        }

        private void LoadGrid(List<InstanceModel> instances)
        {
            if (instances != null && instances.Count > 0)
            {
                InstanceGrid.ItemsSource = instances;
            }
        }

        private void Rdp_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null && EC2Client!=null)
            {
                new Thread(Rdp_Async).Start(item);
            }
        }

        private void Rdp_Async(object instance)
        {
            try
            {
                InstanceModel item = instance as InstanceModel;
                if (item != null && EC2Client != null)
                {
                    Utility.AllowPort(EC2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 3389);

                    // TODO: FixMe
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            Utility.DoRDP(EC2Client, item.AmazonInstance);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Ssh_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null && EC2Client != null)
            {
                Utility.AllowPort(EC2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 22);
                Utility.DoSSH(item.AmazonInstance);
            }
        }

        private void Scp_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null && EC2Client != null)
            {
                Utility.AllowPort(EC2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 22);
                Utility.DoSCP(item.AmazonInstance);
            }
        }

        private void Port_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null && EC2Client != null)
            {
                CustomPortDialogue dialogue = new CustomPortDialogue();
                dialogue.EC2Client = EC2Client;
                dialogue.AmazonInstance = item.AmazonInstance;
                dialogue.ShowInTaskbar = false;
                dialogue.SizeToContent = SizeToContent.WidthAndHeight;
                dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
                dialogue.ResizeMode = ResizeMode.NoResize;

                dialogue.ShowDialog();
            }
        }
    }
}
