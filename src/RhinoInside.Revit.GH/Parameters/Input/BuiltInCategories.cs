using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInCategories : Grasshopper.Special.ValueSet<Types.CategoryId>
  {
    public override Guid ComponentGuid => new Guid("AF9D949F-1692-45AA-9FE4-653CFF5ECA26");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public BuiltInCategories() : base
    (
      name: "Built-In Categories",
      nickname: "Categories",
      description: "Provides a picker for built-in categories",
      category: "Revit",
      subcategory: "Input"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        m_data.AppendRange(Types.CategoryId.EnumValues);
      }

      base.LoadVolatileData();
    }
  }
}
