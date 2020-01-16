---
title: Creating Wall in Revit
order: 4
---

{% include ltr/en/wip_note.html %}

This sample shows how to take normal Rhino curve and create a set of Revit system family walls

This demonstration is meant to show that true native Revit objects can be created from simple Rhino geometry.  Editing the curve in Rhino will update the walls in Revit.

![Creating system family walls in Revit]({{ site.baseurl }}/static/images/create-walls-in-revit.jpg)


## Open Sample files
1. Open the [Walls Tutorial.rvt]({{ site.baseurl }}/static/samples/creating_walls_in_revit/walls_tutorial.rvt) in Revit.
1. Start Rhino.Inside.Revit and open the [Wall Model.3dm]({ site.baseurl }}/static/samples/creating_walls_in_revit/wall_model.3dm) file.
1. Start Grasshopper within Rhino.

## The component necessary
1. Add Wall by Curve component
1. Curve Param component from Grasshopper
1. Curve Split component from Grasshopper
1. Revit Element component
1. Element Decompose component
1. Level Input Selector
1. Slider for Wall Height

![Create Revit walls as system Families]({{ site.baseurl }}/static/images/create-walls-grasshopper-canvas.png)
After selecting the curve(s) in Rhino and the typical Wall in Revit for the wall family type, Grasshopper will generate the system family wall types  in Revit.
