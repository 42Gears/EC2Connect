/********************************************************************
 * Copyright 2019 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2;
using EC2Connect.Code;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace EC2Connect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void OnIPChanged()
        {
            if (Utility.PublicIPs != null && Utility.PublicIPs.Length > 0)
            {
                string ips = string.Join("\t,\t", Utility.PublicIPs);
                if (!string.IsNullOrWhiteSpace(ips))
                {
                    IpAddress.Text = ips;
                }
            }
        }

        private void IpAddress_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IpAddress.Text))
            {
                Clipboard.SetText(IpAddress.Text);
            }
        }

        private async void IpAddress_Loaded(object sender, RoutedEventArgs e)
        {
            await Utility.PublicIpWatcher();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                using (AmazonEC2Client ec2Client = this.MainGrid.AwsEC2Client)
                {
                    new Thread(Utility.CleanUpAddedIPs)
                    {
                        IsBackground = false
                    }.Start(ec2Client);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
