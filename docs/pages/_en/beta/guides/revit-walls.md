---
title: Walls
order: 40
---

{% include ltr/en/wip_note.html %}

## Querying Wall Types

{% capture api_note %}
In Revit API, Wall Types are represented by {% include api_type.html type='Autodesk.Revit.DB.WallType' title='DB.WallType' %}. Walls have three main *System Families* that are represented by {% include api_type.html type='Autodesk.Revit.DB.WallKind' title='DB.WallKind' %} enumeration and could be determined by checking `DB.WallType.Kind`
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

{% include ltr/warning_note.html note='Note that Revit API will return the individual partial walls on a Stacked Wall when using this workflow' %}

### By Wall Kind

A better workflow is to collect walls based on they Wall Kind (System Family). Use a combination of components shared here to collect the walls by kind. Notice that the *Walls By Kind* component also returns the wall types of the given kind:

![]({{ "/static/images/guides/revit-walls03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Wall Kind.ghuser' name='Wall Kind' %}

{% include ltr/download_comp.html archive='/static/ghnodes/Walls By Kind.ghuser' name='Walls By Kind' %}

### By Wall Type

You can also collect walls of a specific type very easily using a workflow described in [Data Model: Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-instances.md %})

![]({{ "/static/images/guides/revit-walls04.png" | prepend: site.baseurl }})

## Analyzing Wall Types

### Reading Type Parameters

### Basic Wall Structure

<!-- https://github.com/mcneel/rhino.inside-revit/issues/42 -->

### Stacked Wall Structure

{% include ltr/warning_note.html note='Currently there is no support in Revit API to access Stacked Wall structure data' %}

## Analyzing Walls

### Reading Instance Parameters

### Common Wall Properties

### Wall Location Curve

<!-- https://github.com/mcneel/rhino.inside-revit/issues/90 -->

### Wall Profile

### Wall Geometry

{% capture api_note %}
explain challenges of getting geometry from standard approach
- no structure
- fails on curtain walls
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Wall Geometry By Structure

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

{% include ltr/warning_note.html note='Currently there is no support in Revit API to create new Stacked Wall types' %}

## Creating Walls

### By Base Curve

### By Profile

<!-- https://github.com/mcneel/rhino.inside-revit/issues/46 -->