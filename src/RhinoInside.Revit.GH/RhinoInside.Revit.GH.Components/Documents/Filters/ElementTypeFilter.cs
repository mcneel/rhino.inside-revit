using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.Filters
{
  public class ElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("4434C470-4CAF-4178-929D-284C3B5A24B5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementTypeFilter()
    : base("Element.TypeFilter", "Type Filter", "Filter used to match elements by their type", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.ElementTypes.ElementType(), "Types", "T", "Types to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var types = new List<DB.ElementType>();
      if (!DA.GetDataList("Types", types))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (types.Any())
      {
        var provider = new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.ELEM_TYPE_PARAM));

        var typeIds = types.Select(x => x?.Id ?? DB.ElementId.InvalidElementId).ToArray();
        if (typeIds.Length == 1)
        {
          var rule = new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), typeIds[0]);
          DA.SetData("Filter", new DB.ElementParameterFilter(rule, inverted));
        }
        else
        {
          if (inverted)
          {
            var rules = typeIds.Select(x => new DB.FilterInverseRule(new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), x))).ToArray();
            DA.SetData("Filter", new DB.ElementParameterFilter(rules));
          }
          else
          {
            var filters = typeIds.Select(x => new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), x)).Select(x => new DB.ElementParameterFilter(x)).ToArray();
            DA.SetData("Filter", new DB.LogicalOrFilter(filters));
          }
        }
      }
    }
  }
}
