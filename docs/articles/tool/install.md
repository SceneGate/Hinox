# hinox-utils: Installation

**hinox-utils** is a console application delivered as a _dotnet-tool_.

First, ensure
[.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is
installed. You can verify the installation by running `dotnet --list-runtimes`
from a terminal. If it says `Microsoft.NETCore.App` 8.0 or higher is installed,
it should be fine.

Then, install the latest version of the tool by running the following command
from a terminal: `dotnet tool install -g SceneGate.Hinox.Utils`

The application will be available to run via `dotnet hinox-utils`.

## Preview versions

In case of wanting to try a preview version, add the following argument to any
of the above commands:
`--prerelease --add-source https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json`

## Updates

To update the application to the latest stable released version run:
`dotnet tool update -g SceneGate.Hinox.Utils`
