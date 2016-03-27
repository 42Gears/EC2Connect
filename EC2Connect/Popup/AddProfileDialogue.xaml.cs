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
using Amazon.Runtime;
using Amazon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            Loaded += AddAccountDialogue_Loaded;
        }

        private void AddAccountDialogue_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Regions_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox regions = sender as ComboBox;
            if(regions!=null)
            {
                foreach(var r in Amazon.RegionEndpoint.EnumerableAllRegions)
                {
                    regions.Items.Add(new ComboBoxItem() {  Content  = r.DisplayName });
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
            if (region==null)
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
            //Properties.Settings.Default["aaa"] = new Dictionary<string, string>();
            this.DialogResult = true;
            this.Close();
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
