using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Filters
{
  [ComponentVersion(introduced: "1.0", updated: "1.11")]
  public class FilterElements : Component
  {
    public override Guid ComponentGuid => new Guid("36180A9E-04CA-4B38-82FE-C6707B32C680");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "Y";

    public FilterElements() : base
    (
      name: "Filter Elements",
      nickname: "FiltElem",
      description: "Evaluate if input Elements pass a Filter",
      category: "Revit",
      subCategory: "Filter"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to filter", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter to apply", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("Pass", "P", "True if the input Element is accepted by the Filter. False if the element is rejected.", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elements = new List<Types.IGH_Element>();
      if (!DA.GetDataList("Elements", elements))
        return;

      var filter = default(Types.ElementFilter);
      if (!DA.GetData("Filter", ref filter))
        return;

      var elementFilter = filter.Value;
      DA.SetDataList
      (
        "Pass",
        elements.Select(x => elementFilter is object && x?.IsValid is true ? new GH_Boolean(elementFilter.PassesFilter(x.Document, x.Id)) : default)
      );
    }
  }

  public abstract class ElementFilterComponent : Component
  {
    protected ElementFilterComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBooleanParameter("Inverted", "I", "True if the results of the filter should be inverted", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", string.Empty, GH_ParamAccess.item);
    }
  }
}
