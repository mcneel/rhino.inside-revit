---
title: Python Component in Revit
order: 100
---

Grasshopper has three scripted components. One for python (IronPython to be specific) programming language and another two for VB.NET and C#. These scripted components allows a user to create custom logic for a Grasshopper component. The component, therefore, can accept a configurable number of input and output connection points.

![]({{ "/static/images/guides/rir-ghpython01.png" | prepend: site.baseurl }})

Since {{ site.terms.rir }} project brings Rhino and Grasshopper into the {{ site.terms.revit }} environment, the scripted components also get access to the Revit API runtime. In this article we will discuss using the python component to create custom components for Revit.

## Setting Up

When adding a new python component into the Grasshopper definition, you will get the default imports:

{% highlight python %}
"""Provides a scripting component.
    Inputs:
        x: The x script variable
        y: The y script variable
    Output:
        a: The a output variable"""

__author__ = ""
__version__ = ""

import rhinoscriptsyntax as rs
{% endhighlight %}

In order to access the various APIs we need to import them into the script scope first. To access Revit and {{ site.terms.rir }} we need to first import the CLR (Common-Language-Runtime) module in python and use that to add the necessary library references:

{% highlight python %}
# Common-Language-Runtime module provided by IronPython
import clr

# add reference so base system types e.g. Enum
clr.AddReference('System.Core')

# add reference to API provided by {{ site.terms.rir }}
clr.AddReference('RhinoInside.Revit')

# add reference to Revit API (two DLLs)
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')
{% endhighlight %}

Now we can import the namespaces into the script scope:

{% highlight python %}
# from System.Core DLL
from System import Enum

# {{ site.terms.rir }} API
import RhinoInside
from RhinoInside.Revit import Revit, Convert

# Revit API
from Autodesk.Revit import DB
from Autodesk.Revit import UI
{% endhighlight %}

## Custom User Component

Since the imports mentioned above need to be done for every single python component, the process can get tedious. You can setup a template python component with a default script importing all the most frequently used APIs and save that as a *User Component* in Grasshopper:

![]({{ "/static/images/guides/rir-ghpython02.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/rir-ghpython03.png" | prepend: site.baseurl }})

After the user object has been created, you can easily create a new python component from the user object and it will have the template python script with all your default imports:

![]({{ "/static/images/guides/rir-ghpython04.png" | prepend: site.baseurl }})

Here is a template script that covers most of the use cases:

{% highlight python %}
import clr
clr.AddReference('System.Core')
clr.AddReference('RhinoInside.Revit')
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')

from System import Enum

import rhinoscriptsyntax as rs
import Rhino
import RhinoInside
import Grasshopper
from Grasshopper.Kernel import GH_RuntimeMessageLevel as RML
from RhinoInside.Revit import Revit, Convert
from Autodesk.Revit import DB

# access to Revit as host
REVIT_VERSION = Revit.ActiveUIApplication.Application.VersionNumber
# access the active document object
doc = Revit.ActiveDBDocument

# a few utility methods
def show_warning(msg):
    ghenv.Component.AddRuntimeMessage(RML.Warning, msg)

def show_error(msg):
    ghenv.Component.AddRuntimeMessage(RML.Error, msg)

def show_remark(msg):
    ghenv.Component.AddRuntimeMessage(RML.Remark, msg)

# write your code here
# ...
{% endhighlight %}

You can download the User Object for this template from this button:

{% include ltr/download_comp.html archive='/static/ghnodes/GhPython Script.ghuser' name='GhPython Script' %}


## Example

This example component will create a sphere of an adjustable radius in Revit and Rhino. It will pass that sphere onto other Grasshopper components through the output and it will create the sphere in Revit and bake into Rhino if the button connected to the input is pressed.

![]({{ "/static/images/guides/rir-ghpython05.png" | prepend: site.baseurl }})

As you see in the image above, we have renamed the input components and also the input and output parameters on the python components. This is a really good practice and it makes the definition a lot more clear to a new user.

Once this foundation is ready, then we can continue to create the script.

### Creating Sphere Geometry

To show how a geometry that is created in Grasshopper, previews in both Rhino and Revit dynamically we will use the script below. This script will create a sphere based on the `Radius` input value:

{% highlight python %}
import clr
clr.AddReference('System.Core')
clr.AddReference('RhinoInside.Revit')
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')

from System import Enum

import rhinoscriptsyntax as rs
import Rhino
import RhinoInside
import Grasshopper
from RhinoInside.Revit import Revit, Convert
from Autodesk.Revit import DB

doc = Revit.ActiveDBDocument

Sphere = Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, Radius)
{% endhighlight %}

The `Sphere()` method is from the `Rhino.Geometry` namespace and is part of the `RhinoCommon` API.

By setting the output to `Sphere` Grasshopper will preview the results in both Rhino and Revit (Grasshopper is smart to know that some geometry is set on the output parameter). It also allows the *Preview* option on the component to be toggled and the sphere geometry to be passed down to the next component.

