---
title: "Revit: Elements & Instances"
subtitle: What is an Element in Revit
order: 21
group: Essentials
home: true
thumbnail: /static/images/guides/revit-elements.png
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

## Querying Instances by Level

The {% include ltr/comp.html uuid="b534489b-" %} allows you to filter project elements by Level.

![]({{ "/static/images/guides/revit-level-filters.png" | prepend: site.baseurl }})

## Querying Instances by Filters

Filter components will allow Grasshopper to select specific Revit elements thru many different properties. These filters can also be combined together to make sophisticated selections.

Normally Filters are created then sent into one of the many [query components]({{ "/guides/rir-grasshopper#revit-aware-components" | prepend: site.baseurl }}) in Rhino.

![]({{ "/static/images/guides/filter-basic.png" | prepend: site.baseurl }})

### Filter Element

This component takes a previous selection of Revit elements and can returns a True/False list of whether each element matches the filter. For example a set of preselected Revit elements in the {% include ltr/comp.html uuid='ef607c2a' %} can be filtered by using the {% include ltr/comp.html uuid='eb266925' %} input to the {% include ltr/comp.html uuid='36180a9e' %} to create the filter. The {% include ltr/comp.html uuid='36180a9e' %} component will return a list of True False that can be used to Cull Pattern the list.

![]({{ "/static/images/guides/filter-elements.png" | prepend: site.baseurl }})

### Exclusion Filter

The {% include ltr/comp.html uuid='396f7e91' %} works the same as above, but returns True when an element does not pass the filter test.

### Logical And Filter

Combine multiple filters together using the {% include ltr/comp.html uuid='0e534afb' %}. All elements must pass all filters.

![]({{ "/static/images/guides/filter-and.png" | prepend: site.baseurl }})

Note that more inputs can be added by zooming in on the component.

### Logical Or Filter

Combine multiple filters together using the {% include ltr/comp.html uuid='3804757f' %}. Elements pass by any one of the input filters.

### Category Filter

Filter all the objects in the selected category.

![]({{ "/static/images/guides/filter-category.png" | prepend: site.baseurl }})

### Class Filter

Use the [Revit API Class names](https://www.revitapidocs.com/2015/eb16114f-69ea-f4de-0d0d-f7388b105a16.htm) to select Elements in the project. The input can be the Element Classes selector or a string of the class name.

![]({{ "/static/images/guides/filter-class.png" | prepend: site.baseurl }})

### Types Filter

Include or exclude Revit elements based on Type name.

In this case the {% include ltr/comp.html uuid='eb266925' %} is an input for the {% include ltr/comp.html uuid='d3fb53d3' %} selector to create a filter with {% include ltr/comp.html uuid='4434c470' %}

![]({{ "/static/images/guides/filter-type.png" | prepend: site.baseurl }})

### Exclude Types Filter

{% include ltr/comp.html uuid='f69d485f' %} can be used to filter out types from a list. Combine thru an Logical And component to filter. This component is similiar to the API method *[WhereElementIsNotElementType](https://www.revitapidocs.com/2015/061cbbb9-26f1-a8f8-a4b2-3d7ff0105199.htm)*.

### Parameter Filter

{% include ltr/comp.html uuid='e6a1f501' %} is used to filter for values of specific parameter on elements. For all Parameter value comparisons, the [Filter Rules](#filter-rules) components should be used.

The list of Parameter names and types are quite long in Revit. Using the {% include ltr/comp.html uuid='c1d96f56' %} is a great way to select the proper parameter with it's additional listed detail in the selector.

![]({{ "/static/images/guides/parameter-rule-filter.png" | prepend: site.baseurl }})

### Bounding Box Filter

Filter used to match Revit elements by their geometric bounding box. The initial geometric object can be either from Rhino or Revit.

Input parameters:

* Bounding Box (Geometry) - World aligned bounding box to query.
* Union (Boolean) - Target union of bounding boxes.
* Strict (Boolean) - True means element should be strictly contained.
* Tolerance (Number) - Tolerance used to query.
* Inverted (Boolean) - True if the results of the filter should be inverted.

### Intersects Brep Filter

Filter used to match Revit elements that geometrically intersect a NURBS Brep.

### Intersects Element

Filter used to match Revit elements that geometrically intersect another Revit element.

### Intersects Mesh Filter

Filter used to match Revit elements that geometrically intersect a Rhino mesh.

### Design Options Filter

Filter used to match Revit elements that belong to a specifc design option.

### Level Filter

Filter for elements only on a specific level.  This component is best used with the {% include ltr/comp.html uuid='bd6a74f3' %}.

![]({{ "/static/images/guides/filter-level.png" | prepend: site.baseurl }})

### Owner View Filter

Filter for elements that belong to a specific view.  This component is best used with a selector that returns a view from the model.

![]({{ "/static/images/guides/filter-view.png" | prepend: site.baseurl }})

### Phase Status Filter

Filter used to match elements associated to the given Phase status. The Phase and the status can be found by right clicking on the inputs.

![]({{ "/static/images/guides/filter-phase.png" | prepend: site.baseurl }})

### Selectable in view filter

Filter used to match selectable elements into the given View

### Filter Rules

Filter Rules can be used with the {% include ltr/comp.html uuid='e6a1f501' %} to compare values.

This example shows using the value of one element to find all other elements in the model with that same parameter value:

![]({{ "/static/images/guides/parameter-rule-element.png" | prepend: site.baseurl }})

### Add Parameter Filter

Create a parameter based filter in the Revit model.  This will also be used as a parameter filter in Grasshopper.

### Add Selection Filter

Create a selection filter in Revit and then use that filter in the Grasshopper definition.

## Extracting Instance Geometry

{% include ltr/comp.html uuid='b3bcbf5b' %} used to extract the geometry of an instance. In the example below, the complete geometry of a *Stacked Wall* instance has been extracted. The {% include ltr/comp.html uuid='b078e48a' %} picker can be used to select the level of detail for geometry extraction:

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
