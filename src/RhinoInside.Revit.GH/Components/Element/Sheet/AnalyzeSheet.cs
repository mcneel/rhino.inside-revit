using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  [Obsolete("Since v1.2")]
  public class AnalyzeSheet : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("15bac151-d31c-4c4d-8570-49cda0d58def");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "AS";

    public AnalyzeSheet() : base(
      name: "Analyze Sheet",
      nickname: "A-S",
      description: "Analyze given sheet view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ViewSheet(), "Sheet", "Sheet", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FamilyInstance(), "Title Block", "TB", "Sheet's title block, if any", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.AssemblyInstance(), "Assembly", "A", "Sheet's associated Assembly, if any", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var sheet = default(DB.ViewSheet);
      if (!DA.GetData("Sheet", ref sheet))
        return;

      // TODO: this should really be collected beforeSolveInstance but document is needed
      using (var collector = new DB.FilteredElementCollector(sheet.Document).
          OfCategory(DB.BuiltInCategory.OST_TitleBlocks).
          WhereElementIsNotElementType())
      {
        DA.SetData
        (
          "Title Block",
          // lets find the associated titleblock by checking its owner view id
          // collecting elements on the sheet to get the titleblock will cause
          // revit to render every sheet and it is very time consuming
          collector.Where(tb => tb.OwnerViewId.Equals(sheet.Id)).FirstOrDefault()
        );
      }

      if (!sheet.AssociatedAssemblyInstanceId.Equals(DB.ElementId.InvalidElementId))
      {
        DA.SetData
        (
          "Assembly",
          Types.AssemblyInstance.FromElementId
          (
            sheet.Document,
            sheet.AssociatedAssemblyInstanceId
          )
        );
      }
    }
  }
}