Now we can change the slider value to adjust the radius. Make sure the slider values are set to a big-enough value to the resulting sphere is visible in your Revit and Rhino models.

### Baking to Revit and Rhino

We can add a custom baking function in this script. This can serve as a template to almost an unlimited number of ways and elements that one might want to create Revit objects from Grasshopper.

Because baking objects to Revit can take a long time and many times only should be done once, this bake functions will only execute if the `Trigger` input is set to `True` on the component. This way we can decide to bake the object once we are happy with the results.

First, let's create a bake function:

{% highlight python %}
def create_geometry(doc):
    # convert the sphere into Brep
    brep = Sphere.ToBrep()
    # let's create a mesh from the Brep
    meshes = Rhino.Geometry.Mesh.CreateFromBrep(
        brep,
        Rhino.Geometry.MeshingParameters.Default
        )

    # now let's pick the Generic Model category for
    # our baked geometry in Revit
    revit_category = DB.ElementId(DB.BuiltInCategory.OST_GenericModel)

    # Finally we can create a DirectShape using Revit API
    # inside the Revit document and add the sphere mesh
    # to the DirectShape
    ds = DB.DirectShape.CreateElement(doc, revit_category)
    # we will use Convert.ToHost method to convert the
    # Rhino mesh to Revit mesh because that is what
    # the AppendShape() method expects
    for geom in Convert.ToHost(meshes):
        ds.AppendShape(geom)
{% endhighlight %}

Once we are done creating this function, we can modify the script to listen for the trigger and call this function.

{% capture api_note %}
All changes to the Revit model need to be completed inside a *Transaction*. To facilitate this, {{ site.terms.rir }} provides the `Revit.EnqueueAction` method that will wrap our function inside a transaction and calls when Revit is ready to accept changes to active document. The transaction mechanism is designed to ensure only one Revit Add-in can make changes to the document at any time. To create your own transactions, see [Handling Transactions](#handling-transactions)
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% highlight python %}
if Trigger:
    Revit.EnqueueAction(
        Action[DB.Document](create_geometry)
    )
{% endhighlight %}

And here is the complete sample code:

{% highlight python %}
import clr
clr.AddReference('System.Core')
clr.AddReference('RhinoInside.Revit')
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')

from System import Enum, Action

import rhinoscriptsyntax as rs
import Rhino
import RhinoInside
import Grasshopper
from RhinoInside.Revit import Revit, Convert
from Autodesk.Revit import DB

doc = Revit.ActiveDBDocument

def create_geometry(doc):
    brep = Sphere.ToBrep()
    meshes = Rhino.Geometry.Mesh.CreateFromBrep(
        brep,
        Rhino.Geometry.MeshingParameters.Default
        )

    revit_category = DB.ElementId(DB.BuiltInCategory.OST_GenericModel)
    ds = DB.DirectShape.CreateElement(doc, revit_category)

    for geom in Convert.ToHost(meshes):
        ds.AppendShape(geom)

Sphere = Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, Radius)

if Trigger:
    Revit.EnqueueAction(
        Action[DB.Document](create_geometry)
    )
{% endhighlight %}

## Handling Transactions

To effectively create new transactions and handle the changes to your model in Grasshopper python components, use the try-catch block example below:

{% highlight python %}
# create and start the transaction
t = DB.Transaction(doc, '<give a descriptive name to your transaction>')
t.Start()
try:
    # change Revit document here
    # commit the changes after all changes has been made
    t.Commit()
except Exception as txn_err:
    # if any errors happen while changing the document, an exception is thrown
    # make sure to print the exception message for debugging
    show_error(txn_err)
    # and rollback the changes made before error
    t.RollBack()
{% endhighlight %}

## Inspecting Revit

To inspect which version of Revit you are using, use the `REVIT_VERSION` global variable provided in the template script above. See example below:

{% highlight python %}
REVIT_VERSION = Revit.ActiveUIApplication.Application.VersionNumber

if REVIT_VERSION == 2019:
    # do stuff using Revit 2019 API
else:
    # do other stuff
{% endhighlight %}

## Additional Resources

Here are a few links to more resources about all the APIs mentioned here:

* [API Docs for Revit, RhinoCommon, Grasshopper and Navisworks](https://apidocs.co/)
* [The Building Coder for expert guidance in BIM and Revit API](https://thebuildingcoder.typepad.com/)
* [The Grasshopper IO project with the largest catalog of Grasshopper components available](https://rhino.github.io/)
* [Python in Rhino Developer Documentation](https://developer.rhino3d.com/guides/rhinopython/)
* [pyRevit project for Revit](http://wiki.pyrevitlabs.io/)
* [Data Hierarchy in Revit](https://www.modelical.com/en/gdocs/revit-data-hierarchy/)
