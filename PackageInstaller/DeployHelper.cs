using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HedgehogDevelopment.PackageInstaller
{
    public class DeployHelper
    {
        public int Verbosity { get; set; }

        public string SitecoreConnectorDll { get; set; }
        public string SitecoreConnectorAsmx { get; set; }
        public string SitecorePostDeployActionsDll { get; set; }




        /// <summary>
        /// Deploys the 
        /// </summary>
        /// <param name="sitecoreDeployFolder"></param>
        /// <returns></returns>
        public bool DeploySitecoreConnector(string sitecoreDeployFolder)
        {
            Debug("Initializing Sitecore connector ...");

            string sourceFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            FileInfo serviceLibrary = new FileInfo(sourceFolder + @"\HedgehogDevelopment.TDS.PackageInstallerService.dll");
            FileInfo signalRLibrary = new FileInfo(sourceFolder + @"\Microsoft.AspNet.SignalR.Client.dll");
            FileInfo servicePostInstallDll = new FileInfo(sourceFolder + @"\HedgehogDevelopment.SitecoreProject.PackageInstallPostProcessor.dll");
            FileInfo serviceFile = new FileInfo(sourceFolder + @"\Includes\TdsPackageInstaller.asmx");

            if (!serviceLibrary.Exists)
            {
                ShowError("Cannot find file " + serviceLibrary);

                return false;
            }

            if (!serviceFile.Exists)
            {
                ShowError("Cannot find file " + serviceFile);

                return false;
            }

            if (!Directory.Exists(sitecoreDeployFolder + Properties.Settings.Default.SitecoreConnectorFolder))
            {
                Directory.CreateDirectory(sitecoreDeployFolder + Properties.Settings.Default.SitecoreConnectorFolder);
            }

            SitecoreConnectorDll = sitecoreDeployFolder + @"bin\" + serviceLibrary.Name;
            SitecorePostDeployActionsDll = sitecoreDeployFolder + @"bin\" + servicePostInstallDll.Name;
            SitecoreConnectorAsmx = sitecoreDeployFolder + Properties.Settings.Default.SitecoreConnectorFolder + @"\" + serviceFile.Name;

            string signalRDeployLocation = sitecoreDeployFolder + @"bin\" + signalRLibrary.Name;

            CopyFileIfNotChanged(serviceLibrary, SitecoreConnectorDll);
            CopyFileIfNotChanged(servicePostInstallDll, SitecorePostDeployActionsDll);
            CopyFileIfNotChanged(signalRLibrary, signalRDeployLocation);
            CopyFileIfNotChanged(serviceFile, SitecoreConnectorAsmx);

            Debug("Sitecore connector deployed successfully.");

            return true;
        }

        public void CopyFileIfNotChanged(FileInfo fileInfo, string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                if (fileInfo.Length != new FileInfo(path).Length)
                {
                    Debug("Already exists - " + path);
                    File.Copy(fileInfo.FullName, path, true);
                }
            }
            else
            {
                Debug("Copying - " + path);
                File.Copy(fileInfo.FullName, path, true);
            }
        }

        /// <summary>
        /// Displays an error message
        /// </summary>
        /// <param name="message"></param>
        public void ShowError(string message)
        {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("Try `packageinstaller --help' for more information.");
        }

        /// <summary>
        /// Removes the sitecore connector from the site
        /// </summary>
        public void RemoveSitecoreConnector()
        {
            if (!string.IsNullOrEmpty(SitecoreConnectorDll) && !string.IsNullOrEmpty(SitecoreConnectorAsmx))
            {
                //File.SetAttributes(SitecoreConnectorASMX, FileAttributes.Normal);
                //File.Delete(SitecoreConnectorASMX);

                Debug("Sitecore connector removed successfully.");
            }
        }

        /// <summary>
        /// Writes a debug message to the console
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Debug(string format, params object[] args)
        {
            if (Verbosity > 0)
            {
                Console.Write($"[{DateTime.Now.ToString("hh:mm:ss")}] ");
                Console.WriteLine(format, args);
            }
        }
    }
}
