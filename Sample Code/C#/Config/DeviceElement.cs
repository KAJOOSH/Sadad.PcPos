using System;
using System.Configuration;
using Sadad.PcPos.Core;

namespace PcPosSampleDll
{
    public class DeviceElement : ConfigurationElement
    {

        [ConfigurationProperty("deviceType", IsRequired = true)]
        public string DeviceType
        {
            get
            {
                return (string)this["deviceType"];
            }
            set
            {
                this["deviceType"] = value;
            }
        }

        //[ConfigurationProperty("deviceTypeName", IsRequired = true)]
        public string DeviceTypeName
        {
            get
            {
                var devType = 0;
                int.TryParse(DeviceType, out devType);
                return Enum.GetName(typeof(DeviceType), devType);
            }
            set
            {
                return;
            }
        }

        [ConfigurationProperty("serialPort", IsRequired = true)]
        public string SerialPort
        {
            get
            {
                return (string)this["serialPort"];
            }
            set
            {
                this["serialPort"] = value;
            }
        }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("baudRate", IsRequired = true)]
        public string BaudRate
        {
            get
            {
                return (string)this["baudRate"];
            }
            set
            {
                this["baudRate"] = value;
            }
        }

        [ConfigurationProperty("stopBits", IsRequired = true)]
        public string StopBits
        {
            get
            {
                return (string)this["stopBits"];
            }
            set
            {
                this["stopBits"] = value;
            }
        }

    }
}