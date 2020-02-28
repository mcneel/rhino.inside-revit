---
title: Getting User Input using Human UI
---

<!-- intro video -->
{% include youtube_player.html id="yDZ4y-ZbBkM" %}


{% include ltr/download_pkg.html archive='/static/samples/getting-input-human-ui.zip' %}


## Files

- **RAC_basic_sample_project.rvt** Standard Revit Template file is used on this example
- **write-sheet-param-humanui.gh** Grasshopper definition for this example

## Description

The example here requires the [HumanUI plugin for Grasshopper](https://www.food4rhino.com/app/human-ui). This is a plugin allowing Grasshopper to design and display dialog box interfaces. In this case the dialog interface will help edit the *Drawn by* and the *Checked by* sections of the title block.

1. Open the *A001 - Title Sheet* * in the standard *RAC_basic_sample_project.rvt*.
2. In the Rhinoceros Toolbar in Revit, select the Rhino Player icon
3. Open the **write-sheet-param-humanui.gh**

This definition will run the *Human UI* dialog immediately without showing Grasshopper.

![]({{ "/static/images/samples/user-input-humanui01.jpg" | prepend: site.baseurl }}){: class="small-image"}

Simply edit the Text filed and click on the update buttons to the right. The Titleblock will change. To see how the definition works, just open the Grasshopper canvas and open the script.

