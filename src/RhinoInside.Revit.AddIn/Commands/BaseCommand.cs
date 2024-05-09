using System;
using System.Collections.Generic;
using System.Drawing.Interop;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands
  /// </summary>
  public abstract class Command : External.UI.ExternalCommand
  {
    internal static readonly string TabName = "Rhino.Inside";

    #region Image
    internal static System.Windows.Media.Imaging.BitmapSource LoadRibbonButtonImage(string name, bool small = false)
    {
      int desiredSize = small ? 16 : 32;
      var assembly = Assembly.GetExecutingAssembly();
      var root = typeof(Loader).Namespace;

      using (var resource = assembly.GetManifestResourceStream($"{root}.Resources.{name}"))
      {
        using (var bitmap = new System.Drawing.Bitmap(resource))
          return bitmap.ToBitmapImage(desiredSize, desiredSize);
      }
    }
    #endregion

    #region Ribbon item creation
    internal static SplitButtonData NewSplitButtonData<CommandType>(string text, string image, string tooltip, string url = default)
      where CommandType : IExternalCommand
    {
      var data = new SplitButtonData
      (
        typeof(CommandType).Name,
        text ?? typeof(CommandType).Name
      )
      {
        Image = LoadRibbonButtonImage(image, true),
        LargeImage = LoadRibbonButtonImage(image),
        ToolTip = tooltip
      };

      if (url != string.Empty)
      {
        if (url is null) url = Core.WebSite;
        else if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
          url = Core.WebSite + url;

        data.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, url));
      }

      return data;
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
        Image = LoadRibbonButtonImage(iconName, true),
        LargeImage = LoadRibbonButtonImage(iconName),
        ToolTip = tooltip
      };

      if (url != string.Empty)
      {
        if (url is null) url = Core.WebSite;
        else if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
          url = Core.WebSite + url;

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
        Image = LoadRibbonButtonImage(iconName, true),
        LargeImage = LoadRibbonButtonImage(iconName),
        ToolTip = tooltip,
      };

      if (url != string.Empty)
      {
        if (url is null) url = Core.WebSite;
        else if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
          url = Core.WebSite + url;

        data.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, url));
      }

      return data;
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
    /// Availability for commands that are available when there is an active document open.
    /// </summary>
    public struct NeedsActiveDocument : IExternalCommandAvailability
    {
      bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) => app.ActiveUIDocument is object;
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
        if(Core.Host.Value is UIApplication app)
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
      public override bool IsRuntimeReady() => Core.CurrentStatus >= Core.Status.Obsolete;
    }

    /// <summary>
    /// Available when Rhino.Inside is not expired, crashed or already active.
    /// </summary>
    protected internal class Availability : External.UI.CommandAvailability
    {
      public override bool IsRuntimeReady() => Core.CurrentStatus >= Core.Status.Available;
    }

    /// <summary>
    /// Available when Rhino.Inside is ready.
    /// </summary>
    protected internal class AvailableWhenReady : External.UI.CommandAvailability
    {
      public override bool IsRuntimeReady() => Core.CurrentStatus >= Core.Status.Ready;
    }
    #endregion
  }
}
