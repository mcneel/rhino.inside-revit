---
title: Rhino.Inside.Revit Interface
order: 10
group: User Interface
---

{% include youtube_player.html id="ogocxN8WXlA" %}

## Loading {{ site.terms.rir }}

One Revit is loaded, click on the *Start* button under *Revit > Rhino.Inside* tab to load the {{ site.terms.rir }}

![]({{ "/static/images/reference/rir-interface01.png" | prepend: site.baseurl }})

### Start Button Click Modes

{% include ltr/kb_shortcut.html keys='Ctrl' click=true %}

Launches the {{ site.terms.rhino }} about window that shows the exact version number

{% include ltr/kb_shortcut.html keys='Ctrl+Shift' click=true %}

Launches the debug info collector window. See **Submitting Debug Info** under [Troubleshooting]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %})

### "More" Slideout

There is a slideout under the *Start* button that contains informational tools and the {{ site.terms.rir }} Settings

![]({{ "/static/images/reference/more-slideout.png" | prepend: site.baseurl }})

## {{ site.terms.rir }} Tab

Once {{ site.terms.rir }} is loaded and Rhino is licensed, it creates a new panels under the *Rhino.Inside* tab

![]({{ "/static/images/ribbon/ribbon.png" | prepend: site.baseurl }})

Here is a description of the buttons and functionality available on this ribbon.

{% for panel in site.data.buttons %}
## {{ panel.title }} Panel
{% include ltr/button_table.html buttons=panel.comps %}
{% endfor %}

### Rhino Options

Rhino options window is easily accessible using the arrow-shaped button at the corner of the *Rhinoceros* panel

![]({{ "/static/images/reference/rhinooptions.png" | prepend: site.baseurl }})

## {{ site.terms.rir }} Options

{{ site.terms.rir }} Settings window has a series of options that can configure the {{ site.terms.rir }} behavior:

### General Options

You can choose to Start Rhino automatically, and adjust other general settings from here:

![]({{ "/static/images/reference/settings-general.png" | prepend: site.baseurl }})

### Checking for Updates

Update channels can be set here. You can subscribe to *Stable Public* updates or *Daily (Work in Progress)*

![]({{ "/static/images/reference/settings-updatechannels.png" | prepend: site.baseurl }})

If there are any updates available, both the Start and Options buttons will show a notification dot and the new update release info on the tooltip:

![]({{ "/static/images/reference/updates-new-notif.png" | prepend: site.baseurl }})

You can get the information and download the installer from *Options / Updates*. Please make sure to close Revit before installing the new version.

![]({{ "/static/images/reference/updates-newavailable.png" | prepend: site.baseurl }})

## Grasshopper Scripts

Grasshopper scripts can be loaded into the Revit UI. Create a directory on your machine that contains Grasshopper (`*.gh` or `*.ghx`) scripts. All the scripts at the root of this directory will be added as button to a new panel with the name of this directory. All other sub-directories will be converted into pull-down buttons on the ribbon.

The `My RIR Tools` panel shown in the image below is created from a directory of Grasshopper scripts:

![]({{ "/static/images/reference/settings-addscripts-loadedtools.png" | prepend: site.baseurl }})

### Loading Grasshopper Scripts into Ribbon

This is an example of a directory (named `My RIR Tools`) with two Grasshopper scripts, and an `Examples` sub-directory with more Grasshopper scripts:

![]({{ "/static/images/reference/settings-gh-scripts-dir.png" | prepend: site.baseurl }})

Now add the path to this directory inside the {{ site.terms.rir }} *Options / Scripts*:

![]({{ "/static/images/reference/settings-addscripts.png" | prepend: site.baseurl }})

The *"Use Script Locations"* list will show all the directories that will be loaded into the UI:

![]({{ "/static/images/reference/settings-addscripts-myrirtools.png" | prepend: site.baseurl }})

Once settings are applied, the scripts will be loaded into the UI and are easily accessible:

![]({{ "/static/images/reference/settings-addscripts-loadedtools.png" | prepend: site.baseurl }})

### Rhino Package Manager

Rhino package manager can be used to install Rhino and Grasshopper packages, and {{ site.terms.rir }} scripts.

![]({{ "/static/images/reference/rhinoyak.png" | prepend: site.baseurl }})