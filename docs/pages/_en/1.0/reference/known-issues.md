---
title: Known Issues
order: 43
group: Deployment & Configs
---

This guide looks at errors that can appear with {{ site.terms.rir }}. This address most of the common errors we have seen. [Please Contact Us](https://www.rhino3d.com/support) whether any of these options worked or did not work. We are working to minimize any of these messages.

## Unsupported openNURBS

### Problem

When {{ site.terms.rir }} attempts to load, the error below appears.

![]({{ "/static/images/reference/known-issues-unsupported-opennurbs.jpg" | prepend: site.baseurl }}){: class="small-image"}

This normally appears when there is a conflict when an older version of the Rhino file reader (openNURBS) has been loaded in Revit.  This normally happens because:

1. A Rhino 3DM file was inserted into Revit before {{ site.terms.rir }} was loaded. Revit is shipped with a different version of the openNURBS module, and loading a Rhino model into the Revit document before activating {{ site.terms.rir }}, cause the conflict
2. Other third-party Revit plugins that have loaded already, reference the Rhino file reader (openNURBS)

### Workaround

Please follow the instructions on [Submitting Debug Info]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %}#submitting-debug-info) to submit the error and debug information to {{ site.terms.rir }} development team.

Saving the project, then restarting Revit is usually the fastest workaround. If {{ site.terms.rir }} is loaded first, then everything should work with no issues.

