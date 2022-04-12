using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.View
{
  [ComponentVersion(introduced: "1.7")]
  public class ViewSectionBox : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("45E7E88C-76CF-45F8-A31B-AC07ACCB6DBD");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "SB";

    public ViewSectionBox() : base
    (
      name: "View Section Box",
      nickname: "SectionBox",
      description: "View Get-Set section Box",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View3D>
      (
        name: "View",
        nickname: "V",
        description: "3D View to access section box"
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Active",
        nickname: "A",
        description:  "Section Box status",
        optional:  true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Box>
      (
        name: "Box",
        nickname: "B",
        description:  "Section Box extents in world coordinate system.",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access section box",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Active",
        nickname: "A",
        description:  "Section Box status",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Box>
      (
        name: "Box",
        nickname: "B",
        description:  "Section Box in world coordinate system.",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.GraphicalElement>
      (
        name: "Section Box",
        nickname: "SB",
        description:  "Section Box element.",
        relevance: ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View3D view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (Params.GetData(DA, "Active", out bool? active) && active.HasValue)
      {
        StartTransaction(view.Document);
        view.Value.IsSectionBoxActive = active.Value;
      }
      Params.TrySetData(DA, "Active", () => view.Value.IsSectionBoxActive);

      if (Params.GetData(DA, "Box", out Box? box))
      {
        StartTransaction(view.Document);
        view.Value.SetSectionBox(box.Value.ToBoundingBoxXYZ());
      }
      Params.TrySetData(DA, "Box", () =>
      {
        var sbox = view.Value.GetSectionBox();
        sbox.Enabled = true;
        var sb = sbox.ToBox();
        return sb.IsValid ? new GH_Box(sb) : null;
      });

      Params.TrySetData(DA, "Section Box", () =>
      {
        var ids = view.Value.GetDependentElements(new ARDB.ElementCategoryFilter(ARDB.BuiltInCategory.OST_SectionBox));
        return Types.GraphicalElement.FromElementId(view.Document, ids.FirstOrDefault());
      });
    }
  }
}
