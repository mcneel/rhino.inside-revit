using System;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentElementTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("7B00F940-4C6E-4F3F-AB81-C3EED430DE96");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(false);

    public DocumentElementTypes() : base
    (
      "Document.ElementTypes", "ElementTypes",
      "Get active document element types list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("FamilyName", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("TypeName", "N", string.Empty, GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "ElementTypes", "T", "Requested element type", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      string familyName = null;
      DA.GetData("FamilyName", ref familyName);

      string name = null;
      DA.GetData("TypeName", ref name);

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        var elementTypes = elementCollector.Cast<DB.ElementType>();

        if (familyName is object)
          elementTypes = elementTypes.Where(x => x.FamilyName == familyName);

        if (name is object)
          elementTypes = elementTypes.Where(x => x.Name == name);

        DA.SetDataList("ElementTypes", elementTypes.Select(x => new Types.ElementType(x)));
      }
    }
  }
}
