using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.ServiceModel;
using Microsoft.Owin.Hosting;

namespace HedgehogDevelopment.PackageInstaller
{
    /// <summary>
    /// Installer command line utility. Uses NDesk.Options to parse the command line. For more information, please see
    /// http://www.ndesk.org/Options. 
    /// </summary>
    public class Program
    {
        static int _verbosity;
        public static bool DisableLogging { get; private set; }

        static string SitecoreConnectorDll { get; set; }
        static string SitecoreConnectorAsmx { get; set; }
        static string SitecorePostDeployActionsDll { get; set; }

        static void Main(string[] args)
        {

                #region Declare options and installer variables

                // Installer variables
                string packagePath = null;
                string sitecoreWebUrl = null;
                string sitecoreDeployFolder = null;
                bool showHelp = args.Length == 0;

                // Options declaration
                OptionSet options = new OptionSet()
                {
                    {
                        "p|packagePath=",
                        "The {PACKAGE PATH} is the path to the package. The package must be located in a folder reachable by the web server.\n",
                        v => packagePath = v
                    },
                    {
                        "u|sitecoreUrl=", "The {SITECORE URL} is the url to the root of the Sitecore server.\n",
                        v => sitecoreWebUrl = v
                    },
                    {
                        "f|sitecoreDeployFolder=",
                        "The {SITECORE DEPLOY FOLDER} is the UNC path to the Sitecore web root.\n",
                        v => sitecoreDeployFolder = v
                    },
                    {
                        "v", "Increase debug message verbosity.\n",
                        v => { if (v != null) ++_verbosity; }
                    },
                    {
                        "h|help", "Show this message and exit.",
                        v => showHelp = v != null
                    },
                    {
                        "l|disableLog=",
                        "Disable extensive logging for installation",
                        v => { if (v != null) DisableLogging = true; }
                    }
                };

                #endregion

                // Parse options - exit on error
                List<string> extra;
                try
                {
                    extra = options.Parse(args);
                }
                catch (OptionException e)
                {
                    ShowError(e.Message);
                    Environment.Exit(100);
                }

                // Display help if one is requested or no parameters are provided
                if (showHelp)
                {
                    ShowHelp(options);
                    return;
                }

                #region Validate and process parameters

                bool parameterMissing = false;

                if (string.IsNullOrEmpty(packagePath))
                {
                    ShowError("Package Path is required.");

                    parameterMissing = true;
                }

                if (string.IsNullOrEmpty(sitecoreWebUrl))
                {
                    ShowError("Sitecore Web URL ie required.");

                    parameterMissing = true;
                }

                if (string.IsNullOrEmpty(sitecoreDeployFolder))
                {
                    ShowError("Sitecore Deploy folder is required.");

                    parameterMissing = true;
                }

                if (!parameterMissing)
                {
                    if (Directory.Exists(sitecoreDeployFolder))
                    {
                        try
                        {
                            Debug("Initializing update package installation: {0}", packagePath);
                            if (sitecoreDeployFolder.LastIndexOf(@"\", StringComparison.Ordinal) != sitecoreDeployFolder.Length - 1)
                            {
                                sitecoreDeployFolder = sitecoreDeployFolder + @"\";
                            }

                            if (sitecoreWebUrl.LastIndexOf(@"/", StringComparison.Ordinal) != sitecoreWebUrl.Length - 1)
                            {
                                sitecoreWebUrl = sitecoreWebUrl + @"/";
                            }

                            // Install Sitecore connector
                            if (DeploySitecoreConnector(sitecoreDeployFolder))
                            {

                                using (WebApp.Start<SignalRStartup>(new StartOptions("http://127.0.0.1:9422")))
                                using (var service = new TdsPackageInstaller.TdsPackageInstaller())
                                {
                                    service.Url = string.Concat(sitecoreWebUrl,
                                        Properties.Settings.Default.SitecoreConnectorFolder, "/TdsPackageInstaller.asmx");
                                    service.Timeout = 5000000;                                   

                                    Debug("=== Initializing package installation ..");

                                    var initialColor = Console.ForegroundColor;
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    var summary = service.InstallPackage(packagePath);
                                    Console.ForegroundColor = initialColor;

                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    foreach (var entry in summary.Entries)
                                    {
                                        if (entry.Level == "Error")
                                        {
                                            Debug("{0} {1} {2}", entry.Action, entry.Level, entry.Message);
                                        }
                                    }

                                    Console.ForegroundColor = ConsoleColor.White;

                                    Debug("Update package installation completed.");

                                    Debug("=== Summary: Errors - {0} Warnings - {1} Collisions - {2}", summary.Errors,
                                        summary.Warnings, summary.Collisions);

                                    Console.ForegroundColor = initialColor;
    

                            }
                        }
                            else
                            {
                                Console.Error.WriteLine("Sitecore connector deployment failed.");

                                Environment.Exit(101);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Exception: {0}({1})\n{2}", ex.Message, ex.GetType().Name,
                                ex.StackTrace);

                            if (ex.InnerException != null)
                            {
                                Console.Error.WriteLine("\n\nInnerException: {0}({1})\n{2}", ex.InnerException.Message,
                                    ex.InnerException.GetType().Name, ex.InnerException.StackTrace);
                            }

                            Environment.Exit(102);
                        }
                        finally
                        {
                            // Remove Sitecore connection
                            RemoveSitecoreConnector();
                        }
                    }
                    else
                    {
                        ShowError(string.Format("Sitecore Deploy Folder {0} not found.", sitecoreDeployFolder));
                    }
                }            

            #endregion
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        /// <param name="opts"></param>
        static void ShowHelp(OptionSet opts)
        {
            Console.WriteLine("Usage: packageinstaller [OPTIONS]");
            Console.WriteLine("Installs a sitecore package.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"-v -sitecoreUrl ""http://mysite.com/"" -sitecoreDeployFolder ""C:\inetpub\wwwroot\mysite\Website"" -packagePath ""C:\Package1.update""");
            Console.WriteLine();
            Console.WriteLine("Options:");

            opts.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Displays an error message
        /// </summary>
        /// <param name="message"></param>
        static void ShowError(string message)
        {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("Try `packageinstaller --help' for more information.");
        }

        /// <summary>
        /// Deploys the 
        /// </summary>
        /// <param name="sitecoreDeployFolder"></param>
        /// <returns></returns>
        static bool DeploySitecoreConnector(string sitecoreDeployFolder)
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

        private static void CopyFileIfNotChanged(FileInfo fileInfo, string path)
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
        /// Removes the sitecore connector from the site
        /// </summary>
        static void RemoveSitecoreConnector()
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
        static void Debug(string format, params object[] args)
        {
            if (_verbosity > 0)
            {
                Console.Write($"[{DateTime.Now.ToString("hh:mm:ss")}] ");
                Console.WriteLine(format, args);
            }
        }
    }
}
