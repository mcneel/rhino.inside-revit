---
title: Create and Place Column Family Along Curve
description: This sample shows how to create a new Revit family that contains geometry created in Rhino and Grasshopper
thumbnail: /static/images/discover/column-family-curve01.jpg
---

<!-- intro video -->
![]({{ "/static/images/discover/column-family-curve01.jpg" | prepend: site.baseurl }})


{% include ltr/download_pkg.html archive='/static/archives/column-family-along-curve.zip' %}


## Files

- **Column Family Along Curve.3dm** Rhino model containing the column Brep geometry and the insertion points along the curve
- **Column Family Along Curve.gh** Grasshopper definition of this example

Open Sample files:

1. Open a new empty project in Revit
2. Start {{ site.terms.rir }}
3. Click on Rhino button and open the **Column Family Along Curve.3dm** file
4. Click on Grasshopper button and open the **Column Family Along Curve.gh**
5. Switch to a 3D view in the Revit model. The column should populate the points along the curve in both Rhino and Revit

## Description

This sample shows how to create a new Revit family that contains geometry created in Rhino/Grasshopper. Specifically, a column is created in Grasshopper, a family is generated for this geometry and instances of the family are placed along a curve in Revit.

There are multiple ways to bring in the Rhino geometry, but in this case we need to bring them in as Revit family instances. Geometry that is wrapped within family, has 3 advantages:

1. The graphic properties can be edited including the hatching and section line. This is a big advantage over `DirectShapes` elements in Revit. They are less flexible.
2. The geometry can be edited directly in the Revit Family Editor. Any change to to the family will be reflected across all the instances.
3. The family instances can be very easily scheduled.


### Bringing Breps into a Revit Family

The first portion of this example, brings in the column Brep geometry from Rhino. Since Revit families are expected to have their geometry located at origin point, the part of the definition also moves the Brep geometry to the world origin (0,0,0 coordinates).

![]({{ "/static/images/discover/column-family-curve02.png" | prepend: site.baseurl }})

### Creating Revit Family

The modified Brep geometry is then passed to the *Family.New* component alongside other inputs required to create a new Revit family.

![]({{ "/static/images/discover/column-family-curve03.png" | prepend: site.baseurl }})


### Placing Family Instances

In this step, instances of the newly created family are places along the curve points. To insert a family instance, we use the *AddFamilyInstance.ByLocation* component. The *Family.GetTypes* component is used to get the first (and default) family type from the newly created family.

![]({{ "/static/images/discover/column-family-curve04.png" | prepend: site.baseurl }})

As it is shown in the definition, the points on the curve are passed to the *AddFamilyInstance.ByLocation* component, however, to get the correct orientation for the column, the curve is evaluated at each point to create a rotated plane matching the curvature of the curve at the point.
