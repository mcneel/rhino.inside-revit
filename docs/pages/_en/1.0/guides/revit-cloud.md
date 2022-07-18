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
Both Rhino files and Grasshopper definitions can be integrated into {{ site.terms.bim360 }}.  The most recent files will be available to all team members as part of the project. All files will benefit from the backup, versioning and file locking that {{ site.terms.bim360 }} offers. 
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-cloud.png' %}

By using the [BIM360 File Locker](https://github.com/eirannejad/BIM360FileLockerForRhino) plugin:
 - Files will lock if currently open, or manually marked as locked by another person
 - Alerts to the lock status will be displayed
 - Files will sync with {{ site.terms.bim360 }} when saved
 - This will work for both Rhino and Grasshopper files
 - The plugin works if using {{ site.terms.rir }}, or running Rhino outside of Revit

**To install the BIM360Locker plugin from Rhino:**
 1. Type PackageManager in Rhino
 1. Search for "BIM360"
 1. Select the *BIM360FileLocker* plugin from the list
 1. Click on Install

For details on install, configuration and use of the {{ site.terms.bim360 }} File Locker for Rhino 23 see the video:

{% include youtube_player.html id="son3aC8kJ2c" %}

[BIM360 File Locker](https://github.com/eirannejad/BIM360FileLockerForRhino) is an Open-Source project by [Ehsan Iran-Nejad](https://github.com/eirannejad)
