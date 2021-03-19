using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands
  /// </summary>
  public abstract class Command : External.UI.ExternalCommand
  {
    #region Ribbon item creation
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

    internal static PushButtonData NewPushButtonData<CommandType, AvailabilityType>(string name, string iconName, string tooltip)
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
        Image = ImageBuilder.LoadRibbonButtonImage(iconName, true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage(iconName),
        ToolTip = tooltip,
      };
    }

    public static ToggleButtonData NewToggleButtonData<CommandType, AvailabilityType>(string name, string iconName, string tooltip)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      return new ToggleButtonData
      (
        typeof(CommandType).Name,
        name ?? typeof(CommandType).Name,
        typeof(CommandType).Assembly.Location,
        typeof(CommandType).FullName
      )
      {
        AvailabilityClassName = typeof(AvailabilityType).FullName,
        Image = ImageBuilder.LoadRibbonButtonImage(iconName, true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage(iconName),
        ToolTip = tooltip,
      };
    }
    #endregion

    #region Ribbon item storage
    /// <summary>
    /// Static storage for buttons created by commands.
    /// Usage is optional for derived classes thru Store and Restore methods
    /// </summary>
    private static Dictionary<string, RibbonButton> _buttons = new Dictionary<string, RibbonButton>();

    /// <summary>
    /// Store given button under given name
    /// </summary>
    public static void StoreButton(string name, RibbonButton button) => _buttons[name] = button;

    /// <summary>
    /// Restore previously stored button under given name
    /// </summary>
    public static RibbonButton RestoreButton(string name)
    {
      if (_buttons.TryGetValue(name, out var button))
        return button;
      return null;
    }
    #endregion

    #region Availability Types
    /// <summary>
    /// Availability class for commands that are always active even when there is no document open
    /// </summary>
    public class AlwaysAvailable : IExternalCommandAvailability
    {
      bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) => true;
    }

    /// <summary>
    /// Available when an active Revit document is available
    /// </summary>
    public class NeedsActiveDocument<T> : External.UI.CommandAvailability
      where T : IExternalCommandAvailability, new()
    {
      T dependency = new T();

      // We can not relay on the UIApplication first argument.
      // Seems other Add-ins are calling this method with wrong values.
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
        AddIn.CurrentStatus >= AddIn.Status.Available;
    }

    /// <summary>
    /// Available when Rhino.Inside is not obsolete
    /// </summary>
    protected class AvailableWhenNotObsolete : External.UI.CommandAvailability
    {
      public override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        AddIn.CurrentStatus >= AddIn.Status.Obsolete;
    }
    #endregion
  }
}
