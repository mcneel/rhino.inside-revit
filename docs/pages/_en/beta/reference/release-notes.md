---
title: Release Notes
order: 40
---

<!-- most recent release should be on top -->

{% include ltr/release-header.html version="0.0.7341.20715" time="02/06/2020 11:30:30" %}

- Added **Structural Usage** input parameter to *Wall.ByCurve* component

  ![]({{ "/static/images/release_notes/79999999_01.png" | prepend: site.baseurl }}){: class="small-image"}

- Added **Principal Parameter** menu option to parameters
  
  ![]({{ "/static/images/release_notes/79999999_02.png" | prepend: site.baseurl }}){: class="small-image"}

- [Fixed Issue #131](https://github.com/mcneel/rhino.inside-revit/issues/131)
- [Fixed Issue #123](https://github.com/mcneel/rhino.inside-revit/issues/123)

{% include ltr/release-header.html version="0.0.7333.32251" time="1/29/2020 17:55:02 AM" %}

- Added HiDPI images for Grasshopper toolbar buttons
- Updated RhinoCommon dependency to `7.0.20028.12435-wip`
- [Resolved Issue #120](https://github.com/mcneel/rhino.inside-revit/issues/120): Grasshopper updates, somehow mess up the Project Browser configurations

{% include ltr/release-header.html version="0.0.7325.6343" time="1/21/2020 03:32:26 AM" %}

- Grasshopper and Rhino shortcuts now work inside Revit (Rhino v7.0.20021.12255, 01/21/2020)
- Fixed a bug when there is no ActiveDocument in Revit
- Fixed bug converting ellipses from Revit to Rhino
- Updated links to the new website
- Added a link to the new website in the About dialog
- Added a report tool for add-in load errors
- Added type picker to the ElementType parameter

  ![]({{ "/static/images/release_notes/0073256343_01.png" | prepend: site.baseurl }}){: class="small-image"}

- Added DetailLevel parameter to the Element.Geometry

  ![]({{ "/static/images/release_notes/0073256343_02.png" | prepend: site.baseurl }}){: class="small-image"}


{% include ltr/release-header.html version="0.0.7317.30902" time="1/13/2020 17:10:04" %}

Started documenting release notes.

![]({{ "/static/images/release_notes/start.png" | prepend: site.baseurl }})