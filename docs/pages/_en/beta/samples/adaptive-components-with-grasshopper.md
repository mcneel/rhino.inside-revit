---
title: Adaptive Component with Grasshopper
---

{% include ltr/en/wip_note.html %}

This sample shows how to how to drive Revit&reg; adaptive components using Grasshopper&reg; and Rhino&reg;. The advantage to using this workflow is in the ability to use Rhino's goemteric ability to lay out a rational set of points on 
complex arbitrary geometry.  Grasshopper can also make descisions as condition across the form change. This example uses the Grasshopper [PanelTools plugin](https://www.food4rhino.com/app/panelingtools-rhino-and-grasshopper) 
to create and filter the facade grid.

{% include youtube_player.html id="etVbQGZ3myg" %}

## Setting up to use Adaptive comonents:

1. Setup a standard Revit Adaptive component.
1. Add the adaptive component to the revit project. 
1. Use the Grasshopper Add Adaptive Component to insert the points needed to drive each adaptive component in Revit.

![Select Type Adaptive  Definition]({{ "/static/images/samples/adaptive-component-type-selection.jpg" | prepend: site.baseurl }})

The input to the AddAdaptiveComponent is a datatree structure with each set of 4 points ordered correctly.

To select the adaptive component by Name combine the Model Category selector with the ElementTypeByName selector.

![Datatree input for Adaptive]({{ "/static/images/samples/adaptive-component-tree-set.jpg" | prepend: site.baseurl }})

### Some notes on using adaptive components:

The PanelingTools plugin in Grasshopper makes it easier to find points that make up each cell to insert an adaptive component. The Cellulate component in PanelingTools can order the points correctly.

![Datatree input for Adaptive]({{ "/static/images/samples/adaptive-component-cellulate.jpg" | prepend: site.baseurl }})

A good strategy for complex trimmed forms normally is to grid out the untrimmed version of the form in Rhino, then use the trimmed version of the form to fiter which gridpoints are only on the trimmed version of the surface. 
Using the Trim Grid component to trim away grid points that do not lie on the trimmed version of the surface.

![Trim Grid]({{ "/static/images/samples/adaptive-component-trim-grid.jpg" | prepend: site.baseurl }})

Note that the internal parameters in the adaptive component can be driven by grasshopper also by setting the parameters on the component instance.

![Adaptive Parameter]({{ "/static/images/samples/adaptive-component-parameter.jpg" | prepend: site.baseurl }})
