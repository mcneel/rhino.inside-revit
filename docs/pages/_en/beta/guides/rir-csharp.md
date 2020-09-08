---
title: C# Component in Revit
order: 101
group: Scripting
---

Grasshopper has three scripted components. One for C# programming language and another two for VB.NET and python (IronPython to be specific). These scripted components allows a user to create custom logic for a Grasshopper component. The component, therefore, can accept a configurable number of input and output connection points.

![]({{ "/static/images/guides/rir-csharp01.png" | prepend: site.baseurl }})

Since {{ site.terms.rir }} project brings Rhino and Grasshopper into the {{ site.terms.revit }} environment, the scripted components also get access to the Revit API runtime. In this article we will discuss using the C# component to create custom components for Revit.

## Setting Up

### API Bindings

When adding a new C# component into the Grasshopper definition, you will get the default bindings:

{% highlight csharp %}
using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
{% endhighlight %}

{% include ltr/warning_note.html note='Under current implementation, the C# component does not automatically bind the Revit API assemblies. This step is manual but will be automated in future releases' %}

{% include ltr/issue_note.html issue_id='184' note='C# Scripted Component should automatically bind to Host API DLLs' %}

In order to access the various APIs we need to import them into the script scope first. To access Revit and {{ site.terms.rir }} we need to reference the Revit API assemblies:

- RevitAPI.dll
- RevitAPIUI.dll

These two DLLs (Dynamically Linked Libraries) are located under the Revit installation directory (by default at `%PROGRAMFILES%/Autodesk/Revit XXXX` where `XXXX` is Revit version e.g. `2019`). To bind these DLLs to your C# component, Right-Click on the component and select **Manage Assemblies**:

![]({{ "/static/images/guides/rir-csharp02.png" | prepend: site.baseurl }})

Click Add inside the **Manage Assemblies** window (or Drag and Drop from the **Recent Assemblies** list on the right, to the left panel):

![]({{ "/static/images/guides/rir-csharp03.png" | prepend: site.baseurl }})

Navigate to the Revit installation directory and add the DLLs listed above to the window:

![]({{ "/static/images/guides/rir-csharp04.png" | prepend: site.baseurl }})

Now we can safely add the Revit API to the script scope:

{% highlight csharp %}
using DB = Autodesk.Revit.DB;
using UI = Autodesk.Revit.UI;
{% endhighlight %}

#### {{ site.terms.rir }} API

{{ site.terms.rir }} provides a few utility methods as well. Later on this guide, we are going to use one of these utility functions to bake a sphere into a Revit model. To access these methods, we need to add another binding to `RhinoInside.Revit.dll` assembly which is shipped with {{ site.terms.rir }}.

Please follow the steps described above, and add a reference to this assembly as well. You can find the DLL under `%APPDATA%/Autodesk/Revit/Addins/XXXX/RhinoInside.Revit/` where `XXXX` is the Revit version e.g. `2019`

![]({{ "/static/images/guides/rir-csharp04b.png" | prepend: site.baseurl }})

Then modify the lines shown above to add a reference to this assembly as well:

{% highlight csharp %}
using DB = Autodesk.Revit.DB;
using UI = Autodesk.Revit.UI;

using RIR = RhinoInside.Revit;
// add extensions methods as well
// this allows calling .ToXXX() convertor methods on Revit objects
using RhinoInside.Revit.Convert.Geometry
{% endhighlight %}

### Implement RunScript

The code structure for C# Components is a bit more complex than the python scripted components due the complexity of the C# language itself. In C#, you can only create functions as methods of a class. Therefore, Grasshopper creates a required class named `Script_Instance` and will automatically create a `RunScript` method for this class. These pre-prepared code parts are marked with gray background and can not be deleted. The is the starting point for your code:

![]({{ "/static/images/guides/rir-csharp04a.png" | prepend: site.baseurl }})

## Custom User Component

Since the imports mentioned above need to be done for every single C# component, the process can get tedious. You can setup a template C# component with a default script importing all the most frequently used APIs and save that as a *User Component* in Grasshopper:

