---
title: Release Notes
order: 40
group: Deployment & Configs
---

{% capture breaking_changes_notes %}
Some of the changes mentioned in sections below, might break your existing Grasshopper definitions. We hope this should not be causing a lot of trouble and rework for you, since in most cases the older components can easily be replaced by new ones without changes to the actual workflow. As always, if you have any issues with loading **Rhino.Inside.Revit** or any of the components, take a look at the [Troubleshooting Guide]({{ site.baseurl }}{% link _en/beta/reference/troubleshooting.md %}) or head out to the [Discussion Forum]({{ site.forum_url }}) to reach out to us. We do our best to resolve the bugs and software conflicts and need your help to make this product better for everyone.
{% endcapture %}
{% include ltr/warning_note.html note=breaking_changes_notes %}

<!-- most recent release should be on top -->
{% include ltr/release-header.html version="0.9" time="07/20/2021" %}

### New Features
* All creation components now keep track of elements created between sessions.
* Added 'Release Elements' to the Ribbon.
* Now Add components can create elements in other documents than the active.
* Now 'Document' parameter in Optional in all Add components.
* Now 'Add Floor' accepts multiple curves per 'Boundary'.
* Now 'Add DirectShape Type' take a 'Family Name' input.
* Added 'Convert Material' component.
* Added some Phasing components.
* Added components for string-based rules #376
* Added 'Category Rule' component.
* Added 'Create Selection Filter' component.
* Added 'Create Parameter Filter' component.
* Added 'Duplicate Element' component.

### Fixes
* Revit location: inconsistent use of degrees and radians #389
* Now 'QueryDesignOptions' returns 'Main Model' when 'Design Option Set' is "None".
* Now 'Filter Element' returns Nulls instead of False for invalid elements.
* Fixed 'Element Name' component when output 'Element' is post-processed.
* Fix for Grasshopper preview when geometry produces an invalid bounding box.
* Fixed some serialization problems on types without a public constructor.
* Fix for Revit minimum version checking on Revit 2018.

### Minor changes
* Minor Rhino version is now 7.8.21196.5001
* Now Rhino.Inside propagate options from and to Rhino 7 standalone.
* Restored back the warning about Rhino needs to be updated.
* Revit elements notifications are now handled a bit faster on big definitions.
* Now Components parameters that contain modified elements don't get expired until the next solution.
* Now 'Query Element' handles FullUniqueId and Built-In UniqueId.
* Disabled 'Group by' context menu.
* Disabled 'Cull' on output parameters.
* Now 'Add Wall (Profile)' reuses wall when possible.
* Added error message when user is opening a file authored with a newer version of RiR than the current installed.
* Now 'Add Model Group' takes a Plane instead of a Point.
* Now 'Level' and 'Grid' pickers are sorted like in Revit UI.
* Renamed 'Thermal Material Type' to 'Thermal Material Class".

### API
* Added DB.Element.IsValid extension method.
* Now `DB.CurveLoop.ToCurve` returns closed `PolyCurve` objects.
* Now `DB.CurveArrArray.ToPolyCurves` returns closed `PolyCurve` objects.
* Now GeometryBase.ToShape extension method never returns nulls.
* Now ConvertAll always return an Array.
* Added `DB.Document.TryGetElement` by name extension method.
* Added `DB.Document.GetNamesakeElements` extension method.
* Added `DB.Curve.IsAlmostEqualTo` extension method.

{% include ltr/release-header.html version="0.8" time="06/24/2021" %}

### New Features

* Added 'Built-In Category' searchable selector.
* Added 'Element Classes' searchable selector.
* Added Layout context menu to 'Value Picker' components.

### Fixes

* Fixed more assembly conflicts.
* Fix to prevent RiR doesn't load when 'KeyboardShortcuts.xml' contains invalid characters.
* Fixed 'Add Wall (Profile)' orientation.

{% include ltr/release-header.html version="0.7" time="06/02/2021" %}

### New Features

