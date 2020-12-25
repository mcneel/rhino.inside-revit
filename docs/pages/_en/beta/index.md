---
title: {{ site.terms.rir }}
layout: ltr/page-home
---

<div class="home-cover">
    <div class="home-gallery">
        <img src="{{ "/static/images/home/background.gif" | prepend: site.baseurl }}" />
    </div>

    <div class="home-contents">
        <div class="home-contents-textbox">
            <h1>Power of Rhino and Grasshopper, Inside Revit®</h1>
            <h3>The {{ site.terms.rir }} project is an exciting new development sponsored by {{ site.terms.mcneel }}. The {{ site.terms.rir }} brings the power of {{ site.terms.rhino }} and Grasshopper to the {{ site.terms.revit }} environment</h3>
        </div>
    </div>
</div>

<div class="home-download">
    <h3>Power of Rhino and Grasshopper, Inside Revit®</h3>

    <a type="button" class="btn btn-danger rir-dl" href="{{ include.version.rir_download }}" target="_blank">
        <img class="button-icon" src="{{ "/assets/img/install.svg" | prepend: site.baseurl }}">
        Download {{ site.terms.rir }} <small>{{ include.version.name }}</small>
    </a>
    <a type="button" class="btn btn-danger rhino-dl" href="{{ include.version.rhino_download }}" target="_blank">
        <img class="button-icon" src="{{ "/assets/img/install.svg" | prepend: site.baseurl }}">
        Download {{ site.terms.rhino }} {{ site.terms.rhinoext }}
    </a>
</div>

<style>
    .footer-languages {
        visibility: hidden;
        margin: 0px;
    }
</style>