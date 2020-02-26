---
title: Walls
order: 40
---

{% include ltr/en/wip_note.html %}

## Querying Wall Types

## Querying Walls

### Querying All Walls

<!-- mention stacked walls show up as multiple -->

### By Wall Kind

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### By Wall Type

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