* Added Preview and Fuzzy search to 'Value Picker'.
* Now 'Built-In Parameter Groups' and 'Built-In Parameters' are a bit more usable.
* Now 'Levels Picker' and 'Component Families Picker' are a bit more usable.
* Now 'DirectShape Categories' is a bit more usable.

### Minor Changes

* Now Revit 2018.2 and 2019.1 or above is required.
* Renamed 'Value Set Picker' to 'Value Picker'.
* Now Linked Elements picker groups selection by document.
* Improved geometry units conversion, now is faster and more accurate.
* Changed default snap spacing from 1/16" to 3' and from 1mm to 1m.
* Now 'Category Identity' returns "ParentName\Name" as 'Name' for subCategories.
* Now 'Query Categories' returns elements sorted by Id as any other Query component.
* Enabled the IconMode UI on parameters.
* Removed `TypeName` from `ToString` result.

### Fixes

* Fixed `Interval.Scale` extension method.
* Fixed conversion from UNC `DB.ModelPath` to `Uri`.
* Fixed a problem with detached workshared models.
* Fixed a units conversion issue related to some 'Structural' parameter types.
* Fixed `Element.Cost` property type.
* Fixed 'Query Views' when filtered by 'Family'.
* Fixed a bug on Import dialog, it was showing same family multiple times.
* Changed the way we obtain Revit document Title, now is always without extension.
* Fixed a data-mismatch problem on 'Element Geometry' component.
* Fixed Types.ParameterValue tooltip for parameters that contain string values.
* Fixed 'Query Types' when managing nulls.
* Fixed pixel jitter on 'Value Picker'.

### API

* Added conversion from `ParameterId` to `Types.ParameterKey`.
* Added conversion from `CategoryId` to `Types.Category`.

{% include ltr/release-header.html version="0.6" time="05/20/2021" %}

### New Features

* Added 'Culling > Empty' option to parameters to cull empty lists.
* Added 'Document' parameter to reference Revit documents.

### Minor Changes

* Added pattern matching capabilities to 'Query Element Parameters'.
* Now 'Value Picker' respects input data branching structure on output.
* Now 'Graphical Element' parameters show the name of the externalized saved selection.
* Now components on clusters that access Active Document notify on the top level object in the canvas.
* Speed up categories listing on UI.
* Added 45 days warning into the installer.

### Fixes

* Fixed 'Types.IGH_Element' comparison method.

{% include ltr/release-header.html version="0.5" time="04/27/2021" %}

### New Features

* Now 'Add Component (Location)' tries to reuse previous iteration element.
* Now 'Add Floor' tries to reuse previous iteration element.
* Now 'Add Roof' tries to reuse previous iteration element.
* Added ability to specify imported 3DM to project origin and to import non visible layers.

### Fixes

* Now Transaction warnings and errors are displayed in the component balloon only.
* Fixed 'Element Host' when working with several Design Options.
* Fixed a `System.StackOverflowException` when selection Description property on the 'Manage Collection' dialog.
* Fixed a problem with 'opennurbs_private.manifest' on Windows server editions.
* Fixed 'Add Wall (Profile)' orientation, now wall faces to external boundary plane normal.

### Minor Changes

* Added 'Default 3D View' and 'Close Inactive Views' to te 'View' parameter context menu.
* Implemented direct casting from 'Material' to 'Colour'.
* Now 'Element Host' works with `DB.Sketch` elements.

### API

* Removed Obsolete `Revit.ApplicationUI` and `Revit.CurrentUsersDataFolderPath` properties.
* Removed Obsolete `Revit.BakeGeometry` method.
* Added `DB.Parameter.GetTypeId` extension method.
* Added `DB.Curve.TryGetPlane` extension method.
* Now `DB.Mesh.ToRhino` returns a mesh with normals.

{% include ltr/release-header.html version="0.4" time="04/20/2021" %}

### New Features

* Added support for Revit 2022.
* Added command to open a Rhino viewport.
* Added UI to 'Import 3DM' command.

### Minor changes

* Renamed 'Element Parameters' to 'Query Element Parameters'.
* Grasshopper 'Bake' command opens the floating viewport if no viewport is already visible.

