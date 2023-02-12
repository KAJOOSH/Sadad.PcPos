using System.Configuration;

namespace PcPosSampleDll
{
    public class PcPosDevicesSection : ConfigurationSection    {        [ConfigurationProperty("", IsDefaultCollection = true, IsRequired = true)]        public DeviceCollection Devices        {            get            {                return (DeviceCollection)this[""];            }            set            {                this[""] = value;            }        }
    }
}