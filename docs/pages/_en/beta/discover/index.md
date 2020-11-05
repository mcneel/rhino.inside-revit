---
title: Discover Rhino.Inside.Revit
layout: ltr/page-fullwidth
toc: false
---

{% capture discover_note %}
Discover more about {{ site.terms.rir }}! The resources here created are by the development team and other contributors, and showcase the power of {{ site.terms.rir }} in different workflows
{% endcapture %}
{% include ltr/bubble_note.html note=discover_note %}


<div class="gallery-large-grid">
{% for collection in site.collections %}
{% if collection.label == page.collection %}
{% assign articles = collection.docs | sort:"order" %}
{% for item in articles %}
{% if item.toc and item.title and item.version == page.version and item.categories == page.categories %}
<div class="gallery-item" >
<a href="{{ item.url | prepend: site.baseurl }}">
    <div class="gallery-thumbnail gallery-thumbnail-dim gallery-thumbnail-large">
    {% if item.thumbnail %}
        <img src="{{ item.thumbnail | prepend: site.baseurl }}" />
    {% else %}
        <img src="{{ site.baseurl }}/assets/img/gallery-placeholder.png" />
    {% endif %}
    </div>
</a>
<div class="gallery-info">
    <a class="title" href="{{ item.url | prepend: site.baseurl }}">{{ item.title }}</a>
    <div class="extra">
        {{ item.description | markdownify }}
    </div>
</div>
</div>
{% endif %}
{% endfor %}
{% endif %}
{% endfor %}
</div>

## Sharing Your Discoveries

Each discover page listed here, is a self-contained article on a specific topic. They also might have a ZIP package attached that includes one or more files (e.g. Sample Screenshot `*.png`, Grasshopper Definition `*.gh`, Rhino Model `*.3dm`, Revit Model `*.rvt`, Revit Family `*.rfa`)

Visitors can download the archive for each article by clicking on the download button included on the page. You can create your own articles, following a similar format, and send us the markdown file of the article, plus all the images and attachments in a package and we can add them to this page. You can also follow the [wiki guidelines]({{ site.metawiki_url | prepend: site.repo_url }}) and submit a PR with your content.
