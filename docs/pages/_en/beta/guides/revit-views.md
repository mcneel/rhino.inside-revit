---
title: Views
order: 62
---

## Querying Views

{% capture api_note %}
In Revit API, Views of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.View' title='DB.View' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

You can use the combination of *Element.ClassFilter*, and *Document.ElementTypes* components to collect views:

![]({{ "/static/images/guides/revit-views01.png" | prepend: site.baseurl }})

Notice that the *Element.ClassFilter* requires the full name of the API class as string input e.g. `Autodesk.Revit.DB.View`

{% include ltr/issue_note.html issue_id='142' note='Add Views to category pickers so an Element.CategoryFilter can be used to list views' %}

## Querying Views by System Family

{% capture api_note %}
In Revit API, View System Families are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewFamily' title='DB.ViewFamily' %} enumeration. However, there is a `ViewType` property on the `DB.View` instances that also provides the System Family of the view instance. The enumeration for this property is {% include api_type.html type='Autodesk.Revit.DB.ViewType' title='DB.ViewType' %}. `DB.ViewFamily` and `DB.ViewType` seems to have been created with the same goal in mind. The components shared here use the `DB.ViewFamily` to list the views by system family, however, the same results might be achieved using `DB.ViewType`
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To collect views of a certain system family in a model, use a combination of *View System Families* and *Views By System Family* components shared here.

