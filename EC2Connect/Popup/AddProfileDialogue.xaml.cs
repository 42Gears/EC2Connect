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
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EC2Connect.Popup
{
    /// <summary>
    /// Interaction logic for AddAccountDialogue.xaml
    /// </summary>
    public partial class AddAccountDialogue : Window
    {
        public AddAccountDialogue()
        {
            InitializeComponent();
        }
        
        private void Regions_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox regions = sender as ComboBox;
            if(regions!=null)
            {
                foreach(var region in RegionEndpoint.EnumerableAllRegions)
                {
                    regions.Items.Add(region);
                }
                regions.SelectedIndex = 0;
            }
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            string profileName = ProfileName.Text.Trim();
            string accessId = AccessId.Text.Trim();
            string secretKey = SecretKey.Text.Trim();
            RegionEndpoint region = RegionsCombo.SelectedItem as RegionEndpoint;
            if (region == null)
            {
                region = RegionEndpoint.USEast1;
            }
            if (ProfileManager.ListProfileNames().Contains(ProfileName.Text.Trim()))
            {
                MessageBox.Show("Profile Name already exists");
                return;
            }

            try
            {
                ProfileManager.RegisterProfile(profileName, accessId, secretKey);
                AWSCredentials credentails = ProfileManager.GetAWSCredentials(profileName);
                AmazonEC2Client ec2 = new AmazonEC2Client(credentails, region);
                ec2.DescribeAvailabilityZones();
                ec2.DescribeRegions();
                ProfileManager.UnregisterProfile(profileName);
                SaveButton.IsEnabled = true;
                MessageBox.Show("Authorized!");
            }
            catch (AmazonEC2Exception ex)
            {
                ProfileManager.UnregisterProfile(profileName);
                MessageBox.Show(ex.Message, "Invalid Credentials");
            }
            catch(Exception ex)
            {
                ProfileManager.UnregisterProfile(profileName);
                MessageBox.Show(ex.Message, "No Internet");
            }
            finally
            {
                ProfileManager.UnregisterProfile(profileName);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            RegionEndpoint region = RegionsCombo.SelectedItem as RegionEndpoint;
            if (region == null)
            {
                region = RegionEndpoint.USEast1;
            }
            ProfileManager.RegisterProfile(ProfileName.Text.Trim(), AccessId.Text.Trim(), SecretKey.Text.Trim());
            DialogResult = true;
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TestButton.IsEnabled = !string.IsNullOrWhiteSpace(ProfileName.Text) &&
                                   !string.IsNullOrWhiteSpace(AccessId.Text) &&
                                   !string.IsNullOrWhiteSpace(SecretKey.Text);
            SaveButton.IsEnabled = false;
        }
    }
}
