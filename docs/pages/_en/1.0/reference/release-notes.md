---
title: Release Notes
order: 40
group: Deployment & Configs
---

<!-- most recent release should be on top -->
{% include ltr/release-header.html title="v1.1 RC2" version="v1.1.7906.21197" time="08/24/2021" %}

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