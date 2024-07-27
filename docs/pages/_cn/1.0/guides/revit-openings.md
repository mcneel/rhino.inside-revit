---
title: Openings
order: 48
thumbnail: /static/images/guides/revit-opening.png
subtitle: Workflows for Revit Openings
group: Modeling
---

## Querying Openings

{% capture api_note %}
在 Revit API 中用 {% include api_type.html type='Autodesk.Revit.DB.Opening' title='DB.Opening' %} 来表达开口类型，在 {{ site.terms.rir }} 中使用 {% include ltr/comp.html uuid='18d46e90' %} 运算器 来表达
{% include ltr/api_note.html note=api_note %}

如果需要选择适当的开口类型，可以使用 {% include ltr/comp.html uuid='0f7da57e' %} + {% include ltr/comp.html uuid='d08f7ab1' %} 配合 {% include ltr/comp.html uuid='af9d949f' %} 中过滤且选择 **矩形直墙开口** 或 **矩形弧墙开口** (针对计算不同的直墙与弧墙几何图形而提供的两个类别）与**竖井洞口** ；

![]({{ "/static/images/guides/revit-openings-queyrwall.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-openings-queryshaft.png" | prepend: site.baseurl }})

想找到曾经在有些Revit 图元上所创建过的开口，可以使用 {% include ltr/comp.html uuid='70ccf7a6' %} 运算器来查找一个 Revit 图元上带有的开口；

![]({{ "/static/images/guides/revit-openings-queryhostwall.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-openings-queryhostroof.png" | prepend: site.baseurl }})

请注意上面的方法并不适合于竖井开口，因为竖井是以相邻的两个楼板为界垂直的图元，它会贯穿其间的任何楼板、天花板与屋顶图元。所以使用 {% include ltr/comp.html uuid='70ccf7a6' %} 运算器不会返回任何竖井开口；

![]({{ "/static/images/guides/revit-openings-queryhostfloor.png" | prepend: site.baseurl }})

## 分析开口

可以使用 {% include ltr/comp.html uuid='fad33c4b' %} 运算器来检索一个开口件的类别属性，例如下面示范的检索 矩形墙开口实例；

![]({{ "/static/images/guides/revit-openings-analyzeprops.png" | prepend: site.baseurl }})

你也可以使用 {% include ltr/comp.html uuid='e76b0f6b' %} 运算器来提取一个给定开口的边框轮廓：

![]({{ "/static/images/guides/revit-openings-analyzeprofile.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='6723beb1' %} 运算器来侦测一个带有指定开口实例的主体图元，例如一个带有指定开口的墙体,

![]({{ "/static/images/guides/revit-openings-queryarcwall.png" | prepend: site.baseurl }})

{% capture api_note %}
当前天窗并没有在 Revit AIP 中完整覆盖， 所以当你检查天窗时除了 Phases 之外没有其他的属性信息：

![]({{ "/static/images/guides/revit-openings-analyzedormerprops.png" | prepend: site.baseurl }})

但天窗开口轮廓依然可以使用 {% include ltr/comp.html uuid='e76b0f6b' %} 运算器来提取：

![]({{ "/static/images/guides/revit-openings-analyzedormerprofile.png" | prepend: site.baseurl }})
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 由竖井切割的图元

如前面所提及的无法指定任何的图元为竖井开口的主体，为了检测图元（下面以楼板为例）是否被竖井开口所切割，我们可以使用 {% include ltr/comp.html uuid='f5a32842' %}  运算器来过滤那些与竖井边框发生碰撞的图元， {% include ltr/comp.html uuid='18d46e90' %} 运算器包含竖井开口；

![]({{ "/static/images/guides/revit-openings-filtershaftfloors.png" | prepend: site.baseurl }})

### 竖井开口轮廓

为了提取一个主体图元（例如下面范例中的楼板）边框轮廓，可以使用 {% include ltr/comp.html uuid='032ad3f7' %} 运算器配合提取BREP顶部或底部的曲面，解构BREP也会得到轮廓曲线：

![]({{ "/static/images/guides/revit-openings-floorprofile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-openings-floorprofile-sc.png" | prepend: site.baseurl }})

## 创建墙体开口

建立一个墙体开口如同当你在 Revit 内建立一个开放的墙体，你需要在墙体上指定两点，用来定义切割矩形的最大与最小的点, {% include ltr/comp.html uuid='c86ed84c' %} 运算器载入输入的墙体与点，以创建如下图所示的一个开口图元：

![]({{ "/static/images/guides/revit-openings-createwall.png" | prepend: site.baseurl }})

## 建立竖井

可以使用 {% include ltr/comp.html uuid='657811b7' %} 运算器来创建竖井， {% include ltr/comp.html uuid='bd6a74f3' %} 用来选择竖井的顶部与底部边框楼板，Boundary 输入项载入一条曲线作为竖井的截面轮廓：

![]({{ "/static/images/guides/revit-openings-createshaft.png" | prepend: site.baseurl }})

也可以使用 {% include ltr/comp.html uuid='01c853d8' %} 运算器来构造一个指定的楼板，配合不同的偏移值得到**顶部** 与**底部** 输入值；

![]({{ "/static/images/guides/revit-openings-createshaftoffset.png" | prepend: site.baseurl }})

## 建立竖直开口

可以使用 {% include ltr/comp.html uuid='c9c0f4d2' %} 运算器配合主体图元与边框曲线在屋顶、天花板与楼板（当前 Revit API 仅支持非倾斜楼板）上建立竖直开口件：

![]({{ "/static/images/guides/revit-openings-vertical.png" | prepend: site.baseurl }})

请注意当前 Revit API 仅支持非倾斜楼板上建立竖直开口，所以主体楼板如果带有斜度时无法建立任何开口件：

![]({{ "/static/images/guides/revit-openings-vertical-nofloor.png" | prepend: site.baseurl }})
