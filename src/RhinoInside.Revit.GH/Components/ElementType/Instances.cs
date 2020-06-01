using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementTypeInstances : Component
  {
    public override Guid ComponentGuid => new Guid("9958995F-CCD4-48DE-B3B3-AE769F04F4DD");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override string IconTag => "I";

    public ElementTypeInstances() : base
    (
      name: "Type Instances",
      nickname: "TypInstas",
      description: "Obtains all elements of the specified Type",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "Type to query for its instances", GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(DB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(elementType.Document))
      {
        var elementCollector = collector.WhereElementIsNotElementType();

        if(elementType.Category?.Id is DB.ElementId categoryId)
          elementCollector = elementCollector.OfCategoryId(categoryId);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          OfTypeId(elementType.Id).
          Select(x => Types.Element.FromElement(x))
        );
      }
    }
  }
}
