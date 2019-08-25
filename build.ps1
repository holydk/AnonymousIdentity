# anonymous
""
"###########################################"
"######## IdentityServer4.Anonymous ########"
"###########################################"
""
$ErrorActionPreference = "Stop";
dotnet run --project build -- $args

if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

Copy-Item -path .\artifacts\*.nupkg -Destination .\nuget