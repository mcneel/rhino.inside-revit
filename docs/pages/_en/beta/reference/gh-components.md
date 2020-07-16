---
title: Components for Revit
order: 11
group: User Interface
---

This guide documents the Grasshopper components that support Revit interaction. It is important to have a basic understanding of the Revit Data Hierarchy when working with Revit-aware components to create and edit Revit content.


{% assign sorted_comp_groups = (site.data.components | group_by: "panel" | sort: "name") %}
{% for comp_group in sorted_comp_groups %}
## {{ comp_group.name }} Components
{% include ltr/comp_table.html components=comp_group.items %}
{% endfor %}
