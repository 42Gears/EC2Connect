/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2.Model;
using EC2Connect.Code;
using System.Windows;
using System.Windows.Controls;

namespace EC2Connect.Popup
{
    /// <summary>
    /// Interaction logic for CustomPortDialogue.xaml
    /// </summary>
    public partial class CustomPortDialogue : Window
    {
        public CustomPortDialogue()
        {
            InitializeComponent();
        }

        private int Port
        {
            get
            {
                int result;
                if (int.TryParse(PortTextBox.Text, out result))
                {
                    return result;
                }
                return -1;
            }
        }

        private string Type
        {
            get
            {
                ComboBoxItem item = TypeCombo.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    return item.Content as string;
                }
                return null;
            }
        }
        public Instance AmazonInstance { get; set; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Port > 0 && !string.IsNullOrWhiteSpace(Type) && AmazonInstance != null)
            {
                using (var ec2Client = ((MainWindow)Application.Current.MainWindow).MainGrid.AwsEC2Client)
                {
                    await Utility.AllowPorts(ec2Client, AmazonInstance, Utility.PublicIPs, Type, Port);
                }
                    
            }
            DialogResult = true;
        }
    }
}