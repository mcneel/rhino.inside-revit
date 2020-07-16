---
title: Modify Geometry
order: 58
group: Modeling
---

{% include ltr/en/wip_note.html %}

## Flip

To check whether an element has been flipped (Revit supports various flip type) use the *Flipped* component shared here.

![]({{ "/static/images/guides/revit-modify01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Flipped.ghuser' name='Flipped' %}

### Query Flipped Elements

You can query elements of any category using the collector components, pass them on to the *Flipped* component and filter the results as needed:

![]({{ "/static/images/guides/revit-modify02.png" | prepend: site.baseurl }})
