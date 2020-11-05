---
title: Creating Roof From Wall Edges
---

<!-- intro video -->
{% include youtube_player.html id="VsE5uWQ-_oM" %}

{% include ltr/download_pkg.html archive='/static/samples/select-revit-elements-as-input.zip' %}


## Files

- **Wall_Roof.rvt** Revit model containing the Walls
- **wall_roof.gh** Grasshopper definition for this sample

## Description

This sample shows how to Select Revit Element edges as input to a Grasshopper lofted surface. Using Revit objects as input to Grasshopper definitions allows for a dynamic editing directly in Revit and making Grasshopper interactive within the the Revit model.

### Selecting Revit Elements as Input
On the Params tool tab is a Revit group which contains Revit element pickers including Revit Elements, Edges, Vertices and Faces.  Also available are the non-model elements such as Category, Type, Family, Grid, Level and Material.

![]({{ "/static/images/samples/select-revit-elements-as-input01.jpg" | prepend: site.baseurl }}){: class="small-image"}

The components necessary:
1. Revit Edge params
1. Document Category picker
1. Curve Join components if multiple curves are selected
1. Curve Flip component to make sure the curves are going in the same direction.
1. Grasshopper Loft component to create the lofted surface
1. A Revit DirectShape component (Disabled by default)

### Live-linking Edges from Revit

To start the definition, right click on the top Edge component: 

![]({{ "/static/images/samples/select-revit-elements-as-input02.jpg" | prepend: site.baseurl }})

Then select one of the Yellow edges on one side of the the Wall model. Use the Finish button under the Revit toolbar to finish the selection.

![]({{ "/static/images/samples/select-revit-elements-as-input03.jpg" | prepend: site.baseurl }}){: class="small-image"}

Then right click and select to opposite edge. A preview loft surface should show up.

### Edit the Revit object with dynamic preview

Click on a wall in Revit twice and use the edit arrows to edit the shape of the walls. The lofted surface should update.

### Creating a Revit DirectShape Roof

By Enabling the DirectShape component in the Grasshopper canvas, Grasshopper will then create a Roof based on the lofted surface from Grasshopper. Of course with a little more work that roof could be a Roof from Boundary and create a Roof System family.