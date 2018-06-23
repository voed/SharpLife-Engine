#Generate a file that contains the current time
$resourcesDirectory = "./Resources"

$absoluteResourcesDirectory = Resolve-Path -Path $resourcesDirectory

$fileName = "$absoluteResourcesDirectory/BuildDate.txt"

if (!(Test-Path $absoluteResourcesDirectory))
{
	New-Item -ItemType directory -Path $absoluteResourcesDirectory
}

$time = [System.DateTimeOffset]::Now.ToString()

#Write without appending a newline
[System.IO.File]::WriteAllText($fileName, $time, [System.Text.Encoding]::UTF8)
