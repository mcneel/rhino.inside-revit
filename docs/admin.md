---
layout: fullwidth-page
title: Administration
permalink: /admin/
order: 1
---

# Administration

This site is the _future home_ of [Flamingo Help Docs](http://help.mcneel.com/en/flamingo/5/website/welcome.html).

The goal of this site is to consolidate all the (now) scattered developer documentation into a single canonical site that is clear, easy-to-navigate, with consistent formatting and nomenclature.

The sources of content-to-be-consolidated are:

- [Flamingo Online Help](http://docs.mcneel.com/flamingo/nxt/help/en-us/index.htm)
- [Flamingo Tutorials](http://nxt.flamingo3d.com/page/tutorials-and-documentation) - a mix of samples, how-tos, and guides
- [Flamingo WIKI](http://wiki.mcneel.com/flamingo/5/primer)
- Parts of [Rhino developer Kit](http://wiki.mcneel.com/labs/rendererdevelopmentkit10)

---

## Pages instructions:

  <a class="page-link" href="https://github.com/mcneel/help-docs/blob/gh-pages/README.md">Getting Started with Dev Docs</a>

## TODO List
To leave To Do Notes in the markdown pages, add a standard HTML comment with the following content:

TODO: 'Put the comments here'

This will list the files with TODO below.

<div class="trigger">
  {% assign pages = site.en | sort: 'url' %}
  <ul>
  {% for page in pages %}
        {% if page.content contains "TODO" and page.path != "admin.md" %}
          <li><a class="page-link" href="http://github.com/mcneel/help-docs/edit/gh-pages/{{page.path}}">{{page.path}}</a></li>
        {% endif %}
  {% endfor %}
  </ul>
</div>
