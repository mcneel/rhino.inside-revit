using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.UI;
using ADW = Autodesk.Windows;
using ADIW = Autodesk.Internal.Windows;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIRibbonExtension
  {
    #region Autodesk.Windows API utility methods
    /// <summary>
    /// Get RibbonPanel as underlying Autodesk.Windows API instance
    /// </summary>
    public static ADW.RibbonPanel GetAdwndRibbonPanel(this RibbonPanel panel, string tabName)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (panel != null)
      {
        foreach (var adwndRibbonTab in ADW.ComponentManager.Ribbon.Tabs)
          if (adwndRibbonTab.Title == tabName)
          {
            foreach (var adwndRibbonPanel in adwndRibbonTab.Panels)
              if (panel.Name == adwndRibbonPanel.Source.Title)
                return adwndRibbonPanel;
          }
      }
      return null;
    }

    /// <summary>
    /// Get RibbonButton as underlying Autodesk.Windows API instance
    /// </summary>
    public static ADW.RibbonButton GetAdwndRibbonButton(this RibbonButton button)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (button != null)
      {
        var getRibbonItemMethodInfo = button.GetType().GetMethod("getRibbonItem", BindingFlags.NonPublic | BindingFlags.Instance);
        if (getRibbonItemMethodInfo != null)
          return getRibbonItemMethodInfo.Invoke(button, null) as Autodesk.Windows.RibbonButton;
      }
      return null;
    }

    /// <summary>
    /// Highlight command button as new or updated
    /// </summary>
    public static void Highlight(this RibbonButton button, bool updated = false)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (GetAdwndRibbonButton(button) is ADW.RibbonButton ribbonButton)
      {
        // set highlight state and update tooltip
        ribbonButton.Highlight =
          updated ? ADIW.HighlightMode.Updated : ADIW.HighlightMode.New;
      }
    }

    /// <summary>
    /// Clear any previously set highlights on command button
    /// </summary>
    public static void ClearHighlight(this RibbonButton button)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (GetAdwndRibbonButton(button) is ADW.RibbonButton ribbonButton)
      {
        // set highlight state and update tooltip
        ribbonButton.Highlight = ADIW.HighlightMode.None;
      }
    }

    /// <summary>
    /// Set an already created button to panel dialog launcher
    /// </summary>
    public static void SetButtonToDialogLauncher(this RibbonPanel panel, string tabName, RibbonButton button)
    {
      if (panel.GetAdwndRibbonPanel(tabName) is ADW.RibbonPanel adwndRibbonPanel)
        if (GetAdwndRibbonButton(button) is ADW.RibbonButton adwndRibbonButton)
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
