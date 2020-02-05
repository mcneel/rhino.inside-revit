---
title: Styles and Patterns
order: 91
---

{% include ltr/en/wip_note.html %}


## Line Patterns

{% capture api_note %}
In Revit API, line patterns are represented by the {% include api_type.html type='Autodesk.Revit.DB.LinePatternElement' title='DB.LinePatternElement' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Find Specific Line Pattern

![]({{ "/static/images/guides/revit-styles01.png" | prepend: site.baseurl }})

Notice that the `Solid` line pattern is being provided as a python object. `Solid` is a special line pattern and Revit does not return a normal API type for this specific line pattern. Hence a temporary python wrapper has been defined for this type in the component:

```python
class SolidLinePatternElement(object):
    @property
    def Id(self):
        return DB.LinePatternElement.GetSolidPatternId()
```
&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Find Line Pattern.ghuser' name='Find Line Pattern' %}

## Fill Patterns

{% capture api_note %}
In Revit API, fill patterns are represented by the {% include api_type.html type='Autodesk.Revit.DB.FillPatternElement' title='DB.FillPatternElement' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Find Specific Fill Pattern

![]({{ "/static/images/guides/revit-styles02.png" | prepend: site.baseurl }})

Similar to `Solid` line pattern described above, `Solid` is a special fill pattern but in this case, Revit provides access to this pattern as a normal Revit API type.

{% include ltr/download_comp.html archive='/static/ghnodes/Find Fill Pattern.ghuser' name='Find Fill Pattern' %}
