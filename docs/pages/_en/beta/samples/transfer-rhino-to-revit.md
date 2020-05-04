---
title: Transfer Rhino to Revit
---

<!-- intro video -->
{% include youtube_player.html id="7kNYSJ3kdqw" %}

{% include ltr/download_pkg.html archive='/static/samples/transfer-rhino-to-revit.zip' %}


## Files

- **rhino_to_revit.rvt** Revit model for the sample
- **rhino_office.3dm** Rhino model containing the source building model
- **rhino to revit.gh** Grasshopper definition for the sample

## Description

This sample shows how to take normal Rhino breps, curves and points into Revit. There are a lot of ways to bring in the Rhino geometry, but in this case we will be bringing them in as DirectShape elements.

### Bringing Breps into a Revit category
Rhino surfaces and brep solids can be imported into Revit as a categorized DirectShape.

The component necessary:
- DirectShape Category picker
- DirectShape from Geometry
- Brep Param component in Grasshopper

![]({{ "/static/images/samples/transfer-rhino-to-revit01.jpg" | prepend: site.baseurl }})
Once you select the Breps, those will feed into the Direct component.

### Live-linking Points
Rhino curves can be linked/embedded into Revit and Grasshopper will keep them up to date.

The component necessary:
- Curve from Geometry
- Curve Param component in Grasshopper

![]({{ "/static/images/samples/transfer-rhino-to-revit02.jpg" | prepend: site.baseurl }})
Once you select the curves, those will feed into Revit.

### Bringing in complex curves
Rhino points can be linked/embedded into Revit and Grasshopper will keep them up to date.

The component necessary:
- Curve from Geometry
- Curve Param component in Grasshopper

![]({{ "/static/images/samples/transfer-rhino-to-revit03.jpg" | prepend: site.baseurl }})
Once you select the points, those will feed into Revit.