![]({{ "/static/images/guides/revit-views01a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/View System Families.ghuser' name='View System Families' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Views By System Family.ghuser' name='Views By System Family' %}

## Querying View Types

{% capture api_note %}
In Revit API, View Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewFamilyType' title='DB.ViewFamilyType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To collect a list of view types in a model associated with a view system family, use the *View Types* component shared here.

![]({{ "/static/images/guides/revit-views01b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/View Types.ghuser' name='View Types' %}

## Find Specific View Type

To find a specific view type in a model, use the *Find View Type* component shared here.

![]({{ "/static/images/guides/revit-views01c.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find View Type.ghuser' name='Find View Type' %}

## Querying Views by Type

To collect views of a certain type, use a combination of *Find View Type* and *Views By Type* components shared here.

![]({{ "/static/images/guides/revit-views01d.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Views By Type.ghuser' name='Views By Type' %}

## Finding Specific Views

To find a view by name or by id in the active document, use the *Find View* component shared here.

![]({{ "/static/images/guides/revit-views02.png" | prepend: site.baseurl }})

As shown above, the *Find View* component, can search for a view by its name (N) or Title on Sheet (TOS).

{% include ltr/download_comp.html archive='/static/ghnodes/Find View.ghuser' name='Find View' %}

## Reading View Properties

Use the *Element.Decompose* component to inspect the properties of a view object.

![]({{ "/static/images/guides/revit-views03.png" | prepend: site.baseurl }})

## View Range

{% capture api_note %}
In Revit API, View Ranges are represented by the {% include api_type.html type='Autodesk.Revit.DB.PlanViewRange' title='DB.PlanViewRange' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To read the view range property of a view, use the *Get View Range* component shared here.

![]({{ "/static/images/guides/revit-views03a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get View Range.ghuser' name='Get View Range' %}

## Collecting Displayed Elements

To collect all the elements owned by a view, use the *Element.OwnerViewFilter* component, passed to the *Document.Elements* as shown below. Keep in mind that the 3D geometry that is usually shown in model views are not "Owned" by that view. All 2d elements e.g. Detail items, detail lines, ... are owned by the view they have created on.

![]({{ "/static/images/guides/revit-views04.png" | prepend: site.baseurl }})

You can use the *Element.SelectableInViewFilter* component to only list the selectable elements on a view.

![]({{ "/static/images/guides/revit-views05.png" | prepend: site.baseurl }})

## Getting V/G Overrides

To get the Visibility/Graphics overrides for an element on a specific view, use the shared *Get VG* component.

![]({{ "/static/images/guides/revit-views06.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Override VG.ghuser' name='Get Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Destruct).ghuser' name='VG (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Destruct).ghuser' name='Line VG Settings (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Destruct).ghuser' name='Surface VG Settings (Destruct)' %}

## Setting V/G Overrides

To set the Visibility/Graphics overrides for an element on a specific view, use the shared *Set VG* component.

![]({{ "/static/images/guides/revit-views07.png" | prepend: site.baseurl }})

See [Styles and Patterns]({{ site.baseurl }}{% link _en/beta/guides/revit-styles.md %}) on how to use the *Find Line Pattern* and *Find Fill Pattern* custom components. Here is an example of running the example above on a series of walls in a 3D view:

![]({{ "/static/images/guides/revit-views08.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Set Override VG.ghuser' name='Set Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Construct).ghuser' name='VG (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Construct).ghuser' name='Line VG Settings (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Construct).ghuser' name='Surface VG Settings (Construct)' %}

## Creating New Views

### Floor Plans

```python
level = get_view_level()
view_fam_typeid = \
    doc.GetDefaultElementTypeId(
        DB.ElementTypeGroup.ViewTypeFloorPlan
        )
new_dest_view = \
    DB.ViewPlan.Create(doc, view_fam_typeid, level.Id)
```

### Reflected Ceiling Plans

```python
    level = get_view_level()
    view_fam_typeid = \
        doc.GetDefaultElementTypeId(
            DB.ElementTypeGroup.ViewTypeCeilingPlan
        )
    new_dest_view = \
        DB.ViewPlan.Create(doc, view_fam_typeid, level.Id)
```

### Elevations

```python
view_fam_typeid = \
    doc.GetDefaultElementTypeId(
        DB.ElementTypeGroup.ViewTypeElevation
        )
elev_marker = \
    DB.ElevationMarker.CreateElevationMarker(
        doc,
        view_fam_typeid,
        DB.XYZ(0, 0, 0),
        1)
default_floor_plan = find_first_floorplan()
new_dest_view = \
    elev_marker.CreateElevation(doc, default_floor_plan.Id, 0)
scale_param = new_dest_view.Parameter[
    DB.BuiltInParameter.SECTION_COARSER_SCALE_PULLDOWN_IMPERIAL
    ]
scale_param.Set(1)
```
### Sections

```python
view_fam_typeid = \
    doc.GetDefaultElementTypeId(
        DB.ElementTypeGroup.ViewTypeSection
        )
view_direction = DB.BoundingBoxXYZ()
trans_identity = DB.Transform.Identity
trans_identity.BasisX = -DB.XYZ.BasisX    # x direction
trans_identity.BasisY = DB.XYZ.BasisZ    # up direction
trans_identity.BasisZ = DB.XYZ.BasisY    # view direction
view_direction.Transform = trans_identity
new_dest_view = \
    DB.ViewSection.CreateSection(doc,
                                    view_fam_typeid,
                                    view_direction)
scale_param = new_dest_view.Parameter[
    DB.BuiltInParameter.SECTION_COARSER_SCALE_PULLDOWN_IMPERIAL
    ]
scale_param.Set(1)
```

### Area Plans

```python
    level = get_view_level()
    areaSchemeId = ?
    new_dest_view = \
        DB.ViewPlan.CreateAreaPlan(doc, areaSchemeId, level.Id)
```

### Legends

```python
def find_first_legend(doc=None):
    doc = doc or HOST_APP.doc
    for view in DB.FilteredElementCollector(doc).OfClass(DB.View):
        if view.ViewType == DB.ViewType.Legend:
            return view
    return None

base_legend = find_first_legend()

new_legend = revit.doc.GetElement(
    base_legend.Duplicate(DB.ViewDuplicateOption.Duplicate)
    )

new_legend.Scale = scale
```

### Detail Views

```python
view_fam_typeid = \
    doc.GetDefaultElementTypeId(
        DB.ElementTypeGroup.ViewTypeDrafting
        )
new_dest_view = DB.ViewDrafting.Create(doc, view_fam_typeid)
```