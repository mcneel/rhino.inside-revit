---
title: Curtain Walls & Systems
order: 41
---

<!-- Curtain Walls -->

This guide discusses a special case of Wall System Families. Basic familiarity with [Walls (Basic & Stacked)]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %}) greatly helps in understanding the *Curtain Walls* in {{ site.terms.revit }}.

## Curtain Grids

*Curtain Walls* are a special *Wall System Family* in {{ site.terms.revit }}. The geometry of these walls is generated based on an underlying UV *Curtain Grid*. The grid configuration is set in the *Curtain Wall Type*:

![]({{ "/static/images/guides/revit-curtains01.jpeg" | prepend: site.baseurl }})

*Curtain Grids* have *Grid Lines* on the U and V axes. In Revit API, the U direction is the vertical axis of the wall, and the V direction is the direction of the wall base curve (U line swept over the V curve):

![]({{ "/static/images/guides/revit-curtains02.jpeg" | prepend: site.baseurl }})

*Grid Lines*  can form angles other than 90° (Currently only on non-curved curtain walls):

![]({{ "/static/images/guides/revit-curtains03.jpeg" | prepend: site.baseurl }})

*Curtain Mullions* are attached to each *Grid Lines* segment and are joined based on the configurations set in the *Curtain Wall Type*:

![]({{ "/static/images/guides/revit-curtains04.png" | prepend: site.baseurl }})

*Curtain Mullions* are also attached to the vertical and horizontal borders of the wall. Remember that these border lines are part of the wall topology definition and not the *Curtain Grid*. The *Grid* only includes the grid lines created inside the wall boundaries:

![]({{ "/static/images/guides/revit-curtains05.png" | prepend: site.baseurl }})

4-Sided areas created by the grid are called *Grid Cells*:

![]({{ "/static/images/guides/revit-curtains06.png" | prepend: site.baseurl }})

 *Curtain Panels* (system families) or *Curtain Panel Family* (special type of flexible families that are designed to be inserted into *Curtain Cells* and act as custom panels) instances can be inserted into these cells to complete the geometry. *Curtain Panels* can be customized to represent solid panels, glass panels, or even empty (!) areas:

![]({{ "/static/images/guides/revit-curtains07.jpeg" | prepend: site.baseurl }})

The types of *Curtain Mullions* and *Panels* used for a wall are defined in the Curtain Wall Type and can also be overridden on the wall instance.

## Walls vs Systems

*Curtain Walls* and *Systems* are almost identical in definition. The only difference is that *Curtain Walls* are vertical and have directionality so the *Grid* configuration is set for **Vertical** and **Horizontal** grid lines. *Curtain Systems* can be created from *Mass* surfaces and thus are not bounded to a certain direction. Hence the *Grid* configurations is set for **Grid 1** and **Grid 2** axes.

{% capture api_note %}
In Revit API, *Curtain Walls* are represented by {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %} and *Curtain Systems* are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Curtain Walls

Similar to collecting *Basic* and *Stacked Walls*, the custom components shared at [Walls (Basic & Stacked)]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %}#by-wall-system-family) section, can help filtering the Curtain Wall instances and types:

![]({{ "/static/images/guides/revit-curtainwalls03.png" | prepend: site.baseurl }})

