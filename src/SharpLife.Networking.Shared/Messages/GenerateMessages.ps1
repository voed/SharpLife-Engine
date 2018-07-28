#Generate Protobuf source files in directory tree

$fileNames = Get-ChildItem -Path $scriptPath -File -Recurse

foreach ($file in $fileNames)
{
    if ($file.Name.EndsWith("proto"))
    {
        $directory = [System.IO.Path]::GetDirectoryName($file.FullName)
        Write-Host "Generating $file"
        protoc "-I=$directory" --csharp_out=$directory $file.FullName
    }
}
