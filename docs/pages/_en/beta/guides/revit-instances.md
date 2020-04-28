---
title: "Data Model: Instances"
order: 33
---

*Instances* are individual elements places in a Revit model e.g. a single Wall, or a single Door, or any other single element. Instances have *Categories* and might also have *Type* e.g. each Door has a Door type. Instances inherit a series of *Parameters* from their *Category* and *Type* and might have instance parameters as well that only belongs to that single instance.

## Querying Instance of a Type

Use a combination of category component, connected to {% include ltr/comp.html uuid="d3fb53d3-9" %} and *Element.TypeFilter*, to query all the instances of a specific type. The example below is collecting all the instance of the **My Basic Wall** type:

![]({{ "/static/images/guides/revit-instances01.png" | prepend: site.baseurl }})

## Filtering Instances by Property

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

## Modifying Instances

Once you have filtered out the desired instance, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element.

## Changing Instance Type

{% include ltr/en/wip_note.html %}