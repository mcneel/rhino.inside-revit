using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  [ComponentVersion(introduced:"1.0", updated:"1.37")]
  public class WallLocationLine : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("4C5260C3-B15E-482B-8A1D-38CD868E3E72");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    public WallLocationLine() : base
    (
      name: "Wall Location Line",
      nickname: "W-Line",
      description: "Get-Set location line of given wall instance",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Wall()
        {
          Name = "Wall",
          NickName = "W",
          Description = "Wall element",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WallLocationLine>()
        {
          Name = "Location Line",
          NickName = "Ll",
          Description = "Wall location line",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Wall()
        {
          Name = "Wall",
          NickName = "W",
          Description = "Wall element",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WallLocationLine>()
        {
          Name = "Location Line",
          NickName = "Ll",
          Description = "Wall location line",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Wall curve at location line",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Offset",
          NickName = "O",
          Description = "Wall curve offset from center line",
        }, ParamRelevance.Primary
      ),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Center Curve") is IGH_Param centerCurve)      centerCurve.Name = "Wall";
      if (Params.Output<IGH_Param>("Location Curve") is IGH_Param locationCurve)  locationCurve.Name = "Curve";
      if (Params.Output<IGH_Param>("Offset Value") is IGH_Param offsetValue)      offsetValue.Name = "Offset";

      base.AddedToDocument(document);
    }
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Wall", out Types.Wall wall)) return;
      Params.TrySetData(DA, "Wall", () => wall);

      Params.TryGetData(DA, "Location Line", out ARDB.WallLocationLine? locationLine);
      if (locationLine.HasValue)
      {
        StartTransaction(wall.Document);
        wall.Value.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(locationLine.Value);
      }

      if ((wall.Value.Location as ARDB.LocationCurve)?.Curve is ARDB.Curve curve)
      {
        locationLine = wall.Value.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).AsEnum<ARDB.WallLocationLine>();
        Params.TrySetData(DA, "Location Line", () => locationLine.Value);

        var offset = -wall.Value.WallType.GetOffsetForLocationLine(locationLine.Value);
        if (wall.Value.Flipped) offset = -offset;

        Params.TrySetData(DA, "Curve", () => curve.CreateOffset(offset, ARDB.XYZ.BasisZ).ToCurve());
        Params.TrySetData(DA, "Offset", () => offset * Revit.ModelUnits);
      }
    }
  }
}