{% capture bubble_note %}
As shown in the images below, a combination of category picker, *Element.CategoryFilter*, and *Document.ElementTypes* can be used to filter the *Curtain Panel* and *Curtain Mullion* types and instances in a model. However, the collector does not return anything for *Curtain Grids*. See [Analyzing Curtain Walls](#analyzing-curtain-walls) for better workflows to extract *Panel* and *Mullion* information. As you can see, the *Curtain Grids* graph does not return any results for Grid Types. The Panel and Mullion types can be collected:

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls01.png" | prepend: site.baseurl }})

And the same for collecting instances:

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls02.png" | prepend: site.baseurl }})
{% endcapture %}
{% include ltr/bubble_note.html note=bubble_note %}

## Analyzing Curtain Wall Types

Now that we can collect the Curtain Wall Types in the mode, use the custom component shared here to analyze the properties:

![]({{ "/static/images/guides/revit-curtainwalls04.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Wall Type.ghuser' name='Analyse Curtain Wall Type' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Curtain Grid Layout.ghuser' name='Curtain Grid Layout' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Curtain Join Condition.ghuser' name='Curtain Join Condition' %}

## Analyzing Curtain Walls

### Extracting Curtain Wall Geometry

The full geometry of a Curtain Wall instance can be extracted using the *Element.Geometry* component:

![]({{ "/static/images/guides/revit-curtainwalls05.png" | prepend: site.baseurl }})

{% include ltr/issue_note.html issue_id='176' note='Element.Geometry does not extract Curtain Wall and System Geometries' %}

The custom component shared here, can be used to extract bounding geometry of a given Curtain Wall instance:

![]({{ "/static/images/guides/revit-curtainwalls06.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls06a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Wall.ghuser' name='Analyse Curtain Wall' %}

### Embedded Curtain Walls

Curtain Walls can be embedded inside other walls. The *Analyse Curtain Wall* component shared above, can provide access the to the parent wall (**HW**), embedding the given *Curtain Wall* instance. If *Curtain Wall* is not embedded, `null` is returned:

![]({{ "/static/images/guides/revit-curtainwalls06c.png" | prepend: site.baseurl }})

### Extracting Grid

{% capture api_note %}
In Revit API, Curtain Grids are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainGrid' title='DB.CurtainGrid' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The *Analyse Curtain Wall* component shared above, can provide access to the *Curtain Grid* of a *Curtain Wall* instance. *Curtain Grid* can be used to extract information about the *Grid*, and access the individual *Cells*, *Mullions*, and *Panels* on the *Curtain Wall*:

![]({{ "/static/images/guides/revit-curtainwalls06b.png" | prepend: site.baseurl }})

## Analyzing Curtain Grids

Using the component shared here, the *Curtain Grid* can be further broken down to its properties and hosted elements:

- **Curtain Cells**: Returns all the individual *Curtain Cells*. Cells are the bounded areas between the grid lines
- **Curtain Mullions**: Returns all the individual *Curtain Mullions*
- **Curtain Inserts**:Returns all the individual *Curtain Panels* or *Family Instances* (e.g. *Curtain Wall Door*) hosted on the *Curtain Cells*
- **Curtain Grid Lines**: Returns all the individual *Curtain Grid Lines* along U and V directions and their properties

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls07.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Grid.ghuser' name='Analyse Curtain Grid' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Curtain Grid Align Type.ghuser' name='Curtain Grid Align Type' %}

## Analyzing Cells

{% capture api_note %}
In Revit API, Curtain Cells are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainCell' title='DB.CurtainCell' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The component shared here, can analyze each curtain cell and extract the cell curves (**CL**) and the panelized curves (**PCL**):

![]({{ "/static/images/guides/revit-curtainwalls08.png" | prepend: site.baseurl }})

Not the difference between the cell curves and the panelized curves:

![]({{ "/static/images/guides/revit-curtainwalls09.png" | prepend: site.baseurl }})

As you can see in the image below, the cell curves follow the curvature of the wall, but the panelized curves, are the curves bounding the flat surface that is bounded inside the cell:

![]({{ "/static/images/guides/revit-curtainwalls10.png" | prepend: site.baseurl }})

The panelized curves can be used to create faces. Notice that each cell is marked with its index and they are in order from bottom row to the top row:

![]({{ "/static/images/guides/revit-curtainwalls10b.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls10a.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Cell.ghuser' name='Analyse Curtain Cell' %}

## Analyzing Mullions

{% capture api_note %}
In Revit API, Curtain Mullions are represented by the {% include api_type.html type='Autodesk.Revit.DB.Mullion' title='DB.Mullion' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The geometry for *Curtain Mullions* can be extracted by passing the *Mullions* from *Analyse Curtain Wall* component, to the *Element.Geometry* component:

![]({{ "/static/images/guides/revit-curtainwalls11.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls11a.png" | prepend: site.baseurl }})

The *Mullion* location curve can also be extracted by passing the *Mullion* to a *Curve* component:

![]({{ "/static/images/guides/revit-curtainwalls12.png" | prepend: site.baseurl }})

{% include ltr/issue_note.html issue_id='177' note='Passing a Curtain Mullion (DB.Mullion) to Curve component should output the location curve of the mullion' %}

The custom component shared here can be used to extract information about the individual *Curtain Mullions*:

![]({{ "/static/images/guides/revit-curtainwalls13.png" | prepend: site.baseurl }})

The C output provides the base curve of the *Mullion*. Notice how the horizontal curves are not intersecting the vertical curves, as the curves follow the join settings for the associated *Mullions*:

![]({{ "/static/images/guides/revit-curtainwalls13a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Mullion.ghuser' name='Analyse Curtain Mullion' %}

### Analyzing Mullion Types

{% capture api_note %}
In Revit API, Curtain Mullion Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.MullionType' title='DB.MullionType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The **Analyse Curtain Mullion Type** custom component shared here can be used to analyze the *Mullion Types* extracted by the *Analyse Curtain Mullion* component:

![]({{ "/static/images/guides/revit-curtainwalls14.png" | prepend: site.baseurl }})

The **Curtain Mullion System Family** value picker can be used to filter the *Mullions* by their *System Family* e.g. *L Corner Mullions*

![]({{ "/static/images/guides/revit-curtainwalls14a.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Mullion Type.ghuser' name='Analyse Curtain Mullion Type' %}

## Analyzing Panels

*Curtain Walls* can host two types of inserts. They can be either *Curtain Panels* or instances of *Custom Families* designed to work with *Curtain Walls* like a *Curtain Wall Door* instance. The components shared here help analyzing the Panels. The family instances can be analyzed using the methods and components provided in [Data Model: Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) and [Data Model: Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-instances.md %}) guides.

{% capture api_note %}
In Revit API, Curtain Panels are represented by the {% include api_type.html type='Autodesk.Revit.DB.Panel' title='DB.Panel' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The geometry for *Curtain Panels* can be extracted by passing the *Panels* from *Analyse Curtain Wall* component, to the *Element.Geometry* component:

![]({{ "/static/images/guides/revit-curtainwalls15.png" | prepend: site.baseurl }})

Notice how the panels are in order from bottom to top row:

![]({{ "/static/images/guides/revit-curtainwalls15a.png" | prepend: site.baseurl }})

Since the **Curtain Grid Insert (CGN)** output parameter can return both System Panels (`DB.Panel`) and Custom Family Instances (`DB.FamilyInstance`), the same workflow can extract the geometry of all these insert types:

![]({{ "/static/images/guides/revit-curtainwalls15b.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls15c.png" | prepend: site.baseurl }})

The custom component shared here can be used to extract information about the individual *Curtain Panels*:

![]({{ "/static/images/guides/revit-curtainwalls16.png" | prepend: site.baseurl }})

This custom component also provides access to the Panel base point(**PB**) and normal/orientation vectors (**PN**):

![]({{ "/static/images/guides/revit-curtainwalls17.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls18.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Panel.ghuser' name='Analyse Curtain Panel' %}

### Analyzing Panel Types

{% capture api_note %}
In Revit API, Curtain Panel Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.PanelType' title='DB.PanelType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The **Analyse Curtain Panel Type** custom component shared here can be used to analyze the *Panel Types* extracted by the *Analyse Curtain Panel* component:

![]({{ "/static/images/guides/revit-curtainwalls19.png" | prepend: site.baseurl }})

Note that **Panel Symbol (PS)** output parameter can return a System Panel Type (`DB.PanelType`) for a Custom Family Symbol (`DB.FamilySymbol`) depending on the type of Panel inserted into the Grid:

![]({{ "/static/images/guides/revit-curtainwalls19a.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Panel Type.ghuser' name='Analyse Curtain Panel Type' %}

## Analyzing Grid Lines

{% capture api_note %}
In Revit API, Curtain Grid Lines are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainGridLine' title='DB.CurtainGridLine' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The curve for *Curtain Grid Lines* can be extracted by passing the *Grid Lines* from *Analyse Curtain Wall* component, to the *Curve* component:

![]({{ "/static/images/guides/revit-curtainwalls20.png" | prepend: site.baseurl }})

{% include ltr/issue_note.html issue_id='178' note='Passing a Curtain Grid Line to Curve component should output the curve of the grid line' %}

The custom component shared here can be used to extract info about each individual **Curtain Grid Line**:

![]({{ "/static/images/guides/revit-curtainwalls21.png" | prepend: site.baseurl }})

Notice the difference between the *Curtain Grid Line* curves and segments. The **CGLC** provide access to a single curve for each line:

![]({{ "/static/images/guides/revit-curtainwalls22.png" | prepend: site.baseurl }})

Where as the **CGLAS** provides access to all the line segments per each *Curtain Grid Line* and it also includes the unused segments outside of the wall boundaries:

![]({{ "/static/images/guides/revit-curtainwalls23.png" | prepend: site.baseurl }})

*Curtain Grid Line* curves and segments are shown here:

![]({{ "/static/images/guides/revit-curtainwalls23a.png" | prepend: site.baseurl }})

### Extract Associated Mullions and Panels

The *Analyse Curtain Grid Line* component also provides the Mullions and Panels that are associated with each *Grid Line*. Notice that the *Mullions* and *Panels* on the wall borders are not included since they are not part of the *Curtain Grid* definition:

![]({{ "/static/images/guides/revit-curtainwalls24.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls24a.png" | prepend: site.baseurl }})

*Panel* association is slightly different from *Mullions*. Each *Mullion* is associated with a single *Grid Line*, however a single *Panel* (since it has two sides) is associated with a *Grid Line* along the U axis and also another *Grid Line* along the V:

![]({{ "/static/images/guides/revit-curtainwalls25.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls25a.gif" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Curtain Grid Line.ghuser' name='Analyse Curtain Grid Line' %}

## Modifying Curtain Wall Types

{% include ltr/en/wip_note.html %}

## Modifying Curtain Walls

{% include ltr/en/wip_note.html %}

## Creating Curtain Wall Types

{% include ltr/en/wip_note.html %}

## Creating Curtain Walls

{% include ltr/en/wip_note.html %}

### Creating Non-Linear Curtain Walls

<!-- https://github.com/mcneel/rhino.inside-revit/issues/47 -->
{% include ltr/en/wip_note.html %}


<!-- Curtain Systems -->

## Querying Curtain System Types

{% capture api_note %}
In Revit API, Curtain System Types are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystemType' title='DB.CurtainSystemType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainsystems01.png" | prepend: site.baseurl }})

## Querying Curtain Systems

{% capture api_note %}
In Revit API, Curtain Systems are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-curtainsystems02.png" | prepend: site.baseurl }})

## Analyzing Curtain System Types

{% include ltr/en/wip_note.html %}

## Analyzing Curtain Systems

{% include ltr/en/wip_note.html %}

## Modifying Curtain System Types

{% include ltr/en/wip_note.html %}

## Modifying Curtain Systems

{% include ltr/en/wip_note.html %}

## Creating Curtain System Types

{% include ltr/en/wip_note.html %}

## Creating Curtain Systems

{% include ltr/en/wip_note.html %}
