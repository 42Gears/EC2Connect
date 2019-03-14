/********************************************************************
 * Copyright 2019 42Gears Mobility Systems                          *
 *                                                                  *
 * Licensed under the Apache License, Version 2.0 (the "License");  *
 * you may not use this file except in compliance with the License. *
 * You may obtain a copy of the License at                          *
 *     http://www.apache.org/licenses/LICENSE-2.0                   *
 ********************************************************************/

using Amazon.EC2.Model;

namespace EC2Connect.Code
{
    public class InstanceModel
    {
        public InstanceModel(Instance amazonInstance)
        {
            _amazonInstance = amazonInstance;
        }

        private readonly Instance _amazonInstance;
        public Instance AmazonInstance
        {
            get
            {
                return _amazonInstance;
            }
        }

        public string Name
        {
            get
            {
                return Utility.GetInstnaceName(AmazonInstance);
            }
        }
        public string Id
        {
            get
            {
                return AmazonInstance.InstanceId;
            }
        }
        public string Type
        {
            get
            {
                return AmazonInstance.InstanceType;
            }
        }

        public string Key
        {
            get
            {
                return AmazonInstance.KeyName;
            }
        }
        public string IpAdd
        {
            get
            {
                return AmazonInstance.PublicIpAddress;
            }
        }

        public string Status
        {
            get
            {
                return AmazonInstance.State.Name;
            }
        }
    }
}