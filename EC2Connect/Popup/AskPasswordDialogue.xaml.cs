/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using System.Windows;

namespace EC2Connect.Popup
{
    /// <summary>
    /// Interaction logic for AskPassword.xaml
    /// </summary>
    public partial class AskPassword : Window
    {
        public string DisplayMessage { get; set; }
        public string ManualPassword { get; private set; }

        public AskPassword()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ManualPassword = Ec2Password.Password;
            this.DialogResult = true;
        }
    }
}