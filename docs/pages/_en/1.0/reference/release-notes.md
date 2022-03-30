---
title: Release Notes
order: 40
group: Deployment & Configs
---

<!-- most recent release should be on top -->
{% include ltr/release-header.html title="v1.6 RC3" version="v1.6.8119.11754" pre_release=true time="03/29/2022" %}
- Fixed {% include ltr/comp.html uuid='4434c470-' %} when inverted
- {{ site.terms.rir }} no longer shows an error window when failing to set shortcut for Grasshopper button
- Minor Fixes and Improvements


{% include ltr/release-header.html title="v1.6 RC2" version="v1.6.8119.11754" pre_release=true time="03/22/2022" %}

- Fixed a problem on `ARDB.XYZ.PerpVector` when tolerance is too small.
- Now *Convert* context menu is a sorted by parameter category.
- Added casting from {% include ltr/comp.html uuid='f3ea4a9c-' %} to {% include ltr/comp.html uuid='5c073f7d-' %}, {% include ltr/comp.html uuid='353ffb47-' %}, {% include ltr/comp.html uuid='3238f8bc-' %} and {% include ltr/comp.html uuid='97dd546d-' %}.
- Now all *Add* components create elements on the last document Phase.
- Now all *Add* components create elements with **Enable Analytical Model** off by default.
- Fix for {% include ltr/comp.html uuid='26411aa6-' %} now it always returns a non joined element.
- Fixed {% include ltr/comp.html uuid='2c374e6d-' %} component on shared-parameters.
- Fix for {% include ltr/comp.html uuid='dcc82eca-' %} when managing structural elements.
- Fixed {% include ltr/comp.html uuid='dcc82eca-' %} when workiong with structural beams and structural columns.
- Now `HostObject.Location` returns a plane centered on the profile curves.
- Now `Opening.Location` returns a plane centered on the profile curves.
- Implemented casting `SpatialElement` to `Surface`
- Added `Types.Railing`. This enables {% include ltr/comp.html uuid='dcc82eca-' %} component on railings.
- Implemented *Type* output on {% include ltr/comp.html uuid='3bde5890-' %} for built-in parameters.
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
- Fixed {% include ltr/comp.html uuid='df634530-' %}  and {% include ltr/comp.html uuid='97e9c6bb-' %}. Now both component filter out non Model groups or types
- Now first access to `RhinoCommon.dll` loads Rhino


{% include ltr/release-header.html title="v1.5" version="v1.5.8101.24584" time="08/03/2022" %}

- Includes all changes under 1.5RC releases listed below
- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.5 RC4" version="v1.5.8088.12286" pre_release=true time="22/02/2022" %}

- Fixed `AssemblyInstance.Location.set`
- Implemented `AssemblyInstance.BoundingBox`
- Now {% include ltr/comp.html uuid='ef607c2a-' %} displays the *BoundingBox* by default.
- Fixed `Types.BasePoint.ClippingBox` when the element is not available.

{% include ltr/release-header.html title="v1.5 RC3" version="v1.5.8082.16096" pre_release=true time="16/02/2022" %}

- {% include ltr/comp.html uuid='b6349dda-' %} now has "Open Design Options…" context menu option.
- Added {% include ltr/comp.html uuid='8621421d-' %} component.
- Added *Error Mode* context menu: Now all named elements creation components use preexisting elements when working in error-mode 'Continue'.

- Fix on {% include ltr/comp.html uuid='26411aa6-' %}, now it checks if family is *Structural Framing* before enabling-disabling joins.
- Fix on the {% include ltr/comp.html uuid='26411aa6-' %} component when working in tracking-mode 'Reuse'.
- Fixed {% include ltr/comp.html uuid='f4c12aa0-' %} component when managing unnamed elements.
- Fixed a bug on selection *Value Set* when there are no elements on the list.
- Fixed a bug when user Undo a grasshopper object creation operation on components that track elements.
- Fixed a problem when Revit is working in *US Survey Feet* units.

{% include ltr/release-header.html title="v1.5 RC2" version="v1.5.8067.24664" pre_release=true time="01/02/2022" %}

