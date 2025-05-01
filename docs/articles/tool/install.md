# hinox-utils: Installation

**hinox-utils** is a console application delivered as a _dotnet-tool_.

## Prerequisites

The utility requires the
[.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). You
can verify the installation by running `dotnet --list-runtimes` from a terminal.
If it says `Microsoft.NETCore.App` 8.0 or higher is installed, it should be
fine.

## Installation

There are two official ways to get the tool. Both methods download the same
binary.

### GitHub release page

Download the latest stable version from the
[GitHub release page](https://github.com/SceneGate/Hinox/releases) of the Hinox
project.

Preview versions can be downloaded for logged GitHub users from the project
_Action_ pipeline artifacts. But they expire after a few days. Using the _.NET
tool_ method may be easier for preview builds.

### .NET tool

If you prefer to use _dotnet tool_, install the program globally for the current
user by running the following command from a terminal:
`dotnet tool install -g SceneGate.Hinox.Utils`

The application will be available to run via `dotnet hinox-utils`.

To try a preview version, add the following argument to any of the above
commands:
`--prerelease --add-source https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json`

To update the application to the latest version run:
`dotnet tool update -g SceneGate.Hinox.Utils`. Add the above argument to update
to the latest preview.
