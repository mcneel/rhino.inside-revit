---
title: Creating Wall in Revit
description: This sample shows how to take normal Rhino curve and create a set of Revit system family walls
thumbnail: /static/images/discover/creating-walls-in-revit01.jpg
---

<!-- banner image -->
![]({{ "/static/images/discover/creating-walls-in-revit01.jpg" | prepend: site.baseurl }})

{% include ltr/download_pkg.html archive='/static/archives/creating-walls-in-revit.zip' %}

## Files

- **walls_tutorial.rvt** Revit model that contains the Wall types
- **wall_model.3dm** Rhino model with wall base curves
- **create_revit_walls.gh** Grasshopper definition for this example

## Description

This sample shows how to take normal Rhino curve and create a set of Revit system family walls. This demonstration is meant to show that true native Revit objects can be created from simple Rhino geometry. Editing the curve in Rhino will update the walls in Revit.

1. Add Wall by Curve component
1. Curve Param component from Grasshopper
1. Curve Split component from Grasshopper
1. Revit Element component
1. Element Decompose component
1. Level Input Selector
1. Slider for Wall Height

![]({{ "/static/images/discover/creating-walls-in-revit02.png" | prepend: site.baseurl }})

After selecting the curve(s) in Rhino and the typical Wall in Revit for the wall family type, Grasshopper will generate the system family wall types in Revit.
