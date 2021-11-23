using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;

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
      var elementType = default(ARDB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      var filter = default(ARDB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(elementType.Document))
      {
        var elementCollector = collector.WhereElementIsNotElementType();

        if(elementType.Category?.Id is ARDB.ElementId categoryId)
          elementCollector = elementCollector.WhereCategoryIdEqualsTo(categoryId);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          WhereTypeIdEqualsTo(elementType.Id).
          Select(Types.Element.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
