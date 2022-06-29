---
title: "Revit: BIM360 Cloud"
subtitle: Working with Rhino Grasshopper and BIM360
order: 26
group: Essentials
home: true
thumbnail: /static/images/guides/revit-wip.png
ghdef: 
---

Both Rhino files and Grasshopper definitions can be integrated into {{ site.terms.bim360 }}.  The most recent files will be available to all team members as part of the project. All files will benefit from the backup, versioning and file locking that {{ site.terms.bim360 }} offers. 

By using the BIM360 File Locker plugin:
 - Files will lock if currently open by another person.
 - Alerts to the lock status will be displayed.
 - Files will sync with {{ site.terms.bim360 }} when saved.
 - This will work for both Rhino and Grasshopper files.
 - The plugin works if running within in Revit or running Rhino outside of Revit.

**To install the BIM360Locker plugin from Rhino:**
 1. Type PackageManager in Rhino.
 1. Search for "BIM360"
 1. SElect the BOM360FileLocker plugin from the list.
 1. Click on Install.

For details on install, configuration and use of the {{ site.terms.bim360 }} File Locker for Rhino 23 see the video:

{% include youtube_player.html id="son3aC8kJ2c" %}

[BIM360 File Locker is an Open source project](https://github.com/eirannejad/BIM360FileLockerForRhino) by [Ehsan Iran Nijad](https://github.com/eirannejad)
