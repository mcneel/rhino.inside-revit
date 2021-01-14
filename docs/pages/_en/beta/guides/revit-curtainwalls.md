---
title: Curtain Walls & Systems
order: 41
group: Modeling
thumbnail: /static/images/guides/revit-curtainwalls.png
subtitle: Workflows for Revit Curtain Walls and Systems
ghdef: revit-curtainwalls.ghx
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

*Curtain Walls* and *Curtain Systems* are almost identical in definition. The only difference is that *Curtain Walls* are vertical and have directionality so the *Grid* configuration is set for **Vertical** and **Horizontal** grid lines. *Curtain Systems* can be created from *Mass* surfaces and thus are not flowing in a certain direction. Hence the *Grid* configurations is set for **Grid 1** and **Grid 2** axes.

{% capture api_note %}
In Revit API, *Curtain Walls* are represented by {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %} and *Curtain Systems* are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Curtain Walls

Similar to collecting *Basic* and *Stacked Walls*, the components shown at [Walls (Basic & Stacked)]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %}#by-wall-system-family) section, can help filtering the Curtain Wall instances and types:

![]({{ "/static/images/guides/revit-curtainwalls-query.png" | prepend: site.baseurl }})

{% capture bubble_note %}
As shown in the images below, a combination of category picker, {% include ltr/comp.html uuid="d08f7ab1-" %}, and {% include ltr/comp.html uuid="7b00f940-" %} can be used to filter the *Curtain Panel* and *Curtain Mullion* types and instances in a model. However, the collector does not return anything for *Curtain Grids*. See [Analyzing Curtain Walls](#analyzing-curtain-walls) for better workflows to extract *Panel* and *Mullion* information. As you can see, the *Curtain Grids* graph does not return any results for Grid Types. The Panel and Mullion types can be collected however:

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-querypartstypes.png" | prepend: site.baseurl }})

&nbsp;

And the same for collecting instances:

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-queryparts.png" | prepend: site.baseurl }})
{% endcapture %}
{% include ltr/bubble_note.html note=bubble_note %}

## Analyzing Curtain Wall Types

Now that we can collect the *Curtain Wall Types*, use the {% include ltr/comp.html uuid='d0874f93' %} shown here to analyze their properties. The {% include ltr/comp.html uuid='c84653dd' %}, {% include ltr/comp.html uuid='cd3e68b2' %}, and {% include ltr/comp.html uuid='9c2d116d' %} components can help filtering based on these values:

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwtype.png" | prepend: site.baseurl }})


## Analyzing Curtain Walls

### Extracting Curtain Wall Geometry

The full geometry of a *Curtain Wall* instance can be extracted using the {% include ltr/comp.html uuid="b3bcbf5b-" %} component:

![]({{ "/static/images/guides/revit-curtainwalls-cwallgeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls-cwallgeomscap.png" | prepend: site.baseurl }})


The {% include ltr/comp.html uuid="3396dbc4-" %} component shown here, can be used to extract bounding geometry of a given *Curtain Wall* instance:

![]({{ "/static/images/guides/revit-curtainwalls-cwallboundinggeom.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-cwallboundinggeomscap.png" | prepend: site.baseurl }})


### Embedded Curtain Walls

*Curtain Walls* can be embedded inside other walls. The {% include ltr/comp.html uuid="734b2dac-" %} component shown above, can provide access the to the parent/host wall (**Host Wall**), embedding the given *Curtain Wall* instance. If *Curtain Wall* is not embedded, `null` is returned:

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwallhost.png" | prepend: site.baseurl }})

### Extracting Grid

{% capture api_note %}
In Revit API, Curtain Grids are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainGrid' title='DB.CurtainGrid' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The {% include ltr/comp.html uuid="734b2dac-" %} component shown above, can provide access to the *Curtain Grid* of a *Curtain Wall* instance. *Curtain Grid* can be used to extract information about the *Grid*, and access the individual *Cells*, *Mullions*, and *Panels* on the *Curtain Wall*:

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwall.png" | prepend: site.baseurl }})

## Analyzing Curtain Grids

Using the component shown here, the *Curtain Grid* can be further broken down to its properties and hosted elements:

- **Curtain Cells**: Returns all the individual *Curtain Cells*. Cells are the bounded areas between the grid lines
- **Curtain Mullions**: Returns all the individual *Curtain Mullions*
- **Curtain Panels**: Returns all the individual *Curtain Panels* or *Family Instances* (e.g. *Curtain Wall Door*) hosted on the *Curtain Cells*
- **Curtain Grid Lines**: Returns all the individual *Curtain Grid Lines* along U and V directions and their properties

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-analyzecgrid.png" | prepend: site.baseurl }})

## Analyzing Cells

{% capture api_note %}
In Revit API, Curtain Cells are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainCell' title='DB.CurtainCell' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The component shown here, can analyze each curtain cell and extract the cell curves (**Curves**) and the panelized curves (**Planarized Curves**):

![]({{ "/static/images/guides/revit-curtainwalls-analyzecell.png" | prepend: site.baseurl }})

Note the difference between the cell curves and the panelized curves:

![]({{ "/static/images/guides/revit-curtainwalls-cellvspanelcurves.png" | prepend: site.baseurl }})

As you can see in the image below, the cell curves follow the curvature of the wall, but the panelized curves, are the curves bounding the flat surface that is bounded inside the cell:

![]({{ "/static/images/guides/revit-curtainwalls-curvedcellvspanelcurves.png" | prepend: site.baseurl }})

The panelized curves can be used to create faces. Notice that each cell is marked with its index and they are in order from bottom row to the top row:

![]({{ "/static/images/guides/revit-curtainwalls-facefrompanelcurves.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls-facefrompanelcurvesscap.png" | prepend: site.baseurl }})

## Analyzing Mullions

{% capture api_note %}
In Revit API, Curtain Mullions are represented by the {% include api_type.html type='Autodesk.Revit.DB.Mullion' title='DB.Mullion' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The geometry for *Curtain Mullions* can be extracted by passing the *Mullions* from {% include ltr/comp.html uuid="734b2dac-" %} component, to the {% include ltr/comp.html uuid="b3bcbf5b-" %} component:

![]({{ "/static/images/guides/revit-curtainwalls-mulliongeom.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-mulliongeomscap.png" | prepend: site.baseurl }})

The component shown here can be used to extract information about the individual *Curtain Mullions*:

![]({{ "/static/images/guides/revit-curtainwalls-analyzemullion.png" | prepend: site.baseurl }})

The **Axis Curve** output provides the axis curve of the *Mullion*. Notice how the horizontal curves are not intersecting the vertical curves, as the curves follow the join settings for the associated *Mullions*:

![]({{ "/static/images/guides/revit-curtainwalls-mullionlines.png" | prepend: site.baseurl }})

The *Mullion* location curve can also be extracted by passing the *Mullion* to a *Curve* component. Note that some of the mullions might have a length of zero and are not visible as geometries on the curtain walls:

![]({{ "/static/images/guides/revit-curtainwalls-mulliontocurve.png" | prepend: site.baseurl }})

### Analyzing Mullion Types

{% capture api_note %}
In Revit API, Curtain Mullion Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.MullionType' title='DB.MullionType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The {% include ltr/comp.html uuid="66a9f189-" %} component shown here can be used to analyze the *Mullion Types* extracted by the {% include ltr/comp.html uuid="4eeca86b-" %} component:

![]({{ "/static/images/guides/revit-curtainwalls-analyzemulliontype.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid='4bbe14f2' %} value picker can be used to filter the *Mullions* by their *System Family* e.g. *L Corner Mullions*

![]({{ "/static/images/guides/revit-curtainwalls-mulliontypefilter.png" | prepend: site.baseurl }})

## Analyzing Panels

*Curtain Walls* can host two types of inserts. They can be either *Curtain Panels* or instances of *Custom Families* designed to work with *Curtain Walls* like a *Curtain Wall Door* instance. The components shown here help analyzing the *Curtain Panels*. The family instances can be analyzed using the methods and components provided in [Data Model: Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) and [Data Model: Elements & Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-elements.md %}#instances) guides.

{% capture api_note %}
In Revit API, Curtain Panels are represented by the {% include api_type.html type='Autodesk.Revit.DB.Panel' title='DB.Panel' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The geometry for *Curtain Panels* can be extracted by passing the *Panels* from {% include ltr/comp.html uuid="734b2dac-" %} component, to the {% include ltr/comp.html uuid="b3bcbf5b-" %} component:

![]({{ "/static/images/guides/revit-curtainwalls-panelgeom.png" | prepend: site.baseurl }})

Notice how the panels are in order from bottom to top row:

![]({{ "/static/images/guides/revit-curtainwalls-panelgeomscap.png" | prepend: site.baseurl }})

Since the **Panels** output parameter can return both Panels (`DB.Panel`) and Custom Family Instances (`DB.FamilyInstance`), the same workflow can extract the geometry of all these insert types:

![]({{ "/static/images/guides/revit-curtainwalls-inserttypes.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-inserttypesscap.png" | prepend: site.baseurl }})

However when extracting information about individual *Curtain Panels*:, the **Panels** output needs to be filtered for Panels (`DB.Panel`), otherwise the {% include ltr/comp.html uuid="08507225-" %} will show an error:

![]({{ "/static/images/guides/revit-curtainwalls-panelerror.png" | prepend: site.baseurl }})

Use the {% include ltr/comp.html uuid='36180a9e' %} component, in combination with other filter components e.g. ({% include ltr/comp.html uuid='d08f7ab1' %}), to filter for Panels (`DB.Panel`):

![]({{ "/static/images/guides/revit-curtainwalls-panelfilter.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid="08507225-" %} component also provides access to the panel base point(**Base Point**) and normal/orientation vectors (**Orientation**):

![]({{ "/static/images/guides/revit-curtainwalls-panelbaseorient.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-panelorientvectors.png" | prepend: site.baseurl }})

### Analyzing Panel Types

{% capture api_note %}
In Revit API, Curtain Panel Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.PanelType' title='DB.PanelType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The {% include ltr/comp.html uuid="6f11977f-" %} component shown here can be used to analyze the *Panel Types* extracted by the {% include ltr/comp.html uuid="08507225-" %} component:

![]({{ "/static/images/guides/revit-curtainwalls-analyzepaneltype.png" | prepend: site.baseurl }})

Note that **Panel Type** output parameter can return a *System Panel Type* (`DB.PanelType`) or a *Custom Family Symbol* (`DB.FamilySymbol`) depending on the type of Panel inserted into the Grid, so we need to filter for panels first:

![]({{ "/static/images/guides/revit-curtainwalls-inserttype.png" | prepend: site.baseurl }})

## Analyzing Grid Lines

{% capture api_note %}
In Revit API, Curtain Grid Lines are represented by the {% include api_type.html type='Autodesk.Revit.DB.CurtainGridLine' title='DB.CurtainGridLine' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The curve for *Curtain Grid Lines* can be extracted by passing the *Grid Lines* from {% include ltr/comp.html uuid="d7b5c58e-" %} component, to the *Curve* component:

![]({{ "/static/images/guides/revit-curtainwalls-analyzegrid.png" | prepend: site.baseurl }})

The component shown here can be used to extract info about each individual **Curtain Grid Line**:

![]({{ "/static/images/guides/revit-curtainwalls-analyzegridline.png" | prepend: site.baseurl }})

Notice the difference between the *Curtain Grid Line* curves and segments. The **Curve** output parameter provide access to a single curve for each *Curtain Grid Line* along the U or V axes:

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinecurve.png" | prepend: site.baseurl }})

Whereas the **Segments** output parameter provides access to all the individual line segments per each *Curtain Grid Line* and it also includes the unused segments outside of the wall boundaries:

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinesegments.png" | prepend: site.baseurl }})

*Curtain Grid Line* curves and segments are shown here:

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinesallegments.png" | prepend: site.baseurl }})

### Extract Associated Mullions and Panels

The {% include ltr/comp.html uuid="face5e7d-" %} component also provides the *Mullions* and *Panels* that are associated with each *Grid Line*. Notice that the *Mullions* and *Panels* on the wall borders are not included since they are not part of the *Curtain Grid* definition:

![]({{ "/static/images/guides/revit-curtainwalls-analyzeassocmullions.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-assocmullions.png" | prepend: site.baseurl }})

*Panel* association is slightly different from *Mullions*. Each *Mullion* is associated with a single *Grid Line*, however a single *Panel* (since it has two sides) is associated with a *Grid Line* along the U axis and also another *Grid Line* along the V:

![]({{ "/static/images/guides/revit-curtainwalls-assocmullionspanels.gif" | prepend: site.baseurl }})

## Modifying Curtain Wall Types

{% include ltr/en/wip_note.html %}

## Modifying Curtain Walls

{% include ltr/en/wip_note.html %}

## Creating Curtain Wall Types

{% include ltr/en/wip_note.html %}

## Creating Curtain Walls

### By Profile

Similar to creating a basic wall based on a profile, you can simply create a curtain wall by passing a *Curtain Wall Type* to the {% include ltr/comp.html uuid='78b02ae8' %} component:

![]({{ "/static/images/guides/revit-curtainwalls-byprofilescap.png" | prepend: site.baseurl }})


### Creating Non-Linear Curtain Walls

<!-- https://github.com/mcneel/rhino.inside-revit/issues/47 -->
{% include ltr/en/wip_note.html %}


<!-- Curtain Systems -->

## Querying Curtain Systems

{% capture api_note %}
In Revit API, Curtain System Types are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystemType' title='DB.CurtainSystemType' %}. Curtain Systems are represented by {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %} 
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Curtain System types and instances can be queries using the generic {% include ltr/comp.html uuid='d08f7ab1' %}, {% include ltr/comp.html uuid='7b00f940' %}, and {% include ltr/comp.html uuid='0f7da57e' %} components:

![]({{ "/static/images/guides/revit-curtainsystems-querytypes.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainsystems-query.png" | prepend: site.baseurl }})

## Analyzing Curtain Systems

{% include ltr/comp.html uuid="83d08b81-" %} component shown here can be used to extract information about *Curtain System* types. Note the similarity between the *Curtain System Type* and *Curtain Wall Types*:

![]({{ "/static/images/guides/revit-curtainsystems-analyzetype.png" | prepend: site.baseurl }})

Extracting information from *Curtain System* instances are very similar to Curtain Wall instances. Similar to {% include ltr/comp.html uuid="734b2dac-" %} component, the {% include ltr/comp.html uuid='83d08b81' %} component provides access to the *Curtain Grid* definition:

{% include ltr/bubble_note.html note='Note that *Curtain Systems* can have multiple *Curtain Grids*, each associated with a single face of the source geometry. These curtain grids have independent boundaries and are generated based on the *Curtain System Type* definition' %}

![]({{ "/static/images/guides/revit-curtainsystems-analyze.png" | prepend: site.baseurl }})

Once you gain access to the *Curtain Grid* definition, you can use the {% include ltr/comp.html uuid="d7b5c58e-" %} to extract information, very similar to grids on *Curtain Walls*:

![]({{ "/static/images/guides/revit-curtainsystems-analyzecgrid.png" | prepend: site.baseurl }})

## Modifying Curtain Systems

{% include ltr/en/wip_note.html %}

## Creating Curtain Systems

{% include ltr/en/wip_note.html %}
