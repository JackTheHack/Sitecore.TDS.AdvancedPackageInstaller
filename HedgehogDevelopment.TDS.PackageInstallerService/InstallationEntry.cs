using System;
using Sitecore.Update.Installer;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    [Serializable]
    public class InstallationEntry
    {
        public InstallationEntry()
        {

        }

        public InstallationEntry(ContingencyEntry i)
        {
            Action = i.Action;
            Message = i.Message.Description;
            Level = i.Level.ToString();
        }

        public string Action { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }
}