![]({{ "/static/images/guides/rir-csharp05.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/rir-csharp06.png" | prepend: site.baseurl }})

After the user object has been created, you can easily create a new C# component from the user object and it will have the template C# script with all your default imports:

![]({{ "/static/images/guides/rir-csharp07.png" | prepend: site.baseurl }})

## Example

This example component will create a sphere of an adjustable radius in Revit and Rhino. It will pass that sphere onto other Grasshopper components through the output and it will create the sphere in Revit and bake into Rhino if the button connected to the input is pressed.

![]({{ "/static/images/guides/rir-csharp08.png" | prepend: site.baseurl }})

As you see in the image above, we have renamed the input components and also the input and output parameters on the C# components. This is a really good practice and it makes the definition a lot more clear to a new user.

Once this foundation is ready, then we can continue to create the script.

### Creating Sphere Geometry

To show how a geometry that is created in Grasshopper, previews in both Rhino and Revit dynamically we will use the script below. This script will create a sphere based on the `Radius` input value:

{% highlight csharp %}
  private void RunScript(object Radius, object Trigger, ref object Sphere)
  {
    Sphere = new Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, (double) Radius);
  }
{% endhighlight %}

The `Sphere()` method is from the `Rhino.Geometry` namespace and is part of the `RhinoCommon` API.

By setting the output to `Sphere` Grasshopper will preview the results in both Rhino and Revit (Grasshopper is smart to know that some geometry is set on the output parameter). It also allows the *Preview* option on the component to be toggled and the sphere geometry to be passed down to the next component.

Now we can change the slider value to adjust the radius. Make sure the slider values are set to a big-enough value to the resulting sphere is visible in your Revit and Rhino models.

### Baking to Revit and Rhino

We can add a custom baking method in this script. This can serve as a template to almost an unlimited number of ways and elements that one might want to create Revit objects from Grasshopper.

Because baking objects to Revit can take a long time and many times only should be done once, this bake method will only execute if the `Trigger` input is set to `True` on the component. This way we can decide to bake the object once we are happy with the results.

First, let's create a bake method:

{% highlight csharp %}
  private void CreateGeometry(DB.Document doc) {
    // convert the sphere into Brep
    var brep = _sphere.ToBrep();

    // now let's pick the Generic Model category for
    // our baked geometry in Revit
    var revitCategory = new DB.ElementId((int) DB.BuiltInCategory.OST_GenericModel);

    // Finally we can create a DirectShape using Revit API
    // inside the Revit document and add the sphere mesh
    // to the DirectShape
    var ds = DB.DirectShape.CreateElement(doc, revitCategory);
    // we will use .ToBrep() method to convert the
    // Rhino brep to Revit DB.Solid because that is what
    // the AppendShape() method expects
    var shapeList = new List<DB.GeometryObject>() { brep.ToSolid() };
    ds.AppendShape(shapeList);
  }
{% endhighlight %}

Notice that there is a reference to `_sphere` on the third line of the script above. Since Revit requires Transactions when making changes to the model, and since {{ site.terms.rir }} manages the Transactions required by all the components to make changes to the Revit model, we will not handle the Transaction inside our script and will need to ask {{ site.terms.rir }} to run the new method for us:

{% highlight csharp %}
  RIR.Revit.EnqueueAction(CreateGeometry);
{% endhighlight %}

But the `RIR.Revit.EnqueueAction` only accepts an `Action<DB.Document>` so we will need to store the created sphere on a private field first, so `CreateGeometry` can later access and bake this sphere. Hence we will need to define this private property:

{% highlight csharp %}
  private Rhino.Geometry.Sphere _sphere;
{% endhighlight %}

Once we are done creating this function and the private property, we can modify the `RunScript` method to listen for the trigger and call this function. Notice that we are now storing the new sphere into `_sphere` first so the `CreateGeometry` can access that bake this sphere:

