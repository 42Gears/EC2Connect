/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using EC2Connect.Code;
using System;
using System.Collections.Generic;
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
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HeadBanner.ProfileChangedListeners += MainGrid.LoadProfile;
            Utility.PublicIPChangedListeners += Utility_PublicIPChangedListeners;
            Thread ipWatcher = new Thread(Utility.PublicIpWatcher);
            ipWatcher.IsBackground = true;
            ipWatcher.Start();
        }

        private void Utility_PublicIPChangedListeners()
        {
            if (Utility.PublicIPs != null && Utility.PublicIPs.Length > 0)
            {
                string ips = string.Join("\t,\t", Utility.PublicIPs);
                if (!string.IsNullOrWhiteSpace(ips))
                {
                    Dispatcher.BeginInvoke(new Action(() => IpAddress.Text = ips));

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
    }
}
