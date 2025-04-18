dotnet tool restore && dotnet xstyler -d /src -r -c .XamlStyler && dotnet run --project ./build/Build.csproj -- "$@"
