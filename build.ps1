dotnet tool restore
& dotnet xstyler -d .\src -r -c .XamlStyler
& dotnet run --project src/build/Build.csproj -- $args
exit $LASTEXITCODE;
