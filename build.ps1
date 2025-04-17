dotnet tool restore
& dotnet run --project build/Build.csproj -- $args
exit $LASTEXITCODE;
