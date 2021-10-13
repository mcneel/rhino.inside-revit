---
title: Release Notes
order: 40
group: Deployment & Configs
---

<!-- most recent release should be on top -->
<!-- most recent release should be on top -->
{% include ltr/release-header.html title="v1.2 Stable" version="v1.2.7955.31573" time="10/12/2021" %}

- Includes all changes under 1.2RC releases listed below
- {% include ltr/comp.html uuid='704d9c1b-' %} component does not have a Title Block input anymore
- Added {% include ltr/comp.html uuid='f2f3d866-' extended=true %}
- Added {% include ltr/comp.html uuid='16f18871-' extended=true %}
- {% include ltr/comp.html uuid='f6b99fe2-' %} now shows pretty names for view families sorted alphabetically
- {% include ltr/comp.html uuid='f737745f-' %} replaces previous {% include ltr/comp_old.html title='Query Title Block Types' %} component
- Removed {% include ltr/comp_old.html title='Analyze Sheet' %} component
- {% include ltr/comp.html uuid='6915b697-' %} component does not require *Category* anymore
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