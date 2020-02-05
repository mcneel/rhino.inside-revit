---
title: Project Settings
order: 90
---

{% include ltr/en/wip_note.html %}


## Project Information

{% capture api_note %}
In Revit API, Project Information is a `DB.Element` and is represented by the {% include api_type.html type='Autodesk.Revit.DB.ProjectInfo' title='DB.ProjectInfo' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the custom component shared here to get access to the `DB.ProjectInfo` element and its properties.

![]({{ "/static/images/guides/revit-psettings01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Project Info.ghuser' name='Project Info' %}

Since `DB.ProjectInfo` is a `DB.Element`, you can use the typical element components to operate on the project information. For example you can use the *Element.ParameterGet* to read the built-in or custom parameters:

![]({{ "/static/images/guides/revit-psettings02.png" | prepend: site.baseurl }})

Or use the *Element.ParameterSet* to set the built-in or custom parameter values:

![]({{ "/static/images/guides/revit-psettings03.png" | prepend: site.baseurl }})