Some plugins may need to be updated.  Common conflicts are seen with older versions of:
1. [Conveyer](https://provingground.io/tools/conveyor/)
2. [Avail](https://getavail.com/avail-adds-integration-with-mcneel-rhino-modeler/)
3. [{{ site.terms.pyrevit }} ](https://www.notion.so/pyRevit-bd907d6292ed4ce997c46e84b6ef67a0) 

We continue to work with all our partners on this error. Information gathered from the Error Reporting enables us to actively target these conflicts.


## Initialization Error -200

### Problem

When {{ site.terms.rir }} loads, the error below appears.

![]({{ "/static/images/reference/known-issues-error-200.png" | prepend: site.baseurl }})

### Microsoft.WindowsAPICodePack DLL Conflict

The underlying issue is that one of the `Microsoft.WindowsAPICodePack` or `Microsoft.WindowsAPICodePack.Shell` dlls that are shipped with the conflicting Revit add-in, does not have a public key, and the conflicting add-in is not compiled to use the exact version of these dlls that are shipped with the product. The latest version of both these dlls are installed by Rhino into the Global Assembly Cache (GAC) when Rhino is installed. When {{ site.terms.rir }} and the conflicting add-in are both loaded into Revit, one of them ends up using the incompatible dll version and this causes the error.

### Workaround

This normally appears when there is a conflict between Rhino.inside and one or more Revit plugins that have loaded already.

#### Preparing new DLL files

  - Make sure you have a ZIP unpacker installed (e.g. [7zip](https://7ziphelp.com))
  - Download [Microsoft.WindowsAPICodePack](https://www.nuget.org/packages/Microsoft.WindowsAPICodePack-Core/1.1.0) nuget package using the *Download Package* link on the right:
    - Unpack the package
    - Browse to the `lib/` directory inside the unpacked content. Copy the `Microsoft.WindowsAPICodePack.dll`
    - Place inside add-in installation directory, overwriting existing files if any. The specific directory is listed below for known conflicting add-ins.

  - Download [Microsoft.WindowsAPICodePack.Shell](https://www.nuget.org/packages/Microsoft.WindowsAPICodePack-Shell/1.1.0) nuget package using the *Download Package* link on the right:
    - Unpack the package
    - Browse to the `lib/` directory inside the unpacked content. Copy the `Microsoft.WindowsAPICodePack.Shell.dll`
    - Place inside add-in installation directory, overwriting existing files if any. The specific directory is listed below for known conflicting add-ins.

#### Fixing the Conflict

**Naviate Add-ins**: A common conflict is with the suite of *Naviate* tools for Revit. Follow the steps listed above to download the necessary dlls and replace the existing ones inside the *Naviate* installation path (usually `C:\Program Files\Symetri\Naviate\Revit 20XX\Dll\`)

**pyRevit Add-in**: Another common conflict is with an older version (older than 4.7) of the {{ site.terms.pyrevit }} plugin.  While the newer versions to {{ site.terms.pyrevit }} do not cause a problem, an older version might.  Information on the {{ site.terms.pyrevit }} conflict can be found on [issue #628](https://github.com/eirannejad/pyRevit/issues/628). To update the older version of {{ site.terms.pyrevit }}, follow the steps listed above to download the necessary dlls and place them under the `bin/` directory inside pyRevit installation (default path is `%APPDATA%\pyRevit-Master\`)

If this does not solve the problem, then using the [Search for Conflicting Plugins]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %}#search-for-conflicting-plugins) section.

### Xceed.Wpf.Toolkit.dll DLL Conflict

Revit plugins listed below carry a different (sometimes older) version of the `Xceed.Wpf.Toolkit.dll` library. This causes conflict with the version shipped with Rhino, when it is launching inside Revit, since a different version of this library is already loaded.

Known conflicting plugins are:

- **VRay for Revit** (latest)
 
  By default installed under `C:\Program Files\Chaos Group\V-Ray\V-Ray for Revit`

- **BIMTrack for Revit** (latest)
  
  By default installed under `%PROGRAMDATA%\Autodesk\ApplicationPlugins\BimOne.BIMTrack.bundle\Contents\Revit\20XX` or `%APPDATA%\Autodesk\ApplicationPlugins\BimOne.BIMTrack.bundle\Contents\Revit\20XX` if installed per user (replace `20XX` with your Revit version)

#### Workaround

- Close Revit
- Go to the installed directory for any of the conflicting plugins and rename the `Xceed.Wpf.Toolkit.dll` file to `Xceed.Wpf.Toolkit.dll.bak`
- Load Revit and {{ site.terms.rir }} again and it should load


## JSON Error

### Problem

A Long JSON error shows up as shown below

![]({{ "/static/images/reference/known-issues-error-json.png" | prepend: site.baseurl }})

### Workaround

Like the previous -200 error, this is a conflict with another plugin. See the Error - 200 solution for this problem, and the [Search for Conflicting Plugins]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %}#search-for-conflicting-plugins) section below.


## Rhino.Inside tab is missing

{{ site.terms.rir }} looks into Windows Registry to determine which Revit versions are installed. There is a known issue with Revit 2021 installer that writes an incomplete install path to the `InstallLocation` key ([See here for the conversation over this issue](https://discourse.mcneel.com/t/rhino-inside-wont-load-in-revit-2021/100769/13)).

This issue might make the installed Revit 2021, invisible to the {{ site.terms.rir }} installer and therefore {{ site.terms.rir }} will not load in Revit 2021. Updates has been applied to the installer to correct for this issue but it makes the assumption that Revit 2021 is installed under its default path (`C:\Program Files\Autodesk\Revit 2021`). If you have installed Revit 2021 at another location the issue will remain. 

You can either change the value of `LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{7346B4A0-2100-0510-0000-705C0D862004}\InstallLocation` to the full path of where Revit is installed, or update Revit 2021 to latest (latest installers have corrected this issue).


## Grasshopper add-ons are missing in revit

### Problem

Depending on how the Grasshopper add-ons are installed, or how the user profile is setup on your network, some of the add-ons might be located on a network location. Revit, by default, prohibits loading external DLL files from a network location. This is primarily a security measure set by the .net runtime.

If you notice one or more of the Grasshopper add-ons are missing when loaded inside Revit, please open Rhino window (inside Revit) and press F2 to open the command history in Rhino. You should be able to see a report of all the Grasshopper add-ons that have been loaded. Below is an example of such report:

```
...
* Loading Grasshopper core assembly...
* Loading CurveComponents assembly...
* Loading FieldComponents assembly...
* Loading GalapagosComponents assembly...
* Loading IOComponents assembly...
...
```

If there are any load errors like the example below, marked as `Exception System.NotSupportedException` then most probably Revit is blocking loading the Grasshopper add-ons from a network location:

```
Message: Could not load file or assembly 'file:///C:\Users\n.bucco\AppData\Roaming\Grasshopper\Libraries\anemone1.gha' or one of its dependencies. Operation is not supported. (Exception from HRESULT: 0x80131515)

Exception System.NotSupportedException:

Message: An attempt was made to load an assembly from a network location which would have caused the assembly to be sandboxed in previous versions of the .NET Framework. This release of the .NET Framework does not enable CAS policy by default, so this load may be dangerous. If this load is not intended to sandbox the assembly, please enable the loadFromRemoteSources switch. See http://go.microsoft.com/fwlink/?LinkId=155569 for more information.
```

### Workaround

Modify the `Revit.exe.config` file (by default located alongside `Revit.exe` at `C:\Program Files\Autodesk\Revit 20XX\Revit.exe.config` where `20XX` is your Revit version) and add the following directive to the `<runtime>` xml element:

```xml
<loadFromRemoteSources  enabled="true"/>
```

Example:

![]({{ "/static/images/reference/loadremoteresources.png" | prepend: site.baseurl }})