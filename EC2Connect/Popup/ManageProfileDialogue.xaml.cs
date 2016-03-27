/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

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
    /// Interaction logic for ManageAccountsDialogue.xaml
    /// </summary>
    public partial class ManageAccountsDialogue : Window
    {
        public ManageAccountsDialogue()
        {
            InitializeComponent();
            Loaded += ManageAccountsDialogue_Loaded;
        }

        void ManageAccountsDialogue_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAccountList();
        }

        private void RefreshAccountList()
        {
            AccountList.Items.Clear();
            var profiles = ProfileManager.ListProfileNames();
            if (profiles != null || profiles.Count() > 0)
            {
                foreach (string profile in profiles)
                {
                    AccountList.Items.Add(profile);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddAccountDialogue dialogue = new AddAccountDialogue();
            dialogue.Owner = this;
            dialogue.ShowInTaskbar = false;
            dialogue.SizeToContent = SizeToContent.WidthAndHeight;
            dialogue.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialogue.WindowStyle = WindowStyle.SingleBorderWindow;
            dialogue.ResizeMode = ResizeMode.NoResize;
            dialogue.ShowDialog();

            RefreshAccountList();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            string profile = AccountList.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(profile))
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    ProfileManager.UnregisterProfile(profile);
                }
            }
            RefreshAccountList();
        }
    }
}