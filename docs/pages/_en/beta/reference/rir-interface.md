---
title: Rhino.Inside.Revit Interface
order: 10
---

Once {{ site.terms.rir }} is loaded and licensed, it creates a new ribbon in Revit interface named *Rhinoceros*

![]({{ "/static/images/ribbon/ribbon.png" | prepend: site.baseurl }})

Here is a description of the buttons and functionality available on this ribbon.

{% for panel in site.data.buttons %}
## {{ panel.title }} Panel
{% include ltr/button_table.html buttons=panel.comps %}
{% endfor %}
