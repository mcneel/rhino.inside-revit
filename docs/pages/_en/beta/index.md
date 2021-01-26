---
title: {{ site.terms.rir }}
layout: ltr/page-full
---

{% capture banner_title %}
Introducing {{ site.terms.rir }} {{ site.versions.beta.name }}
{% endcapture %}

{% capture banner_note %}
The {{ site.terms.rir }} project is an exciting new development sponsored by {{ site.terms.mcneel }} that brings the power of {{ site.terms.rhino }} and Grasshopper to the {{ site.terms.revit }} environment
{% endcapture %}

{% capture rhino_note %}
{{ site.terms.rhino }} is a surface modeling application with an incredibly fast and accurate freeform geometry engine that also supports meshes and point clouds. Rhino supports more that 40+ file formats, making it a great interoperability tool. It is easy to learn, very customizable, extensible, and has a very strong third-party ecosystem with over a thousand add-ons providing lots of additional capabilities
{% endcapture %}

{% capture gh_note %}
Grasshopper is a visual programming environment tightly integrated with Rhino’s 3d modeling tools. Grasshopper requires no knowledge of programming or scripting, but still allows designers to build generative forms from the simple to the awe-inspiring. Grasshopper also has a very rich add-on library
{% endcapture %}

{% capture ghcomp_note %}
{{ site.terms.rir }} adds over 300 Revit-aware components to Grasshopper that can query, modify, analyze, and create native Revit elements. More components are added in each release to support more Revit native types
{% endcapture %}

{% capture api_note %}
{{ site.terms.rir }} provides translation API to allow creating custom conversion workflows between your Revit data and Rhino geometry and metadata. This includes an advanced geometry conversion API to safely translate advanced Rhino shapes into Revit. The API is easily accessible from Grasshopper's Python or C# scripting components
{% endcapture %}

{% include ltr/home.html 
   inside_text=".Inside.Revit"
   
   banner_header=banner_title
   banner_text=banner_note
   banner_img="/static/images/home/background.gif"
   
   rhino_header="Rhinoceros 7"
   rhino_text=rhino_note
   rh_img1="/static/images/home/home_feature_rh1.webp"
   rh_img2="/static/images/home/home_feature_rh2.webp"
   rh_img3="/static/images/home/home_feature_rh3.webp"
   
   gh_header="Grasshopper"
   gh_text=gh_note
   gh_img="/static/images/home/home_feature_gh1.png"
   
   ghcomp_header="Revit-Aware Grasshopper Components"
   ghcomp_text=ghcomp_note
   
   api_header="Scripting API"
   api_text=api_note
   api_img="/static/images/home/home_feature_api.gif"
   
   discover_header="Discover More ..."
   discover_count="9" %}
