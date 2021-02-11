using System;
using System.Reflection;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands
  /// </summary>
  abstract public class Command : External.UI.Command
  {
    // optional static storage for buttons created by commands
    private static Dictionary<string, RibbonButton> _buttons = new Dictionary<string, RibbonButton>();

    internal static PushButton AddPushButton<CommandType, AvailabilityType>(PulldownButton pullDownButton, string text, string iconName, string tooltip = null)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      var buttonData = NewPushButtonData<CommandType, AvailabilityType>(text, iconName, tooltip);

      if (pullDownButton.AddPushButton(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = tooltip;
        return pushButton;
      }

      return null;
    }

    internal static PushButtonData NewPushButtonData<CommandType, AvailabilityType>(
        string name,
        string iconName,
        string tooltip
      )
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      return new PushButtonData
      (
        typeof(CommandType).Name,
        name ?? typeof(CommandType).Name,
        typeof(CommandType).Assembly.Location,
        typeof(CommandType).FullName
      )
      {
        AvailabilityClassName = typeof(AvailabilityType).FullName,
        Image = ImageBuilder.LoadBitmapImage(iconName, true),
        LargeImage = ImageBuilder.LoadBitmapImage(iconName),
        ToolTip = tooltip,
      };
    }

    public static ToggleButtonData NewToggleButtonData<CommandType, AvailabilityType>(string text = null)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      return new ToggleButtonData
      (
        typeof(CommandType).Name,
        text ?? typeof(CommandType).Name,
        typeof(CommandType).Assembly.Location,
        typeof(CommandType).FullName
      )
      {
        AvailabilityClassName = typeof(AvailabilityType).FullName
      };
    }

    public class AlwaysAvailable : IExternalCommandAvailability
    {
      bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) => true;
    }

    public static void StoreButton(string name, RibbonButton button) => _buttons[name] = button;
    public static RibbonButton RestoreButton(string name)
    {
      if (_buttons.TryGetValue(name, out var button))
        return button;
      return null;
    }

    /// <summary>
    /// Get RibbonButton as underlying Autodesk.Windows API instance
    /// </summary>
    /// <param name="button">Revit API RibbonButton</param>
    /// <returns></returns>
    public static Autodesk.Windows.RibbonButton GetAdwndRibbonButton(RibbonButton button)
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
    /// <param name="updated">Highlight as Updated, otherwise as New</param>
    public static void HighlightButton(RibbonButton button, bool updated = false)
    {
      // grab the underlying Autodesk.Windows object from Button
      if (GetAdwndRibbonButton(button) is Autodesk.Windows.RibbonButton ribbonButton)
      {
        // set highlight state and update tooltip
        ribbonButton.Highlight =
          updated ? Autodesk.Internal.Windows.HighlightMode.Updated : Autodesk.Internal.Windows.HighlightMode.New;
      }
    }

    /// <summary>
    /// Set an already created button to panel dialog launcher
    /// </summary>
    /// <param name="tabName">Ribbon Tab name</param>
    /// <param name="panel">Ribbon panel</param>
    /// <param name="button">Ribbon button</param>
    public static void SetButtonToPanelDialogLauncher(string tabName, RibbonPanel panel, RibbonButton button)
    {
      foreach (var adwndRibbonTab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
        if (adwndRibbonTab.Title == tabName)
        {
          foreach (var adwndRibbonPanel in adwndRibbonTab.Panels)
            if (panel.Name == adwndRibbonPanel.Source.Title)
            {
              if (GetAdwndRibbonButton(button) is Autodesk.Windows.RibbonButton adwndRibbonButton)
              {
                adwndRibbonPanel.Source.Items.Remove(adwndRibbonButton);
                adwndRibbonPanel.Source.DialogLauncher = adwndRibbonButton;
              }
            }
        }
    }

    /// <summary>
    /// Available when an active Revit document is available
    /// </summary>
    public class NeedsActiveDocument<T> : External.UI.CommandAvailability
      where T : IExternalCommandAvailability, new()
    {
      T dependency = new T();

      // We can not relay on the UIApplication first argument.
      // Seams other Add-ins are calling this method with wrong values.
      // I add the try-catch just because this is called many times.
      public override bool IsCommandAvailable(UIApplication _, CategorySet selectedCategories)
      {
        if(Revit.ActiveUIApplication is UIApplication app)
        {
          try
          {
            return (app.ActiveUIDocument?.Document?.IsValidObject ?? false) &&
                   dependency.IsCommandAvailable(app, selectedCategories);
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { }
        }

        return false;
      }
    }

    /// <summary>
    /// Available when Rhino.Inside is not expired, crashed or already active
    /// </summary>
    protected class Availability : External.UI.CommandAvailability
    {
      public override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        Addin.CurrentStatus >= Addin.Status.Available;
    }
  }

  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that depends on having
  /// an active Revit document, but do not call RhinoCommon
  /// </summary>
  abstract public class DocumentCommand : Command
  {
    protected new class Availability : NeedsActiveDocument<Command.Availability> { }
  }
}
