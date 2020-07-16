---
title: Release Notes
order: 40
group: Deployment & Configs
---

{% capture breaking_changes_notes %}
Some of the changes mentioned in sections below, might break your existing Grasshopper definitions. We hope this should not be causing a lot of trouble and rework for you, since in most cases the older components can easily be replaced by new ones without changes to the actual workflow. As always, if you have any issues with loading **Rhino.Inside.Revit** or any of the components, take a look at the [Troubleshooting Guide]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %}) or head out to the [Discussion Forum]({{ site.forum_url }}) to reach out to us. We do our best to resolve the bugs and software conflicts and need your help to make this product better for everyone.
{% endcapture %}
{% include ltr/warning_note.html note=breaking_changes_notes %}

<!-- most recent release should be on top -->
{% include ltr/release-header.html version="0.0.7500.18692" time="07/14/2020 10:23:04" %}

### New Features
* Now 'Bounding Box' Grasshopper component works with Revit elements.
* Added support for more `DB.FamilyPlacementType` to the 'Add Component (Location)' component.

### Minor Changes
* Add more info to the report file about where 'opennurbs.dll' is loaded from.

### Fixes
* Resolved units conversion issues in 'Analyse Wall' component (#263).
* Fixed 'Element Geometry' component when managing family geometry. 

### API
* Added `DB.Document.GetActiveGraphicalView` extension method.

{% include ltr/release-header.html version="0.0.7481.2160" time="06/25/2020 01:12:00" %}

### New Features
* Added new input parameter to Element.Geometry component to extract geometry ignoring other elements.
Is useful to extract a wall shape without the Inserts or without extending it to a roof it is extended.
* Added conversion from string to Enum and standardized concept of Invalid or Unset as `<empty>`.
* Added 'Element Purge' component
* Added a button in Revit Ribbon to Enable and Disable Grasshopper solver.
* Added geometry preview to Mullions
* Added 'Graphical Element Geometry' to extract View dependent geometry and geometry category.
* Added 'Reset Element Parameters'
* Removed the samples panel.
* Added Command Import to Revit Ribbon.
* Added Host Boundary Profile component.
* Added 'Element Host' component.
* Added 'Family Identity' component.
* Added support for more ParameterType units conversions.
* Added 'Graphical Element Location' component.
* Implemented previews for Grids and Levels
* Added CurveElement type.
* Added casting from GraphicalElement to GH_Line

### Minor Changes
* Added icon to 'RhinoInside.Revit.GH.gha' module.
* Renamed some component names and parameters to follow Revit terminology
* Now 'Element Dependents' skips the input element on the output.
* Updated 'View Identity' and 'Query Views' to use DB.ViewFamily enum.
* Grasshopper preview server filters out those component params that do not implement IGH_PreviewObject interface.
* Param Enum now shows the same icon as 'Generic Data' in Grasshopper.
* Removed Locked and Lockable feature from Panels and Mullions because is incomplete.

### Fixes
* Fixed DocumentChangedEvent to trigger New Grasshopper solutions when Revit model changes.
* Disable Grasshopper previews when the solver is disabled.
* Added message to report 'opennurbs.dll' is already loaded instead of failing.
* Now expired Rhino.Inside Revit should warn the user instead of simply gray out the button in the Add-Ins tab.
* Check before load the Addin is compiled for correct version of Revit is being loaded.
* Improves the Grasshopper preview in Revit of dense curves.
* Fixed a bug related to units in ToGeometryObjectMany from Brep when it fails and generates a Mesh.
* Fixed a bug in BrepEncoder.ToBRepBuilderEdgeGeometry when the edge domain is the full curve.
* Fixes #139: AddFamilyInstance.ByLocation component does not apply transform on hosted instances
* Fixed a units problem on conversion from Face to Brep or Surface.
* Fixed a bug in 'Add Form' component converting units when converting a Brep to a extrusion DB.Form.
* Fixed RawDecoder.ToRhino from DB.Line when the input line is not bounded.

### API
* Added ToPoint2d, ToVector2d and ToUV for converting Autodesk.Revit.DB.UV objects.
* Renamed ToHost and ToRhino by AsPoint3d, AsPoint2d, AsVector3d and AsVector2d for XYZ and UV, to have conversion to Point and Vector.
* Fixed ToEllipse with factor
* Now OpenAwaiter result returns the previous ActivationGate status (open or closed).
* Added IsElementTypeId extension method to DB.ElementId
* Added GetPurgableElementTypes extension method to DB.Document
* Added DB.Element.IsSameElement extension method and fixed DB.Element.CopyParametersFrom.
* Added System.Type.IsGenericSubclassOf extension method.
* Added support for NameAttribute and DefaultValueAttribute to ReflectedComponent.
* Added DB.Document.Release extension method to close a document in case is not open on UI.
* Added extension methods to convert DB.Rectangle into System.Drawing.Rectangle
* Added TryGetOpenUIDocument & TryGetOpenUIView extension methods.
* Added some extension methods to convert DB.Outline and DB.BoundingBoxXYZ
* Added extension methods to DB.View for extracting the View Rectangle in pixels.
* Added extension methods to DB.XYZ to check for perpendicularity and codirectionality.
* Added AreEquivalentReferences extension method do Autodesk.Revit.DB.Document.
* Added extension methods to DB.Element to access Dependent Elements.


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