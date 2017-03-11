# Sitecore.TDS.AdvancedPackageInstaller

Command line tool for TDS package installation automation.

This is an improved version of the https://github.com/HedgehogDevelopment/UpdatePackageInstaller by @HedgehogDevelopment

The improved version includes:

- Console feedback with the installation progress
- Avoiding the recycling and timeouts by checking if service DLLs are already deployed
- Powershell scripts for easy automation
- Updated Sitecore.Update.dll references to latest version (8.1)

## Usage

Console Application paramters:

- packagePath - Path to the TDS .update package (required)
- sitecoreUrl - URL of the target Sitecore instance where package would be installed (required)
- sitecoreDeployFolder - The root of the target Sitecore instance in IIS where package would be installed (required)
- disableLog - disable informative installation message for package entries
- v - increase the verbosity level for the logging

#### Usage Example:

Tools\PackageInstaller.exe -p 'TDS.PackageName.update' -u 'http://localhost' -f 'C:\inetpub\Sitecore\Website' -v 'true'


## Powershell scripts

Along with the console application Powershell scripts are provided

### InstallPackages.ps1

Performs batch package installation from provided update package list (updatelist.txt by default).

#### Usage example:
installpackages.ps1 -siteUrl "http://localhost" -siteDir "C:\inetpub\Sitecore\Website\"

### Sitecore_Modules.ps1

Contains the list of useful Powershell functions to automate the package installation.

- **installPackage** - installs package using the command line tool using specified paramters
- **replaceConfigs** - goes through the website root and replaces web.config and App_Config/*.config with it's new versions, that were installed from the package.
- **cleanConfigs** - searches the App_Config directory and deletes all configs that were not replaced by the previous TDS package installation
- **pingSitecore** - performs requests to Sitecore instance to check if it's back from recycle

The example of this module usage can be found in installpackages.ps1. 
