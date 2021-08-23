using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementDependents : Component
  {
    public override Guid ComponentGuid => new Guid("97D71AA8-6987-45B9-8F25-B92671E20EF4");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "D";

    public ElementDependents() : base
    (
      name: "Element Dependents",
      nickname: "Dependents",
      description: "Queries for all elements that, from a logical point of view, are the children of Element",
      category: "Revit",
      subCategory: "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter that will be applied to dependant elements", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Dependent elements. From a logical point of view, are the children of this Element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      var elements = element.GetDependentElements(filter);

      DA.SetDataList("Elements", elements.Where(x => x != element.Id).Select(x => Types.Element.FromElementId(element.Document, x)));
    }
  }
}
