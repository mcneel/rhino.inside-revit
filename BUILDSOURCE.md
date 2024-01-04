# Build Rhino.Inside Revit from source

These instructions will get you a copy of the project up and running on your
local machine for development and testing purposes.

## Prerequisites

* Git 
  [📥](https://git-scm.com/downloads)
* Visual Studio 2019 (16.0 or above)
  [📥](https://visualstudio.microsoft.com/downloads/)
* .NET Framework Developer Pack (4.8)
  [📥](https://www.microsoft.com/net/download/visual-studio-sdks)
* Rhino 7-8
  [📥](https://www.rhino3d.com/download/rhino/)
* Autodesk Revit 2018-2024
  [📥](https://www.autodesk.com/products/revit/free-trial)
* Add this link to your bookmarks
  ([API docs](https://apidocs.co/))

## Getting Source & Build

1. Clone the repository. At the command prompt, enter the following command:

    ```console
    git clone --recursive https://github.com/mcneel/rhino.inside-revit.git
    ```

2. In Visual Studio, open `rhino.inside-revit\src\RhinoInside.Revit.sln`.
3. Set the _Solution Configuration_ drop-down to the **Debug <rhino_version>** and
   the _Solution Platform_ to the desired **<revit_version>**.
   Use a Rhino and Revit version you have installed and want to test.
   This will set up the appropriate referenced assemblies on the project.
4. Navigate to _Build_ > _Build Solution_ to begin your build.

## Installing & Uninstalling

The project is configured to copy .addin file as well as output files to the folder
`%APPDATA%\Autodesk\Revit\Addins\<revit_version>\RhinoInside.Revit\`
folder in order to make Revit load this add-in next time it runs.

In order to uninstall it you can use Visual Studio _Build_ > _Clean Solution_ command
or just navigate to the folder `%APPDATA%\Autodesk\Revit\Addins\<revit_version>`
and remove the file `RhinoInside.Revit.addin` and the folder `RhinoInside.Revit`.