### API

* Now `RhinoInside.Revit.GH.Guest.ShowEditor` activates Grasshopper window.

{% include ltr/release-header.html version="0.3" time="04/06/2021" %}

### New Features

* Added 'Select Element' component.
* Added 'Set new Element' to parameters context menu.
* Added 'Externalise selection' to the parameters context menu.

### Minor changes

* Added option to use Revit UI language on Rhino interface.

### Fixes

* Fixed a problem that makes the AddIn can not load when Rhino is not installed.
* Fixed F1 context help for commands.
* Fixed 'Element Parts Geometry' component, it now output geometry branched by layer index.
* Fixed 'Internalise selection' context menu option.
* Fixed 'Change Element collection' context menu option.
* Fixed a problem that makes parameters context menu opens slowly.

{% include ltr/release-header.html version="0.2" time="03/09/2021" %}

### New Features

* Added 'Construct Compount Structure' component.
* Added 'Construct Compount Structure Layer' component.
* Added 'Select Element' component.
* Added 'Set new Element' at the parameters context menu.
* Added auditing capabilities to the `Mesh` conversion routines.

### Fixes

* Fixed `Element Location` component when handling `Curves` or `Meshes`.
* Removed `/captureprintcalls /stopwatch` from the default startup mode.

### API

* Added class `Sum` to do more accurate a summations
* Added accurate `XYZ.GetLength`
* Added accurate `XYZ.Normalize`
* Added accurate `XYZ.CrossProduct`
* Added `XYZExtension.ComputeMeanPoint`
* Added `XYZExtension.ComputeCovariance`
* Added `Transform.GetPrincipalComponent`
* Added `Transform.TryGetInverse`

{% include ltr/release-header.html version="0.1" time="03/15/2021" %}

As part of the preparation for the {{ site.terms.rir }} v1, we have mostly focused on user interface updates in this release. These changes focus on making the {{ site.terms.rir }} easier to use, notify the user about the available updates, and provide a method to deploy and access the Grasshopper scripts easier through the Revit UI.

{% include youtube_player.html id="ogocxN8WXlA" %}

