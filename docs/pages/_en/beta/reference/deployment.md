---
title: Deployment
order: 41
group: Deployment & Configs
---

## Required installers

1. [Autodesk Revit {{ site.terms.revit_versions }}](https://www.autodesk.com/products/revit/free-trial)
2. [{{ site.terms.rhino }}]({{ site.versions.beta.rhino_download }}).
3. [Latest {{ site.terms.rir }} installer]({{ site.versions.beta.rir_download }}) 

## Automated installer for *All Users* in Windows

To determine how to best automate the push of the {{ site.terms.rhino }} installer, follow the [Automating installation of Rhino 7 Guide](https://wiki.mcneel.com/rhino/installingrhino/7)

To push install {{ site.terms.rir }} quietly for all users use this command line:

```
RhinoInside.Revit.msi ALLUSERS="1" /quiet
```

For more information see [Single Package Authoring](https://docs.microsoft.com/windows/win32/msi/single-package-authoring)


## Installing & Uninstalling

The installer copies the necessary files to the `%ProgramData%\Autodesk\Revit\Addins\<revit_version>\RhinoInside.Revit` folder (for each supported version). Restart {{ site.terms.revit }} to load the add-on. To uninstall, at Windows settings, open [*Apps & features*](ms-settings:appsfeatures), select "RhinoInside.Revit" and click "Uninstall".



