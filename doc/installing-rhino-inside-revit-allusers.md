
# Automated Install of Rhino.Inside.Revit for All Users

## Required installers

1. Revit 2017-2020
1. The current [Rhino WIP](https://www.rhino3d.com/download/rhino/wip) which requires a Rhino 6 license to run. 
1. Download the **[latest Rhino.Inside.Revit installer](https://www.rhino3d.com/download/rhino.inside-revit/7/wip)** 

## Automated installer for *All Users* in Windows

To determine how to best automate the push of the Rhino 7 installer, follow the [Automating installation of Rhino 6 Guide](https://wiki.mcneel.com/rhino/installingrhino/6)

To push install Rhino.Inside.Revit quietly for all users use this command line:

```
RhinoInside.Revit.msi ALLUSERS="1" /quiet
```

For more inforamtion see: https://docs.microsoft.com/windows/win32/msi/single-package-authoring


### Installing & Uninstalling
The installer copies the necessary files to the _"%APPDATA%\\Autodesk\\Revit\\Addins\\<revit_version>\\"_ folder (for each supported version). Restart Revit to load the add-on.

To uninstall, open _Programs and Features_, select "RhinoInside.Revit" and click "Uninstall".



