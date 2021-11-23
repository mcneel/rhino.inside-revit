using System;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  class RibbonHandler : IDisposable
  {
    readonly External.UI.UIHostApplication _app;

    public RibbonHandler(External.UI.UIHostApplication app) => _app= app;
    public void Dispose() => _app.Dispose();

    /// <summary>
    /// Get Revit tab as underlying Autodesk.Windows.RibbonTab instance
    /// </summary>
    private static Autodesk.Windows.RibbonTab GetAdwndRibbonTab(string tabName)
    {
      // grab the underlying Autodesk.Windows object from Button
      foreach (var adwndRibbonTab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
        if (adwndRibbonTab.Title == tabName)
          return adwndRibbonTab;
      return null;
    }

    public readonly string AddinTabName = Core.Product;

    public bool HasPanel(string tabName, string panelName)
    {
      if (GetAdwndRibbonTab(tabName) is Autodesk.Windows.RibbonTab tab)
        foreach (var panel in tab.Panels)
          if (panelName == panel.Source.Title)
            return true;
      return false;
    }

    public bool HasAddinPanel(string panelName) => HasPanel(AddinTabName, panelName);

    public void CreateTab(string tabName) => _app.CreateRibbonTab(tabName);

    public void CreateAddinTab() => CreateTab(AddinTabName);

    public RibbonPanel CreatePanel(string tabName, string panelName) =>
      _app.CreateRibbonPanel(tabName, panelName);

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


      // lets disable and hide the panel
      foreach (var panel in _app.GetRibbonPanels(tabName))
      {
        if (panel.Name == panelName)
        {
          //panel.Visible = false;
          panel.Enabled = false;
          return true;
        }
      }

      return false;
    }

    public RibbonPanel CreateAddinPanel(string panelName) => CreatePanel(AddinTabName, panelName);
    public bool RemoveAddinPanel(string panelName) => RemovePanel(AddinTabName, panelName);
  }
}
