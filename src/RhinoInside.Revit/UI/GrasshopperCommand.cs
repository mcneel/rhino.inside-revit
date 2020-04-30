using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.PlugIns;
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

      GH.Guest.Script.LoadEditor();
      if(!GH.Guest.Script.IsEditorLoaded())
        throw new Exception("Failed to startup Grasshopper");
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
      var buttonData = NewPushButtonData<CommandGrasshopper, Availability>("Grasshopper");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = "Shows Grasshopper window";
        pushButton.LongDescription = $"Use CTRL key to open only Grasshopper window without restoring other tool windows";
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Grasshopper.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Grasshopper.png");
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://www.grasshopper3d.com/"));
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      // check to see if any document path is provided in journal data
      // if yes, open the document
      string filename = null;
      if (data.JournalData.TryGetValue("Open", out filename) && File.Exists(filename))
        GH.Guest.ShowAndOpenDocumentAsync(filename);
      // otherwise, just open the GH window
      else
        GH.Guest.ShowAsync();
      // whatever happens say success so Revit does not prompt errors
      return Result.Succeeded;
    }
  }

  #region Solver
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
      var buttonData = NewPushButtonData<CommandGrasshopperRecompute, Availability>("Recompute");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = "Force a complete recompute of all objects";
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Recompute.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Recompute.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        using (var modal = new Rhinoceros.ModalScope())
        {
          if(GH_Document.EnableSolutions) definition.NewSolution(true);
          else
          {
            GH_Document.EnableSolutions = true;
            try { definition.NewSolution(false); }
            finally { GH_Document.EnableSolutions = false; }
          }

          do
          {
            var result = modal.Run(false, false);
            if (result == Result.Failed)
              return result;

          } while (definition.ScheduleDelay >= GH_Document.ScheduleRecursive);

          if (definition.SolutionState == GH_ProcessStep.PostProcess)
            return Result.Succeeded;
        }
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
          var options = new BakeOptions()
          {
            Document = Revit.ActiveUIDocument.Document,
            View = Revit.ActiveUIDocument.Document.ActiveView,
            Category = DB.Category.GetCategory(Revit.ActiveUIDocument.Document, ActiveBuiltInCategory),
            Material = default
          };

          return ObjectsToBake(definition, options).Any();
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
      var items = ribbonPanel.AddStackedItems
      (
        new ComboBoxData("Category"),
        NewPushButtonData<CommandGrasshopperBake, NeedsActiveDocument<Availability>>("Bake Selected")
      );

      if(items[0] is ComboBox comboBox)
      {
        categoriesComboBox = comboBox;
      
        EventHandler<IdlingEventArgs> BuildDirectShapeCategoryList = null;
        Revit.ApplicationUI.Idling += BuildDirectShapeCategoryList = (sender, args) =>
        {
          var doc = (sender as UIApplication).ActiveUIDocument?.Document;
          if (doc == null)
            return;

          var directShapeCategories = Enum.GetValues(typeof(DB.BuiltInCategory)).Cast<DB.BuiltInCategory>().
          Where(categoryId => DB.DirectShape.IsValidCategoryId(new DB.ElementId(categoryId), doc)).
          Select(categoryId => DB.Category.GetCategory(doc, categoryId)).
          Where(x => x is object);

          foreach (var group in directShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
          {
            foreach (var category in group.OrderBy(x => x.Name))
            {
              var comboBoxMemberData = new ComboBoxMemberData(((DB.BuiltInCategory) category.Id.IntegerValue).ToString(), category.Name)
              {
                GroupName = group.Key.ToString()
              };
              var item = categoriesComboBox.AddItem(comboBoxMemberData);

              if ((DB.BuiltInCategory) category.Id.IntegerValue == DB.BuiltInCategory.OST_GenericModel)
                categoriesComboBox.Current = item;
            }
          }

          Revit.ApplicationUI.Idling -= BuildDirectShapeCategoryList;
        };
      }

      if (items[1] is PushButton bakeButton)
      {
        bakeButton.ToolTip = "Bakes selected objects content in the active Revit document";
        bakeButton.LongDescription = "Use CTRL key to group resulting elements";
        bakeButton.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Bake.png", true);
        bakeButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Bake.png");
        bakeButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    static ComboBox categoriesComboBox = null;
    public static DB.BuiltInCategory ActiveBuiltInCategory
    {
      get => Enum.TryParse(categoriesComboBox.Current?.Name ?? string.Empty, out DB.BuiltInCategory builtInCategory) ?
             builtInCategory :
             DB.BuiltInCategory.OST_GenericModel;
    }

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
          var scaleFactor = 1.0 / Revit.ModelUnits;
          var categoryId = options.Category?.Id ?? new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);

          ids = new List<DB.ElementId>();
          foreach (var geometry in geometryToBake)
          {
            var ds = DB.DirectShape.CreateElement(options.Document, categoryId);
            ds.Name = param.NickName;

            var shape = geometry.ToHostMultiple(scaleFactor).ToList();
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
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        bool groupResult = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) != System.Windows.Forms.Keys.None;

        var options = new BakeOptions()
        {
          Document = data.Application.ActiveUIDocument.Document,
          View = data.View,
          Category = DB.Category.GetCategory(data.Application.ActiveUIDocument.Document, ActiveBuiltInCategory),
          Material = default
        };

        var resultingElementIds = new List<DB.ElementId>();
        using (var transGroup = new DB.TransactionGroup(options.Document))
        {
          transGroup.Start("Bake Selected");

          var bakedElementIds = new List<DB.ElementId>();
          foreach (var obj in Availability.ObjectsToBake(definition, options))
          {
            if (obj.Bake(options, out var partial))
              bakedElementIds.AddRange(partial);
          }

          {
            var activeDesignOptionId = DB.DesignOption.GetActiveDesignOptionId(options.Document);
            var elementIdsToAssignDO = new List<DB.ElementId>();
            foreach (var elementId in bakedElementIds)
            {
              if
              (
                options.Document.GetElement(elementId) is DB.Element element &&
                element.DesignOption?.Id is DB.ElementId elementDesignOptionId &&
                elementDesignOptionId != activeDesignOptionId
              )
              {
                elementIdsToAssignDO.Add(elementId);
              }
              else resultingElementIds?.Add(elementId);
            }

            if (elementIdsToAssignDO.Count > 0)
            {
              using (var trans = new DB.Transaction(options.Document, "Assign to Active Design Option"))
              {
                if (trans.Start() == DB.TransactionStatus.Started)
                {
                  // Move elements to Active Design Option
                  var elementIdsCopied = DB.ElementTransformUtils.CopyElements(options.Document, elementIdsToAssignDO, DB.XYZ.Zero);
                  options.Document.Delete(elementIdsToAssignDO);
                  resultingElementIds?.AddRange(elementIdsCopied);

                  trans.Commit();
                }
              }
            }
          }

          if (groupResult)
          {
            using (var trans = new DB.Transaction(options.Document, "Group Bake"))
            {
              if (trans.Start() == DB.TransactionStatus.Started)
              {
                var group = options.Document.Create.NewGroup(resultingElementIds);
                trans.Commit();

                resultingElementIds = new List<DB.ElementId>();
                resultingElementIds.Add(group.Id);
              }
            }
          }

          transGroup.Assimilate();
        }

        data.Application.ActiveUIDocument.Selection.SetElementIds(resultingElementIds);
        Instances.RedrawCanvas();
      }

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
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewOff, Availability>("Off");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.ToolTip = "Don't draw any preview geometry";
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);

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
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewWireframe, Availability>("Wireframe");

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        pushButton.ToolTip = "Draw wireframe preview geometry";
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Wireframe.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Wireframe.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);

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
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Shaded.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Shaded.png");
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);

        if(GH.PreviewServer.PreviewMode == GH_PreviewMode.Shaded)
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
