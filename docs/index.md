# Hinox [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/)

_Hinox_ is a library part of the [_SceneGate_](https://github.com/SceneGate)
framework that provides support for _PS1_ (PSX) file formats.

## Supported formats

🚧 Project in an early development phase. No formats are supported yet.

## Usage

The project provides the following .NET libraries (NuGet packages in nuget.org).
The libraries work on supported versions of .NET.

- [![SceneGate.Hinox](https://img.shields.io/nuget/v/SceneGate.Hinox?label=SceneGate.Hinox&logo=nuget)](https://www.nuget.org/packages/SceneGate.Hinox)
  🚧 **not ready yet**
  - `SceneGate.Hinox.Audio`: audio codecs.

### Preview release

Preview releases can be found in this
[Azure DevOps package repository](https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview).
To use a preview release, create a file `nuget.config` in the same directory of
your solution file (.sln) with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear/>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="SceneGate-Preview" value="https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="SceneGate-Preview">
      <package pattern="SceneGate.Hinox*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Special thanks

The standard file formats were based on the amazing reverse engineering work of
Martin Korth at [PSX Spex](http://problemkaputt.de/psx-spx.htm).
