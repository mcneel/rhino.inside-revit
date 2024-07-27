---
title: Worksets
order: 71
thumbnail: /static/images/guides/revit-worksets.png
subtitle: Workflows for Revit Worksets
group: Containers
---

<!-- https://github.com/mcneel/rhino.inside-revit/issues/92 -->

## 查询工作集

可以使用 {% include ltr/comp.html uuid='311316ba' %} 运算器获取文件中的工作集，鼠标右键点击 Kind (K) 展开选集

![]({{ "/static/images/guides/Revit-Worksets-Query.png" | prepend: site.baseurl }})

## 活动工作集

利用 {% include ltr/comp.html uuid='aa467c94' %} 运算器可以获取当前活动工作集，你还可以通过 Zoom UI 添加输入时设置活动工作集。

![]({{ "/static/images/guides/Revit-Worksets-Active.png" | prepend: site.baseurl }})

## 确保工作集

为了确保文档中特定用户创建的工作集，请使用{% include ltr/comp.html uuid='a406c6a0' %} 运算器。这也是在文档中创建新的工作集的方法。. 

![]({{ "/static/images/guides/revit-workset-ensure.png" | prepend: site.baseurl }})

## 删除工作集

请使用 {% include ltr/comp.html uuid='bf1b9be9' %} 运算器删除 Revit 文档中的工作集。

![]({{ "/static/images/guides/revit-workset-delete.png" | prepend: site.baseurl }})

## 图元工作集

使用 {% include ltr/comp.html uuid='b441ba8c' %} 运算器可以获取或设置一个图元工作集

![]({{ "/static/images/guides/Revit-Worksets-Element.png" | prepend: site.baseurl }})

## 工作集全局可见性

利用 {% include ltr/comp.html uuid='2922af4a' %} 运算器可以获取或设置全局工作集的可见性

![]({{ "/static/images/guides/Revit-Worksets-vis-global.png" | prepend: site.baseurl }})

## 工作集视图覆盖

利用 {% include ltr/comp.html uuid='b062c96e' %} 运算器可以设置、获取指定的可见性会覆盖指定视图，右键点击 Visibility 展开选取可见性

![]({{ "/static/images/guides/Revit-Worksets-vis-overrides.png" | prepend: site.baseurl }})

## 工作集标识

使用 {% include ltr/comp.html uuid='c33cd128' %} 运算器可以设置工作集属性获取与设置访问属性，如果要重命名工作集请使用 ZUI 来扩展 Name 属性

![]({{ "/static/images/guides/Revit-Worksets-Identity.png" | prepend: site.baseurl }})

## 图元所有权信息

使用 {% include ltr/comp.html uuid='f68f96ec' %} 运算器可以获取图元所属属性信息

![]({{ "/static/images/guides/Revit-Worksets-Ownership.png" | prepend: site.baseurl }})

## 文档工作共享信息

使用 {% include ltr/comp.html uuid='f7d56db0' %} 运算器可以获取文档工作共享属性

![]({{ "/static/images/guides/Revit-Worksets-Document.png" | prepend: site.baseurl }})

## 文档服务信息

使用 {% include ltr/comp.html uuid='2577a55b' %} 运算器可以获取文档服务属性

![]({{ "/static/images/guides/Revit-Worksets-Server.png" | prepend: site.baseurl }})