using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Grasshopper;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.DB.Schemas;
using RhinoInside.Revit.GH.Bake;
using DB = Autodesk.Revit.DB;
using WF = System.Windows.Forms;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperBake : GrasshopperCommand
  {
    public static string CommandName => "Bake\nSelected";

    /// <summary>
    /// Available when there are bake aware objects selected on Grasshopper canvas.
    /// </summary>
    protected new class Availability : NeedsActiveDocument<GrasshopperCommand.Availability>
    {
      protected override bool IsCommandAvailable(UIApplication app, DB.CategorySet selectedCategories)
      {
        if (!base.IsCommandAvailable(app, selectedCategories))
          return false;

        if (!DB.DirectShape.IsSupportedDocument(Revit.ActiveUIDocument.Document))
          return false;

        if (Instances.ActiveCanvas?.Document is GH_Document definition)
        {
          if (Revit.ActiveUIDocument.ActiveGraphicalView is DB.View view)
          {
            var options = new BakeOptions()
            {
              Document = view.Document,
              View = view
            };

            return ObjectsToBake(definition, options).Any();
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
      var buttonData = NewPushButtonData<CommandGrasshopperBake, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Bake.png",
        tooltip: "Bakes selected objects content in the active Revit document"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton bakeButton)
      {
        bakeButton.LongDescription = "Use CTRL key to group resulting elements";
        StoreButton(CommandName, bakeButton);
      }
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
        var geometryToBake = param.VolatileData.AllData(true).
          Select(GH_Convert.ToGeometryBase).
          OfType<Rhino.Geometry.GeometryBase>();

        if (geometryToBake.Any())
        {
          var categoryId = options.Category?.Id ?? new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);

          var worksetId = DB.WorksetId.InvalidWorksetId;
          if (options.Document.IsWorkshared)
            worksetId = options.Workset?.Id ?? options.Document.GetWorksetTable().GetActiveWorksetId();

          ids = new List<DB.ElementId>();
          foreach (var geometry in geometryToBake.Where(g => g is object))
          {
            var ds = DB.DirectShape.CreateElement(options.Document, categoryId);
            ds.Name = param.NickName;
            if (options.Document.IsWorkshared)
            {
              if (ds.GetParameter(ParameterId.ElemPartitionParam) is DB.Parameter worksetParam)
                worksetParam.Set(worksetId.IntegerValue);
            }

            ds.SetShape(geometry.ToShape());
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
        var doc = data.Application.ActiveUIDocument.Document;

        var bakeOptsDlg = new Forms.BakeOptionsDialog(data.Application);
        if (bakeOptsDlg.ShowModal() != Eto.Forms.DialogResult.Ok)
          return Result.Cancelled;

        bool groupResult = (WF.Control.ModifierKeys & WF.Keys.Control) != WF.Keys.None;

        var options = new BakeOptions()
        {
          Document = doc,
          View = data.View,
          Category = DB.Category.GetCategory(doc, bakeOptsDlg.SelectedCategory),
          Workset = doc.IsWorkshared ? doc.GetWorksetTable().GetWorkset(bakeOptsDlg.SelectedWorkset) : default,
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

                resultingElementIds = new List<DB.ElementId>() { group.Id };
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
}
