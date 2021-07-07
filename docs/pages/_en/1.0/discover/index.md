---
title: Rhino.Inside.Revit Community
toc: false
layout: ltr/page-full
excerpt_separator: <!--more-->
---

{% capture community_note %}

## Contributing to Community

The {{ site.terms.rir }} community appreciates and benefits from your contributions. In case you have created videos, articles, blog posts, etc or have developed custom scripted components for {{ site.terms.rir }} you can share your creations with the community in a number of ways. Prepare a package containing links and other resources, and

- Share on the [Discussion Forums]({{ site.forum_url }}){: target='_blank'}
<!-- Email link here is obfuscated. See Wiki docs for guidelines -->
- Email to <a href="#" data-dump="bWFpbHRvOnJoaW5vLmluc2lkZS5yZXZpdEBtY25lZWwuY29tP3N1YmplY3Q9Q29tbXVuaXR5IFN1Ym1pc3Npb24=" onfocus="this.href = atob(this.dataset.dump)">{{ site.terms.rir }} Development/Wiki Team</a>
- Make changes to the Wiki and submit a pull request. [See Instructions Here]({{ site.metawiki_url | prepend: site.repo_url }})

## Featuring Your Discoveries

Each feature page listed here, is a self-contained article on a specific topic. They also might have a ZIP package attached that includes one or more files (e.g. Sample Screenshot `*.png`, Grasshopper Definition `*.gh`, Rhino Model `*.3dm`, Revit Model `*.rvt`, Revit Family `*.rfa`)

Visitors can download the archive for each article by clicking on the download button included on the page. You can create your own articles, following a similar format, and send us the markdown file of the article, plus all the images and attachments in a package and we can add them to this page. You can also follow the [wiki guidelines]({{ site.metawiki_url | prepend: site.repo_url }}) and submit a PR with your content.
{% endcapture %}
{% include ltr/discover.html contents=community_note%}
