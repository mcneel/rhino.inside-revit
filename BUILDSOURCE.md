# Build Rhino.Inside Revit from source
These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites
* Git ([download](https://git-scm.com/downloads))
* Visual Studio 2019 (16.0 or above) ([download](https://visualstudio.microsoft.com/downloads/))
* .NET Framework Developer Pack (4.6.1, 4.6.2, 4.7, 4.8) ([download](https://www.microsoft.com/net/download/visual-studio-sdks))
* Rhino WIP ([download](https://www.rhino3d.com/download/rhino/wip))
* Autodesk Revit 2017-2021 ([download](https://www.autodesk.com/products/revit/free-trial))
* Add this link to your bookmarks 😉 ([API docs](https://www.apidocs.co/apps/))

## Getting Source & Build
1. Clone the repository. At the command prompt, enter the following command:
```
git clone --recursive https://github.com/mcneel/rhino.inside-revit.git
```
2. In Visual Studio, open `rhino.inside-revit\src\RhinoInside.Revit.sln`.
3. Set the Solution Configuration drop-down to the **Debug <revit_version>**, with Revit version you have installed or want to test. This will properly link the correct Revit API libraries to the project.
4. Navigate to _Build_ > _Build Solution_ to begin your build.

## Installing & Uninstalling
The project is configured to copy .addin file as well as output files to the folder `%APPDATA%\Autodesk\Revit\Addins\<revit_version>\RhinoInside.Revit\` folder in order to make Revit load this add-on next time it runs.

In order to uninstall it you can use Visual Studio _Build_ > _Clean Solution_ command or just navigate to the folder `%APPDATA%\Autodesk\Revit\Addins\<revit_version>` and remove the file `RhinoInside.Revit.addin` and the folder `RhinoInside.Revit/`
