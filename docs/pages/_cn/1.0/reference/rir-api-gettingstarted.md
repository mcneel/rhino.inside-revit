---
title: "API: Getting Started"
order: 20
group: Developer Interface
---

## Accessing Revit Context

Using {% include rir_api_type.html version="1.0" title="Revit" topic="T_RhinoInside_Revit_Revit" %} static class you can gain access to the running instance of Revit database and ui, and the open Revit documents.

In Python:

{% highlight python %}
import clr
clr.AddReference("RhinoInside.Revit")
from RhinoInside.Revit import Revit

# application
uiapp = Revit.ActiveUIApplication
dbapp = Revit.ActiveDBApplication

# document
uidoc = Revit.ActiveUIDocument
doc = Revit.ActiveDBDocument
{% endhighlight %}

In C#:

{% highlight csharp %}
using UI = Autodesk.Revit.UI;
using DB = Autodesk.Revit.DB;
using AppServ = Autodesk.Revit.ApplicationServices;

using RhinoInside.Revit;

// application
UI.UIApplication uiapp = Revit.ActiveUIApplication;
AppServ.Application dbapp = Revit.ActiveDBApplication;

// document
UI.UIDocument uidoc = Revit.ActiveUIDocument;
DB.Document doc = Revit.ActiveDBDocument;
{% endhighlight %}



## Accessing Rhino Context

Using {% include rir_api_type.html version="1.0" title="Rhinoceros" topic="T_RhinoInside_Revit_Rhinoceros" %} static class you can gain access to the running instance of Rhino and the open Rhino document.

## Unit Conversion

{% highlight python %}
{% endhighlight %}

{% highlight csharp %}
{% endhighlight %}

## Geometry Conversion

### Revit Geometry to Rhino

Use static `To*` methods on {% include rir_api_type.html version="1.0" title="GeometryDecoder" topic="Methods_T_RhinoInside_Revit_Convert_Geometry_GeometryDecoder" %} to convert (decode) geometry and other relative types from Revit to Rhino. Let's take a look at how {% include rir_api_type.html version="1.0" title="ToBoundingBox" topic="M_RhinoInside_Revit_Convert_Geometry_GeometryDecoder_ToBoundingBox" %} can be used to convert a Revit bounding box to equivalent in Rhino.

Snippets below show how to use the converter method as an **extension** method in Python and C#:

{% highlight python %}
import clr
clr.AddReference("RhinoInside.Revit")
import RhinoInside.Revit.Convert.Geometry
clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
# ...
rhino_bbox = revit_bbox.ToBoundingBoxXYZ()
{% endhighlight %}

{% highlight csharp %}
using RhinoInside.Revit.Convert.Geometry;
// ...
var rhinoBBox = revitBBox.ToBoundingBoxXYZ()
{% endhighlight %}

Snippets below show how to use the converter method as an **static** method in Python and C#:

{% highlight python %}
import clr
clr.AddReference("RhinoInside.Revit")
import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as gd
# ...
rhino_bbox = gd.ToBoundingBoxXYZ(revit_bbox)
{% endhighlight %}

{% highlight csharp %}
using RhinoInside.Revit.Convert.Geometry;
// ...
var rhinoBBox = GeometryDecoder.ToBoundingBoxXYZ(revitBBox)
{% endhighlight %}


### Rhino Geometry to Revit

Use static `To*` methods on {% include rir_api_type.html version="1.0" title="GeometryEncoder" topic="Methods_T_RhinoInside_Revit_Convert_Geometry_GeometryEncoder" %} to convert (encode) geometry and other relative types from Rhino to Revit. Let's take a look at how {% include rir_api_type.html version="1.0" title="ToBoundingBoxXYZ" topic="M_RhinoInside_Revit_Convert_Geometry_GeometryEncoder_ToBoundingBoxXYZ" %} can be used to convert a Rhino bounding box to equivalent in Revit:

Snippets below show how to use the converter method as an **extension** method in Python and C#:

{% highlight python %}
import clr
clr.AddReference("RhinoInside.Revit")
import RhinoInside.Revit.Convert.Geometry
clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
# ...
revit_bbox = rhino_bbox.ToBoundingBoxXYZ()
{% endhighlight %}

{% highlight csharp %}
using RhinoInside.Revit.Convert.Geometry;
// ...
var revitBBox = rhinoBBox.ToBoundingBoxXYZ()
{% endhighlight %}

Snippets below show how to use the converter method as an **static** method in Python and C#:

{% highlight python %}
import clr
clr.AddReference("RhinoInside.Revit")
import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
# ...
revit_bbox = ge.ToBoundingBoxXYZ(rhino_bbox)
{% endhighlight %}

{% highlight csharp %}
using RhinoInside.Revit.Convert.Geometry;
// ...
var revitBBox = GeometryEncoder.ToBoundingBoxXYZ(rhinoBBox)
{% endhighlight %}
