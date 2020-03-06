---
title: Openings
order: 47
---

## Querying Openings

{% capture api_note %}
In Revit API, Openings of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Opening' title='DB.Opening' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

You can use the combination of the Openings component shared here, passed to *Element.CategoryFilter* and *Document.Elements* components to collect the openings in a model:

![]({{ "/static/images/guides/revit-openings01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Openings.ghuser' name='Openings' %}

## Analyzing Openings

### Wall Openings

Use the *Analyse Wall Opening* component shared here to extract information about the wall opening element:

![]({{ "/static/images/guides/revit-openings02.png" | prepend: site.baseurl }})

Output parameters are:
- **HE**: Host Element
- **BC**: Base Constraint Level
- **BCO**: Base Offset
- **TC**: Top Constraint Level
- **TCO**: Top Offset
- **H**: Height
- **P1**: Rectangle Point 1
- **P2**: Rectangle Point 2

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Wall Opening.ghuser' name='Analyse Wall Opening' %}

### Shafts

Use the *Analyse Shaft* component shared here to extract information about the shaft element:

![]({{ "/static/images/guides/revit-openings03.png" | prepend: site.baseurl }})

Output parameters are:
- **HE**: Host Element
- **C**: Boundary Curves
- **BC**: Base Constraint Level
- **BCO**: Base Offset
- **TC**: Top Constraint Level
- **TCO**: Top Offset
- **H**: Height

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Shaft.ghuser' name='Analyse Shaft' %}

### Openings By Face

Use the *Analyse Opening* component shared here to extract information about the opening elements that are created by face on a host component e.g. Ceilings Openings, Roof Openings, Floor Openings, etc.

![]({{ "/static/images/guides/revit-openings04.png" | prepend: site.baseurl }})

Output parameters are:
- **HE**: Host Element
- **C**: Boundary Curves

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Opening.ghuser' name='Analyse Opening' %}

## Creating Wall Openings

## Creating Shafts

Use the *Create Shaft* component shared here to create a shaft element from boundary curves and is bounded by two levels:

![]({{ "/static/images/guides/revit-openings05.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Create Shaft.ghuser' name='Create Shaft' %}

## Creating Openings By Face

{% include ltr/en/wip_note.html %}
