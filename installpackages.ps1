param (
    [string]$siteDir = "C:\inetpub\wwwroot\Sitecore\Website",
    [string]$siteUrl = "http://localhost",
	[string]$packageList = 'updatelist.txt'
	);

If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
	{   
	$arguments = "& '" + $myinvocation.mycommand.definition + "'"
	Start-Process powershell -Verb runAs -ArgumentList $arguments
	Break
	}


. ".\sitecore_modules.ps1"


Write-Host "==== Starting Installation ===" -foregroundcolor magenta -backgroundcolor green

Get-Content $packageList | Foreach-Object {
	$str = $_.Trim();
	if(!$str.StartsWith('#'))
	{		
        $fullName = "$PSScriptRoot\installation_files\$str"
        $extension = [System.IO.Path]::GetFileNameWithoutExtension($fullName)      

        Write-Host "===== $fullName ($extension)====="  -foregroundcolor black -backgroundcolor green
               
        pingSitecore -SiteUrl $siteUrl;
	    cleanConfigs -SiteDirectory $siteDir;
	    installPackage -SiteDirectory $siteDir -UpdateName $fullName -SiteUrl $siteUrl
        replaceConfigs -TdsExtension $extension -SiteDirectory $siteDir
	}
}

Write-Host "==== Installation Complete ===" -foregroundcolor magenta -backgroundcolor green