/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon;
using Amazon.EC2;
using Amazon.Runtime;
using Amazon.Util;
using EC2Connect.Code;
using EC2Connect.Popup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EC2Connect.Panels
{
    /// <summary>
    /// Interaction logic for EC2Instance.xaml
    /// </summary>
    public partial class EC2Instance : Grid
    {
        private string LastSelectedProfile = KeyStore.LastSelectedProfile;
        public RegionEndpoint LastSelectedRegion
        {
            get
            {
                return AwsRegion.SelectedItem as RegionEndpoint;
            }
        }

        public AmazonEC2Client AwsEC2Client
        {
            get
            {
                RegionEndpoint region = LastSelectedRegion;
                if (string.IsNullOrWhiteSpace(LastSelectedProfile) || region == null)
                {
                    return null;
                }
                else
                {
                    DateTime t1 = DateTime.Now;
                    AWSCredentials credentails = ProfileManager.GetAWSCredentials(LastSelectedProfile);
                    AmazonEC2Client client = new AmazonEC2Client(credentails, region);
                    DateTime t2 = DateTime.Now;
                    Logger.Log("GetAWSCredentials took " + (t2 - t1).TotalMilliseconds + " ms");
                    return client;
                }
            }
        }

        public EC2Instance()
        {
            InitializeComponent();
        }

        public async Task LoadProfileAsync(string profileName, RegionEndpoint region)
        {
            if (region == null)
            {
                region = LastSelectedRegion;
            }
            if (string.IsNullOrWhiteSpace(profileName) || region == null)
            {
                InstanceGrid.ItemsSource = new List<InstanceModel>();
            }
            else
            {
                LastSelectedProfile = profileName;
                try
                {
                    InstanceGrid.ItemsSource = new List<InstanceModel>();
                    List<InstanceModel> allInstances;
                    using (AmazonEC2Client client = AwsEC2Client)
                    {
                        if (client != null)
                        {
                            allInstances = await Utility.GetInstanceListAsync(client);
                        }
                        else
                        {
                            allInstances = null;
                        }
                    }

                    if (allInstances != null && allInstances.Count > 0)
                    {
                        LoadGrid(allInstances);
                        KeyStore.LastSelectedProfile = LastSelectedProfile;
                        KeyStore.LastSelectedRegion = AwsRegion.SelectedIndex;
                    }
                    else
                    {
                        MessageBox.Show("No instances present in account " + profileName + " " + region);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().FullName);
                }
            }
        }

        private void LoadGrid(List<InstanceModel> instances)
        {
            DateTime t1 = DateTime.Now;
            if (instances != null && instances.Count > 0)
            {
                InstanceGrid.ItemsSource = instances;
            }
            DateTime t2 = DateTime.Now;
            Debug.WriteLine("LoadGrid took " + (t2 - t1).TotalMilliseconds + " ms");
        }

        private async void Rdp_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null)
            {
                using (AmazonEC2Client ec2Client = AwsEC2Client)
                {
                    if (ec2Client != null)
                    {
                        string errorMessage = await Utility.RdpAsync(ec2Client, item);
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            MessageBox.Show(errorMessage);
                        }
                    }
                }
            }
        }

        private async void Ssh_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null)
            {
                using (AmazonEC2Client ec2Client = AwsEC2Client)
                {
                    if (ec2Client != null)
                    {
                        await Utility.AllowPorts(ec2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 22);
                        string errorMessage = await Utility.DoSSH(item.AmazonInstance);
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            MessageBox.Show(errorMessage);
                        }
                    }
                }
            }
        }

        private async void Scp_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null)
            {
                using (AmazonEC2Client ec2Client = AwsEC2Client)
                {
                    if (ec2Client != null)
                    {
                        await Utility.AllowPorts(ec2Client, item.AmazonInstance, Utility.PublicIPs, "TCP", 22);
                        string errorMessage = await Utility.DoSCP(item.AmazonInstance);
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            MessageBox.Show(errorMessage);
                        }
                    }
                }
            }
        }

        private void Port_Click(object sender, RoutedEventArgs e)
        {
            InstanceModel item = InstanceGrid.SelectedItem as InstanceModel;
            if (item != null)
            {
                CustomPortDialogue dialogue = new CustomPortDialogue();
                dialogue.AmazonInstance = item.AmazonInstance;
                dialogue.ShowInTaskbar = false;
                dialogue.SizeToContent = SizeToContent.WidthAndHeight;
                dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
                dialogue.ResizeMode = ResizeMode.NoResize;

                dialogue.ShowDialog();
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox regions = sender as ComboBox;
            if (regions != null)
            {
                foreach (var region in RegionEndpoint.EnumerableAllRegions)
                {
                    regions.Items.Add(region);
                }
                regions.SelectedIndex = KeyStore.LastSelectedRegion;
            }

            LastSelectedProfile = KeyStore.LastSelectedProfile;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox regions = sender as ComboBox;
            if (regions != null)
            {
                InstanceGrid.ItemsSource = new List<InstanceModel>();
                Refresh_Click(sender, e);
            }
        }
        

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProfileAsync(LastSelectedProfile, null);
        }

        private void InstanceGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DataGrid grid = sender as DataGrid;
            if(grid!=null)
            {
                if(grid.ActualHeight > grid.MinHeight)
                {
                    grid.MinHeight = grid.ActualHeight;
                }
            }
        }
    }
}
