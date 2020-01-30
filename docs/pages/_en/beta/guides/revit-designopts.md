---
title: Working with Design Options
order: 72
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

{% include ltr/warning_note.html note='Currently there is not support in Revit API to set element Design Options' %}

<!-- https://forums.autodesk.com/t5/revit-api-forum/expose-design-options-settings/m-p/6451629/highlight/true#M17496 -->
<!-- https://thebuildingcoder.typepad.com/blog/2015/03/list-and-switch-design-options-using-ui-automation.html -->

<!-- my tool is for mass-deleting design options, so I need to keep track of which options are being deleted, which option sets are becoming emptied out (these also get deleted) but also, crucially, which views are looking at each design option so I can reallocate them so no views will get deleted in the process -->
<!-- if you delete a Revit design option and certain views are referencing that option, the view also gets deleted. so i read the  BuiltInParameter.VIEWER_OPTION_VISIBILITY and if it's not null, I can assign it to a new invalid ElementID(-1) to make it non-option specific. That way it won't get deleted when I commit the transaction. Document.Delete(option_id) and Document.Delete(option_set_id) both work fine for deleting design options (in case that's useful to you) -->