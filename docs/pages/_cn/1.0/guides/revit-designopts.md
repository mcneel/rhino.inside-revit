---
title: Design Options
subtitle: How to work with Design Options and Sets
order: 72
thumbnail: /static/images/guides/revit-designopts.png
group: Containers
ghdef: revit-designopts.ghx
---

{% include ltr/warning_note.html note=' 当前在 Revit API 中对设计选项的支持非常少' %}

## 查询设计选项

{% capture api_note %}
在 Revit API 中使用{% include api_type.html type='Autodesk.Revit.DB.DesignOption' title='DB.DesignOption' %}来表达设计选项
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

如果需要在 Revit UI 中查找当前有用的设计选项，请使用 {% include ltr/comp.html uuid='b6349dda' %} 运算器。

![]({{ "/static/images/guides/revit-designopts-active.png" | prepend: site.baseurl }})

然后你可以联合 {% include ltr/comp.html uuid='677ddf10' %} 与 {% include ltr/comp.html uuid='01080b5e' %} 运算器来检查每个**设计选项**或**设计选项设置**的详细数据，

![]({{ "/static/images/guides/revit-designopts-identity.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-designopts-optsetidentity.png" | prepend: site.baseurl }})

如果需要查询一个文档中所有的 **设计选项** 与 **设计选项设置** ，请使用 {% include ltr/comp.html uuid='b31e7605' %} 与 {% include ltr/comp.html uuid='6804582b' %} 来实现，

![]({{ "/static/images/guides/revit-designopts-queryoptsets.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-designopts-queryopts.png" | prepend: site.baseurl }})

{% capture api_note %}
请注意**设计选项设置**对象是一个简单的 `DB.Element` ，因为 Revit API 中对设计选项的支持非常有限
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## 收集设计选项图元

可以将设计选项链接至 {% include ltr/comp.html uuid='1b197e82' %} 运算器来收集一个给定设计选项的所有图元，

![]({{ "/static/images/guides/revit-designopts-queryelements.png" | prepend: site.baseurl }})

## 删除设计选项

{% capture doptsrem_note %}
删除设计选项是非常具有挑战的事情，所以我们并没有准备提供相关工作流程，且请注意：

- 删除一个设计选项也会删除所有引用该设计选项的所有视图，解决方法是先读取视图对象的 `BuiltInParameter.VIEWER_OPTION_VISIBILITY` 参数，如果它有值则意味着它引用了设计选项, 请将这个值设置为 `InvalidElementId` 以删除其引用，且还要通知用户那些视图已经被修改。
- 删除一个设计选项也会删除这个设计选项中的所有图元，理想情况下，用户需要在删除设计选项之前决定是否需要重新定位这些图元。
- 设计选项可使用 `Document.Delete()` 来删除
  {% endcapture %}
  {% include ltr/warning_note.html note=doptsrem_note %}
