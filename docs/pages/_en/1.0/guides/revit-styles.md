---
title: Styles and Patterns
order: 91
group: Settings
thumbnail: /static/images/guides/revit-styles.png
subtitle: Workflows for Revit Styles, Line & Fill Patterns
ghdef: revit-styles.ghx
---

## Line Patterns

{% capture api_note %}
In Revit API, line patterns are represented by the {% include api_type.html type='Autodesk.Revit.DB.LinePatternElement' title='DB.LinePatternElement' %}. `Solid` is a special line pattern and Revit does not return a normal API type for this specific line pattern. The {% include ltr/comp.html uuid='eb5ab657' %} primitive in {{ site.terms.rir }} represents Line Patterns.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Find Specific Line Pattern

Use the context menu on the {% include ltr/comp.html uuid='eb5ab657' %} component to select a specific line pattern:

![]({{ "/static/images/guides/revit-styles-linepattern-select.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-styles-linepatterns.png" | prepend: site.baseurl }})

## Fill Patterns

{% capture api_note %}
In Revit API, fill patterns are represented by the {% include api_type.html type='Autodesk.Revit.DB.FillPatternElement' title='DB.FillPatternElement' %}. `Solid` is also special fill pattern but in this case, Revit provides access to this pattern as a normal Revit API type.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Find Specific Fill Pattern

![]({{ "/static/images/guides/revit-styles-fillpattern-select.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-styles-fillpatterns.png" | prepend: site.baseurl }})