{% highlight csharp %}
  private void RunScript(object Radius, object Trigger, ref object Sphere)
  {
    // make the sphere
    _sphere = new Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, (double) Radius);
    // if requested, ask {{ site.terms.rir }} to run bake method
    if ((bool) Trigger) {
      RIR.Revit.EnqueueAction(CreateGeometry);
    }

    // pass the sphere to output
    Sphere = _sphere;
  }
{% endhighlight %}

And here is the complete sample code:

{% highlight csharp %}
  private Rhino.Geometry.Sphere _sphere;

  private void RunScript(object Radius, object Trigger, ref object Sphere)
  {
    _sphere = new Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, (double) Radius);
    if ((bool) Trigger) {
      RIR.Revit.EnqueueAction(CreateGeometry);
    }

    Sphere = _sphere;
  }

  private void CreateGeometry(DB.Document doc) {
    var brep = _sphere.ToBrep();

    var revitCategory = new DB.ElementId((int) DB.BuiltInCategory.OST_GenericModel);

    var ds = DB.DirectShape.CreateElement(doc, revitCategory);
    var shapeList = new List<DB.GeometryObject>() { brep.ToSolid() };
    ds.AppendShape(shapeList);
  }
}
{% endhighlight %}

## Handling Transactions

To effectively create new transactions and handle the changes to your model in Grasshopper C# components, use the try-catch block example below:

{% highlight csharp %}
// create and start the transaction
var doc = RIR.Revit.ActiveDBDocument;
var t = new DB.Transaction(doc, "<give a descriptive name to your transaction>");
t.Start();
try {
    // change Revit document here
    // commit the changes after all changes has been made
    t.Commit();
}
catch (Exception txnErr) {
    // if any errors happen while changing the document, an exception is thrown
    // make sure to print the exception message for debugging
    Print(txnErr.Message);
    // and rollback the changes made before error
    t.RollBack();
}
{% endhighlight %}

## Inspecting Revit

To inspect which version of Revit you are using, check the Revit version number as shown below. Remember that your script still needs to compile against the active Revit API without errors:

{% highlight csharp %}
if (RIR.Revit.ActiveUIApplication.Application.VersionNumber == 2019) {
    // do stuff that is related to Revit 2019
} else {
    // do other stuff
}
{% endhighlight %}


## Node In Code

You can also use the Grasshopper components that you love, inside your code, as functions and therefore create much more complex and powerful scripted components without writing the necessary complicated codes. The Grasshopper components basically become like powerful library functions that help you get your job done and have a smaller script to maintain. Here is what you need to add to use the Grasshopper components in your code. First we need to import the Node-in-Code handle:

{% highlight csharp %}
using Rhino.NodeInCode;
{% endhighlight %}

Now you can access the component, like a function. These function names are prefixed with the name of their Grasshopper plugin to avoid naming conflicts. The example below shows how we can access the {% include ltr/comp.html uuid="0d9f07e2-" %} inside the code:

{% highlight csharp %}
// Components is inside Rhino.NodeInCode namespace
Components.NodeInCodeFunctions.RhinoInside_AddMaterial()

// alternatively you can use a finder function to find
// the component by name.
var comp = Components.FindComponent("RhinoInside_AddMaterial")
// and call the .Invoke function
comp.Invoke()
{% endhighlight %}

Now lets put this knowledge into use and create a custom scripted component that imports a Brep geometry into Revit and assigns a material to it. This scripted component effectively combines 3 different Grasshopper components into one. Note that there are obviously easier ways to do the same task, however this is a simple example of how components can be chained together in a script.

