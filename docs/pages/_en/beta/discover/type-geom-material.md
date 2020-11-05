---
title: Extract Window Geometry and Material by Sub-Categories
description: The first part of this definition, collects subcategories available on *Windows Category* in the active document
thumbnail: /static/images/discover/type-geom-material01.png
---


<!-- intro video -->
![]({{ "/static/images/discover/type-geom-material01.png" | prepend: site.baseurl }})


{% include ltr/download_pkg.html archive='/static/archives/type-geom-material.zip' %}


## Files
- **Window Geometry and Material.gh** Grasshopper definition of this example
- **Window Geometry and Material.rvt** Rhino model containing the windows

Open Revit model, launch {{ site.terms.rir }}, and then open the Grasshopper definition.

## Description

The first part of this definition, collects subcategories available on *Windows Category* in the active document. This subcategory is later used to extract the information about geometry on that specific subcategory:

![]({{ "/static/images/discover/type-geom-material02.png" | prepend: site.baseurl }})

Next all the window instances in the model are collected:

![]({{ "/static/images/discover/type-geom-material03.png" | prepend: site.baseurl }})

Next, using a custom python component, the geometry of each window instance is extracted into Revit API object, Rhino Brep, and the associated subcategory:

![]({{ "/static/images/discover/type-geom-material04.png" | prepend: site.baseurl }})

Next the extracted geometry is filtered for the subcategory selected in step one:

![]({{ "/static/images/discover/type-geom-material05.png" | prepend: site.baseurl }})

And finally, a python component is used to extract the materials info of each geometry piece:

![]({{ "/static/images/discover/type-geom-material06.png" | prepend: site.baseurl }})
