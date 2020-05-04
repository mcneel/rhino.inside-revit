using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands
  /// </summary>
  abstract public class Command : External.UI.Command
  {
    internal static PushButton AddPushButton<CommandType, AvailabilityType>(PulldownButton pullDownButton, string text, string tooltip)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
    {
      var buttonData = NewPushButtonData<CommandType, AvailabilityType>(text);

      if (pullDownButton.AddPushButton(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = tooltip;
        return pushButton;
      }

      return null;
    }

    internal static PushButtonData NewPushButtonData<CommandType, AvailabilityType>(string text = null)
      where CommandType : IExternalCommand
      where AvailabilityType : IExternalCommandAvailability
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

    public class AllwaysAvailable : IExternalCommandAvailability
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
