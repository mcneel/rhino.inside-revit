---
title: Walls
order: 40
---

{% include ltr/en/wip_note.html %}

## Querying Wall Types

## Collecting Walls

### Collecting All Walls

<!-- mention stacked walls show up as multiple -->

### By Wall Kind

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### By Wall Type

## Wall Parameters

<!-- 
- create necessary enum components for param values like DB.WallFunction
- explain how to set these params and these components are helpers
 -->

## Analyzing Wall Types

### Basic Wall Structure

<!-- https://github.com/mcneel/rhino.inside-revit/issues/42 -->

### Stacked Wall Structure

{% include ltr/warning_note.html note='Currently there is no support in Revit API to access Stacked Wall structure data' %}

## Analyzing Wall Instances

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

### Modifying Basic Wall Structure

### Modifying Stacked Wall Structure

## Modify Wall Instances

### Modify Base Curve

### Modify Profile

## Creating New Wall Types

### Creating New Basic Wall Type

### Creating New Stacked Wall Type

{% include ltr/warning_note.html note='Currently there is no support in Revit API to create new Stacked Wall types' %}

## Creating Walls

### By Base Curve

### By Profile

<!-- https://github.com/mcneel/rhino.inside-revit/issues/46 -->