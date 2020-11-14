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

Use a combination of category component, connected to {% include ltr/comp.html uuid="d3fb53d3-9" %} and *Element.TypeFilter*, to query all the instances of a specific type. The example below is collecting all the instance of the **My Basic Wall** type:

![]({{ "/static/images/guides/revit-instances01.png" | prepend: site.baseurl }})

## Querying Instances by Property

{% include ltr/en/wip_note.html %}

## Extracting Instance Geometry

The *Element Geometry* component can be used to extract the geometry of an instance. In the example below, the complete geometry of a *Stacked Wall* instance has been extracted. The *Level Of Detail* value picker can be used to select the level of detail for geometry extraction:

![]({{ "/static/images/guides/revit-instances02.png" | prepend: site.baseurl }})

### Instance Bounding Box

You can pass an instance into a *Box* component to extract the bounding box of the geometry very easily:

![]({{ "/static/images/guides/revit-instances03.png" | prepend: site.baseurl }})

### Instance Bounding Geometry

{% include ltr/warning_note.html note='Currently, Bounding Geometry component only works with Walls but will be extended to work with other Revit categories in the future.' %}

Sometimes it is necessary to extract the *Bounding Geometry* of an instance. *Bounding Geometry* is a geometry that wraps the instance geometry as close as possible and generally follows the instance geometry topology. You can use the *Extract Bounding Geometry* component to extract this geometry. In the example below, the bounding geometry of a *Stacked Wall* is extracted. Notice that the bounding geometry is as thick as the thickest part of the *Stacked Wall*:

![]({{ "/static/images/guides/revit-instances04.png" | prepend: site.baseurl }})

## Changing Instance Type

{% include ltr/en/wip_note.html %}

## Placing Instances of Types

Use the *AddFamilyInstance.ByLocation* component (under *Revit > Build* panel) to place an instance of a type into the Revit model space.

![]({{ "/static/images/guides/revit-families09a.png" | prepend: site.baseurl }})

For types that require a host, you can pass a host element to the *AddFamilyInstance.ByLocation* component as well.

![]({{ "/static/images/guides/revit-families09b.png" | prepend: site.baseurl }})

The component, places the given type on the nearest location along the host element. In the image below, the green sphere is the actual location that is passed to the component. Notice that the door is placed on the closest point on the wall.

![]({{ "/static/images/guides/revit-families09c.png" | prepend: site.baseurl }})
