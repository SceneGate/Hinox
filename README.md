# Hinox

<!-- markdownlint-disable MD033 -->
<p align="center">
  <a href="https://www.nuget.org/packages/SceneGate.Hinox">
    <img alt="Stable version" src="https://img.shields.io/nuget/v/SceneGate.Hinox?label=nuget.org&logo=nuget" />
  </a>
  &nbsp;
  <a href="https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview">
    <img alt="GitHub commits since latest release (by SemVer)" src="https://img.shields.io/github/commits-since/SceneGate/Hinox/latest?sort=semver" />
  </a>
  &nbsp;
  <a href="https://github.com/SceneGate/Hinox/actions/workflows/build-and-release.yml">
    <img alt="Build and release" src="https://github.com/SceneGate/Hinox/actions/workflows/build-and-release.yml/badge.svg" />
  </a>
  &nbsp;
  <a href="https://choosealicense.com/licenses/mit/">
    <img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat" />
  </a>
  &nbsp;
</p>

_Hinox_ is a library part of the [_SceneGate_](https://github.com/SceneGate)
framework that provides support for **PS1 (PSX) file formats.**

## Supported formats

- :speaker: **VAB** audio containers
  - Versions 5, 6 and 7
  - Header (VH): read and write
  - Body (VB and VAB): read and write

## Usage

The project provides the following .NET libraries (NuGet packages in nuget.org).
The libraries work on supported versions of .NET.

- [![SceneGate.Hinox](https://img.shields.io/nuget/v/SceneGate.Hinox?label=SceneGate.Hinox&logo=nuget)](https://www.nuget.org/packages/SceneGate.Hinox)
  - `SceneGate.Hinox.Audio`: audio formats.

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

## Documentation

Documentation is not yet available, but it will be published in the
[project website](https://scenegate.github.io/Hinox).

Don't hesitate to ask questions in the
[project Discussion site!](https://github.com/SceneGate/Hinox/discussions)

## Build

The project requires .NET 9.0 SDK to build.

To build, test and generate artifacts run:

```sh
# Build and run tests
dotnet run --project build/orchestrator

# (Optional) Create bundles (nuget, zips, docs)
dotnet run --project build/orchestrator -- --target=Bundle
```

To build the documentation only, run:

```sh
dotnet docfx docs/docfx.json --serve
```

## Special thanks

The standard file formats were based on the amazing reverse engineering work of
Martin Korth at [PSX Spex](http://problemkaputt.de/psx-spx.htm).
