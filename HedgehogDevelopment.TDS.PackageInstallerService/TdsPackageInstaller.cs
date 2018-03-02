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
using System.Web.Services.Protocols;
using System.Web;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    [WebService(Namespace = "http://hhogdev.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class TdsPackageInstaller
    {
        public UserCredentials Credentials { get; set; }
        
        public class UserCredentials : SoapHeader
        {
            public string userid;
            public string password;
        }

        /// <summary>
        /// Installs a Sitecore Update Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        [WebMethod(Description = "Installs a Sitecore Update Package.")]
        [SoapHeader("Credentials")]
        public InstallationSummary InstallPackage(string path)
        {
            if (!Authenticate()) return null;

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

                return DoInstallPackage(path, hubProxy);
            }
        }


        /// <summary>
        /// Installs a Sitecore Update Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        [WebMethod(Description = "Installs a Sitecore Update Package.")]
        [SoapHeader("Credentials")]
        public InstallationSummary InstallPackageSilently(string path)
        {
            var azureSitePath = Environment.GetEnvironmentVariable("WEBROOT_PATH");
            string appRoot = HttpContext.Current.Server.MapPath("~");

            if (!string.IsNullOrEmpty(appRoot))
            {
                path = appRoot + path;
            }

            if (!Authenticate())return null;

            return DoInstallPackage(path, null);
        }

        private bool Authenticate()
        {
            if(!string.IsNullOrEmpty(ConfigurationManager.AppSettings["TDSConnector.Username"]))
            {
                var username = ConfigurationManager.AppSettings["TDSConnector.Username"];
                var password = ConfigurationManager.AppSettings["TDSConnector.Password"];
                var base64 = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password)));
                return true;
            }

            return true;
        }

        private InstallationSummary DoInstallPackage(string path, IHubProxy hubProxy)
        {
            // Use default logger with SignalR logging
            ILog log = new CustomLogger(LogManager.GetLogger("root"), hubProxy);

            XmlConfigurator.Configure((XmlElement)ConfigurationManager.GetSection("log4net"));

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
