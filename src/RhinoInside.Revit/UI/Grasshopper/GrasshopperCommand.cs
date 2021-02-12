using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;
using Rhino.PlugIns;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Bake;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call Grasshopper API
  /// </summary>
  abstract public class GrasshopperCommand : RhinoCommand
  {
    protected static readonly Guid PluginId = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);
    public GrasshopperCommand()
    {
      if (!PlugIn.LoadPlugIn(PluginId, true, true))
        throw new Exception("Failed to load Grasshopper");
    }

    /// <summary>
    /// Available when Grasshopper Plugin is available in Rhino
    /// </summary>
    protected new class Availability : RhinoCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        (PlugIn.PlugInExists(PluginId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected));
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopper : GrasshopperCommand
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopper, Availability>(
        "Grasshopper",
        "Resources.Grasshopper.png",
        "Shows Grasshopper window"
      );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Use CTRL key to open only Grasshopper window without restoring other tool windows";
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://www.grasshopper3d.com/"));
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      // Check to see if any document path is provided in journal data
      // if yes, open the document.
      if (data.JournalData.TryGetValue("Open", out var filename))
      {
        if (!GH.Guest.OpenDocument(filename))
          return Result.Failed;
       }

      GH.Guest.ShowEditorAsync();

      return Result.Succeeded;
    }
  }

  #region Solver
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperSolver : GrasshopperCommand
  {
    static readonly System.Windows.Media.ImageSource SolverOnSmall = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.SolverOn.png", true);
    static readonly System.Windows.Media.ImageSource SolverOnLarge = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.SolverOn.png", false);
    static readonly System.Windows.Media.ImageSource SolverOffSmall = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.SolverOff.png", true);
    static readonly System.Windows.Media.ImageSource SolverOffLarge = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.SolverOff.png", false);

    protected new class Availability : GrasshopperCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories)
      {
        return GH.Guest.IsEditorLoaded() && base.IsCommandAvailable(_, selectedCategories);
      }
    }

    static void EnableSolutionsChanged(bool EnableSolutions)
    {
      if (RestoreButton("Toggle\nSolver") is PushButton button)
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

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandGrasshopperSolver, Availability>(
        "Toggle\nSolver",
        "Resources.Ribbon.Grasshopper.SolverOff.png",
        "Toggle the Grasshopper solver"
      );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton("Toggle\nSolver", pushButton);
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // apply a min width to the button so it does not change width
        // when toggling between Enable and Disable on its title
        if (GetAdwndRibbonButton(pushButton) is Autodesk.Windows.RibbonButton ribbonButton)
          ribbonButton.MinWidth = 50;

        EnableSolutionsChanged(GH_Document.EnableSolutions);
        GH_Document.EnableSolutionsChanged += EnableSolutionsChanged;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
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

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperRecompute : GrasshopperCommand
  {
    protected new class Availability : GrasshopperCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        Instances.ActiveCanvas?.Document is object;
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperRecompute, Availability>(
        "Recompute",
        "Resources.Ribbon.Grasshopper.Recompute.png",
        "Force a complete recompute of all objects"
        );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        if(GH_Document.EnableSolutions) definition.NewSolution(true);
        else
        {
          GH_Document.EnableSolutions = true;
          try { definition.NewSolution(false); }
          finally { GH_Document.EnableSolutions = false; }
        }

        // If there are no scheduled solutions return control back to Revit now
        if (definition.ScheduleDelay > GH_Document.ScheduleRecursive)
          WindowHandle.ActiveWindow = Rhinoceros.MainWindow;

        if (definition.SolutionState == GH_ProcessStep.PostProcess)
          return Result.Succeeded;
        else
          return Result.Cancelled;
      }

      return Result.Failed;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperBake : GrasshopperCommand
  {
    protected new class Availability : GrasshopperCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories)
      {
        if (!base.IsCommandAvailable(_, selectedCategories))
          return false;

        if (Instances.ActiveCanvas?.Document is GH_Document definition)
        {
          if (Revit.ActiveUIDocument?.ActiveGraphicalView is DB.View view)
          {
            //var options = new BakeOptions()
            //{
            //  Document = view.Document,
            //  View = view,
            //  Category = DB.Category.GetCategory(view.Document, ActiveBuiltInCategory),
            //  Material = default
            //};

            //return ObjectsToBake(definition, options).Any();
            return true;
          }
        }

        return false;
      }

      public static IEnumerable<IGH_ElementIdBakeAwareObject> ObjectsToBake(GH_Document definition, BakeOptions options) =>
        ElementIdBakeAwareObject.OfType
        (
          definition.SelectedObjects().
          OfType<IGH_ActiveObject>().
          Where(x => !x.Locked)
        ).
        Where(x => x.CanBake(options));
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      //if(items[0] is ComboBox comboBox)
      //{
      //  categoriesComboBox = comboBox;
      
      //  EventHandler<IdlingEventArgs> BuildDirectShapeCategoryList = null;
      //  Revit.ApplicationUI.Idling += BuildDirectShapeCategoryList = (sender, args) =>
      //  {
      //    var doc = (sender as UIApplication).ActiveUIDocument?.Document;
      //    if (doc == null)
      //      return;

      //    var directShapeCategories = Enum.GetValues(typeof(DB.BuiltInCategory)).Cast<DB.BuiltInCategory>().
      //    Where(categoryId => DB.DirectShape.IsValidCategoryId(new DB.ElementId(categoryId), doc)).
      //    Select(categoryId => DB.Category.GetCategory(doc, categoryId)).
      //    Where(x => x is object);

      //    foreach (var group in directShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
      //    {
      //      foreach (var category in group.OrderBy(x => x.Name))
      //      {
      //        var comboBoxMemberData = new ComboBoxMemberData(((DB.BuiltInCategory) category.Id.IntegerValue).ToString(), category.Name)
      //        {
      //          GroupName = group.Key.ToString()
      //        };
      //        var item = categoriesComboBox.AddItem(comboBoxMemberData);

      //        if ((DB.BuiltInCategory) category.Id.IntegerValue == DB.BuiltInCategory.OST_GenericModel)
      //          categoriesComboBox.Current = item;
      //      }
      //    }

      //    Revit.ApplicationUI.Idling -= BuildDirectShapeCategoryList;
      //  };
      //}

      if (ribbonPanel.AddItem(NewPushButtonData<CommandGrasshopperBake, NeedsActiveDocument<Availability>>(
        "Bake\nSelected",
        "Resources.Ribbon.Grasshopper.Bake.png",
        "Bakes selected objects content in the active Revit document"
        )) is PushButton bakeButton)
      {
        bakeButton.LongDescription = "Use CTRL key to group resulting elements";
        bakeButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    //static ComboBox categoriesComboBox = null;
    //public static DB.BuiltInCategory ActiveBuiltInCategory
    //{
    //  get => Enum.TryParse(categoriesComboBox.Current?.Name ?? string.Empty, out DB.BuiltInCategory builtInCategory) ?
    //         builtInCategory :
    //         DB.BuiltInCategory.OST_GenericModel;
    //}

    class ElementIdBakeAwareObject : IGH_ElementIdBakeAwareObject
    {
      public static IEnumerable<IGH_ElementIdBakeAwareObject> OfType(IEnumerable<IGH_ActiveObject> values)
      {
        foreach (var value in values)
        {
          if (value is IGH_ElementIdBakeAwareObject bakeId)
            yield return bakeId;

          else if (value is IGH_BakeAwareObject bake)
            yield return new ElementIdBakeAwareObject(bake);
        }
      }

      readonly IGH_BakeAwareObject activeObject;
      public ElementIdBakeAwareObject(IGH_BakeAwareObject value) { activeObject = value; }
      bool IGH_ElementIdBakeAwareObject.CanBake(BakeOptions options) => activeObject.IsBakeCapable;

      bool IGH_ElementIdBakeAwareObject.Bake(BakeOptions options, out ICollection<DB.ElementId> ids)
      {
        using (var trans = new DB.Transaction(options.Document, "Bake"))
        {
          if (trans.Start() == DB.TransactionStatus.Started)
          {
            bool result = false;

            if (activeObject is IGH_Param param)
            {
              result = Bake(param, options, out ids);
            }
            else if (activeObject is IGH_Component component)
            {
              var list = new List<DB.ElementId>();
              foreach (var outParam in component.Params.Output)
              {
                if (Bake(outParam, options, out var partial))
                {
                  result = true;
                  list.AddRange(partial);
                }
              }

              ids = result ? list : default;
            }
            else ids = default;

            trans.Commit();
            return result;
          }
        }

        ids = default;
        return false;
      }

      bool Bake(IGH_Param param, BakeOptions options, out ICollection<DB.ElementId> ids)
      {
        var geometryToBake = param.VolatileData.AllData(true).Select(x => x.ScriptVariable()).
        Select(x =>
        {
          switch (x)
          {
            case Rhino.Geometry.Point3d point:          return new Rhino.Geometry.Point(point);
            case Rhino.Geometry.GeometryBase geometry:  return geometry;
          }

          return null;
        });

        if (geometryToBake.Any())
        {
          var categoryId = options.Category?.Id ?? new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);

          ids = new List<DB.ElementId>();
          foreach (var geometry in geometryToBake)
          {
            var ds = DB.DirectShape.CreateElement(options.Document, categoryId);
            ds.Name = param.NickName;

            var shape = geometry.ToShape().ToList();
            ds.SetShape(shape);
            ids.Add(ds.Id);
          }

          return true;
        }

        ids = default;
        return false;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      //if (Instances.ActiveCanvas?.Document is GH_Document definition)
      //{
      //  bool groupResult = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) != System.Windows.Forms.Keys.None;

      //  var options = new BakeOptions()
      //  {
      //    Document = data.Application.ActiveUIDocument.Document,
      //    View = data.View,
      //    Category = DB.Category.GetCategory(data.Application.ActiveUIDocument.Document, ActiveBuiltInCategory),
      //    Material = default
      //  };

      //  var resultingElementIds = new List<DB.ElementId>();
      //  using (var transGroup = new DB.TransactionGroup(options.Document))
      //  {
      //    transGroup.Start("Bake Selected");

      //    var bakedElementIds = new List<DB.ElementId>();
      //    foreach (var obj in Availability.ObjectsToBake(definition, options))
      //    {
      //      if (obj.Bake(options, out var partial))
      //        bakedElementIds.AddRange(partial);
      //    }

      //    {
      //      var activeDesignOptionId = DB.DesignOption.GetActiveDesignOptionId(options.Document);
      //      var elementIdsToAssignDO = new List<DB.ElementId>();
      //      foreach (var elementId in bakedElementIds)
      //      {
      //        if
      //        (
      //          options.Document.GetElement(elementId) is DB.Element element &&
      //          element.DesignOption?.Id is DB.ElementId elementDesignOptionId &&
      //          elementDesignOptionId != activeDesignOptionId
      //        )
      //        {
      //          elementIdsToAssignDO.Add(elementId);
      //        }
      //        else resultingElementIds?.Add(elementId);
      //      }

      //      if (elementIdsToAssignDO.Count > 0)
      //      {
      //        using (var trans = new DB.Transaction(options.Document, "Assign to Active Design Option"))
      //        {
      //          if (trans.Start() == DB.TransactionStatus.Started)
      //          {
      //            // Move elements to Active Design Option
      //            var elementIdsCopied = DB.ElementTransformUtils.CopyElements(options.Document, elementIdsToAssignDO, DB.XYZ.Zero);
      //            options.Document.Delete(elementIdsToAssignDO);
      //            resultingElementIds?.AddRange(elementIdsCopied);

      //            trans.Commit();
      //          }
      //        }
      //      }
      //    }

      //    if (groupResult)
      //    {
      //      using (var trans = new DB.Transaction(options.Document, "Group Bake"))
      //      {
      //        if (trans.Start() == DB.TransactionStatus.Started)
      //        {
      //          var group = options.Document.Create.NewGroup(resultingElementIds);
      //          trans.Commit();

      //          resultingElementIds = new List<DB.ElementId>();
      //          resultingElementIds.Add(group.Id);
      //        }
      //      }
      //    }

      //    transGroup.Assimilate();
      //  }

      //  data.Application.ActiveUIDocument.Selection.SetElementIds(resultingElementIds);
      //  Instances.RedrawCanvas();
      //}

      return Result.Succeeded;
    }
  }
  #endregion

  #region Preview
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
    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewOff, Availability>(" Off ");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.ToolTip = "Don't draw any preview geometry";
        pushButton.Image = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Off.png", true);
        pushButton.LargeImage = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Off.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // add spacing to title to get it to be a consistent width
        if (GetAdwndRibbonButton(pushButton) is Autodesk.Windows.RibbonButton ribbonButton)
          ribbonButton.Text = "   Off    ";

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
    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewWireframe, Availability>(" Wire ");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.ToolTip = "Draw wireframe preview geometry";
        pushButton.Image = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Wireframe.png", true);
        pushButton.LargeImage = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Wireframe.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
        // add spacing to title to get it to be a consistent width
        if (GetAdwndRibbonButton(pushButton) is Autodesk.Windows.RibbonButton ribbonButton)
          ribbonButton.Text = "  Wire   ";

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
    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewShaded, Availability>("Shaded");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.ToolTip = "Draw shaded preview geometry";
        pushButton.Image = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Shaded.png", true);
        pushButton.LargeImage = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.Preview_Shaded.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);

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
  #endregion
}
