using System.Configuration;

namespace PcPosSampleDll
{
    public class DeviceCollection : ConfigurationElementCollection    {        protected override ConfigurationElement CreateNewElement()        {            return new DeviceElement();        }        protected override object GetElementKey(ConfigurationElement element)        {            return ((DeviceElement)element).Name;        }
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