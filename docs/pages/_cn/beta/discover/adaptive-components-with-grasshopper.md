---
title: Create Building Shell Using Adaptive Components
---

<!-- intro video -->
{% include youtube_player.html id="f3s84WP1MhI" %}

{% include ltr/download_pkg.html archive='/static/archives/adaptive-components-with-grasshopper.zip' %}

## Files

- **Building Shell.rvt** Revit model containing the adaptive component family
- **Adaptive Paneling.gh** Grasshopper definition that creates the 

Open Revit model first, and then open the Grasshopper definition. The building shell should be automatically generated using the existing adaptive component family:

![]({{ "/static/images/discover/adaptive-component-final.png" | prepend: site.baseurl }})


## Description

This sample shows how to how to drive {{ site.terms.revit }} adaptive components using Grasshopper and Rhino. The advantage to using this workflow is in the ability to use Rhino's geometric ability to lay out a rational set of points on complex arbitrary geometry. Grasshopper can also make decisions as condition across the form change. This example uses the Grasshopper [PANELING TOOLS plugin](https://www.food4rhino.com/app/panelingtools-rhino-and-grasshopper) to create and filter the facade grid.

###  Setting Up to Use Adaptive Components

The Grasshopper definition is grabbing the existing adaptive component type (Category: *Generic Models* Family: *Frame-Panel* Type: *Frame-Panel*) and passes that to the *AddAdaptiveComponent.ByPoints* component:

![]({{ "/static/images/discover/adaptive-component-type-selection.jpg" | prepend: site.baseurl }})

The input to the *AddAdaptiveComponent.ByPoints* is a data-tree structure where each branch contains a set of 4 points, ordered correctly:

![]({{ "/static/images/discover/adaptive-component-tree-set.jpg" | prepend: site.baseurl }}){:height="35%" width="35%"}

### Using Adaptive Components

The [PANELING TOOLS plugin](https://www.food4rhino.com/app/panelingtools-rhino-and-grasshopper) in Grasshopper makes it easier to find points that make up each cell to insert an adaptive component. The *Cellulate* component in *PanelingTools* can order the points correctly.

![]({{ "/static/images/discover/adaptive-component-cellulate.jpg" | prepend: site.baseurl }}){: class="small-image"}

A good strategy for complex trimmed forms normally is to grid out the untrimmed version of the form in Rhino, then use the trimmed version of the form to filter which grid-points are only on the trimmed version of the surface. Use the *Trim Grid* component to trim away grid points that do not lie on the trimmed version of the surface:

![]({{ "/static/images/discover/adaptive-component-trim-grid.jpg" | prepend: site.baseurl }})

Note that the internal parameters in the adaptive component can be driven by Grasshopper also by setting the parameters on the component instance:

![]({{ "/static/images/discover/adaptive-component-parameter.jpg" | prepend: site.baseurl }}){: class="small-image"}
