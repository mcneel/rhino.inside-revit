using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  [ComponentVersion(introduced: "1.10")]
  public class BuiltInFailureDefinitions : Grasshopper.Special.ValueSet<Types.FailureDefinition>
  {
    public override Guid ComponentGuid => new Guid("73E14FBB-24EA-44FE-85ED-5D028154029B");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public BuiltInFailureDefinitions() : base
    (
      name: "Built-In Failure Definitions",
      nickname: "Failure Definitions",
      description: "Provides a picker for built-in failure definitions",
      category: "Revit",
      subcategory: "Document"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        using (var registry = Autodesk.Revit.ApplicationServices.Application.GetFailureDefinitionRegistry())
          m_data.AppendRange(registry.ListAllFailureDefinitions().Select(x => new Types.FailureDefinition(x.GetId().Guid)));
      }

      base.LoadVolatileData();
    }
  }
}
