---
title: Update Door Marks
---

<!-- intro video -->
{% include youtube_player.html id="yDZ4y-ZbBkM" %}

{% include ltr/download_pkg.html archive='/static/samples/update-door-mark.zip' %}


## Files

- **RAC_basic_sample_project.rvt** Standard Revit Template file is used on this example
- **door-param-script.gh** Grasshopper definition for updating door Mark parameter

## Description

1. Open the standard Revit sample file *RAC_basic_sample_project.rvt*
2. Setting the view to the standard Revit 3d view helps see what is happening in this tutorial 
3. Start {{ site.terms.rir }} by pressing the the Rhino icon under Add-Ins
4. In the Rhinoceros Toolbar in Revit, select the Rhino Player icon
5. Open the **door-param-script.gh**

This definition will run immediately. Grasshopper will not show, and the player will start and finish. If you select one of the doors, you will see the Mark and Comment are now duplicated.

![]({{ "/static/images/samples/update-door-mark01.jpg" | prepend: site.baseurl }})

Now take a look at the definition by selecting the Grasshopper Icon in the Revit Rhinoceros toolbar and opening the **door-param-script.gh** definition.

![]({{ "/static/images/samples/update-door-mark02.jpg" | prepend: site.baseurl }})

The definition finds all the Doors setting a Category Filter and finding all the Elements of that Category. Then, using the *ParameterGet* Component each `Mark` is found for each door. Then the `Comments` parameter is set using the *ParameterSet* component.

