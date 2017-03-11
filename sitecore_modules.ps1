
function installPackage($UpdateName, $SiteDirectory, $SiteUrl)
{
	Write-Host 'Update name:' $UpdateName -foregroundcolor green -backgroundcolor black
	Write-Host 'Site:' $SiteUrl $SiteDirectory -foregroundcolor green -backgroundcolor black
	& Tools\PackageInstaller.exe -p $UpdateName -u $SiteUrl -f $SiteDirectory -v 'true'
}

function replaceConfigs($SiteDirectory, $TdsExtension)
{
	$tdsExtension = $TdsExtension

    Write-Host "$SiteDirectory $tdsExtension" -foregroundcolor gray -backgroundcolor black

	Write-Host "Starting searching for changed config files with extension $tdsExtension" -foregroundcolor green -backgroundcolor black

	Get-ChildItem -rec "$SiteDirectory\App_Config\" | ForEach-Object -Process {	
		if($_.FullName.EndsWith($tdsExtension ))
		{
			Write-Host $_.FullName  -foregroundcolor yellow -backgroundcolor black
			
			Copy-Item $_.FullName.Replace($tdsExtension ,"") ($_.FullName.Replace($tdsExtension ,"") + ".bak")
			Copy-Item $_.FullName $_.FullName.Replace($tdsExtension ,"");
			Remove-Item $_.FullName;
		}
	}

	Get-ChildItem $SiteDirectory | ForEach-Object -Process {	
		if($_.FullName.EndsWith($tdsExtension ))
		{
			Write-Host $_.FullName  -foregroundcolor yellow -backgroundcolor black
			
			Copy-Item $_.FullName.Replace($tdsExtension ,"") ($_.FullName.Replace($tdsExtension ,"") + ".bak")
			Copy-Item $_.FullName $_.FullName.Replace($tdsExtension ,"");
			Remove-Item $_.FullName;
		}
	}

	Write-Host "Finished applying config changes" -foregroundcolor green -backgroundcolor black
}

function cleanConfigs($SiteDirectory)
{
	$tdsExtension = ".files"

	Write-Host "Starting searching for changed config files with extension $tdsExtension" -foregroundcolor green -backgroundcolor black

	Get-ChildItem -rec "$SiteDirectory\App_Config\" | ForEach-Object -Process {	
		if($_.FullName.EndsWith($tdsExtension ))
		{
			Write-Host 'Removed $_.FullName' -foregroundcolor yellow -backgroundcolor black
			
			Remove-Item $_.FullName;
		}
	}

	Get-ChildItem $SiteDirectory | ForEach-Object -Process {	
		if($_.FullName.EndsWith($tdsExtension ))
		{
			Write-Host 'Removed $_.FullName' -foregroundcolor yellow -backgroundcolor black
			
			Remove-Item $_.FullName;
		}
	}

	Write-Host "Finished applying config changes" -foregroundcolor green -backgroundcolor black
}

function pingSitecore($siteUrl)
{
	$uri = "http://localhost/sitecore";

    if(-Not [string]::IsNullOrEmpty($siteUrl))
    {
        $uri = $siteUrl + "/sitecore/";
    }

	$expectedCode = [int]200
    $timeoutSeconds = [int]240

	Write-Host "Starting verification request to $uri" -foregroundcolor gray -backgroundcolor black
	Write-Host "Expecting response code $expectedCode." -foregroundcolor gray -backgroundcolor black

	$timer = [System.Diagnostics.Stopwatch]::StartNew()
	$success = $false
	do
	{
		try
		{
		   
			Write-Host "Making request to $uri using anonymous authentication" -foregroundcolor gray -backgroundcolor black
			$response = Invoke-WebRequest -Uri $uri -Method Get -UseBasicParsing
		 
			$code = $response.StatusCode
			Write-Host "Recieved response code: $code" -foregroundcolor gray -backgroundcolor black
			
			if($response.StatusCode -eq 200)
			{
				$success = $true
			}
		}
		catch
		{
			# Anything other than a 200 will throw an exception so
			# we check the exception message which may contain the 
			# actual status code to verify
			
			Write-Host "Request failed :-(" -foregroundcolor white -backgroundcolor red
			Write-Host $_.Exception -foregroundcolor white -backgroundcolor red

			if($_.Exception -like "*(200)*")
			{
				$success = $true
			}
		}

		if(!$success)
		{
			Write-Host "Trying again in 5 seconds..." -foregroundcolor yellow -backgroundcolor black
			Start-Sleep -s 5
		}
	}
	while(!$success -and $timer.Elapsed -le (New-TimeSpan -Seconds $timeoutSeconds))

	$timer.Stop()

	# Verify result

	if(!$success)
	{
		throw "Verification failed - giving up."
	}

	Write-Host "Sucesss! Found status code 200" -foregroundcolor green -backgroundcolor black
}