We will start with the [template script component that we created above](#custom-user-component). Add it to your Grasshopper definition and modify the variable parameters to make it look like this:

![]({{ "/static/images/guides/rir-csharp09.png" | prepend: site.baseurl }})

Let's find our components first, and notify the user if any of them does not exist.

{% highlight csharp %}
// AddMaterial component to create a material
var addMaterial = Components.FindComponent("RhinoInside_AddMaterial");
// and AddGeometryDirectShape to create a DirectShape element in Revit
var addGeomDirectshape = Components.FindComponent("RhinoInside_AddGeometryDirectShape");
{% endhighlight %}

Let's also notify the user if any of these components do not exist:

{% highlight csharp %}
if (addMaterial == null || addGeomDirectshape == null) {
    throw new Exception("One or more of the necessary components are not available as node-in-code");
}
{% endhighlight %}

Now let's create the main logic of our script:

{% highlight csharp %}
// create a color object. modify the logic as you wish
var color = System.Drawing.Color.FromName("DeepSkyBlue");

// now create the material using the node-in-code
// note that just like the Grasshopper component, the node-in-code also
// takes 3 inputs in the exact same order (top to bottom)
var newMaterials = addMaterial.Invoke("Sky Material", true, color);

// and now use the AddGeometryDirectShape node-in-code to
// create the DirectShape element in Revit
// Notes:
//    - BREP is our input Brep object
//    - new_materials is a list of new materials so we are grabbing the first element
//    - get_category is a function that finds a Revit category from its name
var dsElements = addGeomDirectshape.Invoke(
    "Custom DS",
    GetCategory("Walls"),
    BREP,
    newMaterials[0]
);

// assign the new DirectShape element to output
DS = dsElements[0];
{% endhighlight %}

And here is the complete script:

{% highlight csharp %}
using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;
using UI = Autodesk.Revit.UI;
using RIR = RhinoInside.Revit;
using RhinoInside.Revit.Convert.Geometry;

using Rhino.NodeInCode;


public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  private void Print(string text) { }
  private void Print(string format, params object[] args) { }
  private void Reflect(object obj) { }
  private void Reflect(object obj, string method_name) { }
#endregion

#region Members
  private readonly RhinoDoc RhinoDocument;
  private readonly GH_Document GrasshopperDocument;
  private readonly IGH_Component Component;
  private readonly int Iteration;
#endregion

  private void RunScript(object BREP, ref object DS)
  {
    // AddMaterial component to create a material
    var addMaterial = Components.FindComponent("RhinoInside_AddMaterial");
    // and AddGeometryDirectShape to create a DirectShape element in Revit
    var addGeomDirectshape = Components.FindComponent("RhinoInside_AddGeometryDirectShape");
    
    if (addMaterial == null || addGeomDirectshape == null) {
      throw new Exception("One or more of the necessary components are not available as node-in-code");
    }

    if(BREP != null) {
      // create a color object. modify the logic as you wish
      var color = System.Drawing.Color.FromName("DeepSkyBlue");
  
      // now create the material using the node-in-code
      // note that just like the Grasshopper component, the node-in-code also
      // takes 3 inputs in the exact same order (top to bottom)
      var newMaterials = addMaterial.Invoke("Sky Material", true, color);

      // and now use the AddGeometryDirectShape node-in-code to
      // create the DirectShape element in Revit
      // Notes:
      //    - BREP is our input Brep object
      //    - new_materials is a list of new materials so we are grabbing the first element
      //    - get_category is a function that finds a Revit category from its name
      var dsElements = addGeomDirectshape.Invoke(
        "Custom DS",
        GetCategory("Walls"),
        BREP,
        newMaterials[0]
        );
  
        // assign the new DirectShape element to output
      DS = dsElements[0];
    }
  }

  public DB.Category GetCategory(string categoryName){
    var doc = RIR.Revit.ActiveDBDocument;
    foreach(DB.Category cat in doc.Settings.Categories) {
      if (cat.Name == categoryName)
        return cat;
    }
    return null;
  }
}
{% endhighlight %}

![]({{ "/static/images/guides/rir-csharp10.png" | prepend: site.baseurl }})


## Additional Resources

Here are a few links to more resources about all the APIs mentioned here:

* [API Docs for Revit, RhinoCommon, Grasshopper and Navisworks](https://apidocs.co/)
* [The Building Coder for expert guidance in BIM and Revit API](https://thebuildingcoder.typepad.com/)
* [The Grasshopper IO project with the largest catalog of Grasshopper components available](https://rhino.github.io/)
* [pyRevit project for Revit](http://wiki.pyrevitlabs.io/)
* [Data Hierarchy in Revit](https://www.modelical.com/en/gdocs/revit-data-hierarchy/)
