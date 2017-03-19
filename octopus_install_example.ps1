$PackageName = $OctopusParameters['PackageName']
$UpdateName = $OctopusParameters['UpdatePackage']

$ApplicationDirectory = $OctopusParameters['Octopus.Tentacle.Agent.ApplicationDirectoryPath']
$EnvironmentName = $OctopusParameters['Octopus.Environment.Name']
$Version = $OctopusParameters['Octopus.Release.Number']

$siteDir = $SiteDirectory
$siteUrl = $SiteUrl

$fullName = $ApplicationDirectory+"\"+$EnvironmentName+"\"+$PackageName+"\"+$Version+"\"+$UpdateName+".update"
$extension = [System.IO.Path]::GetFileNameWithoutExtension($fullName)
       
pingSitecore;
cleanConfigs -SiteDirectory $siteDir;
installPackage -SiteDirectory $siteDir -UpdateName $fullName -SiteUrl $siteUrl
replaceConfigs -TdsExtension $extension -SiteDirectory $siteDir