using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.9")]
  public class AssemblyOrigin : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("1C1CC766-D782-4C4A-8B2E-FE8508E4A623");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    protected override string IconTag => "⌖";

    public AssemblyOrigin() : base
    (
      name: "Assembly Origin",
      nickname: "Origin",
      description: "Get-Set access component for assembly origin",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Assembly to analyze or modify",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Origin",
          NickName = "O",
          Description = "Assembly origin",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Analyzed or modified Assembly",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Origin",
          NickName = "O",
          Description = "Assembly origin",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Assembly", out Types.AssemblyInstance assembly, x => x is object)) return;
      else DA.SetData("Assembly", assembly);

      if (Params.TryGetData(DA, "Origin", out Plane? origin) && origin.HasValue)
      {
        assembly.Value.GetTransform().GetCoordSystem(out var o, out var x, out var y, out var z);
        var O = origin.Value.Origin.ToXYZ();
        var X = (ERDB.UnitXYZ) origin.Value.XAxis.ToXYZ();
        var Y = (ERDB.UnitXYZ) origin.Value.YAxis.ToXYZ();
        var Z = (ERDB.UnitXYZ) origin.Value.ZAxis.ToXYZ();
        if
        (
          !o.AlmostEqualPoints(O) ||
          !x.AlmostEquals(X) ||
          !y.AlmostEquals(Y) ||
          !z.AlmostEquals(Z))
        {
          StartTransaction(assembly.Document);
          var transform = ARDB.Transform.Identity;
          transform.SetCoordSystem(O, X, Y, Z);
          assembly.Value.SetTransform(transform);
        }
      }

      Params.TrySetData(DA, "Origin", () => assembly.Location);
    }
  }
}
