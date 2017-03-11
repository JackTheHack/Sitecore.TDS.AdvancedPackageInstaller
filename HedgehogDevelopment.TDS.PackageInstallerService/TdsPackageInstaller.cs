using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Services;
using System.ComponentModel;
using System.Xml;
using Sitecore.Update.Installer;
using log4net;
using log4net.Config;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update;
using Sitecore.Update.Metadata;
using System.Configuration;
using Sitecore.Update.Utils;
using Sitecore.IO;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Sitecore.Diagnostics;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    [WebService(Namespace = "http://hhogdev.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class TdsPackageInstaller
    {

        /// <summary>
        /// Installs a Sitecore Update Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        [WebMethod(Description = "Installs a Sitecore Update Package.")]
        public InstallationSummary InstallPackage(string path)
        {
            using (var hubConnection = new HubConnection("http://127.0.0.1:9422"))                
            {
                var hubProxy = hubConnection.CreateHubProxy("InstallerHub");

                try
                {
                    hubConnection.Start().ContinueWith(i =>
                    {
                        Log.Info("[SignalR] Connection to hub - " + i.IsFaulted, this);
                    }).Wait();
                }
                catch (Exception e)
                {
                    Log.Info("[SignalR] Failed to establish the connection", this);
                }

                // Use default logger with SignalR logging
                ILog log = new CustomLogger(LogManager.GetLogger("root"), hubProxy);

                XmlConfigurator.Configure((XmlElement) ConfigurationManager.GetSection("log4net"));

                CustomInstaller installer = new CustomInstaller(UpgradeAction.Upgrade);

                MetadataView view = UpdateHelper.LoadMetadata(path);

                //Get the package entries
                bool hasPostAction;
                string historyPath;
                List<ContingencyEntry> entries = installer.DoInstallPackage(path, InstallMode.Install, log,
                    out hasPostAction, out historyPath);

                installer.ExecutePostInstallationInstructions(path, historyPath, InstallMode.Install, view, log,
                    ref entries);

                SaveInstallationMessages(entries, historyPath);

                return new InstallationSummary()
                {
                    Warnings = entries.Count(i => i.Level == ContingencyLevel.Warning),
                    Collisions = entries.Count(i => i.Level == ContingencyLevel.Collision),
                    Errors = entries.Count(i => i.Level == ContingencyLevel.Error),
                    Entries = entries.Select(i => new InstallationEntry(i)).ToList()
                };
            }
        }       


        private void SaveInstallationMessages(List<ContingencyEntry> entries, string historyPath)
        {
            string path = Path.Combine(historyPath, "messages.xml");

            FileUtil.EnsureFolder(path);

            using (FileStream fileStream = File.Create(path))
            {
                new XmlEntrySerializer().Serialize(entries, (Stream)fileStream);
            }
        }
    }
}
