---
title: Deployment
order: 4
---

## Required installers

1. Revit 2017-2020
2. The current [{{ site.terms.rhino }}]({{ site.versions.beta.rhino_download }}) which requires a Rhino 6 license to run.
3. Download the [latest {{ site.terms.rir }} installer]({{ site.versions.beta.rir_download }}) 

## Automated installer for *All Users* in Windows

To determine how to best automate the push of the {{ site.terms.rhino }} installer, follow the [Automating installation of Rhino 6 Guide](https://wiki.mcneel.com/rhino/installingrhino/6)

To push install {{ site.terms.rir }} quietly for all users use this command line:

```
RhinoInside.Revit.msi ALLUSERS="1" /quiet
```

For more information see [Single Package Authoring](https://docs.microsoft.com/windows/win32/msi/single-package-authoring)


## Installing & Uninstalling

The installer copies the necessary files to the `%APPDATA%\Autodesk\Revit\Addins\<revit_version>\` folder (for each supported version). Restart {{ site.terms.revit }} to load the add-on. To uninstall, open *Programs and Features*, select "RhinoInside.Revit" and click "Uninstall".



