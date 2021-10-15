---
title: Rhino.Inside.Revit Plugins
order: 21
group: Developer Interface
---

## Creating {{ site.terms.rir }} Plugins

To access Rhino and Grasshopper APIs, reference latest [RhinoCommon](https://www.nuget.org/packages/RhinoCommon/7.0.0) and [Grasshopper](https://www.nuget.org/packages/Grasshopper/7.0.0) NuGet packages in your project.

To use Revit API, reference Revit API libraries in your project. Your plugin will be loaded in {{ site.terms.rir }} and will have access to these libraries.

Currently {{ site.terms.rir }} does not have a NuGet package that you can reference in your plugin project. However, if you have {{ site.terms.rir }} installed, you can directly reference the `RhinoInside.Revit.dll` file under:
    - `%PROGRAMDATA%\Autodesk\Revit\Addins\20XX\RhinoInside.Revit\`, or
    - `%APPDATA%\Autodesk\Revit\Addins\20XX\RhinoInside.Revit\`

This will give you access to the types defined in this library.

## Accessing Revit Context

To use Revit API, you would need access to the Revit context. Once you have added the `RhinoInside.Revit.dll` reference to your plugin project, you can use the `RhinoInside.Revit.Revit` static type to get access to Revit context and documents:

{% highlight csharp %}
using Grasshopper.Kernel;

using UI = Autodesk.Revit.UI;
using DB = Autodesk.Revit.DB;
using AppServ = Autodesk.Revit.ApplicationServices;

using RhinoInside.Revit;

public class MyPlugin : GH_Component { 
    public void DoSomethingInRevit() {
        // application
        UI.UIApplication uiapp = Revit.ActiveUIApplication;
        AppServ.Application dbapp = Revit.ActiveDBApplication;

        // document
        UI.UIDocument uidoc = Revit.ActiveUIDocument;
        DB.Document doc = Revit.ActiveDBDocument;

        // tolerances
        double angleTolerance = Revit.AngleTolerance;
        double shortCurveTolerance = Revit.ShortCurveTolerance;
        double vertexTolerance = Revit.VertexTolerance;

        // units
        double units = Revit.ModelUnits;

        /*
            now that you have access to the active document,
            to collect elements from model you can create a collector
        */
        var cl = new DB.FilteredElementCollector(doc);
    }
}
{% endhighlight %}


## Install Locations

Once your Grasshopper plugin project is compiled into a `.gha`, you should install it in a way that only load within {{ site.terms.rir }} environment. This is necessary for plugins that required access to the Revit API as these plugins will fail loading on a stand-alone Rhino.

To install Grasshopper plugins for {{ site.terms.rir }}, place the `.gha` files under paths shown here. `20XX` is the version of Revit that you would want your Grasshopper plugin to load:

- All Users: `%PROGRAMDATA%\Grasshopper\Libraries-Inside-Revit-20XX`
- Current User: `%APPDATA%\Grasshopper\Libraries-Inside-Revit-20XX`

Any `.gha` or `.ghlink` file that is stored there is loaded in the correct Revit version when {{ site.terms.rir }} is loaded.

