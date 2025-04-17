dotnet tool restore
& dotnet run --project src/build/Build.csproj -- $args
exit $LASTEXITCODE;
