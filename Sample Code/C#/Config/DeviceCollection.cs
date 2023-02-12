﻿using System.Configuration;

namespace PcPosSampleDll
{
    public class DeviceCollection : ConfigurationElementCollection
        public void Clear()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                this.BaseRemoveAt(i);
            }
        }

        public void Add(DeviceElement element)
        {
            this.BaseAdd(element);
        }
    }
}