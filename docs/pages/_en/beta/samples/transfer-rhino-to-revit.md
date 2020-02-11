---
title: Transfer Rhino to Revit
published: false
---

{% include ltr/en/wip_note.html %}

This sample shows how to take normal Rhino breps, curves and points into Revit.

There are a lot of ways to bring in the Rhino geometry, but in this case we will be bringing them in as DirectShape elements.
<!-- ![Rhino to Revit as Directshape](/static/images/rhino-to-revit.jpg) -->

{% include youtube_player.html id="dP83XnJS6jQ" %}

## Bringing Breps into a Revit category
Rhino surfaces and brep solids can be imported into Revit as a categorized DirectShape.

Open Sample files:
1. Open the [Rhino to Revit.rvt]({{ "/static/samples/transfer-rhino-to-revit/rhino_to_revit.rvt " | prepend: site.baseurl }}) in Revit.
2. Start Rhino.Inside.Revit and open the [Rhino office.3dm]({{ "/static/samples/transfer-rhino-to-revit/rhino_office.3dm" | prepend: site.baseurl }}) file.
3. Start Grasshopper within Rhino.

The component necessary:
1. DirectShape Category picker
1. Directshape from Geometry
1. Brep Param component in Grasshopper

![Rhino Brep to Revit as Directshape]({{ "/static/images/rhino-to-revit-brep.jpg" | prepend: site.baseurl }})
Once you select the Breps, those will feed into the Direct component.

## Live-linking Points
Rhino curves can be linked/embedded into Revit and Grasshopper will keep them up to date.

The component necessary:
1. Curve from Geometry
1. Curve Param component in Grasshopper

![Rhino Curve to Revit]({{ "/static/images/rhino-to-revit-points.jpg" | prepend: site.baseurl }})
Once you select the curves, those will feed into Revit.

## Bringing in complex curves
Rhino points can be linked/embedded into Revit and Grasshopper will keep them up to date.

The component necessary:
1. Curve from Geometry
1. Curve Param component in Grasshopper

![Rhino curve to Revit]({{ "/static/images/rhino-to-revit-curves.jpg" | prepend: site.baseurl }})
Once you select the points, those will feed into Revit.
