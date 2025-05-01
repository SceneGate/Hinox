# Hinox [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/)

_Hinox_ is a set of libraries and utilities part of the
[_SceneGate_](https://github.com/SceneGate) framework that provides support for
_PS1_ (PSX) file formats.

## Supported formats

- :speaker: **VAB** audio containers
  - Versions 5, 6 and 7
  - Reading and writing header (VH) and body (VB and VAB)
  - Tool to export and import.
  - Limitation: the VAG format and its audio codec are not supported yet.

## Tooling

The project provides an application to convert files between different formats.
This is a _console_ application, it doesn't have a graphical interface (no
window). Use a terminal like _Windows Terminal_ on Windows or bash on Unix.

Follow the [installation](./articles/tool/install.md) instructions, then head
directly to some of its commands like the [VAB export](./articles/tool/vab.md).

## Development libraries

The Hinox .NET (C#) library provides models representing file formats and
[Yarhl](https://scenegate.github.io/Yarhl/docs/core/formats/converters.html)
converters for their (de)serialization. Check-out the additional dev categories
for information in the APIs available.

- [![SceneGate.Hinox](https://img.shields.io/nuget/v/SceneGate.Hinox?label=SceneGate.Hinox&logo=nuget)](https://www.nuget.org/packages/SceneGate.Hinox)
  - `SceneGate.Hinox.Audio`: audio codecs.

It's recommended to become familiar with the basic concepts of Yarhl before
starting to use this project. Check-out its
[tutorial](https://scenegate.github.io/Yarhl/docs/core/getting-started/tutorial.html)
for a quick introduction.

## Special thanks

The standard file formats were based on the amazing reverse engineering work of
Martin Korth at [PSX Spex](http://problemkaputt.de/psx-spx.htm).
