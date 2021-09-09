using System;
using System.Linq;
using Grasshopper.Kernel;

using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class SheetIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("cadf5fbb-9dea-4b9f-8214-9897cec0e54a");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SheetIdentity() : base
    (
      name: "Sheet Identity",
      nickname: "Identity",
      description: "Query sheet identity information",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Sheet(), "Sheet", "Sheet", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Number", "NO", "Sheet number", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "Sheet name", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Placeholder", "IPH", "Sheet is placeholder", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Indexed", "IIDX", "Sheet appears on sheet lists", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Assembly Sheet", "IAS", "Sheet belongs to a Revit assembly", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var sheet = default(DB.ViewSheet);
      if (!DA.GetData("Sheet", ref sheet))
        return;

      DA.SetData("Number", sheet.SheetNumber);
      DA.SetData("Name", sheet.Name);

      DA.SetData("Is Placeholder", sheet.IsPlaceholder);
      DA.SetData("Is Indexed", sheet.GetParameterValue<bool>(DB.BuiltInParameter.SHEET_SCHEDULED));
      DA.SetData("Is Assembly Sheet", sheet.IsAssemblyView);
    }
  }
}
