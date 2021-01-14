---
title: Design Options
subtitle: How to work with Design Options and Sets
order: 72
thumbnail: /static/images/guides/revit-designopts.png
group: Containers
ghdef: revit-designopts.ghx
---

{% include ltr/warning_note.html note='Currently there is very limited support for design options in Revit API' %}

## Querying Design Options

{% capture api_note %}
In Revit API, Design Options are represented by the {% include api_type.html type='Autodesk.Revit.DB.DesignOption' title='DB.DesignOption' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}


Use the {% include ltr/comp.html uuid='b6349dda' %} to find the design option that is currently active in Revit UI.

![]({{ "/static/images/guides/revit-designopts-active.png" | prepend: site.baseurl }})


Then you can use the {% include ltr/comp.html uuid='677ddf10' %} and {% include ltr/comp.html uuid='01080b5e' %} to inspect the identity of each *Design Option* or *Design Option Set*.


![]({{ "/static/images/guides/revit-designopts-identity.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-designopts-optsetidentity.png" | prepend: site.baseurl }})


To query all the *Design Option Sets* and *Design Options* in a document, use the {% include ltr/comp.html uuid='b31e7605' %} and {% include ltr/comp.html uuid='6804582b' %} components respectively.

![]({{ "/static/images/guides/revit-designopts-queryoptsets.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-designopts-queryopts.png" | prepend: site.baseurl }})


{% capture api_note %}
Notice that the Design Option Set object is a simple `DB.Element` since there is very limited support for design options in Revit API
{% endcapture %}
{% include ltr/api_note.html note=api_note %}


## Collecting Design Option Elements

You can pass a design option to the {% include ltr/comp.html uuid='1b197e82' %} component to collect the elements belonging to a given design option.

![]({{ "/static/images/guides/revit-designopts-queryelements.png" | prepend: site.baseurl }})

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
