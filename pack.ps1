# pack global tool

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [String]
    $Version
)

$OutputPath="./nupkg/$Version"
dotnet pack src/Tur.csproj -c Release --include-source --include-symbols -r win-x64 --output $OutputPath -p:Version=$Version -p:PackageVersion=$Version

$PkgPath="$OutputPath/tur.$Version.nupkg"
if (!(Test-Path $PkgPath)){
    Write-Error "No package found under $PkgPath"
}
else{
    $ApiKey=[System.Environment]::GetEnvironmentVariable('NugetApiKey')
    if(!$ApiKey){
        Write-Error "Invalid ApiKey. Please check environment variable NugetApiKey."
    }
    else{
        dotnet nuget push $PkgPath -k $ApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
    }
}