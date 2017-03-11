using Sitecore.Install;
using Sitecore.Install.Framework;
using Sitecore.Install.Utils;
using Sitecore.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    public class CustomEntryData : IEntryData
    {
        private ZipEntry entry;
        private string key;
        public string Key
        {
            get
            {
                return this.key;
            }
        }
        
        public CustomEntryData(ZipEntry entry, string key)
        {
            this.entry = entry;
            this.key = key;
            if (this.key.Length > 0 && this.key[0] == Constants.KeySeparator)
            {
                this.key = this.key.Substring(1);
            }
        }

        public CustomEntryData(ZipEntry entry)
        {
            this.entry = entry;
            this.key = entry.Name;
            if (this.key.Length > 0 && this.key[0] == Constants.KeySeparator)
            {
                this.key = this.key.Substring(1);
            }
        }

        public IStreamHolder GetStream()
        {
            return new WeakStreamHolder(this.entry.GetStream());
        }
    }
}
