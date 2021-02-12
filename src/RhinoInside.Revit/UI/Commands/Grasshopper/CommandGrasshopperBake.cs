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
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperBake : GrasshopperCommand
  {
    public static string CommandName => "Bake\nSelected";

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
        CommandName,
        "Ribbon.Grasshopper.Bake.png",
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
            case Rhino.Geometry.Point3d point: return new Rhino.Geometry.Point(point);
            case Rhino.Geometry.GeometryBase geometry: return geometry;
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
}
