---
title: Components for Revit
order: 11
group: User Interface
---

This guide documents the Grasshopper components that support Revit interaction. It is important to have a basic understanding of the Revit Data Hierarchy when working with Revit-aware components to create and edit Revit content.


{% assign comp_groups = site.data.components | sort: "panel" | group_by: "panel" %}
{% for comp_group in comp_groups %}
## {{ comp_group.name }} Components
{% assign sorted_comp_groups = comp_group.items | sort: "title" %}
{% include ltr/comp_table.html components=sorted_comp_groups %}
{% endfor %}
