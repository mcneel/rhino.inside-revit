using System.Reflection;
using ADIW = Autodesk.Internal.Windows;
using ADW = Autodesk.Windows;

namespace Autodesk.Revit.UI
{
  static class UIRibbonExtension
  {
    #region RibbonItem
    /// <summary>
    /// Get UI.RibbonItem as underlying Autodesk.Windows.RibbonItem instance
    /// </summary>
    static ADW.RibbonItem GetAdwndRibbonItem(this RibbonItem item)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (item?.GetType().GetMethod("getRibbonItem", BindingFlags.NonPublic | BindingFlags.Instance) is MethodInfo getRibbonItem)
        return getRibbonItem.Invoke(item, null) as ADW.RibbonItem;

      return null;
    }

    /// <summary>
    /// Highlight command button as new or updated
    /// </summary>
    public static void Highlight(this RibbonItem item, bool updated = false)
    {
      if (GetAdwndRibbonItem(item) is ADW.RibbonItem ribbonItem)
        ribbonItem.Highlight = updated ? ADIW.HighlightMode.Updated : ADIW.HighlightMode.New;
    }

    /// <summary>
    /// Clear any previously set highlights on command button
    /// </summary>
    public static void ClearHighlight(this RibbonItem item)
    {
      if (GetAdwndRibbonItem(item) is ADW.RibbonItem ribbonItem)
        ribbonItem.Highlight = ADIW.HighlightMode.None;
    }

    public static void SetMinWidth(this RibbonItem item, int value)
    {
      if (item.GetAdwndRibbonItem() is ADW.RibbonItem ribbonItem)
        ribbonItem.MinWidth = value;
    }

    /// <summary>
    /// Sets Ribbon item text content
    /// </summary>
    /// <param name="button"></param>
    /// <param name="value"></param>
    public static void SetText(this RibbonItem item, string value)
    {
      if (item.GetAdwndRibbonItem() is ADW.RibbonItem ribbonItem)
        ribbonItem.Text = value;
    }

    /// <summary>
    /// Enable/Disable Ribbon item text visibility
    /// </summary>
    /// <param name="button"></param>
    /// <param name="value"></param>
    public static void ShowText(this RibbonItem item, bool value)
    {
      if (item.GetAdwndRibbonItem() is ADW.RibbonItem ribbonItem)
        ribbonItem.ShowText = value;
    }
    #endregion

    #region RibbonPanel
    /// <summary>
    /// Get UI.RibbonPanel as underlying Autodesk.Windows.RibbonPanel instance
    /// </summary>
    private static ADW.RibbonPanel GetAdwndRibbonPanel(this RibbonPanel panel, string tabName)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (panel != null)
      {
        foreach (var adwndRibbonTab in ADW.ComponentManager.Ribbon.Tabs)
        {
          if (adwndRibbonTab.Title == tabName)
          {
            foreach (var adwndRibbonPanel in adwndRibbonTab.Panels)
              if (panel.Name == adwndRibbonPanel.Source.Title)
                return adwndRibbonPanel;
          }
        }
      }

      return null;
    }


    /// <summary>
    /// Set an already created button to panel dialog launcher
    /// </summary>
    public static void SetDialogLauncherButton(this RibbonPanel panel, string tabName, RibbonButton button)
    {
      if (panel.GetAdwndRibbonPanel(tabName) is ADW.RibbonPanel adwndRibbonPanel)
        if (GetAdwndRibbonItem(button) is ADW.RibbonButton adwndRibbonButton)
        {
          adwndRibbonPanel.Source.Items.Remove(adwndRibbonButton);
          adwndRibbonPanel.Source.DialogLauncher = adwndRibbonButton;
        }
    }

    /// <summary>
    /// Collapse Ribbon Panel
    /// </summary>
    public static void Collapse(this RibbonPanel panel, string tabName)
    {
      if (panel.GetAdwndRibbonPanel(tabName) is ADW.RibbonPanel adwndRibbonPanel)
        adwndRibbonPanel.IsCollapsed = true;
    }

    /// <summary>
    /// Expand Ribbon Panel
    /// </summary>
    public static void Expand(this RibbonPanel panel, string tabName)
    {
      if (panel.GetAdwndRibbonPanel(tabName) is ADW.RibbonPanel adwndRibbonPanel)
        adwndRibbonPanel.IsCollapsed = false;
    }
    #endregion
  }
}
