---
title: Walls (Basic & Stacked)
order: 40
group: Modeling
thumbnail: /static/images/guides/revit-walls.png
subtitle: Workflows for Revit Basic and Stacked Walls
ghdef: revit-walls.ghx
---

## Querying Wall Types

{% capture api_note %}
In Revit API, Wall Types are represented by {% include api_type.html type='Autodesk.Revit.DB.WallType' title='DB.WallType' %}. Walls have three main *System Families* that are represented by {% include api_type.html type='Autodesk.Revit.DB.WallKind' title='DB.WallKind' %} enumeration and could be determined by checking `DB.WallType.Kind`. In {{ site.terms.rir }}, the term *Wall System Family* is used instead for consistency.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use a combination of {% include ltr/comp.html uuid="d08f7ab1-" %} and {% include ltr/comp.html uuid="7b00f940-" %} components to collect all the wall types in a Revit model:

![]({{ "/static/images/guides/revit-walls-querywalltypes.png" | prepend: site.baseurl }})


## Querying Walls

{% capture api_note %}
In Revit API, Walls are represented by {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Querying All Walls

Use a combination of {% include ltr/comp.html uuid="d08f7ab1-" %} and {% include ltr/comp.html uuid="0f7da57e-" %} components to collect all the wall instances in a Revit model:

![]({{ "/static/images/guides/revit-walls-querywalls.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='Note that Revit API will return the individual partial walls on a *Stacked Wall* when using this workflow' %}

### By Wall System Family

A better workflow is to collect walls based on the *Wall System Family*. Use the {% include ltr/comp.html uuid="15545e80-" %} component to select any combination of the builtin **Basic**, **Stacked**, or **Curtain** walls in Revit. You can then pass this selection as the input to the {% include ltr/comp.html uuid="118f5744-" %} as shown below:

![]({{ "/static/images/guides/revit-walls-querybysystem.png" | prepend: site.baseurl }})

### By Wall Type

You can also collect walls of a specific type very easily using a workflow described in [Data Model: Elements & Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-elements.md %}#instances)

![]({{ "/static/images/guides/revit-walls-querywalltype.png" | prepend: site.baseurl }})


## Analyzing Wall Types

### Reading Type Parameters

Once you have filtered out the desired wall type using workflows described above, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element type.

### Analyzing Basic Walls

*Basic Walls* are a special *Wall System Family* in Revit. They are constructed from a set of layers that are defined as part of the wall type definition. The also have a series of other unique options e.g. **Wrapping at Inserts**. The {% include ltr/comp.html uuid="00a650ed-" %} component shown here provide a method to analyze the *Basic Wall* types in Revit document:

![]({{ "/static/images/guides/revit-walls-analyzebasictype.png" | prepend: site.baseurl }})

Some of the outputs on this component (e.g. **Wrapping at Inserts** and **Wrapping at Ends**) return an integer value that corresponds to an enumeration in the Revit API. You can use the {% include ltr/comp.html uuid='141f0da4' %} and {% include ltr/comp.html uuid='c84653dd' %} components (shown above in front of the parameter values panel) to determine which value is set on the parameter and filter the source wall types. The examples below show how this component are used to filter the wall types by *Wrapping* and *Function*:

![]({{ "/static/images/guides/revit-walls-analyzebasictype-filter.png" | prepend: site.baseurl }})

### Basic Wall Structure

{% capture api_note %}
In Revit API, {% include api_type.html type='Autodesk.Revit.DB.CompoundStructure' title='DB.CompoundStructure' %} type represents the structure definition of categories that allow such configuration e.g. *Basic Walls*, *Floors*, *Roofs*, *Compound Ceilings*, etc. The `DB.CompoundStructure` can provide access to individual layers represented by {% include api_type.html type='Autodesk.Revit.DB.CompoundStructureLayer' title='DB.CompoundStructureLayer' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The {% include ltr/comp.html uuid="00a650ed-" %} component shown above, provides access to the *Compound Structure* definition of the *Basic Wall* type. Use the {% include ltr/comp.html uuid="d0853b76-" %} component shown here to extract information on *Compound Structure Layers*. Similar to above, {% include ltr/comp.html uuid='55b31952' %} and {% include ltr/comp.html uuid='8d73d533' %} components are provided to allow value comparison and filtering of the structure layers:

![]({{ "/static/images/guides/revit-walls-compstruct.png" | prepend: site.baseurl }})

As shown above, layers are ordered from **Exterior** to **Interior**, matching the Revit GUI layer ordering. The example below shows a workflow to access individual layers by their index:

![]({{ "/static/images/guides/revit-walls-compstructlayer.png" | prepend: site.baseurl }})

### Basic Wall Structure Layers

Use the {% include ltr/comp.html uuid="bc64525a-" %} component to extract information about each individual *Compound Structure Layer*. {% include ltr/comp.html uuid='439ba763' %} and {% include ltr/comp.html uuid='db470316' %} components are also provide for value comparison:

![]({{ "/static/images/guides/revit-walls-analyzecompstructlayer.png" | prepend: site.baseurl }})

### Stacked Wall Structure

{% include ltr/warning_note.html image='/static/images/guides/revit-walls-stackedwallstruct.png' note='Currently there is no support in Revit API to access *Stacked Wall* structure data. However you can use the *Analyse Stacked Wall* component to extract the embedded *Basic Wall* instances and analyze their structure layers individually' %}

## Analyzing Walls

### Reading Instance Parameters

Once you have filtered out the desired wall instance using workflows described above, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element.

### Common Wall Properties

Use the {% include ltr/comp.html uuid="1169ceb6-" %} component shown here, to grab the common properties between all *Wall System Families*. {% include ltr/comp.html uuid='15545e80' %} and {% include ltr/comp.html uuid='1f3053c0' %} components are also provided for value comparison:

![]({{ "/static/images/guides/revit-walls-analyzewall.png" | prepend: site.baseurl }})

{% include ltr/api_note.html note="Slant Angle property is only supported on Revit >= 2021" %}

The example below uses the shared *Wall Structural Usage* value list component to filter for **Shear** walls:

![]({{ "/static/images/guides/revit-walls-analyzewall-filter.png" | prepend: site.baseurl }})

The **Orientation** output parameter is the wall orientation vector:

![]({{ "/static/images/guides/revit-walls-analyzewall-orient.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-analyzewall-orientvectors.png" | prepend: site.baseurl }})


### Wall Location Curve

{% capture api_note %}
In Revit API, *Location Line* of a *Basic* or *Stacked Wall* is represented by the {% include api_type.html type='Autodesk.Revit.DB.WallLocationLine' title='DB.WallLocationLine' %} enumeration and is stored in `DB.BuiltInParameter.WALL_KEY_REF_PARAM` parameter on the wall instance
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

*Basic* and *Stacked Walls* have a concept known as *Location Line*. The location line defines the vertical reference plane for the wall instance. The wall stays fixed on this vertical reference plane when it is flipped or its structure is modified. The {% include ltr/comp.html uuid="4c5260c3-" %} component shown here, can extract information about a wall location line. This component returns the center line curve, location line setting, curve, offset, and offset direction:

![]({{ "/static/images/guides/revit-walls-walllocation.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-walllocationlines.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid='a4eb9313' %} component is also provided to assist in filtering walls by the **Location Line** value:

![]({{ "/static/images/guides/revit-walls-walllocation-filter.png" | prepend: site.baseurl }})

If you only need the center line of the wall, an easier and more Grasshopper-like method is to pass the Wall elements to a Curve component:

![]({{ "/static/images/guides/revit-walls-convertcurve.png" | prepend: site.baseurl }})


### Wall Profile

Use the {% include ltr/comp.html uuid="9d2e9d8d-" %} component shown here to extract the profile curves for a *Basic* or *Stacked Wall* element. Note that these profile curves are extracted along the center plane of the wall:

![]({{ "/static/images/guides/revit-walls-profile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-profilelines.png" | prepend: site.baseurl }})


### Wall Geometry

You can use the {% include ltr/comp.html uuid="b3bcbf5b-" %} component to grab the basic geometry of a wall instance:

![]({{ "/static/images/guides/revit-walls-geometry.png" | prepend: site.baseurl }})

### Wall Geometry By Structure

{% capture api_note %}
Normally in Revit API, geometry of an element can be extracted using the `DB.Element.Geometry` property. In case of walls, the extracted geometry does not contain the structural layers of the wall. A [different method described here](https://thebuildingcoder.typepad.com/blog/2011/10/retrieving-detailed-wall-layer-geometry.html), has been used to extract the layer geometry. However this method adds some overhead to the definition runtime due to the temporary transactions that are needed
{% endcapture %}
{% include ltr/warning_note.html note=api_note %}

Use the {% include ltr/comp.html uuid="3dbaaae8-" %} component shown here to extract the layer geometry of a *Basic Wall* instance:

![]({{ "/static/images/guides/revit-walls-walllayers.png" | prepend: site.baseurl }})

This component can be used with *Stack Walls* as well. The component will extract the structure layers of all the partial *Basic Walls* that are part of the given *Stacked Wall*:

![]({{ "/static/images/guides/revit-walls-stackedwalllayers.png" | prepend: site.baseurl }})

A better method is to extract the *Basic Wall* instances first from the *Stacked Wall*, and then use the component to extract their layer geometry. This method would result in a more appropriate data structure that keeps the layer orders intact:

![]({{ "/static/images/guides/revit-walls-everybasicwall.png" | prepend: site.baseurl }})

To keeps the geometry list extracted from layers in identical order as other components that deal with layers, you can sort the geometry list by the distance from the wall orientation vector. This works best with flat walls of course but similar methods can be used to sort the layer geometry on other walls (think of basic walls stacked in a stacked wall instance with different structures):

![]({{ "/static/images/guides/revit-walls-layersinorder.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-walls-layersinorder.gif" | prepend: site.baseurl }})


## Modifying Wall Types

{% include ltr/en/wip_note.html %}

### Modifying Basic Wall Structure

{% include ltr/en/wip_note.html %}

### Modifying Stacked Wall Structure

{% include ltr/en/wip_note.html %}

## Modifying Walls

{% include ltr/en/wip_note.html %}

### Modifying Base Curve

{% include ltr/en/wip_note.html %}

### Modifying Profile

{% capture api_note %}
Modifying wall profile curves are not supported by the API at the moment
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Creating Wall Types

{% include ltr/en/wip_note.html %}

### Creating Basic Wall Type

{% include ltr/en/wip_note.html %}

### Creating Stacked Wall Type

{% include ltr/warning_note.html note='Currently there is no support in Revit API to create new *Stacked Wall* types' %}

## Creating Walls

### By Base Curve

Use the {% include ltr/comp.html uuid='37a8c46f' %} component to create a new wall based on the given curve. In this example the {% include ltr/comp.html uuid='ef607c2a' %} parameter is referencing a series of Revit model lines:

![]({{ "/static/images/guides/revit-walls-bycurve.png" | prepend: site.baseurl }})

### By Profile

Use the {% include ltr/comp.html uuid='78b02ae8' %} component to create a new wall based on the given profile curves. Note that the profile must be a closed loop, planar, and __vertical__. In this example we are using the **Join Curves** component from Grasshopper to join the curves. The {% include ltr/comp.html uuid='ef607c2a' %} parameter is referencing a series of Revit model lines that have modeled vertically and on a single plane so we know they are planar:

![]({{ "/static/images/guides/revit-walls-byprofile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-byprofilescap.png" | prepend: site.baseurl }})

<!-- https://github.com/mcneel/rhino.inside-revit/issues/46 -->