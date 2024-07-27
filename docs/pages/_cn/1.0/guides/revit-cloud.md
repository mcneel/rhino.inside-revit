---
title: "Revit: BIM360 Cloud"
subtitle: Working with Rhino Grasshopper and BIM360
order: 26
group: Essentials
home: true
thumbnail: /static/images/guides/revit-cloud.png
ghdef: 
---

{% capture link_note %}
Rhino 与 Grasshopper 文件都可以整合至 {{ site.terms.bim360 }} 中，作为项目的一部分，所有团队成员都可以使用最新的文件，所有文件都支持 {{ site.terms.bim360 }} 所提供的备份、版本控制与文件锁定功能. 
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-cloud.png' %}

使用 [BIM360 File Locker](https://github.com/eirannejad/BIM360FileLockerForRhino) 可以:

- 如果当前文件被打开或被其他人标记为锁定，这个文件会被锁定
- 会显示锁定状态警报
- 保存文件后会与 {{ site.terms.bim360 }} 同步
- 同时支持 Rhino 与 Grasshopper 文件
- 插件既可以既可以在 {{ site.terms.rir }}内运行，也可以在 Rhino 内运行

**安装 BIM360Locker from Rhino 插件:**

1. 在 Rhino 指令行执行 Type PackageManager 指令
2. 弹出框搜索 "BIM360"
3. 从搜索结果列表中点选 *BIM360FileLocker* 插件
4. 点击 **安装** 按钮开始安装

更多关于 {{ site.terms.bim360 }} File Locker for Rhino 插件的安装、设置与调试请查看下面的视频 :

{% include youtube_player.html id="son3aC8kJ2c" %}

[BIM360 File Locker](https://github.com/eirannejad/BIM360FileLockerForRhino) 是由 [Ehsan Iran-Nejad](https://github.com/eirannejad) 主导的开源项目
