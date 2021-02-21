using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.UI;
using ADW = Autodesk.Windows;
using ADIW = Autodesk.Internal.Windows;

namespace RhinoInside.Revit.UI
{
  class RibbonHandler : IDisposable
  {
    UIControlledApplication _uiCtrlApp = default;
    UIApplication _uiApp = default;

    private RibbonHandler() { }
    public RibbonHandler(UIControlledApplication uiCtrlApp) => _uiCtrlApp = uiCtrlApp;
    public RibbonHandler(UIApplication uiApp) => _uiApp = uiApp;

    /// <summary>
    /// Get Revit tab as underlying Autodesk.Windows.RibbonTab instance
    /// </summary>
    private static ADW.RibbonTab GetAdwndRibbonTab(string tabName)
    {
      // grab the underlying Autodesk.Windows object from Button
      foreach (var adwndRibbonTab in ADW.ComponentManager.Ribbon.Tabs)
        if (adwndRibbonTab.Title == tabName)
          return adwndRibbonTab;
      return null;
    }

    public readonly string AddinTabName = Addin.AddinName;

    public bool HasPanel(string tabName, string panelName)
    {
      if (GetAdwndRibbonTab(tabName) is ADW.RibbonTab tab)
        foreach (var panel in tab.Panels)
          if (panelName == panel.Source.Title)
            return true;
      return false;
    }

    public bool HasAddinPanel(string panelName) => HasPanel(AddinTabName, panelName);

    public void CreateTab(string tabName)
    {
      if (_uiCtrlApp != null)
        _uiCtrlApp.CreateRibbonTab(tabName);
      else if (_uiApp != null)
        _uiApp.CreateRibbonTab(tabName);
    }

    public void CreateAddinTab() => CreateTab(AddinTabName);

    public RibbonPanel CreatePanel(string tabName, string panelName)
    {
      if (_uiCtrlApp != null)
        return _uiCtrlApp.CreateRibbonPanel(tabName, panelName);
      else if (_uiApp != null)
        return _uiApp.CreateRibbonPanel(tabName, panelName);
      return null;
    }

    public bool RemovePanel(string tabName, string panelName)
    {
      /*
       * Removing panel through Autodesk.Windows API does not work
       * Revit UI API seems to hold its own references internally
       */
      //if (GetAdwndRibbonTab(tabName) is ADW.RibbonTab tab)
      //  foreach (var panel in tab.Panels)
      //    if (panelName == panel.Source.Title)
      //      return tab.Panels.Remove(panel);

      List<RibbonPanel> panels = new List<RibbonPanel>();
      if (_uiCtrlApp != null)
        panels = _uiCtrlApp.GetRibbonPanels(tabName);
      else if (_uiApp != null)
        panels = _uiApp.GetRibbonPanels(tabName);

      // lets disable and hide the panel
      foreach(var panel in panels)
        if (panel.Name == panelName)
        {
          //panel.Visible = false;
          panel.Enabled = false;
          return true;
        }
      return false;
    }

    public RibbonPanel CreateAddinPanel(string panelName) => CreatePanel(AddinTabName, panelName);
    public bool RemoveAddinPanel(string panelName) => RemovePanel(AddinTabName, panelName);

    public void Dispose()
    {
      _uiCtrlApp = null;
      _uiApp = null;
    }
  }
}
