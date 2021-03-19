using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Grasshopper.Kernel;
using Rhino.PlugIns;
using RhinoInside.Revit.External.UI.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  abstract class CommandGrasshopperPreview : GrasshopperCommand
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var radioData = new RadioButtonGroupData("GrasshopperPreview");

      if (ribbonPanel.AddItem(radioData) is RadioButtonGroup radioButton)
      {
        CommandGrasshopperPreviewOff.CreateUI(radioButton);
        CommandGrasshopperPreviewWireframe.CreateUI(radioButton);
        CommandGrasshopperPreviewShaded.CreateUI(radioButton);
      }
#endif
    }

    protected new class Availability : NeedsActiveDocument<GrasshopperCommand.Availability>
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        Revit.ActiveUIDocument?.Document.IsFamilyDocument == false;
    }
  }

#if REVIT_2018
  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewOff : CommandGrasshopperPreview
  {
    public static string CommandName => "Off";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewOff, Availability>(CommandName, "Ribbon.Grasshopper.Preview_Off.png", "Don't draw any preview geometry");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("   Off    ");

        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Disabled)
          radioButtonGroup.Current = pushButton;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Disabled;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewWireframe : CommandGrasshopperPreview
  {
    public static string CommandName => "Wire";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewWireframe, Availability>(CommandName, "Ribbon.Grasshopper.Preview_Wireframe.png", "Draw wireframe preview geometry");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("  Wire   ");

        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Wireframe)
          radioButtonGroup.Current = pushButton;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Wireframe;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewShaded : CommandGrasshopperPreview
  {
    public static string CommandName => "Shaded";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewShaded, Availability>(CommandName, "Ribbon.Grasshopper.Preview_Shaded.png", "Draw shaded preview geometry");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // set this toggle to active by default
        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Shaded)
          radioButtonGroup.Current = pushButton;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Shaded;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }
#endif
}
