using log4net;
using Sitecore.Install;
using Sitecore.Install.Framework;
using Sitecore.Zip;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    public class CustomPackageReader : ISource<PackageEntry>
    {
        private readonly string _filename;

        public CustomPackageReader(string filename, ILog log)
        {
            _filename = filename;
        }

        public void Populate(ISink<PackageEntry> sink)
        {
            ISink<IEntryData> sink2 = new EntryBuilder(sink);
            ZipReader zipReader = new ZipReader(_filename, System.Text.Encoding.UTF8);
            string tempFileName = System.IO.Path.GetTempFileName();
            ZipEntry entry = zipReader.GetEntry("package.zip");
            if (entry != null)
            {
                using (System.IO.FileStream fileStream = System.IO.File.Create(tempFileName))
                {
                    StreamUtil.Copy(entry.GetStream(), fileStream, 16384);
                }
                zipReader.Dispose();
                zipReader = new ZipReader(tempFileName, System.Text.Encoding.UTF8);
            }
            try
            {
                foreach (ZipEntry current in zipReader.Entries)
                {
                    if (CanInstallEntry(current))
                    {
                            sink2.Put(new CustomEntryData(current));
                    }
                }
                sink2.Flush();
            }
            finally
            {
                zipReader.Dispose();
                System.IO.File.Delete(tempFileName);
            }
        }

        private bool CanInstallEntry(ZipEntry current)
        {
            if (
               current.Name == @"addedfiles/bin/HedgehogDevelopment.SitecoreProject.PackageInstallPostProcessor.dll" ||
               current.Name == @"properties/addedfiles/bin/HedgehogDevelopment.SitecoreProject.PackageInstallPostProcessor.dll" )
               //current.Name == @"addedfiles/_DEV/DeployedItems.xml" ||
               //current.Name == @"properties/addedfiles/_DEV/DeployedItems.xml")
            {
                return false;
            }

            

            return true;
        }
    }
}
