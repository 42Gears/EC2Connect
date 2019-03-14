/********************************************************************
 * Copyright 2019 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Formatters.Binary;

namespace EC2Connect.Code
{
    public class KeyStore
    {
        private static readonly Object syncronized = new Object();
        public static string GetPassword(string InstanceId)
        {
            lock (syncronized)
            {
                Dictionary<string, string> settings = null;
                try
                {
                    using (IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("settings.cfg", FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        settings = new BinaryFormatter().Deserialize(stream) as Dictionary<string, string>;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Cannot fetch password", ex);
                }
                if (settings != null && settings.ContainsKey(InstanceId))
                {
                    return settings[InstanceId];
                }
                return null;
            }
        }

        public static void SetPassword(string InstanceId, string password)
        {
            lock (syncronized)
            {
                Dictionary<string, string> settings = null;
                try
                {
                    using (IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("settings.cfg", FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        settings = new BinaryFormatter().Deserialize(stream) as Dictionary<string, string>;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Cannot save password", ex);
                }
                if (settings == null)
                {
                    settings = new Dictionary<string, string>();
                }

                if (settings.ContainsKey(InstanceId))
                {
                    settings[InstanceId] = password;
                }
                else
                {
                    settings.Add(InstanceId, password);
                }
                using (var stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("settings.cfg", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    new BinaryFormatter().Serialize(stream, settings);
                }
            }
        }
        
        public static int LastSelectedRegion
        {
            get
            {
                try
                {
                    string getPassword = GetPassword("LAST_SELECTED_REGION");
                    if (!string.IsNullOrWhiteSpace(getPassword))
                    {
                        return int.Parse(getPassword);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Cannot fetch LAST_SELECTED_REGION", ex);
                }
                return 0;
            }

            set
            {
                SetPassword("LAST_SELECTED_REGION", value.ToString());
            }
        }

        public static string LastSelectedProfile
        {
            get
            {
                string profileName = GetPassword("LAST_SELECTED_PROFILE");
                if (!string.IsNullOrWhiteSpace(profileName))
                {
                    return profileName;
                }
                return string.Empty;
            }

            set
            {
                SetPassword("LAST_SELECTED_PROFILE", value);
            }
        }
    }
}