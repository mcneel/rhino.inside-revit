---
title: Rhino.Inside.Revit Guides
toc: false
---

This section includes many articles that guide you through solving many {{ site.terms.revit }} challenges using {{ site.terms.rir }}. Make sure to take a look at the *Getting Started* guide on this Wiki before continuing.

{% include ltr/warning_note.html note='Keep in mind that this project is in beta and does not fully cover all functions of the Revit API in its custom Revit components. Many of the guides and examples in this Wiki, use custom python components to create the necessary functionality. You are, however, free to grab the python components in these examples and use them in your own Grasshopper definitions' %}

<!-- 10 -->
## Basic Interactions
These pages guide you through automating some of the basic workflows using {{ site.terms.rir }}

- [Interacting with Revit]({{ site.baseurl }}{% link _en/beta/guides/revit-interact.md %})

<!-- 20 -->
###  Geometry Conversion

These pages guide you through converting geometry between Revit and Rhino (or other applications) using {{ site.terms.rir }}

- [Revit Geometry to Rhino]({{ site.baseurl }}{% link _en/beta/guides/revit-to-rhino.md %})
- [Rhino Geometry to Revit]({{ site.baseurl }}{% link _en/beta/guides/rhino-to-revit.md %})

<!-- 30 -->
## Understanding Revit
These pages guide you through understanding the underlying concepts in Revit e.g. its data model, and explains the concepts behind *Categories*, *Families*, *Types*, *Elements*, *Parameters*, etc.

- [Data Model: Elements]({{ site.baseurl }}{% link _en/beta/guides/revit-elements.md %})
- [Data Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %})
- [Data Model: Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %})

<!-- 40 -->
## Modeling in Revit
Grasshopper addon, included with {{ site.terms.rir }}, provides custom Revit-aware nodes that can create native content in Revit. These pages guide you through generating native Revit elements using {{ site.terms.rir }}

- [Walls]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %})
- [Curtain Walls]({{ site.baseurl }}{% link _en/beta/guides/revit-curtainwalls.md %})
- [Spatial Elements]({{ site.baseurl }}{% link _en/beta/guides/revit-spatial.md %})
- [Stairs]({{ site.baseurl }}{% link _en/beta/guides/revit-stairs.md %})
- [Levels & Grids]({{ site.baseurl }}{% link _en/beta/guides/revit-levels.md %})
- [Structural Elements]({{ site.baseurl }}{% link _en/beta/guides/revit-struct.md %})
- [Materials]({{ site.baseurl }}{% link _en/beta/guides/revit-materials.md %})
<!-- 48: last -->
- [Modify Geometry]({{ site.baseurl }}{% link _en/beta/guides/revit-modify.md %})
- [Custom Families]({{ site.baseurl }}{% link _en/beta/guides/revit-customfamilies.md %})

<!-- 50 -->

<!-- 60 -->
## Documentation in Revit
These pages guide you through working with Revit views and sheets using {{ site.terms.rir }}

- [Dimensions]({{ site.baseurl }}{% link _en/beta/guides/revit-dims.md %})
- [Tags and Keynotes]({{ site.baseurl }}{% link _en/beta/guides/revit-tags.md %})
- [Drafting]({{ site.baseurl }}{% link _en/beta/guides/revit-drafting.md %})
- [Views]({{ site.baseurl }}{% link _en/beta/guides/revit-views.md %})
- [View Templates]({{ site.baseurl }}{% link _en/beta/guides/revit-viewtemplates.md %})
- [Schedules & Reports]({{ site.baseurl }}{% link _en/beta/guides/revit-schedules.md %})
- [Sheets]({{ site.baseurl }}{% link _en/beta/guides/revit-sheets.md %})
- [Revisions]({{ site.baseurl }}{% link _en/beta/guides/revit-revisions.md %})

<!-- 70 -->
## Geometry Containers
Revit has a few ways to containerize geometry inside a Revit model. These pages guide you through working with these containers in {{ site.terms.rir }}

- [Groups]({{ site.baseurl }}{% link _en/beta/guides/revit-groups.md %})
<!-- add Assemblies -->
- [Worksets]({{ site.baseurl }}{% link _en/beta/guides/revit-worksets.md %})
- [Design Options]({{ site.baseurl }}{% link _en/beta/guides/revit-designopts.md %})
- [Phases]({{ site.baseurl }}{% link _en/beta/guides/revit-phases.md %})
- [Links]({{ site.baseurl }}{% link _en/beta/guides/revit-links.md %})

<!-- 80 -->

<!-- 90 -->
## Configurations
These pages guide you through working with global model global configurations using {{ site.terms.rir }}

- [Project Settings]({{ site.baseurl }}{% link _en/beta/guides/revit-psettings.md %})
- [Styles and Patterns]({{ site.baseurl }}{% link _en/beta/guides/revit-styles.md %})

## Scripting
<!-- 100 -->
These pages guide you through using Rhino python editor, and Grasshopper scripting components in {{ site.terms.rir }}

- [Grasshopper Python Component]({{ site.baseurl }}{% link _en/beta/guides/rir-ghpython.md %})
- [Grasshopper C# Component]({{ site.baseurl }}{% link _en/beta/guides/rir-csharp.md %})
- [Rhino Python]({{ site.baseurl }}{% link _en/beta/guides/rir-rhpython.md %})
