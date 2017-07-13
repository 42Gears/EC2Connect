/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2;
using Amazon.Runtime;
using Amazon.Util;
using EC2Connect.Popup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EC2Connect.Panels
{
    /// <summary>
    /// Interaction logic for Banner.xaml
    /// </summary>
    public partial class Banner : Grid
    {
        public Banner()
        {
            InitializeComponent();
            AccountManagement.Loaded += AccountManagement_Loaded;
        }

        private void AccountManagement_Click(object sender, RoutedEventArgs e)
        {
            // Show popup
            //ProfileManager.RegisterProfile("Random Profile Name" + DateTime.Now.ToBinary(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            ManageAccountsDialogue dialogue = new ManageAccountsDialogue();
            dialogue.Owner = Application.Current.MainWindow;
            dialogue.ShowInTaskbar = false;
            dialogue.SizeToContent = SizeToContent.WidthAndHeight;
            dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
            dialogue.ResizeMode = ResizeMode.NoResize;
            dialogue.ShowDialog();

            RefreshMenuItems();
        }

        private void RefreshMenuItems()
        {
            AccountManagement.Items.Clear();
            var profiles = ProfileManager.ListProfileNames();
            if (profiles != null || profiles.Count() > 0)
            {
                foreach (string profile in profiles)
                {
                    MenuItem item = new MenuItem();
                    item.Header = profile;
                    item.Click += AccountManagement_SelectionChanged;
                    AccountManagement.Items.Add(item);
                }
            }

            MenuItem manage = new MenuItem()
            {
                Header = "Manage Accounts"
            };
            manage.Click += AccountManagement_Click;
            AccountManagement.Items.Add(manage);
        }

        private async void AccountManagement_SelectionChanged(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                string profile = item.Header as string;
                if (!string.IsNullOrWhiteSpace(profile))
                {

                    await (Application.Current.MainWindow as MainWindow).MainGrid.LoadProfileAsync(profile, null);
                }
            }
        }

        private void AccountManagement_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMenuItems();
        }
    }
}
