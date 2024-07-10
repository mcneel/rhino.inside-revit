---
title: {{ site.terms.rir }}
layout: ltr/page-full

---

{% capture banner_title %}
{{ site.terms.rir }} {{ site.versions.one.name }}简介
{% endcapture %}

{% capture banner_note %}
{{ site.terms.rir }}是由{{ site.terms.mcneel }}赞助的一个备受关注的项目，旨在将{{ site.terms.rhino }} 与 Grasshopper的强大功能无缝整合至 {{ site.terms.revit }} 环境中
{% endcapture %}

{% capture rhino_note %}
{{ site.terms.rhino }} 是一个曲面建模程序，拥有极其快速且精准的自由形态引擎，且支持网格与点云。Rhino原生支持40多种文件格式，具有非常好的兼容性。它易学易用，高度的可定制性与扩展性，拥有一个非常强大的第三方生态系统，提供成千的行业插件带来更多额外功能。
{% endcapture %}

{% capture gh_note %}
Grasshopper 是一个内置于 Rhino 的可视化编程环境，使用 Grasshopper 并不需要编程或写脚本的知识，也能让设计师创建形式各异的派生式造型，Grasshopper 也拥有非常丰富的插件库。
{% endcapture %}

{% capture ghcomp_note %}
{{ site.terms.rir }} 目前包含300多个 Revit专属运算器，可以用来查询、修改、分析和创建原生的 Revit 图元，将持续开发更多的运算器的新新版本，未来能支持更多的 Revit 原生类型。
{% endcapture %}

{% capture bim360_note %}
我们提供 [BIM360 File Locker](https://github.com/eirannejad/BIM360FileLockerForRhino) for Rhino {{ site.terms.rhino_versions }}插件，可以帮你的团队在同一个{{ site.terms.bim360 }} 项目下使用 Revit、Rhino与Grasshopper,  可以利用[Autodesk Desktop Connector](https://www.autodesk.com/bim-360/desktop-connector/) 链接、共享、锁定访问、获取提醒且与 {{ site.terms.bim360 }} 同步
{% endcapture %}

{% capture api_note %}
{{ site.terms.rir }} 提供API转换，允许在 Revit 数据与 Rhino 几何和元数据之间建立自定义转换工作流程，包括高级的几何转换API，可以将复杂的 Rhino 几何形状安全的转换至 Revit,可以通过 Grasshopper 的 Python 或 C# 脚本工具来轻松访问这些API 
{% endcapture %}

{% include ltr/home.html
   inside_text=".Inside.Revit"
   version=site.versions.one

   banner_header=banner_title
   banner_text=banner_note
   banner_img="/static/images/home/background.gif"

   rhino_header="Rhinoceros"
   rhino_text=rhino_note
   rhino_learn_title="了解更多 Rhino..."
   rhino_learn_link="https://www.rhino3d.com"
   rhino_img1="/static/images/home/home_feature_rh3.webp"
   rhino_img2="/static/images/home/home-nbbj-stadium.webp"
   rhino_img3="/static/images/home/home-QMA.webp"

   gh_header="Grasshopper"
   gh_text=gh_note
   gh_learn_title="了解更多 Grasshopper..."
   gh_learn_link="https://www.grasshopper3d.com"
   gh_img="/static/images/home/home_GH_drag.gif"

   ghcomp_header="Revit专属Grasshopper运算器"
   ghcomp_text=ghcomp_note
   ghcomp_learn_link="guides/"
   ghcomp_learn_title="查看更多手册内容 ..."

   bim360_header="BIM360 集成"
   bim360_text=bim360_note
   bim360_img="/static/images/home/home-bim360rhino.png"

   api_header="Python 与 C# 脚本"
   api_text=api_note
   api_img="/static/images/home/home_feature_api.gif"

   discover_header="发现更多 ..."
   discover_link="discover/"
   discover_count="9" %}
