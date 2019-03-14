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
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace EC2Connect
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                using (AmazonEC2Client ec2Client = ((MainWindow)Application.Current.MainWindow).MainGrid.AwsEC2Client)
                {
                    new Thread(Utility.CleanUpAddedIPs)
                    {
                        IsBackground = false
                    }.Start(ec2Client);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while removing IPs " + ex.Message);
            }
        }
    }
}
