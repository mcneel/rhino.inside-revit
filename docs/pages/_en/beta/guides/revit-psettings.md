---
title: Project Settings
order: 90
thumbnail: /static/images/guides/rir-ghpython.png

group: Settings
---

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

## Project Locations

{% capture api_note %}
In Revit API, Project Locations is a `DB.Element` and is represented by the {% include api_type.html type='Autodesk.Revit.DB.ProjectLocation' title='DB.ProjectLocation' %} type. The `DB.Document` type will provide access to active project location (`.ActiveProjectLocation`) as well as all the project locations available in the model (`.ProjectLocations`)
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the *Project Locations* component shared here to grab a list of all project locations as well as the active one. Since `DB.ProjectLocation` is a `DB.Element`, you can use the *Element.Identity* component to grab the location name:

![]({{ "/static/images/guides/revit-psettings04.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='Note that all Revit models return an `Internal` project location. [This is used for internal shared coordinates](https://thebuildingcoder.typepad.com/blog/2017/05/finding-the-right-project-location.html). Avoid using or making changes to this project location' %}

Use the *Project Location (Destruct)* component shared here, to dig one level deeper and grab information about each project location. The component has a `XYZ` input parameter that you can use to specify the exact point that the component needs to use to extract project position into `POS` output parameter:

![]({{ "/static/images/guides/revit-psettings05.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Project Locations.ghuser' name='Project Locations' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Project Location (Destruct).ghuser' name='Project Location (Destruct)' %}

### Site Locations

{% capture api_note %}
In Revit API, Site Location is represented by the {% include api_type.html type='Autodesk.Revit.DB.SiteLocation' title='DB.SiteLocation' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the *Site Location (Destruct)* component shared here, to extract information about the site location associated with each project location:

![]({{ "/static/images/guides/revit-psettings06.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Site Location (Destruct).ghuser' name='Site Location (Destruct)' %}

### Project Positions

{% capture api_note %}
In Revit API, Project Position is represented by the {% include api_type.html type='Autodesk.Revit.DB.ProjectPosition' title='DB.ProjectPosition' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the *Project Position (Destruct)* component shared here, to extract information about project position:

![]({{ "/static/images/guides/revit-psettings07.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Project Position (Destruct).ghuser' name='Project Position (Destruct)' %}
