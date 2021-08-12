---
title: Schedules & Reports
order: 65
thumbnail: /static/images/guides/rir-ghpython.png

group: Documentation
---

## Querying Schedules

{% capture api_note %}
In Revit API, Schedules are a type of view ({% include api_type.html type='Autodesk.Revit.DB.View' title='DB.View' %}) and are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewSchedule' title='DB.ViewSchedule' %}. Schedules have a different rendering method. Instead of showing the elements graphically, they list the element and their properties according to the schedule settings, and in a spreadsheet style
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-schedules01.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='In Revit, Keynote Legends (although named Legend) are actually schedules. You can check whether a schedule is a keynote schedule by checking `DB.ViewSchedule.IsInternalKeynoteSchedule` property' %}

## Querying Schedules Types

Since schedules are actually views, we can use the same workflow as for [Views]({{ site.baseurl }}{% link _en/beta/guides/revit-views.md %}), to work with schedules:

![]({{ "/static/images/guides/revit-schedules02.png" | prepend: site.baseurl }})

### Find Specific Schedules Type

![]({{ "/static/images/guides/revit-schedules03.png" | prepend: site.baseurl }})

### Querying Schedules by Type

![]({{ "/static/images/guides/revit-schedules04.png" | prepend: site.baseurl }})

### Finding Specific Schedule

![]({{ "/static/images/guides/revit-schedules05.png" | prepend: site.baseurl }})
