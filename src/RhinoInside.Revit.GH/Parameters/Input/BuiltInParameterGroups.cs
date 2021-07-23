using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInParameterGroups : Grasshopper.Special.ValueSet<Types.ParameterGroup>
  {
    public override Guid ComponentGuid => new Guid("5D331B12-DA6C-46A7-AA13-F463E42650D1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public BuiltInParameterGroups() : base
    (
      name: "Built-In Parameter Groups",
      nickname: "Parameter Groups",
      description: "Provides a picker for built-in parameter Groups",
      category: "Revit",
      subcategory: "Parameter"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
      LayoutLevel = 2;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        m_data.AppendRange(Types.ParameterGroup.EnumValues);
      }

      base.LoadVolatileData();
    }
  }
}
