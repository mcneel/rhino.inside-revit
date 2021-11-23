using System;
using System.Windows.Media;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Grasshopper;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperSolver : GrasshopperCommand
  {
    public static string CommandName => "Toggle\nSolver";

    static readonly ImageSource SolverOnSmall = LoadRibbonButtonImage("Ribbon.Grasshopper.SolverOn.png", true);
    static readonly ImageSource SolverOnLarge = LoadRibbonButtonImage("Ribbon.Grasshopper.SolverOn.png", false);
    static readonly ImageSource SolverOffSmall = LoadRibbonButtonImage("Ribbon.Grasshopper.SolverOff.png", true);
    static readonly ImageSource SolverOffLarge = LoadRibbonButtonImage("Ribbon.Grasshopper.SolverOff.png", false);

    protected new class Availability : GrasshopperCommand.Availability
    {
      protected override bool IsCommandAvailable(UIApplication _, CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        GH.Guest.IsEditorLoaded();
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandGrasshopperSolver, Availability>
      (
        CommandName,
        "Ribbon.Grasshopper.SolverOff.png",
        "Toggle the Grasshopper solver"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        // apply a min width to the button so it does not change width
        // when toggling between Enable and Disable on its title
        pushButton.SetMinWidth(50);
        StoreButton(CommandName, pushButton);
      }

      AssemblyResolver.References["Grasshopper"].Activated += Grasshopper_AssemblyActivated;
    }

    private static void Grasshopper_AssemblyActivated(object sender, AssemblyLoadEventArgs args)
    {
      EnableSolutionsChanged(GH_Document.EnableSolutions);
      GH_Document.EnableSolutionsChanged += EnableSolutionsChanged;
    }

    static void EnableSolutionsChanged(bool EnableSolutions)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        if (EnableSolutions)
        {
          button.ToolTip = "Disable the Grasshopper solver";
          button.ItemText = "Disable\nSolver";
          button.Image = SolverOnSmall;
          button.LargeImage = SolverOnLarge;
        }
        else
        {
          button.ToolTip = "Enable the Grasshopper solver";
          button.ItemText = "Enable\nSolver";
          button.Image = SolverOffSmall;
          button.LargeImage = SolverOffLarge;
        }
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      GH_Document.EnableSolutions = !GH_Document.EnableSolutions;

      if (GH_Document.EnableSolutions)
      {
        if (Instances.ActiveCanvas?.Document is GH_Document definition)
          definition.NewSolution(false);
      }
      else
      {
        Revit.RefreshActiveView();
      }

      return Result.Succeeded;
    }
  }
}