* **New Rhino.Inside Ribbon:** Checkout the updated [Rhino.Inside Tab]({{ site.baseurl }}{% link _en/beta/reference/rir-interface.md %}#rhinoinsiderevit-tab) page for instructions on how to use the new layout, and the new buttons added to the ribbon.
* **Settings**: This release adds a Settings window to {{ site.terms.rir }} as well. Checkout the [documentation here]({{ site.baseurl }}{% link _en/beta/reference/rir-interface.md %}#rhinoinsiderevit-settings)
* **Update Checks**: Checkout the new section on [Checking for Updates]({{ site.baseurl }}{% link _en/beta/reference/rir-interface.md %}#checking-for-updates) to learn how to check for updates and install the *Stable* or *Daily* releases.
* **Deploying Grasshopper Scripts**: Checkout the [documentation]({{ site.baseurl }}{% link _en/beta/reference/rir-interface.md %}#deploying-grasshopper-scripts) on how to add your scripts to the Revit UI, or install {{ site.terms.rir }} scripts using Rhino package manager

{% include ltr/release-header.html version="0.0.7733.38548" time="03/04/2021 17:48:12" %}

### Fixes

* Fixed some problems transferring Meshes when non working in feet in Rhino.
* Now `Brep` to `Solid` re-parameterize each Brep face and edge with some tolerance values more Revit friendly.
* Breps with out of tolerance edges are now rebuilt using more Revit friendly tolerances.
* Added some null checking at reconstruct DirectShape components.
* Fixed `Curve.TryGetEllipse` orientation issue.
* Fixed `PolyCurve.ToCurveMany`, it was wrongly scaling the curve twice.
* Now `NurbsCurve.ToCurve` splits input on G2 segments before transfer.
* Now `Curve.Simplify` is used to simplify Brep edges before transfer.

### API

* Added `DB.Solid.IsWatertight` extension method.
* Added `TransactionBaseComponent.TryGetCurveAtPlane`
* Added `Curve.IsParallelToPlane` extension method.
* Added `Curve.TryGetPolyCurve` extension method, to split curve into smooth G2 segments.
* Added `Rhino.Geometry.Curve.GetSpanVector` extension method.

{% include ltr/release-header.html version="0.0.7688.36802" time="01/18/2021 20:26:44" %}

### Fixes

* Now is possible to transfer geometry with short-edges.
* Added geometry conversion errors feedback to some components.

{% include ltr/release-header.html version="0.0.7683.19842" time="01/13/2021 11:01:24" %}

### Fixes

* Fixed 'Set Element Parameter' casting from integer.

{% include ltr/release-header.html version="0.0.7679.63" time="01/09/2021 00:02:06" %}

### Fixes

* Now Rhino.Inside Revit resolves at load time any dependency on assemblies installed with Rhino.
* Isolated 'opennurbs.dll' installed with Revit from 'opennurbs.dll' installed with Rhino.

### Changes

* Now installer needs admin privileges. Please remember to manually uninstall any previous version of Rhino.Inside Revit you already have installed before applying this new one.

### Important Note

This release is a major update to {{ site.terms.rir }}. This build attempts to resolve the loading and runtime errors some have experienced in Revit. It would be great if you could download and install this new version to see if it solves all the load problems.

Because this is a major change, it does require additional steps to install properly:

1. Make sure Revit is closed
2. This step is quite important: 👉 Uninstall any previous versions of {{ site.terms.rir }} through 
   **[Windows Control Panel > Programs > Programs & Features](ms-settings:appsfeatures)**
3. [Download the latest {{ site.terms.rir }}](https://www.rhino3d.com/inside/revit/beta/)
4. Install the new {{ site.terms.rir }}. This new installer requires administrator privileges to install properly

Any feedback to this new build is welcome on the [{{ site.terms.rir }} Forum]({{ site.forum_url }})

### Why are admin privileges required to install now?

In order to isolate OpenNURBS Library (`opennurbs.dll`) that is deployed by Revit from the library deployed by Rhino we do need to install a manifest file (`opennurbs_private.manifest`) inside the Revit main folder, and this requires administrator privileges. This change does not affect the Revit installation or behavior in any other way. It only directs Revit to use its own version of the OpenNURBS library when importing 3DM files. Please remember to manually uninstall any previous version of {{ site.terms.rir }} you already have installed before applying this new one.

### What’s new about .NET conflicts in this release?

Normally users have a myriad of Revit add-ons installed to support their specific workflows. Since we can not test every possible setup the user may have, we have tried in this release to make the Rhino.Inside load process more robust to handle “any” unexpected circumstance like conflicts with other installed add-ons.


{% include ltr/release-header.html version="0.0.7661.35155" time="12/22/2020 19:31:50" %}

### Fixes
* Fixed 'Deconstruct Compound Structure' units conversion.
* Fixed 'Deconstruct Compound Structure Layer' units conversion.
* Fixed "Highlight Elements" context menu when there is no active Revit document or not selected elements in the active document.
* Fixed "Set one linked element" and "Set Multiple linked elements" context menu.

### Minor Changes

* Moved 'Set CPlane' context menu item to the Bake area.

### API

* Fix: `Types.HostObjectType` constructor should be public for serialization purposes.

{% include ltr/release-header.html version="0.0.7653.37544" time="12/14/2020 20:55:12" %}

### New featues

* Improved 'Element Location' when managing `DB.DirectShape` elements.

### Fixes

* Fix for `DB.Document.Release` extension method when document is already closed.
* Fix for `DB.Solid` to `Brep` conversion when there are singular edges.
* Fixed a problem on faces that have surface orientation reversed.

### Minor changes

* Improved `BrepEncoder.ToACIS` to reuse the same Internal document.
* Trim curves are now computed with more precision (1e-5)

{% include ltr/release-header.html version="0.0.7643.31783" time="12/04/2020 17:39:26" %}

### New featues

* Implemented 'Bake…' context menu into Categories, Line patterns, Materials and Graphical Elements into Rhino blocks. Levels and Shared Site are baked as named CPlanes in Rhino.
* Added 'Activate Element CPlane' to the 'GraphicalElement' parameters.

### Fixes
* Fixed some geometry conversion problems using SAT file export-import on those cases.
* Fixed a units conversion problem on 'Bitmap Asset' components.
* Fixed a bug in `ValueSetPicker` when comparing `GH_StructurePath` objects.
* Fixed `Types.BasePoint.Location` for shared locations like the 'Survey Point'.

### Minor Changes

* Now we keep Revit window disabled while Rhino.Inside is loading. This prevents Rhino `MessageBox` windows that appear during startup go behind Revit window.

### API

* Added `IsEquivalent` extension method for `DB.Document` and `DB.Element` to compare if two references point to the same internal Revit object.

{% include ltr/release-header.html version="0.0.7626.34420" time="11/19/2020 14:00:20" %}

### New featues

* Added 'Query Element' component.
* Added 'Project Location' component.
* Added 'Query Shared Sites' component.
* Added 'Query Site Locations' component.
* Added 'Site Location Identity' component.

### Fixes

* Fixed Level elevation by 'Survey Point'.
* Fixed 'Export Type Image' for types that do not generate any bitmap.

### API

* Implemented `IGH_QuickCast` interface at `Types.ElementId` for interoperability with List-Set components.

{% include ltr/release-header.html version="0.0.7626.21365" time="11/13/2020 11:52:10" %}

### New features

* Added 'Level Identity' component.
* Added 'Project Information' component.
* Added special 'Elevation' and 'Elevation Interval' parameter to manage levels elevations from different base points.

### Fixes

* Fixed 'Element Location' when working with groups.

{% include ltr/release-header.html version="0.0.7622.22831" time="11/13/2020 12:41:02" %}

### New features

* Added 'Active Design Option' component.
* Added 'Design Option Identity' component.
* Added 'Design Option Set Identity' component.
* Added 'Query Design Options' component.
* Added 'Query Design Option Sets' component.

### Fixes

* Fixed `Types.CurveElement` serialization.
* Fixed `Types.Dimension.Location` and `Types.Dimension.Curve` properties.
* Fixed `Types.ParameterValue.CastTo<IGH_Goo>`.
* Fixed error message at 'Add Wall (Profile)'.

### Minor Changes

* Enabled Materials support in Revit 2018.

### API

* Added special cases for conversion from `DB.Parameter` integer value to `GH_Enumerate`.

{% include ltr/release-header.html version="0.0.7618.21861" time="11/09/2020 12:08:42" %}

### New features

* Added 'Material Identity' component.
* Added 'Material Graphics' component.
* Added 'Extract Material Assets' component.
* Added 'Replace Material Assets' component.
* Added 'Create-Analyze-Modify Appearance Asset' components.
* Added 'Create-Analyze-Modify Physical Asset' components.
* Added 'Create-Analyze-Modify Thermal Asset' components.
* Added 'Construct-Deconstruct Bitmap Asset' components.
* Added 'Construct-Deconstruct Checker Asset' components.
* Added 'Add Wall (Profile)' component.
* Added 'Fill Pattern' parameter.
* Added 'Line Pattern' parameter.
* Added modify capabilities to the 'Categories Object Styles' component.
* Added context menu pickers to 'Level' and 'Grid' parameters.
* Implemented `Name` property in to `Types.Category` this enables subCategory renaming.
* Improved 'Query Categories', now is faster and able to report internal categories.
* Added 'Element Location' component.
* Added 'Element Curve' component.
* Added 'Host Curtain Grids' component.
* Added 'Query Grids' component.
* Added more params to 'Query Levels' component.

### Fixes

* Fixed 'Similar Types' to work with multiple documents.
* Now every *Parameter* that references Revit elements will be expired when the user modify the Revit document. If the modify operation is not UNDO or REDO the Grasshopper solution will be computed again.
* Now 'Bounding Box Filter' accepts any 'Geometry' to extract the bounding box.
* Fixed 'Document Save' component.
* Fixed #331: Set Linked Levels/Grids not working

### Minor Changes

* Added at 'Query Elements' an input parameter 'Limit' and an output parameter 'Count' to help on big models.
* Renamed 'Graphical Element Geometry' by 'Element View Geometry'
* Renamed 'Query Graphical Elements' by 'Query View Elements'
* Component 'Inspect' now ignores Parameters that are not basic types.
* Rewritten 'Logical And Filter' and 'Logical Or Filter' to take multiple filters.
* Added support for `DB.ReferencePlane`.
* Improved non-axis-aligned bounding box support.
* Improved `Types.Grid` preview on Rhino.
* Improved `Types.CurtainGrid` preview on Rhino.

### API

* Now Rhino.Inside Revit requires Rhino 7.0.20301.12003-beta
* Added *DEBUG* Rhino System folder to `Addin.SystemDir`
* Added `TransactionChain` class.
* Added `AdaptiveTransaction` class.
* Added `CommitScope` extension method to `DB.Document` class.
* Added `RollBackScope` extension method to `DB.Document` class.
* Now `DB.Element.GetParameters` ignores parameters that are not basic types.
* Added `WhereCategoryIdEqualsTo` extension method to `DB.FilteredElementCollector`. `DB.FilteredElementCollector.OfCategoryId` ignores `DB.ElementId.InvalidElementId`.
* Added `RhinoInside.Revit.External.DB.BuiltInLinePattern` enum.
* Added `DB.Document.GetCategories` extension method to enumerate all `DB.Category` instances.
* Added concept of <None> elements.
* Now `Types.ElementId` caches the `DB.Element` value. This improve performance specially for `Types.Category`.
* Now `ErrorReport.CLRVersion` returns the running CLR Product version. `ErrorReport.CLRMaxVersion` returns the maximum installed CLR version.
* Added `Types.Element.Value` to access the internal `DB.Element`.
* Added `Types.GraphicalElement.DisableJoinsScope` to temporary disallow joins on and from this element.

{% include ltr/release-header.html version="0.0.7571.33757" time="09/23/2020 18:45:14" %}

### New features

* Added icon for 'Add Railing' component.
* Added 'Pin Element' component.
* Added 'Flip Element' component.

### Fixes

* Fixed 'Add 3d View' component.

### Minor Changes

* Added a warning message that warns the user that Rhino and Active Revit model are in different units.

### API

* Implemented GetDependentElements in more robust way. Now can be called while a DB.Transaction or DB.SubTransaction is open on the element document.
* Added extension method `WhereParameterEqualsTo` to `FilteredElementCollector`.
* Added extension method `GetAssociatedLevelId` to `ViewPlan`.
* Added extension method `GetActiveView` to `DB.Document`.

{% include ltr/release-header.html version="0.0.7557.24227" time="09/19/2020 13:27:34" %}

### New Features

* Added 'Add SubCategory' component.
* Added 'Add Railing' component.

### Fixes

* Fixed 'Document Links' component to make it work with BIM 360 linked files.
* Improved `DB.HemiteSurface` conversion to `NurbsSurface`.

{% include ltr/release-header.html version="0.0.7536.22136" time="08/19/2020 12:17:52" %}

### New Features

* Added new icons for 'Element Name', 'Element Category' and 'Element Type' components.
* Added support for .ghlink files.
* Enabled 'Add Topography (Mesh)' in Revit 2019.2.

### Fixes

* Fixed a bug when no `MeshingParameters` is available for Grasshopper previews.
* Fixed a bug when user selects 'Disable Meshing' in Grasshopper UI for previews.
* Improved `DB.Elements` meshing for previews. Now uses Grasshopper meshing preview settings and do not compute nGons on those meshes.
* Fixed bug into `RawDecoder.AddSurface` when surface is a `SumSurface`
* Fixed bug into `RawDecoder.AddSurface` when `DB.Surface.OrientationMatchesParametricOrientation` is `false`.
* Now `RhinoInside.Revit.Convert.Geometry.BrepEncoder.EncodeRaw` splits kinky faces before transferring geometry.
* Improved `RhinoInside.Revit.Convert.Geometry.NurbsSplineEncoder.ToDoubleArray` precision.
* Now `RhinoInside.Revit.Convert.Geometry.BrepEncoder.EncodeRaw` normalizes brep faces to increase chances of Revit API detects pipe like surfaces as a `DB.CylindricalSurface`.
* Now `RhinoInside.Revit.Convert.Geometry.Raw.ToHost` from `BrepFace` is a bit more robust to code changes.
* Fixed a bug in `RhinoInside.Revit.Geometry.Extensions.TryGetExtrusion` from `Brep` when there are faces that are "near" planar.
* Fixed a bug in 'Query Graphical Elements'. It was not collecting `DB.Element` without category.

### Minor Changes

* Now component 'Element Preview' extracts meshes using 0.5 as 'Quality' when no value is provided.

### API

* Moved `OwnerView` property from `IGH_InstanceElement` to `IGH_GraphicalElement`
* Now `RawDecoder.ToRhinoSurface` from `DB.RuledFace` uses `DB.ExportUtils.GetNurbsSurfaceDataForFace`to extract the surface NURBS form.

{% include ltr/release-header.html version="0.0.7524.21907" time="08/07/2020 12:10:14" %}

### New Features

* Now `DB.Part` is considered a Graphical Element.

### Fixes

* Added some null checking to 'Query Graphical Elements'
* Fixed `RawDecoder.FromRuledSurface`. Resulting surface should be transposed.
* Added `RawDecoder.FromExtrudedSurface` to handle cases where `DB.RuledFace` is an extrusion.
* Fixed #312: Search Families by using *Asterisk*
* Fixed a bug in 'Element Geometry' and 'Graphical Element Geometry' managing trees.
* Fixed 'Element Parts Geometry' when element parts where already created in the document.

### Minor Changes

* Renamed MaterialQuanities.cs to MaterialQuantities.cs.
* Renamed ElementMaterialQuanities to ElementMaterialQuantities
* Moved 'Geometric Element' components as secondary.
* Renamed 'Element Compound Structure Geometry' to 'Element Parts Geometry'.
* Moved back 'Compound Structure Layer' components under the 'Host' Panel.

{% include ltr/release-header.html version="0.0.7517.32978" time="07/31/2020 18:19:16" %}

### New Features

* Grouped all transactions opened in a Grasshopper solution as one UNDO operation in Revit.

### Fixes

* Fixed a bug on NurbsCurve conversion to DB.NurbsSpline when original curve is not C1.
* Fixed a bug in Types.GraphicalElement.ClippingBox when DB.Element is not available.
* Fixed a bug in 'Add Beam', now default Cross-Section Rotation is 0.0

### Minor Changes

* Disabled Grasshopper previews when Solver is locked.
* Now Grasshopper ignores disabled params or components when occurs a change in Revit document in order to expire the solution.

### API

* Added ApplicationServices.Application.GetOpenDocuments extension method.

{% include ltr/release-header.html version="0.0.7513.21931" time="07/27/2020 12:11:02" %}

### New Features

* Updated Installer bitmaps.
* Added 'Filter Element' component.
* Added 'Query Graphical Elements' component.
* Added 'Element Name', 'Element Category' and 'Element Type' components.

### Minor Changes

* Renamed 'Exclude ElementType' Filter component to 'Exclude Types'.
* Renamed 'Document Levels Picker' to 'Levels Picker'.
* Renamed 'All documents' to 'Open Documents'

### Fixes

* Fixed a bug in ActivationGate that provokes Grassopper window not to activate once is deactivated.
* Fixed Parameters UI when have no connected inputs.
* Fixed RhinoInside.Revit.GH.Types.Panel.IsValidElement to recognize DB.Panel as a Types.Panel.
* Fixed 'Document Links' component. Now it works even when there is no instance to the link placed in the model.

### API

* Added Extension method DB.Document.HasModelPath to check if a DB.Document has the specified path.

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

* One of the major additions in this release is the Document-aware components. These components can query information from all the active documents at the same time so you can analyze and compare projects easier.

![]({{ "/static/images/release_notes/2019-04-0103.png" | prepend: site.baseurl }})

* The new Rhino.Inside.Revit have also improved the geometry transfer logic between Rhino and Revit in both directions and improved the edge tolerance and trimmed curve conversion as well. This will allow more geometry to pass between Rhino and Revit as a Brep Solids.
  * Degree 2 curves with more than 3 points are upgraded to degree 3 to fulfill the Revit requirements
  * And Curves that are not C2 are approximated moving the knots near the discontinuity
  * Curves are scaled on the fly without copying to improve performance. This means on Rhino models in mm that should be converted to feet in Revit is done without duplicating the curve

* If you have been following the project closely, you might have noticed that we had included a large collection of python components to get you started with different workflows using the Revit API. In the meantime, we have been testing out the methods and ideas behind these components and happy to announce that we have started porting them into the Rhino.Inside.Revit source code. This would standardize the workflows and improve the performance of your Grasshopper definitions.

![]({{ "/static/images/release_notes/2019-04-0102.png" | prepend: site.baseurl }})

![]({{ "/static/images/release_notes/2019-04-0101.png" | prepend: site.baseurl }})

{% include ltr/release-header.html version="0.0.7348.18192" time="02/13/2020 10:06:24" %}

* {{ site.terms.rir }} now notifies user when the units settings of Revit model and Rhino document do not match
* AddModelLine.BySketchPlane does not fail on periodic curves anymore [Issue #143](https://github.com/mcneel/rhino.inside-revit/issues/143)
* Automatically disable active Grasshopper Document when we lost access to Revit API. This means Grasshopper timers will be disabled until we get access back.

{% include ltr/release-header.html version="0.0.7341.20715" time="02/06/2020 11:30:30" %}

* Added **Structural Usage** input parameter to *Wall.ByCurve* component

![]({{ "/static/images/release_notes/79999999_01.png" | prepend: site.baseurl }}){: class="small-image"}

* Added **Principal Parameter** menu option to parameters
  
![]({{ "/static/images/release_notes/79999999_02.png" | prepend: site.baseurl }}){: class="small-image"}

* [Fixed Issue #131](https://github.com/mcneel/rhino.inside-revit/issues/131)
* [Fixed Issue #123](https://github.com/mcneel/rhino.inside-revit/issues/123)

{% include ltr/release-header.html version="0.0.7333.32251" time="1/29/2020 17:55:02 AM" %}

* Added HiDPI images for Grasshopper toolbar buttons
* Updated RhinoCommon dependency to `7.0.20028.12435-wip`
* [Resolved Issue #120](https://github.com/mcneel/rhino.inside-revit/issues/120): Grasshopper updates, somehow mess up the Project Browser configurations

{% include ltr/release-header.html version="0.0.7325.6343" time="1/21/2020 03:32:26 AM" %}

* Grasshopper and Rhino shortcuts now work inside Revit (Rhino v7.0.20021.12255, 01/21/2020)
* Fixed a bug when there is no ActiveDocument in Revit
* Fixed bug converting ellipses from Revit to Rhino
* Updated links to the new website
* Added a link to the new website in the About dialog
* Added a report tool for add-in load errors
* Added type picker to the ElementType parameter

![]({{ "/static/images/release_notes/0073256343_01.png" | prepend: site.baseurl }}){: class="small-image"}

* Added DetailLevel parameter to the Element.Geometry

![]({{ "/static/images/release_notes/0073256343_02.png" | prepend: site.baseurl }}){: class="small-image"}

{% include ltr/release-header.html version="0.0.7317.30902" time="1/13/2020 17:10:04" %}

Started documenting release notes.

![]({{ "/static/images/release_notes/start.png" | prepend: site.baseurl }})