/********************************************************************
 * Copyright 2017 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using System;
using System.Diagnostics;
using System.IO;

namespace EC2Connect.Code
{
    internal static class Logger
    {
        private static string fileName = "EC2Connect.log";
        internal static void Log(string message)
        {
            Debug.WriteLine(message);
            if(!string.IsNullOrWhiteSpace(fileName))
            {
                File.AppendAllText(fileName, message);
                File.AppendAllText(fileName, Environment.NewLine);
            }
        }

        internal static void Log(string message, Exception ex)
        {
            Debug.WriteLine(message);
            Debug.WriteLine(ex);

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                File.AppendAllText(fileName, message);
                File.AppendAllText(fileName, Environment.NewLine);
                File.AppendAllText(fileName, ex.ToString());
                File.AppendAllText(fileName, Environment.NewLine);
            }
        }
    }
}
