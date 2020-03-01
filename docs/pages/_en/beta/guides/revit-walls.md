---
title: Walls
order: 40
---

{% include ltr/en/wip_note.html %}

## Querying Wall Types

{% capture api_note %}
In Revit API, Wall Types are represented by {% include api_type.html type='Autodesk.Revit.DB.WallType' title='DB.WallType' %}. Walls have three main *System Families* that are represented by {% include api_type.html type='Autodesk.Revit.DB.WallKind' title='DB.WallKind' %} enumeration and could be determined by checking `DB.WallType.Kind`. In {{ site.terms.rir }}, the term *Wall System Family* is used instead for consistency.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use a combination of *Element.CategoryFilter* and *Document.ElementTypes* components to collect all the wall types in a Revit model:

![]({{ "/static/images/guides/revit-walls01.png" | prepend: site.baseurl }})


## Querying Walls

{% capture api_note %}
In Revit API, Walls are represented by {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Querying All Walls

Use a combination of *Element.CategoryFilter* and *Document.Elements* components to collect all the wall instances in a Revit model:

![]({{ "/static/images/guides/revit-walls02.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='Note that Revit API will return the individual partial walls on a *Stacked Wall* when using this workflow' %}

### By Wall System Family

A better workflow is to collect walls based on they *Wall System Family*. Use a combination of components shared here to collect the walls by their *System Family*. Notice that the *Walls By System Family* component also returns the wall types of the given *System Family*:

![]({{ "/static/images/guides/revit-walls03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Wall System Family.ghuser' name='Wall System Family' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Walls By System Family.ghuser' name='Walls By System Family' %}

### By Wall Type

You can also collect walls of a specific type very easily using a workflow described in [Data Model: Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-instances.md %})

![]({{ "/static/images/guides/revit-walls04.png" | prepend: site.baseurl }})


## Analyzing Wall Types

### Reading Type Parameters

Once you have filtered out the desired wall type using workflows described above, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element type.

### Analyzing Basic Walls

*Basic Walls* are a special *Wall System Family* in Revit. They are constructed from a set of layers that are defined as part of the wall type definition. The also have a series of other unique options e.g. *Wrapping at Inserts*. The *Analyse Basic Wall Type* component shared here provide a method to analyze the *Basic Wall* types in Revit document:

![]({{ "/static/images/guides/revit-walls05.png" | prepend: site.baseurl }})

Some of the outputs on this component (e.g. **WI** and **WE**) return an integer value that corresponds to an enumeration in the Revit API. You can use the *Value List* components (shown above in front of the parameter values panel) to determine which value is set on the parameter and filter the source wall types. The examples below show how these *Value List* components are used to filter the wall types by *Wrapping* and *Function*:

![]({{ "/static/images/guides/revit-walls06.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls07.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Basic Wall Type.ghuser' name='Analyse Basic Wall Type'%}

{% include ltr/download_comp.html archive='/static/ghnodes/Wall Wrapping.ghuser' name='Wall Wrapping'%}

{% include ltr/download_comp.html archive='/static/ghnodes/Wall Function.ghuser' name='Wall Function'%}

### Basic Wall Structure

{% capture api_note %}
In Revit API, {% include api_type.html type='Autodesk.Revit.DB.CompoundStructure' title='DB.CompoundStructure' %} type represents the structure definition of categories that allow such configuration e.g. *Basic Walls*, *Floors*, *Roofs*, *Compound Ceilings*, etc. The `DB.CompoundStructure` can provide access to individual layers represented by {% include api_type.html type='Autodesk.Revit.DB.CompoundStructureLayer' title='DB.CompoundStructureLayer' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The *Analyse Basic Wall Type* component shared above, provides access to the *Compound Structure* definition of the *Basic Wall* type. Use the *Compound Structure (Destruct)* component shared here to extract information and *Compound Structure Layers*. Similar to above, a series of Value List components are provided to allow value comparison and filtering of the structure layers:

![]({{ "/static/images/guides/revit-walls08.png" | prepend: site.baseurl }})

As shown above, layers are ordered from **Exterior** to **Interior**, matching the Revit GUI layer ordering. The example below shows a workflow to access individual layers by their index:

![]({{ "/static/images/guides/revit-walls09.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Compound Structure (Destruct).ghuser' name='Compound Structure (Destruct)' %}

{% include ltr/download_comp.html archive='/static/ghnodes/End Cap Condition.ghuser' name='End Cap Condition' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Opening Wrapping Condition.ghuser' name='Opening Wrapping Condition' %}

### Basic Wall Structure Layers

Use the Compound Structure Layer (Destruct) component to extract information about each individual *Compound Structure Layer*. Custom *Value List* components are also provide for value comparison:

![]({{ "/static/images/guides/revit-walls10.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Compound Structure Layer (Destruct).ghuser' name='Compound Structure Layer (Destruct)' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Layer Function.ghuser' name='Layer Function' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Deck Embedding Type.ghuser' name='Deck Embedding Type' %}

### Stacked Wall Structure

{% include ltr/warning_note.html note='Currently there is no support in Revit API to access *Stacked Wall* structure data. However you can use the Stack Wall instance analysis components to extract the partial height *Basic Wall* instances and analyze their structure layers individually' %}


## Analyzing Walls

### Reading Instance Parameters

Once you have filtered out the desired wall instance using workflows described above, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element.

### Common Wall Properties

Use the *Analyse Wall* component shared here, to grab the common properties between all *Wall System Families*. Custom *Value List* components are also provided for value comparison:

![]({{ "/static/images/guides/revit-walls11.png" | prepend: site.baseurl }})

The example below uses the shared *Wall Structural Usage* value list component to filter for **Shear** walls:

![]({{ "/static/images/guides/revit-walls12.png" | prepend: site.baseurl }})

The **OV** output parameter is the wall orientation vector:

![]({{ "/static/images/guides/revit-walls12a.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls12b.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Wall.ghuser' name='Analyse Wall' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Wall Structural Usage.ghuser' name='Wall Structural Usage' %}

### Wall Location Curve

{% capture api_note %}
In Revit API, *Location Line* of a *Basic* or *Stacked Wall* is represented by the {% include api_type.html type='Autodesk.Revit.DB.WallLocationLine' title='DB.WallLocationLine' %} enumeration and is stored in `DB.BuiltInParameter.WALL_KEY_REF_PARAM` parameter on the wall instance
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

*Basic* and *Stacked Walls* have a concept known as Location line. The location line defines the reference axis for the wall instance. The wall stays fixed on this reference axis when it is flipped or its structure is modified. The *Analyse Wall Location* component shared here, can extract information about a wall location line. This component returns the center line curve, location line setting, curve, offset, and offset direction:

![]({{ "/static/images/guides/revit-walls13.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls13a.png" | prepend: site.baseurl }})

A custom Value List component is also provided to assist in filtering walls by the Location Line value:

![]({{ "/static/images/guides/revit-walls14.png" | prepend: site.baseurl }})

If you only need the center line of the wall, an easier and more Grasshopper-like method is to pass the Wall elements to a Curve component:

![]({{ "/static/images/guides/revit-walls14a.png" | prepend: site.baseurl }})


&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Wall Location.ghuser' name='Analyse Wall Location' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Wall Location Line.ghuser' name='Wall Location Line' %}

### Wall Profile

Use the *Extract Wall Profile* component shared here to extract the profile curves for a *Basic* or *Stacked Wall* element. Note that these profile curves are extracted along the center plane of the wall:

![]({{ "/static/images/guides/revit-walls15.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls16.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Extract Wall Profile.ghuser' name='Extract Wall Profile' %}

### Wall Geometry

You can use the Element.Geometry component to grab the basic geometry of a wall instance:

![]({{ "/static/images/guides/revit-walls17.png" | prepend: site.baseurl }})

### Wall Geometry By Structure

{% capture api_note %}
Normally in Revit API, geometry of an element can be extracted using the `DB.Element.Geometry` property. In case of walls, the extracted geometry does not contain the structural layers of the wall. A [different method described here](https://thebuildingcoder.typepad.com/blog/2011/10/retrieving-detailed-wall-layer-geometry.html), has been used to extract the layer geometry. However this method adds some overhead to the definition runtime due to the temporary transactions that are needed
{% endcapture %}
{% include ltr/warning_note.html note=api_note %}

Use the *Extract Compound Structure* Geometry component shared here to extract the layer geometry of a *Basic Wall* instance:

![]({{ "/static/images/guides/revit-walls17a.png" | prepend: site.baseurl }})

This component can be used with *Stack Walls* as well. The component will extract the structure layers of all the partial *Basic Walls* that are part of the given *Stacked Wall*:

![]({{ "/static/images/guides/revit-walls18.png" | prepend: site.baseurl }})

A better method is to extract the *Basic Wall* instances first from the *Stacked Wall*, and then use the component to extract their layer geometry. This method would result in a more appropriate data structure that keeps the layer orders intact:

![]({{ "/static/images/guides/revit-walls19.png" | prepend: site.baseurl }})

Moreover, this component keeps the layers in identical order as other components that deal with layers so you can work on the layer data and geometry easily later on:

![]({{ "/static/images/guides/revit-walls20.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-walls20a.gif" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Extract Compound Structure Geometry.ghuser' name='Extract Compound Structure Geometry' %}


## Modifying Wall Types

### Modifying Type Parameters

### Modifying Basic Wall Structure

### Modifying Stacked Wall Structure

## Modifying Walls

### Modifying Instance Parameters

### Modifying Base Curve

### Modifying Profile


## Creating Wall Types

### Creating Basic Wall Type

### Creating Stacked Wall Type

{% include ltr/warning_note.html note='Currently there is no support in Revit API to create new *Stacked Wall* types' %}

## Creating Walls

### By Base Curve

### By Profile

<!-- https://github.com/mcneel/rhino.inside-revit/issues/46 -->