---
title: Rhino.Inside.Revit Community
layout: ltr/page-fullwidth
toc: false
---
{% capture community_note %}
Welcome to the {{ site.terms.rir }} community. On this page, you will find the resources created by, and for the community. Please see the [Discussion Forums]({{ site.forum_url }}){: target='_blank'} to discuss features and potential issues and ask questions
{% endcapture %}
{% include ltr/bubble_note.html note=community_note %}

<div class="gallery-large-grid">
    {% for item in site.data.community %}
    <div class="gallery-item" >
    <a href="{{ item.url }}" target="blank">
        <div class="gallery-thumbnail gallery-thumbnail-dim">
        {% if item.thumbnail %}
            <img src="{{ item.thumbnail}}" />
        {% else %}
            <img src="{{ site.baseurl }}/assets/img/gallery-placeholder.png" />
        {% endif %}
        </div>
    </a>
    <div class="gallery-info">
        {% if item.type == 'video' %}
            <img width="28" height="28" src="{{ site.baseurl }}/assets/img/video.svg" />
        {% elsif item.type == 'blogpost' %}
            <img width="28" height="28" src="{{ site.baseurl }}/assets/img/post.svg" />
        {% else %}
            <img width="28" height="28" src="{{ site.baseurl }}/assets/img/link.svg" />
        {% endif %}
        <a class="title" href="{{ item.url }}" target="blank">{{ item.title }}</a>
        <a class="author" href="{{ item.authorUrl }}">{{ item.author }}</a>
        <div class="extra">
            {{ item.description | markdownify }}
        </div>
    </div>
    </div>
    {% endfor %}
</div>
    
## Contributing to Community

The {{ site.terms.rir }} community appreciates and benefits from your contributions. In case you have created videos, articles, blog posts, etc or have developed custom scripted components for {{ site.terms.rir }} you can share your creations with the community in a number of ways. Prepare a package containing links and other resources, and

- Share on the [Discussion Forums]({{ site.forum_url }}){: target='_blank'}
<!-- Email link here is obfuscated. See Wiki docs for guidelines -->
- Email to <a href="#" data-dump="bWFpbHRvOnJoaW5vLmluc2lkZS5yZXZpdEBtY25lZWwuY29tP3N1YmplY3Q9Q29tbXVuaXR5IFN1Ym1pc3Npb24=" onfocus="this.href = atob(this.dataset.dump)">{{ site.terms.rir }} Development/Wiki Team</a>
- Make changes to the Wiki and submit a pull request. [See Instructions Here]({{ site.metawiki_url | prepend: site.repo_url }})

