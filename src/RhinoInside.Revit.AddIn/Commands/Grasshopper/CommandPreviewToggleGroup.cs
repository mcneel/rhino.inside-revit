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

    static RadioButtonGroup ButtonGroup;
    static ToggleButton Off;
    static ToggleButton Wireframe;
    static ToggleButton Shaded;

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var radioData = new RadioButtonGroupData(CommandName);

      ButtonGroup = ribbonPanel.AddItem(radioData) as RadioButtonGroup;
      {
        Off = CommandGrasshopperPreviewOff.CreateUI(ButtonGroup);
        Wireframe = CommandGrasshopperPreviewWireframe.CreateUI(ButtonGroup);
        Shaded = CommandGrasshopperPreviewShaded.CreateUI(ButtonGroup);
      }

      AssemblyResolver.References["Grasshopper"].Activated += Grasshopper_AssemblyActivated;
#endif
    }

#if REVIT_2018
    private static void Grasshopper_AssemblyActivated(object sender, AssemblyLoadEventArgs args)
    {
      if (ButtonGroup is object)
      {
        if (Enum.TryParse(Properties.AddInOptions.Current.CustomOptions.Get("Grasshopper", "PreviewMode"), out GH_PreviewMode previewMode))
          GH.PreviewServer.PreviewMode = previewMode;

        GH.PreviewServer.PreviewModeChanged += (_, previous) =>
        {
          var mode = GH.PreviewServer.PreviewMode;

          switch (mode)
          {
            case GH_PreviewMode.Disabled: ButtonGroup.Current = Off; break;
            case GH_PreviewMode.Wireframe: ButtonGroup.Current = Wireframe; break;
            case GH_PreviewMode.Shaded: ButtonGroup.Current = Shaded; break;
          }

          Properties.AddInOptions.Current.CustomOptions.Set("Grasshopper", "PreviewMode", mode.ToString());
        };
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

    public static ToggleButton CreateUI(RadioButtonGroup radioButtonGroup)
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
        return pushButton;
      }

      return null;
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

    public static ToggleButton CreateUI(RadioButtonGroup radioButtonGroup)
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
        return pushButton;
      }

      return null;
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

    public static ToggleButton CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewShaded, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Shaded.png",
        tooltip: "Draw shaded preview geometry"
      );

      return radioButtonGroup.AddItem(buttonData);
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
