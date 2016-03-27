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
    public class LinuxUser
    {
        private static readonly Object syncronized = new Object();
        public static string GetUser(string InstanceId)
        {
            lock (syncronized)
            {
                Dictionary<string, string> settings = null;
                try
                {
                    using (IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("users.cfg", FileMode.OpenOrCreate, FileAccess.Read))
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

        public static void SetUser(string InstanceId, string userName)
        {
            lock (syncronized)
            {
                Dictionary<string, string> settings = null;
                try
                {
                    using (IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("users.cfg", FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        settings = new BinaryFormatter().Deserialize(stream) as Dictionary<string, string>;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                if (settings == null)
                {
                    settings = new Dictionary<string, string>();
                }

                if (settings.ContainsKey(InstanceId))
                {
                    settings[InstanceId] = userName;
                }
                else
                {
                    settings.Add(InstanceId, userName);
                }
                using (var stream = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("users.cfg", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    new BinaryFormatter().Serialize(stream, settings);
                }
            }
        }
    }
}
