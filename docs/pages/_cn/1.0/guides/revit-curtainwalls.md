---
title: Curtain Walls & Systems
order: 41
group: Modeling
home: true
thumbnail: /static/images/guides/revit-curtainwalls.png
subtitle: Workflows for Revit Curtain Walls and Systems
ghdef: revit-curtainwalls.ghx
---

<!-- Curtain Walls -->

这一章将讨论一组特殊的墙系统族 - 幕墙， 对 [Walls (Basic & Stacked)]({{ site.baseurl }}{% link _en/1.0/guides/revit-walls.md %}) 基础认知有助于更好的理解 {{ site.terms.revit }} 中的幕墙系统

## 幕墙网格

**幕墙** 是 {{ site.terms.revit }} 中的一类特殊墙系统族，这些墙的几何形状都是基于底层的UV网格来创建，网格配置是设置在幕墙类型中:

![]({{ "/static/images/guides/revit-curtains01.jpeg" | prepend: site.baseurl }})

**幕墙网格** 分为 U、V两个轴（方向），在 Revit API 中U方向是墙体的垂直轴、V方向是墙体的基先（U线扫过V线且通常相互垂直）：

![]({{ "/static/images/guides/revit-curtains02.jpeg" | prepend: site.baseurl }})

**网格** 也可以是非90度（在曲面幕墙上比较常见）：

![]({{ "/static/images/guides/revit-curtains03.jpeg" | prepend: site.baseurl }})

**幕墙竖梃** 都附加至每个网格段上，且都基于幕墙类型内的设置组合链接：

![]({{ "/static/images/guides/revit-curtains04.png" | prepend: site.baseurl }})

幕墙水平与垂直边框线上也会附加**幕墙竖梃** ，但请注意这些边框线虽然是墙体结构定义的一部分，但并不是墙体网格，墙体网格仅指幕墙边框内的线段；

![]({{ "/static/images/guides/revit-curtains05.png" | prepend: site.baseurl }})

由网格构成的四边区域称之为**网格单元** ：

![]({{ "/static/images/guides/revit-curtains06.png" | prepend: site.baseurl }})

 可以将幕墙嵌板（系统族）或幕墙族（设计用于插入幕墙单元充当自定义面板的特殊灵活族）实例插入到这些单元中完成几何图形的定义，幕墙嵌板还可以自定义为实心嵌板、模板嵌板，甚至是空置 (!) 区域：

![]({{ "/static/images/guides/revit-curtains07.jpeg" | prepend: site.baseurl }})

可以在幕墙类型中定义幕墙竖梃与嵌板，也可以直接覆盖在墙体实例。

## 墙体 vs 系统

在定义上**幕墙** 与**幕墙系统** 几乎相同，唯一的区别是**幕墙** 是垂直且具有方向性的，因此网格定义设置为水平与垂直的网格线，幕墙系统可以基于大量的曲面来创建尔不会限制下垂的方向，因此其网格配置了 **Grid1** 与 **Grid2** 两个轴向

{% capture api_note %}
在 Revit API 中 幕墙由 {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %} 来表达，幕墙系统由 {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %}表达。 {% include ltr/comp.html uuid='15ad6bf9' %} 在 {{ site.terms.rir }} 中来代表各类墙体，也有一些特定的图元中含有与幕墙相关数据的数据，例如 {% include ltr/comp.html uuid='e94b20e9' %}, {% include ltr/comp.html uuid='7519d945' %}, {% include ltr/comp.html uuid='f25fac7b' %}, 等。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## 查询幕墙

拾取基础墙与叠层墙至 **Wall System Family** 运算器, 可以帮助筛选幕墙类型与类别；

![]({{ "/static/images/guides/revit-curtainwalls-query.png" | prepend: site.baseurl }})

