using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyGeometryOpeningByCurve : Component
  {
    public override Guid ComponentGuid => new Guid("72FDC627-09C7-4D9F-8D7F-5F6812FB1873");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "O";

    public FamilyGeometryOpeningByCurve()
    : base("Component Family Opening", "FamOp", string.Empty, "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
      manager[manager.AddBooleanParameter("Opening", "O", string.Empty, GH_ParamAccess.item, true)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var curve = default(Rhino.Geometry.Curve);
      if (!DA.GetData("Curve", ref curve))
        return;

      curve = curve.DuplicateCurve();

      var opening = true;
      if (DA.GetData("Opening", ref opening))
        curve.SetUserString("IS_OPENING_PARAM", opening ? "1" : null);

      DA.SetData("Curve", curve);
    }
  }
}
