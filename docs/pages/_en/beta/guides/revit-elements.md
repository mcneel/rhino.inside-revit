---
title: "Revit: Elements & Instances"
subtitle: What is an Element in Revit
order: 21
thumbnail: /static/images/guides/revit-elements.png
group: Essentials
---

Elements are the basic building blocks in Revit data model. Elements are organized into Categories. The list of categories is built into each Revit version and can not be changed. Elements have [Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) that hold data associated with the Element. Depending on their category, Elements will get a series of built-in parameters and can also accept custom parameters defined by user. Elements might have geometry e.g. Walls (3D) or Detail Components (2D), or might not include any geometry at all e.g. *Project Information* (Yes even that is an Element in Revit data model, although it is not selectable since Revit views are designed around geometric elements, therefore Revit provides a custom window to edit the project information). Elements have [Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) that define how the element behaves in the Revit model.

{% capture api_note %}
In Revit API, Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} and each element parameter is represented by {% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}. The {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} has multiple methods to provide access to its collection of properties

&nbsp;

Each element has an Id (`DB.Element.Id`) that is an integer value. However this Id is not stable across upgrades and workset operations such as *Save To Central*, and might change. It is generally safer to access elements by their Unique Id (`DB.Element.UniqueId`) especially if you intend to save a reference to an element outside the Revit model e.g. an external database. Note that although the `DB.Element.UniqueId` looks like a UUID number, it is not. Keep that in mind if you are sending this information to your external databases.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

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