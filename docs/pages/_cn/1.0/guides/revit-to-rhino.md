---
title: "Revit to Rhino"
subtitle: How to extract geometry and data from Revit into Rhino
order: 30
thumbnail: /static/images/guides/revit-to-rhino.png
group: Essentials
---

{% capture link_note %}
这里将讨论如何把 Rhino 数据无损导入Revit 中
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-to-rhino.png' %}

如何借助 Rhino.Inside.Revit 将 Revit 的图元无损导入至 Rhino ？ 通常需要下面三个步骤

1. 选择准备输出的 Revit 图元；
2. 提取每个Revit 图元的几何、类别名称或其他需要的信息；
3. 烘焙几何与信息至Rhino，

![]({{ "/static/images/guides/revit-view-to-rhino.png" | prepend: site.baseurl }})

**Element Geometry 运算器** 有一个隐藏的输出选项 Category 非常有用，放大运算器且点击 Geometry 输出项下面的 + 号，就可以调出 Category 输出项。

有很多第三方的插件也可以将物件信息烘焙至 Rhino，包括图层、物件名称、颜色、材质或是用户数据（关键值/数值）信息，常用的有 [Human](https://www.food4rhino.com/en/app/human), [LunchBox](https://www.food4rhino.com/en/app/lunchbox) 与 [Elefront](https://www.food4rhino.com/en/app/elefront).

{% include youtube_player.html id="DVzsSyxTQS0" %}

## 择需要输出的图元

Grasshopper 提供多种方法从 Revit 中选择物件，主要包括下面三个方法；

1. 使用视图过滤工具引入可见内容，这可能是最简单的方法。
2. 大多数情况下可以利用类别过滤工具，虽然使用稍稍有一点复杂，因为类别比较广泛。
3. 直接在 Revit 中选择图元也是一个很简单直接的方法。

### 视图中可见内容 (推荐)

选择要导入 Rhino 的 Revit 对象的最佳方法之一是根据特定视图中的可见内容，视图中保存的是所需的类别、工作集和阶段。创建专门用于导入到 Rhino 的特定视图是存储配置的好方法，这样任何工作视图的当前状态都不是问题。

![]({{ "/static/images/guides/revit-to-rhino-select.png" | prepend: site.baseurl }})

关于上面截图的注意事项:

1. 视图名称 *To Rhino* 是已经在当前项目中保存过的3D视图。
2. {% include ltr/comp.html uuid="df691659-" %} 返回视图中的图元，作为{% include ltr/comp.html uuid="ac546f16-" %} 运算器的输入。
3. 使用 *QuareyElements* 来过滤，*L（Limite）* 输入端通过放到运算器然后点击 - 号隐藏了这个输入项。
4. 选择图元后，其他的一些运算器设置请参考前面的截图。

### 过滤筛选

推荐使用 {% include ltr/comp.html uuid="d08f7ab1-" %}. 来进行筛选，这样可以快速的获取需要的内容，但要注意 Revit 的类别非常多，所以通常使用 {% include ltr/comp.html uuid="d08f7ab1-" %} 都会搭配一些其他的过滤器来选择具体的图元，例如 {% include ltr/comp.html uuid="6804582b-" %} 或 {% include ltr/comp.html uuid="805c21ee-" %} 用来限制选择范围。

另外在尝试获取一些不同类别时，所需要的类别列表就会更丰富，也会造成列表非常庞大…

![]({{ "/static/images/guides/categories-rir-to-rhino.png" | prepend: site.baseurl }})

### 选择图元

获取 Revit 图元最简单的方法时使用 {% include ltr/comp.html uuid="ef607c2a-" %} + {% include ltr/comp.html uuid="b3bcbf5b-" %}，这样将仅出输出所选择的图元

![]({{ "/static/images/guides/gh-revit-select-to-rhino.png" | prepend: site.baseurl }})

## 空间图元

Revit 空间图元例如房间与区域可以烘焙至 Rhino , 更多详情请浏览  [Getting Spatial Element Geometry Guide](https://www.rhino3d.com/inside/revit/1.0/guides/revit-spatial#getting-spatial-element-geometry)

## 分析图元

Revit 中的很多图元可以建立分析图元且能导入至 Rhino 中，更多详情请浏览  [Working with Analytical Models Guide](https://www.rhino3d.com/inside/revit/1.0/guides/revit-struct) 
