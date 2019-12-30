---
title: Selection filters for Revit objects
description: Official developer resources for Rhino and Grasshopper.  Rhino developer tools are royalty free and include support.
lang: en
authors: ['scott_davidson']
layout: fullwidth-page
---

<div class="embed-responsive embed-responsive-16by9">
  <img src="{{ site.baseurl }}/images/rir-floors.jpg" class="img-fluid" alt="Responsive image">
</div>


# Rhino.Inside.Revit

Wecome to the Revolution!
The Rhino.Inside.Revit Project is a super exciting new devlopement sponsored by Robert McNeel and Associates.

## Getting Started

- [Installing and launching Rhino.Inside.Revit]({{ site.baseurl }}/en/guides/getting-started-with-rhino.html)
- [Troubleshooting Installation]({{ site.baseurl }}/en/guides/troubleshooting-rhino-in-revit.html)
- [Developer source on GitHub](https://github.com/mcneel/rhino.inside-revit)
- [Rhino.Inside.Revit discussions on Discourse](https://discourse.mcneel.com/c/rhino-inside/Revit)

## Basic Guides

<div class="trigger">
  {% assign guides = site.en | sort:"order" %}
  <ul>
  {% for guide in guides %}
    {% if guide.categories contains 'started' %}
      {% if guide.title and guide.order %}
        <li><a class="page-link" href="{{ guide.url | prepend: site.baseurl }}" title="{{ guide.description }}">{{ guide.title }}</a></li>
      {% endif %}
    {% endif %}
  {% endfor %}
  </ul>
</div>


## Sample projects using Rhino.Inside.Revit

<div class="trigger">
  {% assign guides = site.en | sort:"order" %}
  <ul>
  {% for guide in guides %}
    {% if guide.categories contains 'general' %}
      {% if guide.title and guide.order %}
        <li><a class="page-link" href="{{ guide.url | prepend: site.baseurl }}" title="{{ guide.description }}">{{ guide.title }}</a></li>
      {% endif %}
    {% endif %}
  {% endfor %}
  </ul>
</div>


---
