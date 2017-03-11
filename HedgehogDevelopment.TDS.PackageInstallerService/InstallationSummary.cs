using System;
using System.Collections.Generic;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    [Serializable]
    public class InstallationSummary
    {
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public List<InstallationEntry> Entries { get; set; }
        public int Collisions { get; internal set; }
    }
}
