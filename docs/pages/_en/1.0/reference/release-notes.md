---
title: Release Notes
order: 40
group: Deployment & Configs
---

<!-- list the changes in WIP branch -->

{% capture rc_release_notes %}

### WIP

- Added 'Host Sub Elements' component.
- Added 'Profile' input to 'Host Boundary Profile' component.

### RC 

- Added 'Tag By Category' component.
- Added 'Multi-Category Tag' component.
- Added 'Material Tag' component.

{% endcapture %}
{% include ltr/release_header_next.html title="Upcoming Changes" note=rc_release_notes %}

{% include ltr/release-header.html title="v1.8 RC1" version="v1.8.8200.21840" pre_release=true time="06/14/2022" %}

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
- Added {% include ltr/comp.html uuid='0644989d' %} component
- Added 'Element Subcategory' component.
- Added 'Curve Line Style' component.
- Added 'Add Reference Line' componet.
- Added 'Add Reference Plane' component.
- Added 'Reference Plane' parameter.

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
- Fixed a bug on {% include ltr/comp.html uuid='0f251f87' %} [#507](https://github.com/mcneel/rhino.inside-revit/issues/507)
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
- Fixed {% include ltr/comp.html uuid='b3bcbf5b-' %} and {% include ltr/comp.html uuid='8b85b1fb-' %}: Now both have an option _Expand Dependents_ in the context menu to extract dependent elements geometry. Outputs are grafted accordingly. Closes #509.
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