- Fixed unit conversions on {% include ltr/comp.html uuid='8a162ee6-' %} component
- Added `Types.SectionBox` type.
- Fixed [#531](https://github.com/mcneel/rhino.inside-revit/issues/531).

{% include ltr/release-header.html title="v1.5 RC1" version="v1.5.8056.10037" pre_release=true time="21/01/2022" %}

- Minor Fixes and Improvements

{% include ltr/release-header.html title="v1.4 Stable" version="v1.4.8053.19650" time="21/01/2022" %}

- Includes all changes under 1.4RC releases listed below
- New {% include ltr/comp.html uuid='716903d0-' %} component

{% include ltr/release-header.html title="v1.4 RC6" version="v1.4.8048.43002" pre_release=true time="18/01/2022" %}

- Added 'Link' output to 'Document Links' component.
- Renamed 'Document Links' to {% include ltr/comp.html uuid='ebccfdd8-' %}.
- Renamed 'Binding' to 'Scope' in parameter components.
- Updated 'Element Dependents' to return original 'Elements' and also 'Referentials'.
- Fixed {% include ltr/comp.html uuid='b3bcbf5b-' %} and {% include ltr/comp.html uuid='8b85b1fb-' %}: Now both have an option *Expand Dependents* in the context menu to extract dependent elements geometry. Outputs are grafted accordingly. Closes #509.
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

- Fixed *Select All* and *Invert Selection* on **Value Set Picker**
- Fixed {% include ltr/comp.html uuid='79daea3a-' %} when *Categories* input contains nulls
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

- Now *Open Viewport* command needs CTRL pressed to synchronize camera and workplane
- {% include ltr/comp.html uuid='2dc4b866' %} now converts to *Plane*, *Box*, *Surface*, and *Material*
- {% include ltr/comp.html uuid='4a962a0c' %} defaults to a temp folder when no *Folder* is provided

{% include ltr/release-header.html title="v1.3 RC1" version="v1.3.7970.19099" pre_release=true time="10/27/2021" %}

- Component Changes
  - New {% include ltr/comp.html uuid='7fcea93d-' extended=true %}
  - New {% include ltr/comp.html uuid='a39bbdf2-' extended=true %}
  - New {% include ltr/comp.html uuid='b344f1c1-' extended=true %}
  - ZUI components e.g. {% include ltr/comp.html uuid='fad33c4b-' %} now have *Show all parameters* and *Hide unconnected parameters* on context menu
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
  - {% include ltr/comp.html uuid='6915b697-' %} component does not require *Category* anymore

- Issues
  - Fixed Non-C2-BREP edge conversion when knots are below tolerance. RE [#382](https://github.com/mcneel/rhino.inside-revit/issues/382).

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

- 👉 Updated *Tracking Mode* context wording.
  - Supersede -> **Enabled : Replace**
  - Reconstruct -> **Enabled : Update**
- 👉 Performance Improvements:
  - Disabled expiring objects when a Revit document change comes from Grasshopper itself
  - Improved *Element Type* component speed when updating several elements
  - Improved *Add Component (Location)* speed when used in *Supersede* mode
- 👉 Renamed:
  - *Query Types* output from "E" to "T"
  - *Add ModelLine* -> *Add Model Line*
  - *Add SketchPlane* -> *Add Sketch Plane*
  - *Thermal Asset Type* -> *Thermal Asset Class*
  - *Physical Asset Type* -> *Physical Asset Class*
- Now parameter rule components guess the parameter type from the value on non built-in parameters
- Parameter setter does not change the value when the value to set is already the same
- Fixed the way we solve BuiltIn parameters on family documents
- Added *Behavior* input to Modify Physical and Thermal asset
- Removed some default values on *Query Views* that add some confusion
- Deleting an open view is not allowed. We show an message with less information in that case
- Fix on *Add View3D* when managing a locked view
- Fix on *Duplicate Type* when managing types without Category like `DB.ViewFamilyType`
- Only *Graphical Element* should be pinned
- Closed [Issue #410](https://github.com/mcneel/rhino.inside-revit/issues/410)
- Merged [PR #475](https://github.com/mcneel/rhino.inside-revit/issues/475)


{% include ltr/release-header.html title="v1.0 Stable / v1.1 RC1" version="1.0.7894.17525 / v1.1.7894.19956" time="08/12/2021" %}

Finally!! 🎉 See [announcement post here](https://discourse.mcneel.com/t/rhino-inside-revit-version-1-0-released/128738?u=eirannejad)
