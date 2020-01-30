<img src="docs/assets/img/logo.svg" width="128px"/>
<h1>Rhino.Inside®.Revit</h1>
<p>
<a href="https://ci.appveyor.com/project/mcneel/rhino-inside-revit/branch/master"><img src="https://ci.appveyor.com/api/projects/status/9ot0iyjqwb1jdn6m/branch/master?svg=true"></a>
</p>
<p>Please see the <a href="https://www.rhino3d.com/inside/revit/">Rhino.Inside.Revit Wiki</a> for more information on how to use the project. Sections listed below provide more information about the codebase for the developers who want to contribute to this project or the Wiki.
</p>

## Overview

The Rhino.Inside® Technology allows Rhino, Grasshopper, and their addons to be embedded within other products. This repository is the Rhino.Inside® for Autodesk Revit®, named Rhino.Inside.Revit

## Architecture

See Wiki pages below for more information about the architecture of this project.

- [Architecture](https://www.rhino3d.com/inside/revit/beta/reference/rir-arch) for more information
- [Design Challenges](https://www.rhino3d.com/inside/revit/beta/reference/rir-design) for more information

## Build Process

McNeel team is using internal [AppVeyor-based](https://www.appveyor.com/docs/) build systems to build this project branches. If you need custom builds, you are encouraged to fork the project and adapt to your own CI/CD system.

## Installer

The installer is generated using [WiX toolset](https://wixtoolset.org/) (see `src/RhinoInside.Revit.Setup.sln`) and is updated automatically on every new build. See the [Rhino.Inside.Revit Wiki](https://www.rhino3d.com/inside/revit/) homepage to download them most recent installers.

## Wiki

The `docs/` directory in this repo contains the contents of [Rhino.Inside.Revit Wiki](https://www.rhino3d.com/inside/revit/).

See [Wiki Readme](docs/readme.md) for more information.

## API Docs

Currently the project does not have any documentation for the API. This is an area that needs improvement. [The Wiki has a reference page for the API](https://www.rhino3d.com/inside/revit/beta/reference/rir-api).

## Known Issues

Known issues with the product are listed at [References > Known Issues](https://www.rhino3d.com/inside/revit/beta/reference/known-issues) on the Wiki. Please keep this page updated when new and persistent issues are identified.
