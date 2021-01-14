---
title: "Revit: Elements & Instances"
subtitle: What is an Element in Revit
order: 21
thumbnail: /static/images/guides/revit-elements.png
group: Essentials
---

## Referencing Elements

There are more elaborate ways to collect various elements in Revit. This section shows how you can manually reference a specific element and bring that into your Grasshopper definition. Later sections discuss the generic ways of collecting elements of various types.

### By Selection

Use the context menu on the {% include ltr/comp.html uuid='ef607c2a' %} parameter to reference geometrical Revit elements in your definition:

![]({{ "/static/images/guides/revit-elements-select.gif" | prepend: site.baseurl }})


### By Element Id

You can use the {% include ltr/comp.html uuid='f3ea4a9c' %} parameter and add the element Ids into the *Manage Revit Element Collection* on the component context menu:

![]({{ "/static/images/guides/revit-elements-byid.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-byid.gif" | prepend: site.baseurl }})

## Instances

*Instances* are individual graphical/geometric elements placed in a Revit model e.g. a single Wall, or a single Door, or any other single element. As a subset of Revit Elements, Instances inherit a series of *Parameters* from their *Category* and *Type* and might have instance parameters as well that only belongs to that single instance.

## Querying Instance by Type

Use a combination of {% include ltr/comp.html uuid='eb266925' %} component, connected to {% include ltr/comp.html uuid="d3fb53d3-9" %}, {% include ltr/comp.html uuid='4434c470' %}, and {% include ltr/comp.html uuid='0f7da57e' %} to query all the instances of a specific type. The example below is collecting all the instance of a *Basic Wall* type:

![]({{ "/static/images/guides/revit-elements-querybytype.png" | prepend: site.baseurl }})

## Querying Instances by Parameter

You can use the {% include ltr/comp.html uuid='e6a1f501' %} component in combination with a *Filter Rule* (e.g. {% include ltr/comp.html uuid='05bbaedd' %} or {% include ltr/comp.html uuid='0f9139ac' %}) to filter elements by their parameter values.

![]({{ "/static/images/guides/revit-elements-querybyparam.png" | prepend: site.baseurl }})


## Extracting Instance Geometry

The {% include ltr/comp.html uuid='b3bcbf5b' %} component can be used to extract the geometry of an instance. In the example below, the complete geometry of a *Stacked Wall* instance has been extracted. The {% include ltr/comp.html uuid='b078e48a' %} picker can be used to select the level of detail for geometry extraction:

![]({{ "/static/images/guides/revit-elements-getgeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getgeomscap.png" | prepend: site.baseurl }})

### Instance Base Curve

For elements that are constructed on a base curve (e.g. Basic Walls) you can use the {% include ltr/comp.html uuid='dcc82eca' %} to get and set the base curve.


![]({{ "/static/images/guides/revit-elements-basecurve.png" | prepend: site.baseurl }})

### Instance Bounding Box

You can pass an instance into a Grasshopper *Box* component to extract the bounding box of the geometry very easily:

![]({{ "/static/images/guides/revit-elements-getbbox.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getbboxscap.png" | prepend: site.baseurl }})


### Instance Bounding Geometry

{% include ltr/warning_note.html note='Currently, Bounding Geometry component only works with Walls but will be extended to work with other Revit categories in the future.' %}

Sometimes it is necessary to extract the *Bounding Geometry* of an instance. *Bounding Geometry* is a geometry that wraps the instance geometry as close as possible and generally follows the instance geometry topology. You can use the {% include ltr/comp.html uuid='3396dbc4' %} component to extract this geometry. In the example below, the bounding geometry of a *Stacked Wall* is extracted. Notice that the bounding geometry is as thick as the thickest part of the *Stacked Wall*:

![]({{ "/static/images/guides/revit-elements-getboundinggeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getboundinggeomscap.png" | prepend: site.baseurl }})

## Changing Instance Type

Use the {% include ltr/comp.html uuid='fe427d04' %} component to both query the *Type* of an instance, and to change it to another type.

![]({{ "/static/images/guides/revit-elements-gettype.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-changetype.png" | prepend: site.baseurl }})

## Placing Instances of Types

Use the {% include ltr/comp.html uuid='0c642d7d' %} component to place an instance of a *Type* into the Revit model space.

![]({{ "/static/images/guides/revit-elements-placeinst.png" | prepend: site.baseurl }})

For types that require a host, you can pass a host element to the {% include ltr/comp.html uuid='0c642d7d' %} component as well.

![]({{ "/static/images/guides/revit-elements-placeinstonhost.png" | prepend: site.baseurl }})

The component, places the given type on the nearest location along the host element. In the image below, the green sphere is the actual location that is passed to the component. Notice that the door is placed on the closest point on the wall.

![]({{ "/static/images/guides/revit-elements-placeinstonhostscap.png" | prepend: site.baseurl }})
