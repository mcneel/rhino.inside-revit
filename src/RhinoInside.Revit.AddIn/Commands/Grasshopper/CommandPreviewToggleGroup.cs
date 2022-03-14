using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.AddIn.Commands
{
  abstract class CommandGrasshopperPreview : GrasshopperCommand
  {
    public static string CommandName => "GrasshopperPreview";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var radioData = new RadioButtonGroupData(CommandName);

      if (ribbonPanel.AddItem(radioData) is RadioButtonGroup radioButton)
      {
        CommandGrasshopperPreviewOff.CreateUI(radioButton);
        CommandGrasshopperPreviewWireframe.CreateUI(radioButton);
        CommandGrasshopperPreviewShaded.CreateUI(radioButton);

        StoreButton(CommandName, radioButton);
      }

      AssemblyResolver.References["Grasshopper"].Activated += Grasshopper_AssemblyActivated;
#endif
    }

#if REVIT_2018
    private static void Grasshopper_AssemblyActivated(object sender, AssemblyLoadEventArgs args)
    {
      if (RestoreButton(CommandName) is RadioButtonGroup radioButton)
      {
        var options = Properties.AddInOptions.Current;
        GH.PreviewServer.PreviewModeChanged += (_, previous) =>
        {
          var mode = GH.PreviewServer.PreviewMode;
          options.CustomOptions.Set("Grasshopper", "PreviewMode", mode.ToString());

          switch (mode)
          {
            case GH_PreviewMode.Disabled:  radioButton.Current = RestoreButton(CommandGrasshopperPreviewOff.CommandName) as ToggleButton;       break;
            case GH_PreviewMode.Wireframe: radioButton.Current = RestoreButton(CommandGrasshopperPreviewWireframe.CommandName) as ToggleButton; break;
            case GH_PreviewMode.Shaded:    radioButton.Current = RestoreButton(CommandGrasshopperPreviewShaded.CommandName) as ToggleButton;    break;
          }
        };

        if (Enum.TryParse(options.CustomOptions.Get("Grasshopper", "PreviewMode"), out GH_PreviewMode previewMode))
          GH.PreviewServer.PreviewMode = previewMode;
      }
    }

    /// <summary>
    /// Available when current Revit document is a project, not a family.
    /// </summary>
    protected new class Availability : GrasshopperCommand.Availability
    {
      protected override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        DirectContext3DServer.IsAvailable(Revit.ActiveUIDocument?.ActiveGraphicalView);
    }
#endif
  }

#if REVIT_2018
  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewOff : CommandGrasshopperPreview
  {
    public static new string CommandName => "Off";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewOff, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Off.png",
        tooltip: "Don't draw any preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("   Off    ");
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Disabled;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewWireframe : CommandGrasshopperPreview
  {
    public static new string CommandName => "Wire";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewWireframe, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Wireframe.png",
        tooltip: "Draw wireframe preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("  Wire   ");
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Wireframe;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewShaded : CommandGrasshopperPreview
  {
    public static new string CommandName => "Shaded";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewShaded, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Shaded.png",
        tooltip: "Draw shaded preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Shaded;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }
#endif
}
