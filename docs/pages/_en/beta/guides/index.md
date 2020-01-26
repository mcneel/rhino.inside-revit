---
title: Rhino.Inside.Revit Guides
---

This section includes many articles that guide you through solving many {{ site.terms.revit }} challenges using {{ site.terms.rir }}. Make sure to take a look at the *Getting Started* guide on this Wiki before continuing.

{% include ltr/warning_note.html note='Keep in mind that this project is in beta and does not fully cover all functions of the Revit API in its custom Revit components. Many of the guides and examples in this Wiki, use custom python components to create the necessary functionality. You are, however, free to grab the python components in these examples and use them in your own Grasshopper definitions.' %}

<!-- 10 -->
## Basic Interactions

- [Reading & Writing Revit Parameter Values]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %})

<!-- 20 -->
## Geometry Conversion

These pages guide you through converting geometry between Revit and Rhino (or other applications) using {{ site.terms.rir }}
- [Revit Geometry to Rhino]({{ site.baseurl }}{% link _en/beta/guides/revit-to-rhino.md %})
- [Rhino Geometry to Revit]({{ site.baseurl }}{% link _en/beta/guides/rhino-to-revit.md %})

<!-- 30 -->
## Revit Element Types
These pages guide you through working with Revit element types using {{ site.terms.rir }}

- [Revit Categories, Types, and Families]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %})

<!-- 40 -->
## Revit Elements
{{ site.terms.rir }} provides custom Revit-aware nodes that can create native content in Revit. These pages guide you through generating native Revit elements using {{ site.terms.rir }}

- [Revit Walls]({{ site.baseurl }}{% link _en/beta/guides/revit-walls.md %})
- [Revit Curtain Walls]({{ site.baseurl }}{% link _en/beta/guides/revit-curtainwalls.md %})
- [Revit Spatial Elements]({{ site.baseurl }}{% link _en/beta/guides/revit-spatial.md %})
- [Revit Structural Elements]({{ site.baseurl }}{% link _en/beta/guides/revit-struct.md %})

<!-- 60 -->
## Revit Views & Sheets
These pages guide you through working with Revit views and sheets using {{ site.terms.rir }}

- [Revisions]({{ site.baseurl }}{% link _en/beta/guides/revit-revisions.md %})

<!-- 70 -->
## Revit Geometry Containers
Revit has a few ways to containerize geometry inside a Revit model. These pages guide you through working with these containers in {{ site.terms.rir }}

- [Revit Groups]({{ site.baseurl }}{% link _en/beta/guides/revit-groups.md %})
<!-- add Assemblies -->
- [Revit Worksets]({{ site.baseurl }}{% link _en/beta/guides/revit-worksets.md %})
- [Revit Design Options]({{ site.baseurl }}{% link _en/beta/guides/revit-designopts.md %})
- [Revit Phases]({{ site.baseurl }}{% link _en/beta/guides/revit-phases.md %})
- [Revit Links]({{ site.baseurl }}{% link _en/beta/guides/revit-links.md %})

<!-- 80 -->
## Revit Parameters
These pages guide you through working with Revit parameters using {{ site.terms.rir }}

<!-- 90 -->
## Revit Model Configs
These pages guide you through working with global model configurations using {{ site.terms.rir }}

- [Revit Line Styles]({{ site.baseurl }}{% link _en/beta/guides/revit-linestyles.md %})

## Scripting
<!-- 100 -->
These pages guide you through using Rhino python editor, and Grasshopper scripting components in {{ site.terms.rir }}

- [Grasshopper Python Component]({{ site.baseurl }}{% link _en/beta/guides/rir-ghpython.md %})
- [Grasshopper C# Component]({{ site.baseurl }}{% link _en/beta/guides/rir-csharp.md %})
- [Rhino Python]({{ site.baseurl }}{% link _en/beta/guides/rir-rhpython.md %})
