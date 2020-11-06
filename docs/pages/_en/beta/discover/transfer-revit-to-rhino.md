---
title: Transfer Revit to Rhino
description: This sample shows how to take Revit objects into Rhino
thumbnail: /static/images/discover/transfer-revit-to-rhino03.jpg
tags: ["convert"]
---

<!-- intro video -->
{% include youtube_player.html id="lnKTkVhjztY" %}

{% include ltr/download_pkg.html archive='/static/archives/transfer-revit-to-rhino.zip' %}


## Files

- **RAC_basic_sample_project.rvt** Standard Revit Template file is used on this example
- **Sample_Revit_to_Rhino.gh** Grasshopper definition for this example

## Description

This sample shows how to take Revit objects into Rhino

1. Open the standard Revit sample file **RAC_basic_sample_project.rvt**
2. Setting the view to the standard Revit 3d view helps see what is happening in this tutorial 
3. Start Rhino inside Revit by pressing the the Rhino icon under Add-Ins
4. In the Rhinoceros Toolbar in Revit, open Rhino and a new blank Rhino model
5. Start Grasshopper within Rhino.
6. Open the **Sample_Revit_to_Rhino.gh**

### Bringing across the model:

The Grasshopper definition is split up in a series of categories with a button to activate each one individually.  Please note that the first one you use may not show as the Rhino model probably is not zoomed into the correct area of the model by default.

Zoom into the top section of the definition where `Roofs` are going to be transferred.

![]({{ "/static/images/discover/transfer-revit-to-rhino01.jpg" | prepend: site.baseurl }})

Click on the `Button` to the right of the definition.  This will activate and import the Roof geometry on a Layer in Rhino called Roof. 
Click on the Zoom Extents icon in Rhino to find the Roof geometry.  This will set the view so you can see the rest of the transfers.
Zoom in the next section in Grasshopper definition on the`Walls` section.

![]({{ "/static/images/discover/transfer-revit-to-rhino02.jpg" | prepend: site.baseurl }})

Click on the `Button` to the right of the definition. This will bring in the walls.
Go to each successive section in Grasshopper to bring in the rest of the categories.
Set the view type in Rhino to Shaded

![]({{ "/static/images/discover/transfer-revit-to-rhino03.jpg" | prepend: site.baseurl }})
