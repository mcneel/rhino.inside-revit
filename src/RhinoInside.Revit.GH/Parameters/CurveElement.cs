using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurveElement : GraphicalElementT<Types.CurveElement, ARDB.CurveElement>
  {
    public override Guid ComponentGuid => new Guid("24892092-5A53-4A12-8A90-436C2559FF56");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public CurveElement() : base("Curve Element", "Curve", "Contains a collection of Revit curve elements", "Params", "Revit Primitives") { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve" }
    );
    #endregion
  }
}
