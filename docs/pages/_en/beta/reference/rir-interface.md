---
title: Rhino.Inside.Revit Interface
order: 10
group: User Interface
---

## Loading {{ site.terms.rir }}

One Revit is loaded, click on the Rhino button under *Revit > Addins* tab to load the {{ site.terms.rir }}

![]({{ "/static/images/reference/rir-interface01.png" | prepend: site.baseurl }})

### Button Click Modes

{% include ltr/kb_shortcut.html keys='Ctrl' click=true %}

Launches the {{ site.terms.rhino }} about window that shows the exact version number

{% include ltr/kb_shortcut.html keys='Ctrl+Shift' click=true %}

Launches the debug info collector window. See **Submitting Debug Info** under [Troubleshooting]({{ site.baseurl }}{% link _en/beta/reference/toubleshooting.md %})

## {{ site.terms.rir }} Tab

Once {{ site.terms.rir }} is loaded and licensed, it creates a new ribbon in Revit interface named *Rhinoceros*

![]({{ "/static/images/ribbon/ribbon.png" | prepend: site.baseurl }})

Here is a description of the buttons and functionality available on this ribbon.

{% for panel in site.data.buttons %}
## {{ panel.title }} Panel
{% include ltr/button_table.html buttons=panel.comps %}
{% endfor %}




