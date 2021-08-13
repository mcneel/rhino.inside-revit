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

    internal static PushButtonData NewPushButtonData<CommandType, AvailabilityType>(string name, string iconName, string tooltip, string url = default)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      var data = new PushButtonData
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
        ToolTip = tooltip
      };

      if (url != string.Empty)
      {
        if (url is null) url = AddIn.AddInWebSite;
        else if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
          url = AddIn.AddInWebSite + url;

        data.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, url));
      }

      return data;
    }

    public static ToggleButtonData NewToggleButtonData<CommandType, AvailabilityType>(string name, string iconName, string tooltip, string url = default)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      var data = new ToggleButtonData
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

      if (url != string.Empty)
      {
        if (url is null) url = AddIn.AddInWebSite;
        else if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
          url = AddIn.AddInWebSite + url;

        data.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, url));
      }

      return data;
    }
    #endregion

    #region Ribbon item storage
    /// <summary>
    /// Static storage for buttons created by commands.
    /// Usage is optional for derived classes thru Store and Restore methods
    /// </summary>
    private static readonly Dictionary<string, RibbonItem> _buttons = new Dictionary<string, RibbonItem>();

    /// <summary>
    /// Store given button under given name
    /// </summary>
    protected static void StoreButton(string name, RibbonItem button) => _buttons[name] = button;

    /// <summary>
    /// Restore previously stored button under given name
    /// </summary>
    protected static RibbonItem RestoreButton(string name)
    {
      if (_buttons.TryGetValue(name, out var button))
        return button;
      return null;
    }
    #endregion

    #region Availability Types
    /// <summary>
    /// Availability for commands that are always available even when there is no document open.
    /// </summary>
    public struct AlwaysAvailable : IExternalCommandAvailability
    {
      bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) => true;
    }

    /// <summary>
    /// Available when an active Revit document is loaded on Revit UI.
    /// </summary>
    public class NeedsActiveDocument<T> : External.UI.CommandAvailability
      where T : External.UI.CommandAvailability, new()
    {
      readonly T dependency = new T();

      public override bool IsRuntimeReady() => dependency.IsRuntimeReady();

      // We can not relay on the UIApplication first argument.
      // Seems other Add-Ins are calling this method with strange values.
      // I add the try-catch just because this is called many times.
      protected override bool IsCommandAvailable(UIApplication _, CategorySet selectedCategories)
      {
        if(AddIn.Host.Value is UIApplication app)
        {
          try
          {
            return (app.ActiveUIDocument?.Document?.IsValidObject ?? false) &&
                   (dependency as IExternalCommandAvailability).IsCommandAvailable(app, selectedCategories);
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { }
        }

        return false;
      }
    }

    /// <summary>
    /// Available even when Rhino.Inside is obsolete.
    /// </summary>
    protected internal class AvailableEvenObsolete : External.UI.CommandAvailability
    {
      public override bool IsRuntimeReady() => AddIn.CurrentStatus >= AddIn.Status.Obsolete;
    }

    /// <summary>
    /// Available when Rhino.Inside is not expired, crashed or already active.
    /// </summary>
    protected internal class Availability : External.UI.CommandAvailability
    {
      public override bool IsRuntimeReady() => AddIn.CurrentStatus >= AddIn.Status.Available;
    }

    /// <summary>
    /// Available when Rhino.Inside is ready.
    /// </summary>
    protected internal class AvailableWhenReady : External.UI.CommandAvailability
    {
      public override bool IsRuntimeReady() => AddIn.CurrentStatus >= AddIn.Status.Ready;
    }
    #endregion
  }
}
