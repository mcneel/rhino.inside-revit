---
title: Getting Started with Rhino.Inside.Revit
layout: ltr/page-h2-toc
---

## What is {{ site.terms.rir }}

[Rhino.Inside](https://github.com/mcneel/rhino.inside) is a new technology developed by {{ site.terms.mcneel }} that allows embedding {{ site.terms.rhino }} into other applications. Rhino.Inside is being embedded into many applications from a wide variety of disciplines.

{{ site.terms.rir }} is one of the Rhino.Inside's most exciting projects. It is an addon for {{ site.terms.revit }} that allows {{ site.terms.rhino }} to be loaded into the memory of Revit just like other Revit addons.

{{ site.terms.rir }} brings the power of {{ site.terms.rhino }} and Grasshopper to the {{ site.terms.revit }} environment

## Installation

Download {{ site.terms.rir }} and {{ site.terms.rhino }} from the links below

<!-- download links -->
{% include ltr/download_buttons.html version=site.versions.beta %}

The {{ site.terms.rir }} installer is also available on [Food4Rhino Website]({{ site.foodrhino_url }})

Let's install {{ site.terms.rhino }} first

- Run the installer and go through the setup process until {{ site.terms.rhino }} is fully installed
- Run {{ site.terms.rhino }} and make sure it is licensed and runs with no issues

Now let's install {{ site.terms.rir }}

- Run the installer and go through the setup process until {{ site.terms.rir }} is fully installed

Now that we have installed both dependencies, we can proceed to loading {{ site.terms.rir }}

## Loading {{ site.terms.rir }}

Launch {{ site.terms.revit }}. You will be prompted to confirm loading {{ site.terms.rir }}. Make sure to press **Always Load** to skip this dialog in the future.

![]({{ "/static/images/started/revit-prompt.png" | prepend: site.baseurl }})

After load is complete, note the new *Rhinoceros* panel under the *Add-ins* tab

![]({{ "/static/images/started/rir-addon.png" | prepend: site.baseurl }})

Click on the *Rhino* button to start loading {{ site.terms.rir }}. The addon, attempts to load {{ site.terms.rhino }} inside Revit's memory and make sure it is licensed. Once the load process is completed, a new *Rhinoceros* toolbar will appear in Revit.

![]({{ "/static/images/started/revit-toolbar.png" | prepend: site.baseurl }})

The new toolbar contains many new buttons that give you access to

- {{ site.terms.rhino }} itself
- Python IDE (with access to Revit API)
- Grasshopper (with custom Revit components)

See [{{ site.terms.rir }} Interface]({{ site.baseurl }}{% link _en/beta/reference/rir-interface.md %}) for a complete list of buttons in the *Rhinoceros* tab

If you encountered any errors, please consult the [Known Issues]({{ site.baseurl }}{% link _en/beta/reference/known-issues.md %}) page for a list of already known issues and their temporary workarounds.

## Extracting Revit Geometry

To get started, let's create a simple definition in Grasshopper to extract geometry of a Revit element. Grasshopper is by far one of the most exciting addons for Rhino and as part of the {{ site.terms.rir }} project has the potential to improve the design and documentation in {{ site.terms.revit }} dramatically.

Open a simple Revit model and draw an single wall

![]({{ "/static/images/started/revit-doc.png" | prepend: site.baseurl }})

Now open Grasshopper by clicking on the Grasshopper button in the new *Rhinoceros* tab

![]({{ "/static/images/started/rir-gh.png" | prepend: site.baseurl }})

From the *Params > Revit* panel, add a *Geometric Element* parameter

![]({{ "/static/images/started/rir-gcomp1.png" | prepend: site.baseurl }})

Now Right-Click on the component and Select One Revit Geometric Element. Grasshopper switches to Revit window and asks you to select a Revit element. Select the single Wall element we created earlier.

![]({{ "/static/images/started/rir-gcomp2.png" | prepend: site.baseurl }})

Now drop a *Panel* component into the definition and connect the *Geometric Element* output to its input

![]({{ "/static/images/started/rir-gcomp3.png" | prepend: site.baseurl }})

You can see that this parameter now contains the selected wall element.

![]({{ "/static/images/started/rir-gcomp4.png" | prepend: site.baseurl }})

Let's grab the Wall geometry by using a Revit-specific component. From *Revit > Elements* add an *Element.Geometry* component.

![]({{ "/static/images/started/rir-gcomp5.png" | prepend: site.baseurl }})

After passing the *Geometric Element* output to the input of the *Element.Geometry*, the new Revit-specific component extracts the Wall geometry from the Wall element using the Revit API. The geometry is then converted to Rhino Breps (since other Grasshopper components might not be familiar with Revit geometry) so it can be passed to other Grasshopper components for further processing.

![]({{ "/static/images/started/rir-gcomp6.png" | prepend: site.baseurl }})

Similar to other Grasshopper geometric components, the output geometry is shows as preview in both Revit and Rhino viewports

![]({{ "/static/images/started/rir-gcomp7.png" | prepend: site.baseurl }})

As you have seen, working with {{ site.terms.rir }} is very intuitive and simple. The Revit-specific Grasshopper components are one of the most important aspects of the {{ site.terms.rir }} project. Grasshopper script components (python and C#) can also be used to access Rhino or Revit APIs simultaneously and create custom components in Grasshopper for your specific workflows.

## Creating Revit Elements

In the section above, we saw an example of converting Revit geometry into Rhino using custom Revit components in Grasshopper.

Grasshopper has many other Revit-specific components. A subset of these components allow the user to create new content inside the Revit document.

Let's create a simple wall in Revit using a few of these components. To create a wall in Revit we need:

- A line that is the basis of the wall. It defines that start and end point
- Wall type
- Level to host the new wall
- Wall height

Open Rhino (inside Revit) and create a simple line

![]({{ "/static/images/started/rir-rhino1.png" | prepend: site.baseurl }})

Now open Grasshopper and add a curve component. Right-Click the component and select the newly created line in Rhino.

![]({{ "/static/images/started/rir-rhino2.png" | prepend: site.baseurl }})

Now from the *Revit > Input* panel and a *Model.CategoriesPicker* component,

![]({{ "/static/images/started/rir-rhino3.png" | prepend: site.baseurl }})

and also let's add an *ElementType.ByName*, 

![]({{ "/static/images/started/rir-rhino4.png" | prepend: site.baseurl }})

and a *Document.LevelPicker* component as well.

![]({{ "/static/images/started/rir-rhino5.png" | prepend: site.baseurl }})

Finally let's add a Grasshopper integer slider as well to provide the height for our new wall

![]({{ "/static/images/started/rir-rhino6.png" | prepend: site.baseurl }})

To create a wall, we are going to use a custom Grasshopper node that can create a Revit wall by curve. From *Revit > Build* panel add a *AddWall.ByCurve* component.

![]({{ "/static/images/started/rir-rhino7.png" | prepend: site.baseurl }})

Now that we have all these components inside the grasshopper definition, let's organize them before connecting the parameters

![]({{ "/static/images/started/rir-rhino8.png" | prepend: site.baseurl }})

From the list of categories shown on the *Model.CategoriesPicker* component, select the **Walls** category

Now connect the output of the *Model.CategoriesPicker* to the input of *ElementType.ByName* (the input parameter is not visible by default. Drag the arrow over to the left of the component where the input parameter is expected to be)

![]({{ "/static/images/started/rir-rhino9.png" | prepend: site.baseurl }})

The *ElementType.ByName* now shows a list of wall types collected from the model. Select a basic wall type. This wall type is going to be used to create the new wall.

Now connect the rest of the components as shown below

![]({{ "/static/images/started/rir-rhino10.png" | prepend: site.baseurl }})

The *AddWall.ByCurve* component now has all the information to create a new wall in Revit.

![]({{ "/static/images/started/rir-rhino11.png" | prepend: site.baseurl }})

The same wall geometry is also visible in Rhino

![]({{ "/static/images/started/rir-rhino12.png" | prepend: site.baseurl }})


## Grasshopper Interactivity

Arguably the most important feature of a visual programming environment like Grasshopper is the interactivity of its components. {{ site.terms.rir }} project brings this interactivity to the Revit environment and allows the designer to explore the design space a lot more efficiently and create novel solutions.

Let's grab the height slider from the example above, and move it back and forth a bit.

![]({{ "/static/images/started/rir-ghinter.gif" | prepend: site.baseurl }})

Imagine the possibilities!

## GHPython in Revit

Since Rhino is running inside the memory of Revit, potentially all the Rhino and Grasshopper addons can also have access to the Revit API. This feature makes the Python scripting in Rhino and Grasshopper exponentially more powerful since the python scripts can use Rhino API, Grasshopper API, and Revit API at the same time. Alongside these APIs, the {{ site.terms.rir }} addon also provides extra functionality that is mostly focused translating Rhino/Grasshopper data types to Revit and vice versa.

Take a look at this example python script. It imports symbols from all the mentioned APIs into the script.

```python
# adding references to the System, RhinoInside
import clr
clr.AddReference('System.Core')
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')
clr.AddReference('RhinoInside.Revit')

# now we can import symbols from various APIs
from System import Enum

# rhinoscript
import rhinoscriptsyntax as rs

# rhino API
import Rhino

# grasshopper API
import Grasshopper

# revit API
from Autodesk.Revit import DB

# rhino.inside utilities
import RhinoInside API
from RhinoInside.Revit import Revit, Convert

# getting active Revit document
doc = Revit.ActiveDBDocument
```

So to use the example above, we can add the lines below to our script to read the geometry of input Revit element (`E`) using Revit API (`.Geometry[DB.Options()]`) and the pass that to the utility method provided by {{ site.terms.rir }} API to convert the Revit geometry into Rhino (`Convert.ToRhino()`) and finally pass the Rhino geometry to Grasshopper output.

```python
O = Convert.ToRhino(
    E.Geometry[DB.Options()]
    )
```

![]({{ "/static/images/started/rir-ghpy.png" | prepend: site.baseurl }})

{{ site.terms.rir }} is already a very powerful tool but with Python and C# components, the possibilities are endless.

## What's Next

The *Guides* section listed on the navigation bar, is a great next point to see how {{ site.terms.rir }} can be used in tackling many design and documentation challenges in {{ site.terms.revit }}. The articles under this page provide many examples on creating Grasshopper definitions and writing your own custom scripts.

Reach out to {{ site.terms.rir }} developers and the users community on the [forum]({{ site.forum_url }}) if you came across a special condition that might need a new component or a more detailed explanation.