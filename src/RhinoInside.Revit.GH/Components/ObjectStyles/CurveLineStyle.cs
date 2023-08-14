using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ObjectStyles
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8")]
  public class CurveLineStyle : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("60BE53C5-11C8-42BC-8634-294540D59580");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public CurveLineStyle()
    : base
    (
      "Curve Line Style",
      "Linestyle",
      "Curve Line Style Property. Get-Set access component to Curve Line Style property.",
      "Revit",
      "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Curve element to access Line Style",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicsStyle()
        {
          Name = "Line Style",
          NickName = "LS",
          Description = "Curve linestyle",
          Optional = true
        },ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Curve element to access Line Style",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicsStyle()
        {
          Name = "Line Style",
          NickName = "SC",
          Description = "Curve element Line Style",
        },ParamRelevance.Primary
      ),
    };

    readonly HashSet<ARDB.SketchPlane> sketchPlanes = new HashSet<ARDB.SketchPlane>(ElementEqualityComparer.InterDocument);

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.CurveElement curve, x => x.IsValid)) return;
      else DA.SetData("Element", curve);

      if (Params.GetData(DA, "Line Style", out Types.GraphicsStyle linestyle))
        UpdateElement
        (
          curve.Value,
          () =>
          {
            if (!curve.LineStyle.Equals(linestyle))
            {
              var validStyles = curve.Value.GetLineStyleIds();
              if (curve.Document.IsFamilyDocument)
                validStyles.Add(curve.Document.OwnerFamily.FamilyCategory.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection).Id);

              if (!validStyles.Contains(linestyle.Id))
                throw new Exceptions.RuntimeArgumentException("Line Style", $"'{linestyle.Nomen}' is not a valid Line Style for '{curve.Nomen}'. {{{curve.Id}}}");

              curve.LineStyle = linestyle;

              if (curve.SketchPlane.Value is ARDB.SketchPlane sketchPlane && sketchPlane.GetSketchId() != ElementIdExtension.Invalid)
                sketchPlanes.Add(sketchPlane);
            }
          }
        );

      Params.TrySetData(DA, "Line Style", () => curve.LineStyle);
    }

    protected override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      base.OnPrepare(documents);

      // This forces a graphic refresh
      foreach (var sketckPlane in sketchPlanes)
      {
        var pinned = sketckPlane.Pinned;
        sketckPlane.Pinned = false;
        sketckPlane.Location.Move( ARDB.XYZ.BasisZ);
        sketckPlane.Location.Move(-ARDB.XYZ.BasisZ);
        sketckPlane.Pinned = pinned;
      }
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();
      sketchPlanes.Clear();
    }
  }
}
