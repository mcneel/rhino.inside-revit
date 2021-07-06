<img src="docs/assets/img/logo.svg" width="128px"/>

# Rhino.Inside® Revit

[![Build status](https://ci.appveyor.com/api/projects/status/9ot0iyjqwb1jdn6m/branch/master?svg=true)](https://ci.appveyor.com/project/mcneel/rhino-inside-revit/branch/master)

Please see the [Rhino.Inside.Revit Wiki](https://www.rhino3d.com/inside/revit/)
for more information on how to use the project. Sections listed below provide
more information about the codebase for the developers who want to contribute to
this project or the Wiki.

## Overview

The Rhino.Inside® Technology allows Rhino, Grasshopper, and their add-ons to be
embedded within other products.
This repository is the Rhino.Inside® for Autodesk Revit®, named Rhino.Inside.Revit.

## Architecture

See Wiki pages below for more information about the architecture of this project.

- [Architecture](https://www.rhino3d.com/inside/revit/beta/reference/rir-arch)

## Build Process

McNeel team is using internal [AppVeyor-based](https://www.appveyor.com/docs/)
build systems to build this project branches. If you need custom builds, you
are encouraged to fork the project and adapt to your own CI/CD system.

### Building from Source

See [Building from Source](BUILDSOURCE.md) page for instructions on how to
build the project from source.

## Installer

The installer is generated using [WiX toolset](https://wixtoolset.org/)
(see `src/RhinoInside.Revit.Setup.sln`) and is updated automatically on
every new build.
See the [Rhino.Inside.Revit Wiki](https://www.rhino3d.com/inside/revit/)
homepage to download them most recent installers.

## Wiki

The `docs/` directory in this repo contains the contents of
[Rhino.Inside.Revit Wiki](https://www.rhino3d.com/inside/revit/).

See [Wiki Readme](docs/readme.md) for more information.

## Artwork

All the artwork source and exported files are under `art/` directory.

See [Artwork Readme](art/README.md) for more information.

## Development Conventions

See [Development Conventions](CONVENTIONS.md) page for conventions and
guidelines in contributing to the codebase.

## API Docs

Currently the project does not have any documentation for the API.
This is an area that needs improvement.
The Wiki has a [reference page](https://www.rhino3d.com/inside/revit/beta/reference/rir-api)
for the API.

## Known Issues

Known issues with the product are listed at
[References > Known Issues](https://www.rhino3d.com/inside/revit/beta/reference/known-issues)
on the Wiki.
Please keep this page updated when new and persistent issues are identified.
