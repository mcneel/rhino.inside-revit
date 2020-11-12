---
title: Design Options
order: 72
thumbnail: /static/images/guides/revit-wip.png
group: Containers
---

{% include ltr/warning_note.html note='Currently there is very limited support for design options in Revit API' %}

## Querying Design Options

Use the *Document Design Options* component shared here, to query the design options in your model.

{% capture api_note %}
In Revit API, Design Options are represented by the {% include api_type.html type='Autodesk.Revit.DB.DesignOption' title='DB.DesignOption' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-designopts01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Document Design Options.ghuser' name='Document Design Options' %}

Use the *Design Options Properties* to read the properties of each design option e.g. Design Option Set

{% capture api_note %}
Notice that the Design Option Set object is a simple `DB.Element` since there is very limited support for design options in Revit API
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-designopts02.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Design Option Properties.ghuser' name='Design Option Properties' %}

## Collecting Design Option Elements

You can pass a design option to the *Element.DesignOptionFilter* component to collect the elements belonging to a given design option.

![]({{ "/static/images/guides/revit-designopts03.png" | prepend: site.baseurl }})

## Setting Element Design Option

{% include ltr/warning_note.html note='Currently there is no support in Revit API to set element Design Options' %}

<!-- https://forums.autodesk.com/t5/revit-api-forum/expose-design-options-settings/m-p/6451629/highlight/true#M17496 -->
<!-- https://thebuildingcoder.typepad.com/blog/2015/03/list-and-switch-design-options-using-ui-automation.html -->

## Deleting Design Options

{% capture doptsrem_note %}
Due to challenges of deleting Design Options, we have not created a workflow yet.
- Deleting a Design Option, also deletes all the views referencing that design option. A workaround is to read `BuiltInParameter.VIEWER_OPTION_VISIBILITY` parameter of the view object, and if it has a value, meaning it is referencing a design option, set the value to `InvalidElementId` to remove the reference. User must also be notified of which views have been changed.
- Deleting a Design Option, also deletes all the elements inside the design option. Ideally the user needs to decide if any of the elements need to be relocated before Design Option is removed.
- Design Options can be deleted using `Document.Delete()`
{% endcapture %}
{% include ltr/warning_note.html note=doptsrem_note %}
