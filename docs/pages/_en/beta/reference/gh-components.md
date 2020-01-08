---
title: Components for Revit
order: 2
---

This guide documents the Grasshopper components that support Revit interaction. It is important to have a basic understanding of the [Revit Data Hierarchy](https://www.modelical.com/en/gdocs/revit-data-hierarchy/) when working with Revit-aware components to create and edit Revit content.


{% for comp_group in site.data.components %}
## {{ comp_group.title }} Components
{% include ltr/comp-table.html components=comp_group.comps %}
{% endfor %}
