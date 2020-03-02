---
title: Curtain Walls
order: 41
---

{% include ltr/en/wip_note.html %}


<!-- Curtain Walls -->

{% capture api_note %}
In Revit API, Wall Types are represented by {% include api_type.html type='Autodesk.Revit.DB.WallType' title='DB.WallType' %}. Walls have three main *System Families* that are represented by {% include api_type.html type='Autodesk.Revit.DB.WallKind' title='DB.WallKind' %} enumeration and could be determined by checking `DB.WallType.Kind`. In {{ site.terms.rir }}, the term *Wall System Family* is used instead for consistency.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Curtain Wall Types

![]({{ "/static/images/guides/revit-curtainwalls01.png" | prepend: site.baseurl }})

see [Querying By System Family](#querying-by-system-family)

## Querying Curtain Walls

![]({{ "/static/images/guides/revit-curtainwalls02.png" | prepend: site.baseurl }})

see [Querying By System Family](#querying-by-system-family)

## Querying By System Family

![]({{ "/static/images/guides/revit-curtainwalls03.png" | prepend: site.baseurl }})

See [Walls]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %}#by-wall-system-family)

## Analyzing Curtain Wall Types

![]({{ "/static/images/guides/.png" | prepend: site.baseurl }})

Explain what curtain wall is. then explain properties listed below

![]({{ "/static/images/guides/revit-curtainwalls04.png" | prepend: site.baseurl }})

ref to panel type below for analysis

## Analyzing Curtain Walls

explain parts of curtain wall

- wall
- grid
  - cells
  - mullions
  - inserts
  - grid lines


### Extracting Curtain Wall Geometry

show bounding geometry and explain what it is

<!-- https://github.com/mcneel/rhino.inside-revit/issues/42 -->
![]({{ "/static/images/guides/revit-curtainwalls05.png" | prepend: site.baseurl }})

explain bug here

{% include ltr/issue_note.html issue_id='' note='' %}

![]({{ "/static/images/guides/revit-curtainwalls06a.png" | prepend: site.baseurl }})

### Embedded Curtain Walls

show and explain embedded curtain walls

![]({{ "/static/images/guides/revit-curtainwalls06c.png" | prepend: site.baseurl }})

### Extracting Grid

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainwalls06b.png" | prepend: site.baseurl }})

### Analyzing Grid

![]({{ "/static/images/guides/revit-curtainwalls07.png" | prepend: site.baseurl }})

### Analyzing Cells

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainwalls08.png" | prepend: site.baseurl }})

Cell actual curves

![]({{ "/static/images/guides/revit-curtainwalls09.png" | prepend: site.baseurl }})

Cells planarized

![]({{ "/static/images/guides/revit-curtainwalls10.png" | prepend: site.baseurl }})

### Analyzing Mullions

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainwalls11.png" | prepend: site.baseurl }})

curve should convert add issue here

![]({{ "/static/images/guides/revit-curtainwalls12.png" | prepend: site.baseurl }})

{% include ltr/issue_note.html issue_id='' note='' %}

![]({{ "/static/images/guides/revit-curtainwalls13.png" | prepend: site.baseurl }})

### Analyzing Mullion Types

![]({{ "/static/images/guides/revit-curtainwalls14.png" | prepend: site.baseurl }})

### Analyzing Inserts

explain inserts can be `DB.Panel` or `DB.FamilyInstance`

### Analyzing Panels

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainwalls15.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls16.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls17.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls18.png" | prepend: site.baseurl }})

### Analyzing Panel Types

![]({{ "/static/images/guides/revit-curtainwalls19.png" | prepend: site.baseurl }})

### Analyzing Family Inserts

<!-- ref to family instance -->

### Analyzing Grid Lines

{% capture api_note %}
In Revit API,  are represented by the {% include api_type.html type='Autodesk.Revit.DB.' title='DB.' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainwalls20.png" | prepend: site.baseurl }})

explain this should work

{% include ltr/issue_note.html issue_id='' note='' %}

show how to analyse

![]({{ "/static/images/guides/revit-curtainwalls21.png" | prepend: site.baseurl }})

explain the diff between full curves and partial curves

![]({{ "/static/images/guides/revit-curtainwalls22.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-curtainwalls23.png" | prepend: site.baseurl }})


### Extract Associated Mullions and Panels

explain the grid lines do not contain border lines

![]({{ "/static/images/guides/revit-curtainwalls23.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls23.png" | prepend: site.baseurl }})

## Modifying Curtain Wall Types
## Modifying Curtain Walls

## Creating Curtain Wall Types
## Creating Curtain Walls

### Creating Non-Linear Curtain Walls

<!-- https://github.com/mcneel/rhino.inside-revit/issues/47 -->




<!-- Curtain Systems -->

## Querying Curtain System Types

![]({{ "/static/images/guides/revit-curtainsystems01.png" | prepend: site.baseurl }})

## Querying Curtain Systems

![]({{ "/static/images/guides/revit-curtainsystems02.png" | prepend: site.baseurl }})

## Analyzing Curtain System Types
## Analyzing Curtain Systems

## Modifying Curtain System Types
## Modifying Curtain Systems

## Creating Curtain System Types
## Creating Curtain Systems
