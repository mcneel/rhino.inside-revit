using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInParameters : Grasshopper.Special.ValueSet<Types.ParameterId>
  {
    public override Guid ComponentGuid => new Guid("C1D96F56-F53C-4DFC-8090-EC2050BDBB66");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public BuiltInParameters() : base
    (
      name: "Built-In Parameters",
      nickname: "Parameters",
      description: "Provides a picker for built-in parameters",
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
        m_data.AppendRange(Types.ParameterId.EnumValues);
      }

      base.LoadVolatileData();
    }
  }
}
