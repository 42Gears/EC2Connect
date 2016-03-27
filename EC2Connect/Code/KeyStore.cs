/********************************************************************
 * Copyright 2016 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace EC2Connect.Code
{
    public class KeyStore
    {
        private static readonly Object syncronized = new Object();
        public static string GetPassword(string InstanceId)
        {
            lock(syncronized)
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
                    Debug.WriteLine(ex);
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
            lock(syncronized)
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
                    Debug.WriteLine(ex);
                }
                if(settings==null)
                {
                    settings = new Dictionary<string, string>();
                }

                if(settings.ContainsKey(InstanceId))
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
    }
}