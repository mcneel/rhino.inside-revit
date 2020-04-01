using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  static class Extension
  {
    public static bool ActivateRibbonTab(this UIApplication application, string tabName)
    {
      var ribbon = Autodesk.Windows.ComponentManager.Ribbon;
      foreach (var tab in ribbon.Tabs)
      {
        if (tab.Name == tabName)
        {
          tab.IsActive = true;
          return true;
        }
      }

      return false;
    }

    internal static PushButton AddPushButton(this RibbonPanel ribbonPanel, Type commandType, string text = null, string tooltip = null, Type availability = null)
    {
      var buttonData = new PushButtonData
      (
        commandType.Name,
        text ?? commandType.Name,
        commandType.Assembly.Location,
        commandType.FullName
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = tooltip;
        if (availability != null)
          pushButton.AvailabilityClassName = availability.FullName;

        return pushButton;
      }

      return null;
    }

    internal static PushButton AddPushButton(this PulldownButton pullDownButton, Type commandType, string text = null, string tooltip = null, Type availability = null)
    {
      var buttonData = new PushButtonData
      (
        commandType.Name,
        text ?? commandType.Name,
        commandType.Assembly.Location,
        commandType.FullName
      );

      if (pullDownButton.AddPushButton(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = tooltip;
        if (availability != null)
          pushButton.AvailabilityClassName = availability.FullName;

        return pushButton;
      }

      return null;
    }
  }

  /// <summary>
  /// Base class for all Rhino.Inside Revit commands
  /// </summary>
  abstract public class Command : External.UI.Command
  {
    public static PushButtonData NewPushButtonData<CommandType>(string text = null)
    where CommandType : IExternalCommand
    {
      return new PushButtonData
      (
        typeof(CommandType).Name,
        text ?? typeof(CommandType).Name,
        typeof(CommandType).Assembly.Location,
        typeof(CommandType).FullName
      );
    }

    public static PushButtonData NewPushButtonData<CommandType, AvailabilityType>(string text = null)
    where CommandType : IExternalCommand where AvailabilityType : IExternalCommandAvailability
    {
      return new PushButtonData
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

    public static ToggleButtonData NewToggleButtonData<CommandType, AvailabilityType>(string text = null)
    where CommandType : IExternalCommand where AvailabilityType : IExternalCommandAvailability
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

    public class AllwaysAvailable : IExternalCommandAvailability
    {
      bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        true;
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
