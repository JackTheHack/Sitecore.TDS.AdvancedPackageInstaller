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
        internal static bool DisableLogging;

        internal static bool SimpleDeploy;
        private static string _password;
        private static string _userName;

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

        static void DeployWithSignalR(DeployHelper deployHelper, string sitecoreDeployFolder, string sitecoreWebUrl, string packagePath)
        {
            if (Directory.Exists(sitecoreDeployFolder))
            {
                try
                {
                    deployHelper.Debug("Initializing update package installation: {0}", packagePath);
                    if (sitecoreDeployFolder.LastIndexOf(@"\", StringComparison.Ordinal) != sitecoreDeployFolder.Length - 1)
                    {
                        sitecoreDeployFolder = sitecoreDeployFolder + @"\";
                    }

                    if (sitecoreWebUrl.LastIndexOf(@"/", StringComparison.Ordinal) != sitecoreWebUrl.Length - 1)
                    {
                        sitecoreWebUrl = sitecoreWebUrl + @"/";
                    }

                    // Install Sitecore connector
                    if (deployHelper.DeploySitecoreConnector(sitecoreDeployFolder))
                    {

                        using (WebApp.Start<SignalRStartup>(new StartOptions("http://127.0.0.1:9422")))
                        using (var service = new TdsPackageInstaller.TdsPackageInstaller())
                        {
                            service.Url = string.Concat(sitecoreWebUrl,
                                Properties.Settings.Default.SitecoreConnectorFolder, "/TdsPackageInstaller.asmx");
                            service.Timeout = 5000000;

                            if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                            {
                                service.UserCredentialsValue = new TdsPackageInstaller.UserCredentials() { userid = _userName, password = _password };
                            }

                            deployHelper.Debug("=== Initializing package installation ..");

                            var initialColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Gray;

                            var summary = service.InstallPackage(packagePath);

                            Console.ForegroundColor = initialColor;

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            foreach (var entry in summary.Entries)
                            {
                                if (entry.Level == "Error")
                                {
                                    deployHelper.Debug("{0} {1} {2}", entry.Action, entry.Level, entry.Message);
                                }
                            }

                            Console.ForegroundColor = ConsoleColor.White;

                            deployHelper.Debug("Update package installation completed.");

                            deployHelper.Debug("=== Summary: Errors - {0} Warnings - {1} Collisions - {2}", summary.Errors,
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
                    deployHelper.RemoveSitecoreConnector();
                }
            }
            else
            {
                deployHelper.ShowError(string.Format("Sitecore Deploy Folder {0} not found.", sitecoreDeployFolder));
            }
        }

        private static void DeployPackage(DeployHelper deployHelper, string sitecoreWebUrl, string packagePath)
        {
            try
            {
                deployHelper.Debug("Initializing update package installation: {0}", packagePath);

                if (sitecoreWebUrl.LastIndexOf(@"/", StringComparison.Ordinal) != sitecoreWebUrl.Length - 1)
                {
                    sitecoreWebUrl = sitecoreWebUrl + @"/";
                }

                var serviceUrl = string.Concat(sitecoreWebUrl, Properties.Settings.Default.SitecoreConnectorFolder, "/TdsPackageInstaller.asmx");

                Console.WriteLine("Connecting to " + serviceUrl);

                    using (var service = new TdsPackageInstaller.TdsPackageInstaller())
                    {
                        service.Url = serviceUrl;
                        service.Timeout = 5000000;


                        if(!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                        {
                            service.UserCredentialsValue = new TdsPackageInstaller.UserCredentials() { userid = _userName, password = _password };
                        }

                        deployHelper.Debug("=== Initializing package installation ..");

                        var initialColor = Console.ForegroundColor;

                        Console.ForegroundColor = ConsoleColor.Gray;
                    
                        var summary = service.InstallPackageSilently(packagePath);

                        Console.ForegroundColor = initialColor;

                        Console.ForegroundColor = ConsoleColor.Yellow;

                        foreach (var entry in summary.Entries)
                        {
                            if (entry.Level == "Error")
                            {
                                deployHelper.Debug("{0} {1} {2}", entry.Action, entry.Level, entry.Message);
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.White;

                        deployHelper.Debug("Update package installation completed.");

                        deployHelper.Debug("=== Summary: Errors - {0} Warnings - {1} Collisions - {2}", summary.Errors,
                                summary.Warnings, summary.Collisions);

                        Console.ForegroundColor = initialColor;


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
        }

        static void Main(string[] args)
        {
            #region Declare options and installer variables

            var deployHelper = new DeployHelper();

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
                        v => { if (v != null) ++ deployHelper.Verbosity; }
                    },
                    {
                        "h|help", "Show this message and exit.",
                        v => showHelp = v != null
                    },
                    {
                        "l|disableLog=",
                        "Disable extensive logging for installation",
                        v => { if (v != null) DisableLogging = true; }
                    },
                    {
                        "s|simpleDeploy",
                        "Just deploy the package from the installation folder.",
                        v => {if (v!=null) SimpleDeploy = true; }
                    },
                    {
                        "user",
                        "Username",
                        v => {_userName = !string.IsNullOrEmpty(_userName) ? _userName = v : _userName = null; }
                    },
                    {
                        "password",
                        "Password",
                        v => {_password = !string.IsNullOrEmpty(_password) ? _password = v : _password = null; }
                    }
                };

            #endregion

            #region Parse options
            // Parse options - exit on error
            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                deployHelper.ShowError(e.Message);
                Environment.Exit(100);
            }

            // Display help if one is requested or no parameters are provided
            if (showHelp)
            {
                ShowHelp(options);
                return;
            }
            #endregion

            #region Validate and process parameters

            bool parameterMissing = false;

            if (string.IsNullOrEmpty(packagePath))
            {
                deployHelper.ShowError("Package Path is required.");

                parameterMissing = true;
            }

            if (string.IsNullOrEmpty(sitecoreWebUrl))
            {
                var azureSiteUrl = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");

                if (!string.IsNullOrEmpty(azureSiteUrl))
                {
                    sitecoreWebUrl = azureSiteUrl;
                    Console.WriteLine("Azure Website Url: " + azureSiteUrl);
                }
                else
                {
                    deployHelper.ShowError("Sitecore Web URL ie required.");

                    parameterMissing = true;
                }
            }

            if (!SimpleDeploy && string.IsNullOrEmpty(sitecoreDeployFolder))
            {
                var azureSitePath = Environment.GetEnvironmentVariable("WEBROOT_PATH");

                if (!string.IsNullOrEmpty(azureSitePath))
                {
                    sitecoreDeployFolder = azureSitePath;
                    Console.WriteLine("Azure Website Path: " + azureSitePath);
                }
                else
                {
                    deployHelper.ShowError("Sitecore Deploy folder is required.");

                    parameterMissing = true;
                }


            }

            if (parameterMissing)
            {
                return;
            }

            #endregion

            if (!SimpleDeploy)
            {
                DeployWithSignalR(deployHelper, sitecoreDeployFolder, sitecoreWebUrl, packagePath);
            }
            else
            {
                DeployPackage(deployHelper, sitecoreWebUrl, packagePath);
            }
        }
    }
}

