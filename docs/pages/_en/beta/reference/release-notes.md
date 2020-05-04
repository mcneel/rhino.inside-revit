---
title: Release Notes
order: 40
---

{% capture breaking_changes_notes %}
Some of the changes mentioned in sections below, might break your existing Grasshopper definitions. We hope this should not be causing a lot of trouble and rework for you, since in most cases the older components can easily be replaced by new ones without changes to the actual workflow. As always, if you have any issues with loading **Rhino.Inside.Revit** or any of the components, take a look at the [Troubleshooting Guide]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %}) or head out to the [Discussion Forum]({{ site.forum_url }}) to reach out to us. We do our best to resolve the bugs and software conflicts and need your help to make this product better for everyone.
{% endcapture %}
{% include ltr/warning_note.html note=breaking_changes_notes %}

<!-- most recent release should be on top -->

{% include ltr/release-header.html version="0.0.7429.17299" time="5/4/2020 9:36:38 AM" %}

{% include youtube_player.html id="3OKoTQt-a28" %}

- One of the major additions in this release is the Document-aware components. These components can query information from all the active documents at the same time so you can analyze and compare projects easier.

![]({{ "/static/images/release_notes/2019-04-0103.png" | prepend: site.baseurl }})

- The new Rhino.Inside.Revit have also improved the geometry transfer logic between Rhino and Revit in both directions and improved the edge tolerance and trimmed curve conversion as well. This will allow more geometry to pass between Rhino and Revit as a Brep Solids.
  - Degree 2 curves with more than 3 points are upgraded to degree 3 to fulfill the Revit requirements
  - And Curves that are not C2 are approximated moving the knots near the discontinuity
  - Curves are scaled on the fly without copying to improve performance. This means on Rhino models in mm that should be converted to feet in Revit is done without duplicating the curve

- If you have been following the project closely, you might have noticed that we had included a large collection of python components to get you started with different workflows using the Revit API. In the meantime, we have been testing out the methods and ideas behind these components and happy to announce that we have started porting them into the Rhino.Inside.Revit source code. This would standardize the workflows and improve the performance of your Grasshopper definitions.

![]({{ "/static/images/release_notes/2019-04-0102.png" | prepend: site.baseurl }})

![]({{ "/static/images/release_notes/2019-04-0101.png" | prepend: site.baseurl }})


{% include ltr/release-header.html version="0.0.7348.18192" time="02/13/2020 10:06:24" %}

- {{ site.terms.rir }} now notifies user when the units settings of Revit model and Rhino document do not match
- AddModelLine.BySketchPlane does not fail on periodic curves anymore [Issue #143](https://github.com/mcneel/rhino.inside-revit/issues/143)
- Automatically disable active Grasshopper Document when we lost access to Revit API. This means Grasshopper timers will be disabled until we get access back.


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