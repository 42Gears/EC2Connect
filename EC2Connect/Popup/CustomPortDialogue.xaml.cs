/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2;
using Amazon.EC2.Model;
using EC2Connect.Code;
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

        public AmazonEC2Client EC2Client { get; set; }
        public Instance AmazonInstance { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Port > 0 && !string.IsNullOrWhiteSpace(Type) && EC2Client != null && AmazonInstance != null)
            {
                Utility.AllowPort(EC2Client, AmazonInstance, Utility.PublicIp, Type, Port);
            }
            DialogResult = true;
        }
    }
}