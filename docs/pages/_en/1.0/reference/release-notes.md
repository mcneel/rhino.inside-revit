---
title: Release Notes
order: 40
group: Deployment & Configs
---

<!-- list the changes in WIP branch -->

{% capture rc_release_notes %}

### WIP

### RC

- Fixed 'Symbolic' input on 'Component Family Curve' component.
  [#1061](https://github.com/mcneel/rhino.inside-revit/issues/1061)
- Fixed 'Revit geometry is not accepted into New Component Family component'
  [#1051](https://github.com/mcneel/rhino.inside-revit/issues/1051)

{% endcapture %}
{% include ltr/release_header_next.html title="Upcoming Changes" note=rc_release_notes %}

{% include ltr/release-header.html title="v1.19" version="v1.19.8816.24975" time="02/20/2024" %}

- Includes all changes under 1.19 RC releases listed below

{% include ltr/release-header.html title="v1.19 RC2" version="v1.19.8797.16328" pre_release=true time="01/02/2024" %}

- Re-Released with newly signed libraries and installer


{% include ltr/release-header.html title="v1.18" version="v1.18.8797.15011" time="01/02/2024" %}

- Re-Released with newly signed libraries and installer

{% include ltr/release-header.html title="v1.19 RC2" version="v1.19.8780.23124" pre_release=true time="01/16/2024" %}

- Improves {% include ltr/comp.html uuid='e996b34d' %}
- Improves {% include ltr/comp.html uuid='b8677884' %}

{% include ltr/release-header.html title="v1.19 RC1" version="v1.19.8780.17135" pre_release=true time="01/15/2024" %}

- Misc Fixes and Improvements

{% include ltr/release-header.html title="v1.18" version="v1.18.8780.17023" time="01/15/2024" %}

- Includes all changes under 1.18 RC releases listed below

{% include ltr/release-header.html title="v1.18 RC4" version="v1.18.8753.16182" pre_release=true time="12/19/2023" %}

- Improved 'Add Wall (Profile)' component to accept non vertical profiles. 

{% include ltr/release-header.html title="v1.18 RC3" version="v1.18.8746.17424" pre_release=true time="12/12/2023" %}

- Misc Fixes and Improvements

{% include ltr/release-header.html title="v1.18 RC2" version="v1.18.8739.18374" pre_release=true time="12/05/2023" %}

- Added {% include ltr/comp.html uuid='e13fc388' %} component

{% include ltr/release-header.html title="v1.18 RC1" version="v1.18.8734.21200" pre_release=true time="11/28/2023" %}

- Nothing new.

{% include ltr/release-header.html title="v1.17" version="v1.17.8734.20954" time="11/28/2023" %}

<div style="text-align: center;">
<img src="/rhino.inside-revit/static/images/release_notes/rhino8.png" alt="" style="width: 256px;">
<p style="margin: 0;">{{ site.terms.rir }} now supports Rhino 8!</p>
<p style="color: darkgray;">[ v8.1 and above ]</p>
</div>

- Renamed 'Document Warnings' -> {% include ltr/comp.html uuid='3917adb2' %}.
- Added context menu to {% include ltr/comp.html uuid='73e14fbb' %} to filter by severity.
- Improved element-tracking on views.
- Fix on {% include ltr/comp.html uuid='71f014de' %} component when used on an `ARDB.TextNote`.
- Fix on {% include ltr/comp.html uuid='97c8cb27' %} when wildcard is used as input.
- Fixed a bug that makes Grasshopper previews visible on Revit type preview dialog.
- Fix on `PersistentParam` context menu when it contains deleted elements.
- Fix on {% include ltr/comp.html uuid='b18ef2cc' %} conversion on Rhino 8.
- Implemented conversion from 'Element' to 'Model Content' on some types.

{% include ltr/release-header.html title="v1.17 RC1" version="v1.17.8620.27747" pre_release=true time="08/08/2023" %}

- Misc fixes and improvements

{% include ltr/release-header.html title="v1.16" version="v1.16.8620.27325" time="08/08/2023" %}

- Added 'Add Toposolid' component. ({{ site.terms.revit }} 2024)
- Added 'Add Toposolid Sub-Division' component. ({{ site.terms.revit }} 2024)
- Added {% include ltr/comp.html uuid='86d56bea' %} component.
- Added {% include ltr/comp.html uuid='aae738e5' %} component.
- Added {% include ltr/comp.html uuid='2ab03aaf' %} component.
- Improved {% include ltr/comp.html uuid='516b2771' %} performance.
- Renamed {% include ltr/comp.html uuid='ecc6fa17' %} -> {% include ltr/comp.html uuid='79daea3a' %}.
- Now {% include ltr/comp.html uuid='79daea3a' %} filters out hidden UI _Categories_.
- Added 'Is Visible UI' to {% include ltr/comp.html uuid='d150e40e' %} and {% include ltr/comp.html uuid='d794361e' %}.
- Added {% include ltr/comp.html uuid='ecc6fa17' %} component.
- Added {% include ltr/comp.html uuid='92b3f600' %} component.
- Added {% include ltr/comp.html uuid='ec5cd3bb' %} component.
- Added {% include ltr/comp.html uuid='63c816d8' %} component.
- Fixed 'Element Name' when used to rename _Subcategories_ in multiple families at once.
  [#898](https://github.com/mcneel/rhino.inside-revit/issues/898)
- Fix on {% include ltr/comp.html uuid='d150e40e' %} it should output ordered by id.
- Fix on {% include ltr/comp.html uuid='79daea3a' %} when a `<None>` category is used.
- {% include ltr/comp.html uuid='495330db' %} compoennt now works on `GenericForm` elements.
- Added {% include ltr/comp.html uuid='1caafc26' %} component.
  [#871](https://github.com/mcneel/rhino.inside-revit/issues/871)
- Fix on DirectShape components.
- Renamed 'Analyze Instance Space' -> {% include ltr/comp.html uuid='6ac37380' %}.
- Updated Topological components to return linked elements.

{% include ltr/release-header.html title="v1.16 RC1" version="v1.16.8596.34292" pre_release=true time="07/15/2023" %}

- Misc fixes and improvements

{% include ltr/release-header.html title="v1.15" version="v1.15.8596.34075" time="07/15/2023" %}

- Added {% include ltr/comp.html uuid='440b6beb' %}
- Added {% include ltr/comp.html uuid='6388cfc0' %}
- Exposed {% include ltr/comp.html uuid='f9bc3f5e' %}
- Includes all changes under 1.15RC releases listed below

{% include ltr/release-header.html title="v1.15 RC2" version="v1.15.8571.17168" pre_release=true time="06/20/2023" %}

- Fix on {% include ltr/comp.html uuid='ad88cf11' %} component when 'Line Style' input is used.
- Fix on {% include ltr/comp.html uuid='91757ae0' %} when used on linked model faces.
- Added 'Sketch Lines' component.
- Added 'Lines' output to 'Element References' component.
- Added 'Curve Point References' component.
- Implemented casting for 'Face' -> 'Category' & 'Material'
- Added 'Element View' component to query for the owner view of an element.
- Implemented casting for 'View' -> 'Sheet' & 'Viewport' (when placed on a sheet)
- Implemented casting for 'Viewport' -> 'View' & 'Sheet'

{% include ltr/release-header.html title="v1.15 RC1" version="v1.15.8557.7999" pre_release=true time="06/07/2023" %}

- Misc fixes and improvements

{% include ltr/release-header.html title="v1.14" version="v1.14.8557.24642" time="06/07/2023" %}

- Includes all changes under 1.14RC releases listed below
- Misc fixes and improvements

{% include ltr/release-header.html title="v1.14 RC4" version="v1.14.8543.20042" pre_release=true time="05/23/2023" %}

- Now Area, Room and Space Tag components require a View.
- Added 'Area Scheme' parameter.
- Made {% include ltr/comp.html uuid='2101fff6' %} component hidden.

{% include ltr/release-header.html title="v1.14 RC3" version="v1.14.8536.14258" pre_release=true time="05/19/2023" %}

- Added {% include ltr/comp.html uuid='bbd8187b' %} component.
- Added {% include ltr/comp.html uuid='8ed1490f' %} component.
- Added {% include ltr/comp.html uuid='dda08563' %} component.


{% include ltr/release-header.html title="v1.14 RC2" version="v1.14.8529.14229" pre_release=true time="05/09/2023" %}

- Added 'Add Beam System' component.
- Added 'Add Truss' component.
- Renamed component 'Add Structural Foundation' to 'Add Foundation (Isolated)'.
- Added 'Add Foundation (Slab)' component.
- Added 'Add Foundation (Wall)' component.

{% include ltr/release-header.html title="v1.14 RC1" version="v1.14.8510.35994" pre_release=true time="04/20/2023" %}

- Minimum Rhino version is now 7.28.
- Added support for '{{ site.terms.revit }} 2024'
- Added 'View Range Elevations' component.

{% include ltr/release-header.html title="v1.13" version="v1.13.8511.13600" time="04/20/2023" %}

- Includes all changes under 1.13RC releases listed below
- Added {% include ltr/comp.html uuid='e2435930' %} parameter
- Added {% include ltr/comp.html uuid='4326c4aa' %} parameter
- Misc fixes and improvements

{% include ltr/release-header.html title="v1.13 RC6" version="v1.13.8494.18380" pre_release=true time="04/04/2023" %}

- Improved {% include ltr/comp.html uuid='6723beb1' %} component, now identifies more types.
- Fixed {% include ltr/comp.html uuid='d1940eb3' %} on areas that have internal loops.
- Fixed {% include ltr/comp.html uuid='0ea8d61a' %} when cloning named elements.
- {% include ltr/comp.html uuid='a5c63076' %} and *Bounding Box* components can now correctly provide location and bounds info for a Revit Scope Box .
- Fixed issue with not reading Admin configuration file correctly

{% include ltr/release-header.html title="v1.13 RC5" version="v1.13.8486.7511" pre_release=true time="03/28/2023" %}

- Fixed {% include ltr/comp.html uuid='96d578c0' %} component when the referenced element is a Rebar
- Fixed AssemblyResolver. Now first call to RhinoCommon fully loads it

{% include ltr/release-header.html title="v1.13 RC4" version="v1.13.8480.18315" pre_release=true time="03/21/2023" %}

- Renamed 'Annotation' panel to 'Annotate'.
- Merged 'Build', 'Host' and 'Wall' panel under a new 'Architecture' panel.
- Moved structural element creation components to 'Structure' panel.
- Moved Component Family related components to 'Component' panel.
- Renamed 'Host Inserts' component to {% include ltr/comp.html uuid='70ccf7a6' %}.
- Now {% include ltr/comp.html uuid='70ccf7a6' %} keep linked elements on linked documents.
- Fixd a bug on {% include ltr/comp.html uuid='ad88cf11' %} component 'Line Style' input when used on a Family document.
  [#788](https://github.com/mcneel/rhino.inside-revit/issues/788)
- Fixed a bug on previews when there are Groups on the canvas.
- Now {% include ltr/comp.html uuid='c2b9b045' %} treats relative paths as temporary.
- Now {% include ltr/comp.html uuid='c2b9b045' %} has a 'Path' output to allow chaining with 'Load Component Family'.
- Now {% include ltr/comp.html uuid='82523911' %} creates a Work Plane-Based family when no template is provided.
- Added 'Offset from Host' parameters to work plane-based components.

{% include ltr/release-header.html title="v1.13 RC3" version="v1.13.8474.22250" pre_release=true time="03/15/2023" %}

- Added {% include ltr/comp.html uuid='5a6d9a20' %} component.
- Added {% include ltr/comp.html uuid='be2c26c7' %} component.
- Added {% include ltr/comp.html uuid='72b92e6a' %} component.

{% include ltr/release-header.html title="v1.13 RC2" version="v1.13.8466.15699" pre_release=true time="03/07/2023" %}

- Improved how components recognize verticality in Revit
- Now the Grasshopper Editor window stays at same position after picking from Revit.

{% include ltr/release-header.html title="v1.13 RC1" version="v1.13.8458.21732" pre_release=true time="02/28/2023" %}

- Added {% include ltr/comp.html uuid='2101fff6' %} component.
- Added {% include ltr/comp.html uuid='08586f77' %}.

{% include ltr/release-header.html title="v1.12" version="v1.12.8449.6358" time="02/28/2023" %}

- Added {% include ltr/comp.html uuid='71f014de' %} component.
- Added {% include ltr/comp.html uuid='3e2a753b' %} component.
- Added {% include ltr/comp.html uuid='91757ae0' %} component.

{% include ltr/release-header.html title="v1.12 RC4" version="v1.12.8438.12884" pre_release=true time="02/07/2023" %}

- Added {% include ltr/comp.html uuid='96d578c0' %} component.
- Added {% include ltr/comp.html uuid='d4873f18' %} component.
- Added **Horizontal Align** and **Vertical Align** inputs to {% include ltr/comp.html uuid='49acc84c' %} component.

{% include ltr/release-header.html title="v1.12 RC3" version="v1.12.8431.23288" pre_release=true time="01/31/2023" %}

- Updated `MeshEncoder` to produce Meshes whithout internal wires. (Revit 2023)

{% include ltr/release-header.html title="v1.12 RC2" version="v1.12.8425.21537" pre_release=true time="01/24/2023" %}

- Added {% include ltr/comp.html uuid='3ae4fa67' %} component.
- Added {% include ltr/comp.html uuid='369b6109' %} component.
- Added {% include ltr/comp.html uuid='8484e108' %} component.
- Updated {% include ltr/comp.html uuid='f7b775c9' %} component, now it takes a Frame as input.
- Now 'View' bake creates a named View in Rhino.
- Now 'Open Viewport` grabs Revit active view settings.
    * If CTRL is pressed grabs orientation.
    * If SHIFT is also pressed zoom is also applied.

{% include ltr/release-header.html title="v1.12 RC1" version="v1.12.8417.6530" pre_release=true time="01/17/2023" %}

- Fixed {% include ltr/comp.html uuid='01e86d7c' %} name is getting an unexpected integer added in creation.
  [#754](https://github.com/mcneel/rhino.inside-revit/issues/754)
- Added {% include ltr/comp.html uuid='e4e08f99' %} component.
- Added {% include ltr/comp.html uuid='2922af4a' %} component.
- Added {% include ltr/comp.html uuid='b062c96e' %} component.
  [#753](https://github.com/mcneel/rhino.inside-revit/issues/753)


{% include ltr/release-header.html title="v1.11" version="v1.11.8425.15605" time="01/10/2023" %}

- Fix for "Comments" parameter `DataType`.
- Improved `Types.CurtainGridLine` previews.
- Improved conversion from `CurtainGrid` and `CurtainCell` to `Brep`.
- Fixed `Types.Panel` and `Types.PanelType` recognizing a `ARDB.FamilyInstance` as valid.
- Implemented Previews on `Types.CurtainCell`.
- Fixed {% include ltr/comp.html uuid='fe427d04' %} component when managing Types.Panel elements.
- Improved {% include ltr/comp.html uuid='6723beb1' %} for walls that are member of a stacked wall.
- Now conversion from {% include ltr/comp.html uuid='15ad6bf9' %} to `Surface` gives a `Brep` correctly oriented.

{% include ltr/release-header.html title="v1.11 RC2" version="v1.11.8402.17166" pre_release=true time="01/03/2023" %}

- Added {% include ltr/comp.html uuid='2120c0fb' %} component.
- Moved {% include ltr/comp.html uuid='221e53a6' %} and {% include ltr/comp.html uuid='8ead987d' %} to the View panel.
- Added {% include ltr/comp.html uuid='cc7790a0' %} component.
- Now {% include ltr/comp.html uuid='39e42448' %} works across documents.
- Added {% include ltr/comp.html uuid='bfd4a970' %} component.
- Added {% include ltr/comp.html uuid='d296f72f' %} component.
- Added {% include ltr/comp.html uuid='6f5e3619' %} component.
- Added {% include ltr/comp.html uuid='1a137425' %} component.
- Added {% include ltr/comp.html uuid='61812ade' %} component.
- Added {% include ltr/comp.html uuid='71c06438' %} component.

{% include ltr/release-header.html title="v1.11 RC1" version="v1.11.8390.7504" pre_release=true time="12/20/2022" %}

- Added {% include ltr/comp.html uuid='09bd0aa8' %} component.
- Added {% include ltr/comp.html uuid='506d5c19' %} component. (Revit 2020)
- Fixed 'Element Geometry' to work with `FamilySymbol`.
- Added `ARDB.AppearanceAssetElement.ToRenderMaterial` extension method.

{% include ltr/release-header.html title="v1.10" version="v1.10.8390.5758" time="12/20/2022" %}

- Now 'Import 3DM' imports block geometry in families.
- Now Baked blocks name use `::` as a separator.
- Now 'Light Source' category ís hidden and transparent by default on bake.
- Added some null checking at {% include ltr/comp.html uuid='ad88cf11' %} component.
- Added some null checking at {% include ltr/comp.html uuid='0bfbda45' %} component.
- Now Raster Images do have preview in Grasshopper.
- Fix on `BoundingBoxXYZ.ToOutline` method.
- Now {% include ltr/comp.html uuid='de5e832b' %} uses view Phase.
- Now Bake try to use extrusions if the geometry do not have material per face and is closed.
- Fix for {% include ltr/comp.html uuid='a5c63076' %} component when managing mirrored elements.


{% include ltr/release-header.html title="v1.10 RC5" version="v1.10.8375.11132" pre_release=true time="12/06/2022" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.10 RC4" version="v1.10.8368.15363" pre_release=true time="11/29/2022" %}

- Now minimum Revit 2022 is 2022.1 to enable {% include ltr/comp.html uuid='bf1b9be9' %}
- Now Grasshopper previews persist between solutions. This means that only geometry that is being modified is redrawn and preview performance is much better.
- Added type icon column to 'Value Picker' component.
- Enabled content panning on 'Value Picker'.
- Added 'Edit Type…' context menu to {% include ltr/comp.html uuid='97dd546d' %} parameters.
- Added support for far-from-origin on DirectShape components.
- Added warning when a component receives geometry far-from-origin.
- Fixed pick linked element on other parameters than {% include ltr/comp.html uuid='ef607c2a' %}.
- Added support for geometry on linked files.
- Added support for closed curves and poly-lines to `ShapeEncoder`.
- Added conversion from {% include ltr/comp.html uuid='3238f8bc' %} and {% include ltr/comp.html uuid='2dc4b866' %} to {% include ltr/comp.html uuid='93bf1f61' %}.
- Added {% include ltr/comp.html uuid='a406c6a0' %} component.
- Fix for `NurbsCurve.ToCurve` when is periodic.
- Fixed a problem with ellipses on model-line creation components.
- Fix for `PolylineCurve.ToCurve` and `PolyCurve.ToCurve` extension methods when Rhino is not in feet.
- Fix for `HostObject` creation components when using closed curves.
- Fix on {% include ltr/comp.html uuid='78b02ae8' %} when profile is moved parallel to its plane.
- Fix on recent version of Revit for {% include ltr/comp.html uuid='d4593785' %} component. Now it does not take into account Grasshopper previews.
- Fix for {% include ltr/comp.html uuid='2a4a95d5' %}.
- Fix for {% include ltr/comp.html uuid='d3fb53d3' %} parameter.
- Fixed `Types.Category.BakeElement`, it was baking more than necessary.
- Improved `ARDB.Arc` conversion from Revit to Rhino.
- Fix for Line creation components when used with closed curves.
- Added `ARDB.View.GetClipBox` extension method. 
- Added `ARDB.View.GetOutlineFilter` extension method.
- Added `ARDB.Element.GetGeometryObjectFromReference` extension method that also returns the transform.
- Added `Selection.PickPoints` method.

{% include ltr/release-header.html title="v1.10 RC3" version="v1.10.8342.22296" pre_release=true time="11/08/2022" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.10 RC2" version="v1.10.8336.14469" pre_release=true time="10/28/2022" %}

- Now Grasshopper is not loaded at start up.
- Now changing element selection in Revit, expires 'Active Selection' in Grasshopper.

{% include ltr/release-header.html title="v1.10 RC1" version="v1.10.8326.27328" pre_release=true time="10/18/2022" %}

- Added context menu to 'Project Location' component.
- Now `Types.BasePoint` bake as a named construction-plane.
- New {% include ltr/comp.html uuid='2a4a95d5' %}
- New {% include ltr/comp.html uuid='3917adb2' %}
- New {% include ltr/comp.html uuid='c62d18a8' %}
- Added 'Built-In Failure Definitions' picker.

{% include ltr/release-header.html title="v1.9" version="v1.9.8326.25768" time="10/18/2022" %}

- Includes all changes under 1.9RC releases listed below
- New {% include ltr/comp.html uuid='1c1cc766' %}
- New {% include ltr/comp.html uuid='516b2771' %}
- New {% include ltr/comp.html uuid='e3d32938' %}
- New {% include ltr/comp.html uuid='cb3d697e' %}
- New {% include ltr/comp.html uuid='825d7ab3' %}
- New {% include ltr/comp.html uuid='4bfeb1ee' %}
- New {% include ltr/comp.html uuid='ace507e5' %}
- New {% include ltr/comp.html uuid='bf1b9be9' %}
- New {% include ltr/comp.html uuid='f2277265' %}
- Fix on {% include ltr/comp.html uuid='84ab6f3c' %} when definition parameter group is not a built-in one.
- Renamed 'Add LoftForm' component to {% include ltr/comp.html uuid='42631b6e' %}.
- Improved {% include ltr/comp.html uuid='d4593785' %} component, not it returns a more accurate 'Depth'.
- Added 'Computation Height' to {% include ltr/comp.html uuid='e996b34d' %} component.
- Fix for `Viewport.Location`.
- Implemented casting from `Viewport` to `ViewSheet` and `View`.
- Fix on Brep.TryGetExtrusion extension method.

{% include ltr/release-header.html title="v1.9 RC5" version="v1.9.8319.16206" pre_release=true time="10/11/2022" %}

- Fix for {% include ltr/comp.html uuid='c33cd128' %} component.
- Fix for {% include ltr/comp.html uuid='29618f71' %} when a null is used as input.
- Fix for {% include ltr/comp.html uuid='fad33c4b' %} when input 'Element' is null.
- Improved how {% include ltr/comp.html uuid='f4c12aa0' %} handles 'None' element.
- Fixed `GH.Gest.LoadEditor` to make Grasshopper window owned by Rhino. Even before it is ever been shown.
- Fix for 'Import 3DM' command when input model has No-Units.
- Fix for `Types.View.GenLevelId` when view is not based on a Level.
- Added `ARDB.ViewSection.GetElevationMarker` extension method.

{% include ltr/release-header.html title="v1.9 RC4" version="v1.9.8272.12095" pre_release=true time="08/30/2022" %}

- Fix for `ARDB.Mesh` encoding and decoding when the mesh is not closed.
- Fixed `ARDB.Mesh.ComputeCentroid` extension method.
- Added `ARDB.GeometryObject.GetBoundingBox` extension method.

{% include ltr/release-header.html title="v1.9 RC3" version="v1.9.8259.4446" pre_release=true time="08/16/2022" %}

- Fix for `BakeElements` when no attributes are provided.
- Fixed some blurry icons on HDPI screens.
- Fixed a `System.FormatException` when building linked scripts toolbar.

{% include ltr/release-header.html title="v1.9 RC2" version="v1.9.8256.24423" pre_release=true time="08/09/2022" %}

- Fix for {% include ltr/comp.html uuid='b3bcbf5b' %} component when receiving empty branches.
- Fixed {% include ltr/comp.html uuid='2beb60ba' %} component. It was moving two times the specified distance.
- Fixed 'Add Detail' component. It was moving two times the specified distance.
- Fixed value range validation on {% include ltr/comp.html uuid='8c5cd6fb' %} component.
- Added item `<None>` to some parameters context menu.
- Fix for the case Grasshopper had run with No-Units.
- Fix for `BakeElements` when no attributes are provided.
- Fix for `Types.View.DrawViewportWires` when showing an `ARDB.ImageView`.
- Fix for `ARDB.Instance.GetLocation` when instance is scaled.

{% include ltr/release-header.html title="v1.9 RC1" version="v1.9.8234.21248" pre_release=true time="07/18/2022" %}

- Added 'Host Sub Elements' component.
- Added 'Profile' input to 'Host Boundary Profile' component.
- Added 'Revit Version' component.
- Added 'Revit User' component.
- Added 'Document Tolerances' component.
- Added 'Default File Locations' component.
- Added 'Spatial Element Identity' component.
- Added 'Delete Workset' component (Revit 2023).
- Added 'Default 3D View' component.
- Added 'Assembly Origin' component.

{% include ltr/release-header.html title="v1.8" version="v1.8.8221.17917" time="07/18/2022" %}

- Includes all changes under 1.8RC releases listed below
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.8 RC4" version="v1.8.8221.17917" pre_release=true time="07/05/2022" %}

- Added {% include ltr/comp.html uuid='8ff70eef' %}
- Added {% include ltr/comp.html uuid='8ead987d' %} component
- Added {% include ltr/comp.html uuid='82a7462c' %} parameter
- Improved {% include ltr/comp.html uuid='ad88cf11' %} reuse logic
- Geometry conversion improvements
- Added warning to {% include ltr/comp.html uuid='134b7171' %} to avoid Shared Parameters Service invalid characters
- Now default document annotation scales are set to 1:100 by default


{% include ltr/release-header.html title="v1.8 RC3" version="v1.8.8215.8714" pre_release=true time="06/28/2022" %}

- Fixed several geometry bugs.
- Fixed structural framing creation on a family document.
- Fixed curved Beams when in a vertical plane.
- Fixed dimensioning components, when working with Detail Lines and Reference Planes.
- {% include ltr/comp.html uuid='8f1ee110' %} casting issue with enums
  [#613](https://github.com/mcneel/rhino.inside-revit/issues/613)

{% include ltr/release-header.html title="v1.8 RC2" version="v1.8.8207.14855" pre_release=true time="06/21/2022" %}

- Added {% include ltr/comp.html uuid='689d4059' %} component.
- Added {% include ltr/comp.html uuid='e6e4a2ee' %} component.
- Added {% include ltr/comp.html uuid='11424062' %} component.
- Added {% include ltr/comp.html uuid='fe258116' %} component.
- Added {% include ltr/comp.html uuid='2beb60ba' %} component.

{% include ltr/release-header.html title="v1.8 RC1" version="v1.8.8200.21840" pre_release=true time="06/14/2022" %}

- Now RiR requires Rhino v7.15.
- Moved {% include ltr/comp.html uuid='ff951e5d' %}, {% include ltr/comp.html uuid='3b95eff0' %}, {% include ltr/comp.html uuid='f3eb3a21' %} from Topology to Annotation tab.
- Added {% include ltr/comp.html uuid='49acc84c' %} component
- Added {% include ltr/comp.html uuid='ad88cf11' %} component
- Added {% include ltr/comp.html uuid='5a94ea62' %} component
- Added {% include ltr/comp.html uuid='0dbe67e7' %} component
- Added {% include ltr/comp.html uuid='df47c980' %} component
- Added {% include ltr/comp.html uuid='00c729f1' %} component
- Added {% include ltr/comp.html uuid='449b853b' %} component
- Added {% include ltr/comp.html uuid='493035d3' %} component
- Added {% include ltr/comp.html uuid='0644989d' %} component
- Added {% include ltr/comp.html uuid='495330db' %} component.
- Added {% include ltr/comp.html uuid='60be53c5' %} component.
- Added {% include ltr/comp.html uuid='a2adb132' %} componet.
- Added {% include ltr/comp.html uuid='4be42ec7' %} component.
- Added {% include ltr/comp.html uuid='d35eb2a7' %} parameter.

{% include ltr/release-header.html title="v1.7" version="v1.7.8194.7007" time="06/14/2022" %}

- Includes all changes under 1.7RC releases listed below
- Minor Fixes and Improvements
 
{% include ltr/release-header.html title="v1.7 RC3" version="v1.7.8188.17314" pre_release=true time="06/07/2022" %}

- Now {% include ltr/comp.html uuid='de5e832b' %}, {% include ltr/comp.html uuid='07711559' %} take a {% include ltr/comp.html uuid='2dc4b866' %} as input
- Added more error checking to {% include ltr/comp.html uuid='36842b86' %}
- Fixed casting from {% include ltr/comp.html uuid='1e6825b6' %}, {% include ltr/comp.html uuid='30473b1d' %}, and {% include ltr/comp.html uuid='66aaae96' %}  to {% include ltr/comp.html uuid='353ffb47' %}
- Implemented casting from {% include ltr/comp.html uuid='2dc4b866' %} to {% include ltr/comp.html uuid='01c853d8' %}
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.6 (Hotfix)" version="v1.6.8159.20547" time="05/04/2022" %}

- Fixed a bug extracting Level elements from a parameter ([RE Discourse: Null Revit Levels Resulting In Missing Components](https://discourse.mcneel.com/t/rir-version-1-68-null-revit-levels-resulting-in-missing-components/142125))

{% include ltr/release-header.html title="v1.7 RC2" version="v1.7.8158.15230" pre_release=true time="05/03/2022" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.7 RC1" version="v1.7.8151.26629" pre_release=true time="04/12/2022" %}

- Added context menu to generate a transparent background image
- Added _Crop Extents_, _Template_ and _Filter_ input to {% include ltr/comp.html uuid='4a962a0c' %}
- Exposed {% include ltr/comp.html uuid='972b6fbe' %} parameter
- Added {% include ltr/comp.html uuid='d4593785' %}, {% include ltr/comp.html uuid='45e7e88c' %} components
- Added {% include ltr/comp.html uuid='f3c35fb2' %} view parameter
- Added {% include ltr/comp.html uuid='bf2effd6' %} parameter and associated {% include ltr/comp.html uuid='51f9e551' %} component
- Added {% include ltr/comp.html uuid='1bde7f9f' %} parameter and associated {% include ltr/comp.html uuid='3896729d' %} component
- Added {% include ltr/comp.html uuid='33e34bd8' %} parameter and associated {% include ltr/comp.html uuid='782d0460' %} component
- Added {% include ltr/comp.html uuid='d0d3d169' %}, {% include ltr/comp.html uuid='0744d339' %}, {% include ltr/comp.html uuid='6effb4b8' %}, {% include ltr/comp.html uuid='ca537732' %} view parameters
- Added {% include ltr/comp.html uuid='a1878f3d' %} component
- Added {% include ltr/comp.html uuid='53fdab6f' %} selector
- Added {% include ltr/comp.html uuid='36842b86' %}
- Added {% include ltr/comp.html uuid='34d68cdc' %} component
- Added {% include ltr/comp.html uuid='34186815' %}, {% include ltr/comp.html uuid='dea31165' %} components
- Added {% include ltr/comp.html uuid='2ee360f3' %}, {% include ltr/comp.html uuid='de5e832b' %}, {% include ltr/comp.html uuid='07711559' %} components
- Added {% include ltr/comp.html uuid='d1940eb3' %}, {% include ltr/comp.html uuid='5ddcb816' %}, {% include ltr/comp.html uuid='a1ccf034' %} components
- Added {% include ltr/comp.html uuid='ff951e5d' %}, {% include ltr/comp.html uuid='3b95eff0' %}, {% include ltr/comp.html uuid='f3eb3a21' %} components

{% include ltr/release-header.html title="v1.6" version="v1.6.8151.18094" time="04/26/2022" %}

- Added {% include ltr/comp.html uuid='54c795d0' %} and associated {% include ltr/comp.html uuid='63f4a581' %} parameter.
- Added {% include ltr/comp.html uuid='01c853d8' %} and associated {% include ltr/comp.html uuid='4150d40a' %} parameter.
- Updated {% include ltr/comp.html uuid='657811b7' %} to accept {% include ltr/comp.html uuid='4150d40a' %} parametes as **Base** and **Top** inputs as well and thus removed the optional offset parameters
- Includes all changes under 1.6RC releases listed below
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.6 RC5" version="v1.6.8134.6334" pre_release=true time="04/12/2022" %}

- Added support for Revit 2023
- Fixed conversion from {% include ltr/comp.html uuid='15ad6bf9' %} to Surface when slant angle is negative
- Fixed a bug on {% include ltr/comp.html uuid='0f251f87' %}
  [#507](https://github.com/mcneel/rhino.inside-revit/issues/507)
- Updated Revit download link when loaded in an unsupported version

{% include ltr/release-header.html title="v1.6 RC4" version="v1.6.8124.18574" pre_release=true time="04/05/2022" %}

- Fixed {% include ltr/comp.html uuid='cec2b3df-' %} component when Tracking mode is set to _Update_.
- Added _Elevation_ input to {% include ltr/comp.html uuid='cec2b3df-' %}.
- Fixed a bug on _Import 3DM_ command when importing polylines on a family document.

{% include ltr/release-header.html title="v1.6 RC3" version="v1.6.8123.20268" pre_release=true time="03/29/2022" %}

- Fixed {% include ltr/comp.html uuid='4434c470-' %} when inverted
- {{ site.terms.rir }} no longer shows an error window when failing to set shortcut for Grasshopper button
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.6 RC2" version="v1.6.8119.11754" pre_release=true time="03/22/2022" %}

- Fixed a problem on `ARDB.XYZ.PerpVector` when tolerance is too small.
- Now _Convert_ context menu is a sorted by parameter category.
- Added casting from {% include ltr/comp.html uuid='f3ea4a9c-' %} to {% include ltr/comp.html uuid='5c073f7d-' %}, {% include ltr/comp.html uuid='353ffb47-' %}, {% include ltr/comp.html uuid='3238f8bc-' %} and {% include ltr/comp.html uuid='97dd546d-' %}.
- Now all _Add_ components create elements on the last document Phase.
- Now all _Add_ components create elements with **Enable Analytical Model** off by default.
- Fix for {% include ltr/comp.html uuid='26411aa6-' %} now it always returns a non joined element.
- Fixed {% include ltr/comp.html uuid='2c374e6d-' %} component on shared-parameters.
- Fix for {% include ltr/comp.html uuid='dcc82eca-' %} when managing structural elements.
- Fixed {% include ltr/comp.html uuid='dcc82eca-' %} when workiong with structural beams and structural columns.
- Now `HostObject.Location` returns a plane centered on the profile curves.
- Now `Opening.Location` returns a plane centered on the profile curves.
- Implemented casting `SpatialElement` to `Surface`
- Added `Types.Railing`. This enables {% include ltr/comp.html uuid='dcc82eca-' %} component on railings.
- Implemented _Type_ output on {% include ltr/comp.html uuid='3bde5890-' %} for built-in parameters.
- Now Grasshopper preview status is persistent between sessions.

{% include ltr/release-header.html title="v1.6 RC1" version="v1.6.8102.16819" pre_release=true time="08/03/2022" %}

- Added {% include ltr/comp.html uuid='18d46e90-' %} parameter
- Added {% include ltr/comp.html uuid='657811b7-' %} component
- Added {% include ltr/comp.html uuid='c86ed84c-' %} component
- Added {% include ltr/comp.html uuid='e76b0f6b-' %} component
- Added {% include ltr/comp.html uuid='3848c899-' %} component
- Added {% include ltr/comp.html uuid='8a2da785-' %} component
- Added {% include ltr/comp.html uuid='f68f96ec-' %} component
- Added {% include ltr/comp.html uuid='0ea8d61a-' %} component
- Added **Is Subcategory** input to {% include ltr/comp.html uuid='d794361e-' %} component
- Added **Is Subcategory** input to {% include ltr/comp.html uuid='d150e40e-' %} component
- Now {% include ltr/comp.html uuid='70ccf7a6-' %} component returns openings on floor, ceiling and roofs
- Fixed {% include ltr/comp.html uuid='37a8c46f-' %} component. It was failing to update 'Unconnected Height' when the wall is constrained at the top
- Fixed {% include ltr/comp.html uuid='df634530-' %} and {% include ltr/comp.html uuid='97e9c6bb-' %}. Now both component filter out non Model groups or types
- Now first access to `RhinoCommon.dll` loads Rhino

{% include ltr/release-header.html title="v1.5" version="v1.5.8101.24584" time="03/08/2022" %}

- Includes all changes under 1.5RC releases listed below
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.5 RC4" version="v1.5.8088.12286" pre_release=true time="02/22/2022" %}

- Fixed `AssemblyInstance.Location.set`
- Implemented `AssemblyInstance.BoundingBox`
- Now {% include ltr/comp.html uuid='ef607c2a-' %} displays the _BoundingBox_ by default.
- Fixed `Types.BasePoint.ClippingBox` when the element is not available.

{% include ltr/release-header.html title="v1.5 RC3" version="v1.5.8082.16096" pre_release=true time="02/16/2022" %}

- {% include ltr/comp.html uuid='b6349dda-' %} now has "Open Design Options…" context menu option.
- Added {% include ltr/comp.html uuid='8621421d-' %} component.
- Added _Error Mode_ context menu: Now all named elements creation components use preexisting elements when working in error-mode 'Continue'.

- Fix on {% include ltr/comp.html uuid='26411aa6-' %}, now it checks if family is _Structural Framing_ before enabling-disabling joins.
- Fix on the {% include ltr/comp.html uuid='26411aa6-' %} component when working in tracking-mode 'Reuse'.
- Fixed {% include ltr/comp.html uuid='f4c12aa0-' %} component when managing unnamed elements.
- Fixed a bug on selection _Value Set_ when there are no elements on the list.
- Fixed a bug when user Undo a grasshopper object creation operation on components that track elements.
- Fixed a problem when Revit is working in _US Survey Feet_ units.

{% include ltr/release-header.html title="v1.5 RC2" version="v1.5.8067.24664" pre_release=true time="02/01/2022" %}

- Fixed unit conversions on {% include ltr/comp.html uuid='8a162ee6-' %} component
- Added `Types.SectionBox` type.
- Fixed [#531](https://github.com/mcneel/rhino.inside-revit/issues/531).

{% include ltr/release-header.html title="v1.5 RC1" version="v1.5.8056.10037" pre_release=true time="01/21/2022" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.4 Stable" version="v1.4.8053.19650" time="21/01/2022" %}

- Includes all changes under 1.4RC releases listed below
- New {% include ltr/comp.html uuid='716903d0-' %} component

{% include ltr/release-header.html title="v1.4 RC6" version="v1.4.8048.43002" pre_release=true time="01/18/2022" %}

- Added 'Link' output to 'Document Links' component.
- Renamed 'Document Links' to {% include ltr/comp.html uuid='ebccfdd8-' %}.
- Renamed 'Binding' to 'Scope' in parameter components.
- Updated 'Element Dependents' to return original 'Elements' and also 'Referentials'.
- Fixed {% include ltr/comp.html uuid='b3bcbf5b-' %} and {% include ltr/comp.html uuid='8b85b1fb-' %}: Now both have an option _Expand Dependents_ in the context menu to extract dependent elements geometry. Outputs are grafted accordingly.
  [#509](https://github.com/mcneel/rhino.inside-revit/issues/509).
- Updated some 'Query' component input parameters names to match Revit parameter name.

{% include ltr/release-header.html title="v1.4 RC5" version="v1.4.8007.15883" pre_release=true time="12/07/2021" %}

- Continued work on {{ site.terms.rir }} API
- {% include ltr/comp.html uuid='3a5f6af7-' %} defaults to temp folder when **Folder** input is not provided
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.4 RC4" version="v1.4.8004.19290" pre_release=true time="11/30/2021" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.4 RC3" version="v1.4.7997.16502" pre_release=true time="11/23/2021" %}

- A major work in 1.4 is cleaning up and preparing the {{ site.terms.rir }} API. In this release we added an option to see the documentation for current state of the API in python editor (`EditPythonScript` command)

![]({{ "/static/images/release_notes/pythoneditor-docs.png" | prepend: site.baseurl }})

- Fixed _Select All_ and _Invert Selection_ on **Value Set Picker**
- Fixed {% include ltr/comp.html uuid='79daea3a-' %} when _Categories_ input contains nulls
- Fixed {% include ltr/comp.html uuid='97d71aa8-' %} component when managing nulls

{% include ltr/release-header.html title="v1.4 RC2" version="v1.4.7989.18759" pre_release=true time="11/16/2021" %}

- Fix for `DB.InternalOrigin` on Revit 2020.2
- Fixed a crash, when an element can't be deleted from the context menu
- Fixed {% include ltr/comp.html uuid='b3bcbf5b-' %} and {% include ltr/comp.html uuid='8b85b1fb-' %}: Now both have an option **Expand Dependents** in the context menu to extract dependent elements geometry. Outputs are grafted accordingly
- Added {% include ltr/comp.html uuid='8fad6039-' %}
- Added back {% include ltr/comp.html uuid='754c40d7-' %}
- Added back {% include ltr/comp.html uuid='61f75de1-' %}

{% include ltr/release-header.html title="v1.4 RC1" version="v1.4.7983.32601" pre_release=true time="11/09/2021" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.3 Stable" version="v1.3.7983.15227" time="10/12/2021" %}

- Includes all changes under 1.2RC releases listed below
- Fixed {% include ltr/comp.html uuid='8b85b1fb-' %} was returning invisible elements geometry

{% include ltr/release-header.html title="v1.3 RC2" version="v1.3.7976.20198" pre_release=true time="10/27/2021" %}

- Now _Open Viewport_ command needs CTRL pressed to synchronize camera and workplane
- {% include ltr/comp.html uuid='2dc4b866' %} now converts to _Plane_, _Box_, _Surface_, and _Material_
- {% include ltr/comp.html uuid='4a962a0c' %} defaults to a temp folder when no _Folder_ is provided

{% include ltr/release-header.html title="v1.3 RC1" version="v1.3.7970.19099" pre_release=true time="10/27/2021" %}

- Component Changes
  - New {% include ltr/comp.html uuid='7fcea93d-' extended=true %}
  - New {% include ltr/comp.html uuid='a39bbdf2-' extended=true %}
  - New {% include ltr/comp.html uuid='b344f1c1-' extended=true %}
  - ZUI components e.g. {% include ltr/comp.html uuid='fad33c4b-' %} now have _Show all parameters_ and _Hide unconnected parameters_ on context menu
- Performance
  - Grasshopper now caches converted geometries from Rhino to Revit. This improves performance between Grasshopper runs, or when the same geometry is being converted many times.
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.2 Stable" version="v1.2.7955.32919" time="10/12/2021" %}

- Includes all changes under 1.2RC releases listed below
- Component Changes

  - {% include ltr/comp.html uuid='704d9c1b-' %} component does not have a Title Block input anymore
  - Added {% include ltr/comp.html uuid='f2f3d866-' extended=true %}
  - Added {% include ltr/comp.html uuid='16f18871-' extended=true %}
  - {% include ltr/comp.html uuid='f6b99fe2-' %} now shows pretty names for view families sorted alphabetically
  - {% include ltr/comp.html uuid='f737745f-' %} replaces previous {% include ltr/comp_old.html title='Query Title Block Types' %} component
  - Removed {% include ltr/comp_old.html title='Analyze Sheet' %} component
  - {% include ltr/comp.html uuid='6915b697-' %} component does not require _Category_ anymore

- Issues

  - Fixed Non-C2-BREP edge conversion when knots are below tolerance. 
    [#382](https://github.com/mcneel/rhino.inside-revit/issues/382).

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.2 RC4" version="v1.2.7948.6892 " pre_release=true time="10/05/2021" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.2 RC3" version="v1.2.7937.23994" pre_release=true time="09/28/2021" %}

- Improved 'Host Faces' component. Now skips invalid faces and avoids exceptions to be faster
- Fixes and improvements on Ellipse conversion routines.

{% include ltr/release-header.html title="v1.2 RC2" version="v1.2.7934.7099" pre_release=true time="09/21/2021" %}

- Corrected component name spelling {% include ltr/comp.html uuid='ff0f49ca-' %}
- Removed {% include ltr/comp_old.html title='Assembly Views' %} component
- Convert {% include ltr/comp.html uuid='cadf5fbb-' %} component to pass-through
- Revised {% include ltr/comp.html uuid='704d9c1b-' %} parameters to match Revit
- Added 'Assembly' parameter to {% include ltr/comp.html uuid='b0440885-' %} and {% include ltr/comp.html uuid='df691659-' %}

{% include ltr/release-header.html title="v1.2 RC1" version="v1.2.7927.28069" pre_release=true time="09/14/2021" %}

- New View Components!

  - {% include ltr/comp.html uuid='97c8cb27-' extended=true %}
  - {% include ltr/comp_old.html title='Query Title Block Types' %}
  - {% include ltr/comp.html uuid='cadf5fbb-' extended=true %}
  - {% include ltr/comp.html uuid='704d9c1b-' extended=true %}
  - {% include ltr/comp_old.html title='Analyze Sheet' %}
  - {% include ltr/comp.html uuid='f6b99fe2-' extended=true %}

- New Assembly Components!

  - {% include ltr/comp.html uuid='fd5b45c3-' extended=true %}
  - {% include ltr/comp_old.html title='Assembly Views' %}
  - {% include ltr/comp.html uuid='6915b697-' extended=true %}
  - {% include ltr/comp.html uuid='26feb2e9-' extended=true %}
  - {% include ltr/comp.html uuid='33ead71b-' extended=true %}
  - {% include ltr/comp.html uuid='ff0f49ca-' extended=true %}

- New Workset Components!

  - {% include ltr/comp.html uuid='5c073f7d-' extended=true %}
  - {% include ltr/comp.html uuid='aa467c94-' extended=true %}
  - {% include ltr/comp.html uuid='b441ba8c-' extended=true %}
  - {% include ltr/comp.html uuid='c33cd128-' extended=true %}
  - {% include ltr/comp.html uuid='311316ba-' extended=true %}
  - {% include ltr/comp.html uuid='3380c493-' extended=true %}

- New Phase Components!

  - {% include ltr/comp.html uuid='353ffb47-' extended=true %}
  - {% include ltr/comp.html uuid='3ba4524a-' extended=true %}
  - {% include ltr/comp.html uuid='91e4d3e1-' extended=true %}
  - {% include ltr/comp.html uuid='805c21ee-' extended=true %}

- Merged [PR #486](https://github.com/mcneel/rhino.inside-revit/pull/486)

{% include ltr/release-header.html title="v1.1 Stable" version="v1.1.7927.27937" time="09/14/2021" %}

- Includes all changes under 1.1RC releases listed below
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.1 RC4" version="v1.1.7916.15665" pre_release=true time="09/07/2021" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.1 RC3" version="v1.1.7912.2443" pre_release=true time="08/30/2021" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.1 RC2" version="v1.1.7906.21197" pre_release=true time="08/24/2021" %}

- 👉 Updated _Tracking Mode_ context wording.
  - Supersede -> **Enabled : Replace**
  - Reconstruct -> **Enabled : Update**
- 👉 Performance Improvements:
  - Disabled expiring objects when a Revit document change comes from Grasshopper itself
  - Improved _Element Type_ component speed when updating several elements
  - Improved _Add Component (Location)_ speed when used in _Supersede_ mode
- 👉 Renamed:
  - _Query Types_ output from "E" to "T"
  - _Add ModelLine_ -> _Add Model Line_
  - _Add SketchPlane_ -> _Add Sketch Plane_
  - _Thermal Asset Type_ -> _Thermal Asset Class_
  - _Physical Asset Type_ -> _Physical Asset Class_
- Now parameter rule components guess the parameter type from the value on non built-in parameters
- Parameter setter does not change the value when the value to set is already the same
- Fixed the way we solve BuiltIn parameters on family documents
- Added _Behavior_ input to Modify Physical and Thermal asset
- Removed some default values on _Query Views_ that add some confusion
- Deleting an open view is not allowed. We show an message with less information in that case
- Fix on _Add View3D_ when managing a locked view
- Fix on _Duplicate Type_ when managing types without Category like `DB.ViewFamilyType`
- Only _Graphical Element_ should be pinned
- Closed [Issue #410](https://github.com/mcneel/rhino.inside-revit/issues/410)
- Merged [PR #475](https://github.com/mcneel/rhino.inside-revit/issues/475)

{% include ltr/release-header.html title="v1.0 Stable / v1.1 RC1" version="1.0.7894.17525 / v1.1.7894.19956" time="08/12/2021" %}

Finally!! 🎉 See [announcement post here](https://discourse.mcneel.com/t/rhino-inside-revit-version-1-0-released/128738?u=eirannejad)