{% capture bubble_note %}
下面的截图示范如何利用 {% include ltr/comp.html uuid="d08f7ab1-" %} 与 {% include ltr/comp.html uuid="7b00f940-" %} 运算器来过滤模型中幕墙嵌板、幕墙竖梃的类型与实例，当然这个方法不会反馈幕墙网格信息。浏览  [Analyzing Curtain Walls](#analyzing-curtain-walls) 章节了解更好的流程来提取面板与竖梃信息，正如你所看到的，幕墙网格不会返回任何网格类型结果，但可以收集面板与竖梃类型：

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-querypartstypes.png" | prepend: site.baseurl }})

&nbsp;

选择实例也相同：

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-queryparts.png" | prepend: site.baseurl }})
{% endcapture %}
{% include ltr/bubble_note.html note=bubble_note %}

## 分析幕墙类型

可以使用 {% include ltr/comp.html uuid='d0874f93' %} 对收集的幕墙类型进行属性分析， {% include ltr/comp.html uuid='c84653dd' %}, {% include ltr/comp.html uuid='cd3e68b2' %}, 与 {% include ltr/comp.html uuid='9c2d116d' %} 等运算器可以帮助过滤相关的参数值：

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwtype.png" | prepend: site.baseurl }})

## 分析幕墙

### 提取幕墙几何

利用 {% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器可以提取任何一个幕墙的实例的全部几何：

![]({{ "/static/images/guides/revit-curtainwalls-cwallgeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls-cwallgeomscap.png" | prepend: site.baseurl }})

如下图所示 {% include ltr/comp.html uuid="3396dbc4-" %} 运算器可以提取给定的幕墙实例的边框几何内容：

![]({{ "/static/images/guides/revit-curtainwalls-cwallboundinggeom.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-cwallboundinggeomscap.png" | prepend: site.baseurl }})

### 嵌入幕墙

幕墙可以嵌入其他墙体内，前面示范过 {% include ltr/comp.html uuid="734b2dac-" %} 运算器可以提供对主墙体的访问，将给定的幕墙嵌入其中，如果没有给定嵌入幕墙会返回  `null` 值：

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwallhost.png" | prepend: site.baseurl }})

### 提取网格

{% capture api_note %}
在 Revit API 中幕墙网格由  {% include api_type.html type='Autodesk.Revit.DB.CurtainGrid' title='DB.CurtainGrid' %} 表达
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

前面示范过 {% include ltr/comp.html uuid="734b2dac-" %} 运算器还可以访问给定幕墙实例的幕墙网格，基于幕墙网格可以获取网格的准确信息，也可以访问每一个幕墙单元、竖梃和嵌板的相关内容：

![]({{ "/static/images/guides/revit-curtainwalls-analyzecwall.png" | prepend: site.baseurl }})

## 分析幕墙网格

利用下面这些运算器可以进一步的分解幕墙网格的属性与构成原件：

- **Curtain Cells**: 返回所有的幕墙单元信息，单元为网格线之间的有界区域
- **Curtain Mullions**: 返回所有单独的幕墙竖梃信息
- **Curtain Panels**: 返回所有嵌入在幕墙的幕墙嵌板或系统实例（例如幕墙门）
- **Curtain Grid Lines**: 返回所有沿网格U与V方向的幕墙网格线及其属性信息

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-analyzecgrid.png" | prepend: site.baseurl }})

## 分析单元

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.CurtainCell' title='DB.CurtainCell' %} 来表达幕墙单元
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

可以使用下面的运算器来分析每个幕墙单元，且能提取单元曲线（**曲线**）与嵌板的曲线（**平直曲线**）：

![]({{ "/static/images/guides/revit-curtainwalls-analyzecell.png" | prepend: site.baseurl }})

请注意单元曲线（下图左侧）与嵌板曲线（下图右侧）并不相同

![]({{ "/static/images/guides/revit-curtainwalls-cellvspanelcurves.png" | prepend: site.baseurl }})

如下图所示单元曲线与墙体曲率一致，但墙板曲线是单元框内的平面曲线：

![]({{ "/static/images/guides/revit-curtainwalls-curvedcellvspanelcurves.png" | prepend: site.baseurl }})

嵌板曲线可用来创建面板，请注意每一个单元都可以标记其自身的序号且是从底行往顶行排序：

![]({{ "/static/images/guides/revit-curtainwalls-facefrompanelcurves.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainwalls-facefrompanelcurvesscap.png" | prepend: site.baseurl }})

## 分析竖梃

{% capture api_note %}
 在 Revit API 中 使用 {% include api_type.html type='Autodesk.Revit.DB.Mullion' title='DB.Mullion' %} 来表达幕墙竖梃
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

经由 {% include ltr/comp.html uuid="734b2dac-" %} 运算器来提取给定幕墙的幕墙竖梃，再链接 {% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器提取相关的几何信息：

![]({{ "/static/images/guides/revit-curtainwalls-mulliongeom.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-mulliongeomscap.png" | prepend: site.baseurl }})

可以使用下面的这些运算器来提取每一个幕墙竖梃：

![]({{ "/static/images/guides/revit-curtainwalls-analyzemullion.png" | prepend: site.baseurl }})

**Axis Curve** 输出端会输出每一个竖梃的轴向曲线，请注意水平线与垂直线相交与否，由竖梃连接设置来决定：

![]({{ "/static/images/guides/revit-curtainwalls-mullionlines.png" | prepend: site.baseurl }})

还可以继续接入曲线运算器来提取竖梃的位置曲线，请注意带有零长度的竖梃线，这些线在幕墙上并不可见：

![]({{ "/static/images/guides/revit-curtainwalls-mulliontocurve.png" | prepend: site.baseurl }})

### 分析竖梃类型

{% capture api_note %}
在 Revit API 中使用  {% include api_type.html type='Autodesk.Revit.DB.MullionType' title='DB.MullionType' %} 来表达幕墙竖梃类型
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

使用 {% include ltr/comp.html uuid="66a9f189-" %} 运算器来分析经由 {% include ltr/comp.html uuid="4eeca86b-" %} 运算器提取的竖梃类型：

![]({{ "/static/images/guides/revit-curtainwalls-analyzemulliontype.png" | prepend: site.baseurl }})

 {% include ltr/comp.html uuid='4bbe14f2' %} 运算器可以依据系统族的值用来筛选竖梃，例如 *L Corner Mullions*

![]({{ "/static/images/guides/revit-curtainwalls-mulliontypefilter.png" | prepend: site.baseurl }})

## 分析嵌板

幕墙可以承载两种类型的对象，既可以是幕墙嵌板，也可以是设计用于与幕墙一起使用的自定义族实例，例如幕墙门实例。这里的一些运算器可以帮忙来用于分析幕墙嵌板。也可以使用在 [Data Model: Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) 与 [Data Model: Elements & Instances]({{ site.baseurl }}{% link _en/1.0/guides/revit-elements.md %}#instances) 指南中提供的方法和运算器来分析族实例

{% capture api_note %}
在 Revit API 中使用  {% include api_type.html type='Autodesk.Revit.DB.Panel' title='DB.Panel' %} 来表达幕墙嵌板
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

使用 {% include ltr/comp.html uuid="734b2dac-" %} 运算器与 {% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器来提取幕墙嵌板的几何物件：

![]({{ "/static/images/guides/revit-curtainwalls-panelgeom.png" | prepend: site.baseurl }})

请注意嵌板的排序是从底部往顶部逐行排序：

![]({{ "/static/images/guides/revit-curtainwalls-panelgeomscap.png" | prepend: site.baseurl }})

因为 嵌板输出参数可以返回嵌板 (`DB.Panel`) 与自定义族实例 (`DB.FamilyInstance`), 这样也可以提取所有插入类型的几何物件：

![]({{ "/static/images/guides/revit-curtainwalls-inserttypes.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-inserttypesscap.png" | prepend: site.baseurl }})

请注意，如果要提取每一个独立的幕墙嵌板，嵌板 (`DB.Panel`) 输出需要进行筛选，否则 {% include ltr/comp.html uuid="08507225-" %} 运算器会报错：

![]({{ "/static/images/guides/revit-curtainwalls-panelerror.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='36180a9e' %} 运算器联合其他类似 ({% include ltr/comp.html uuid='d08f7ab1' %}) 过滤运算器来筛选嵌板 (`DB.Panel`):

![]({{ "/static/images/guides/revit-curtainwalls-panelfilter.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid="08507225-" %} 运算器也会提供访问嵌板的基准点（Base Point)与法线/方向向量（**Orientation**):

![]({{ "/static/images/guides/revit-curtainwalls-panelbaseorient.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-panelorientvectors.png" | prepend: site.baseurl }})

### 分析嵌板类型

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.PanelType' title='DB.PanelType' %} 来表达幕墙嵌板类型
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

 {% include ltr/comp.html uuid="6f11977f-" %} 运算器可以提取经由 {% include ltr/comp.html uuid="08507225-" %} 运算器抓取的幕墙嵌板的嵌板类型：

![]({{ "/static/images/guides/revit-curtainwalls-analyzepaneltype.png" | prepend: site.baseurl }})

注意 Panel Type 输出参数会返回的是一个 *System Panel Type* (`DB.PanelType`) 还是一个 *Custom Family Symbol* (`DB.FamilySymbol`) 这取决于插入至幕墙网格的嵌板的类型，因此我们首先需要筛选嵌板：

![]({{ "/static/images/guides/revit-curtainwalls-inserttype.png" | prepend: site.baseurl }})

## 分析幕墙网格线

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.CurtainGridLine' title='DB.CurtainGridLine' %} 来表达幕墙网格线
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

 {% include ltr/comp.html uuid="d7b5c58e-" %} 运算器可以提取幕墙网格线，然后接入至 **Curve** 运算器：

![]({{ "/static/images/guides/revit-curtainwalls-analyzegrid.png" | prepend: site.baseurl }})

进一步的提取还可以提取每一条幕墙**网格直线**：

![]({{ "/static/images/guides/revit-curtainwalls-analyzegridline.png" | prepend: site.baseurl }})

要注意幕墙网格线与线段的区别，在 Curve 输出选项提供输出每一段幕墙网格U、V轴向线段的选项：

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinecurve.png" | prepend: site.baseurl }})

**Segments** 输出端除了输出每条幕墙网格线还包括幕墙边框外未使用的线段：

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinesegments.png" | prepend: site.baseurl }})

幕墙网格线与线段显示如下：

![]({{ "/static/images/guides/revit-curtainwalls-cgridlinesallegments.png" | prepend: site.baseurl }})

### 提取关联竖梃与嵌板

 {% include ltr/comp.html uuid="face5e7d-" %} 运算器也可以提取与每个幕墙网格线段关联竖梃与嵌板的功能，请注意，不包含边框上的竖梃与嵌板，因为它们不是幕墙网格定义的一部分：

![]({{ "/static/images/guides/revit-curtainwalls-analyzeassocmullions.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-curtainwalls-assocmullions.png" | prepend: site.baseurl }})

嵌板关联与竖梃稍有不同，每一个竖梃都关联一个网格线段，但一个嵌板（因为它又两条边）会同时与一条沿着U轴向和V轴向的网格线关联:

![]({{ "/static/images/guides/revit-curtainwalls-assocmullionspanels.gif" | prepend: site.baseurl }})

## 创建幕墙

### 以定义断面

可以快速的根据一个定义断面来创建一段基础墙体，然后经过 {% include ltr/comp.html uuid='78b02ae8' %} 运算器的 Curtain Wall Type 来创建一个简单的幕墙：

![]({{ "/static/images/guides/revit-curtainwalls-byprofilescap.png" | prepend: site.baseurl }})

<!-- Curtain Systems -->

## 查询幕墙系统

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.CurtainSystemType' title='DB.CurtainSystemType' %}来表达幕墙系统类型，幕墙系统由 {% include api_type.html type='Autodesk.Revit.DB.CurtainSystem' title='DB.CurtainSystem' %} 
{% endcapture %} 来表达
{% include ltr/api_note.html note=api_note %}

可以使用 {% include ltr/comp.html uuid='d08f7ab1' %}, {% include ltr/comp.html uuid='7b00f940' %}与 {% include ltr/comp.html uuid='0f7da57e' %} 等运算器来查询幕墙系统类型与实例：

![]({{ "/static/images/guides/revit-curtainsystems-querytypes.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-curtainsystems-query.png" | prepend: site.baseurl }})

## 分析幕墙系统

利用 {% include ltr/comp.html uuid="83d08b81-" %} 运算器可以提取幕墙系统类型的所有信息，但要注意**幕墙系统** 与**幕墙系统类型** 的差别：

![]({{ "/static/images/guides/revit-curtainsystems-analyzetype.png" | prepend: site.baseurl }})

从幕墙系统实例中提取信息与幕墙实例非常类似，最简单的的方法是使用 {% include ltr/comp.html uuid="734b2dac-" %} 运算器， {% include ltr/comp.html uuid='83d08b81' %} 运算器可以访问幕墙网格脚本：

{% include ltr/bubble_note.html note='注意幕墙系统可以包含多个幕墙网格，且每个都和源几何关联一个单一的面板，这些幕墙网格都有独立的边框且基于幕墙系统类型脚本来生成' %}

![]({{ "/static/images/guides/revit-curtainsystems-analyze.png" | prepend: site.baseurl }})

一旦建立访问幕墙网格脚本，你就可以通过 {% include ltr/comp.html uuid="d7b5c58e-" %} 来提取信息，这和提取幕墙信息很类似：:

![]({{ "/static/images/guides/revit-curtainsystems-analyzecgrid.png" | prepend: site.baseurl }})
