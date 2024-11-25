using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AnalyticalMember : GraphicalElement<Types.AnalyticalMember, ARDB_AnalyticalMember>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("0A7B685C-98E9-4684-A441-54C7B9192FDC");

    public AnalyticalMember() : base("Analytical Member", "Analytical Member", "Contains a collection of Revit analytical members", "Params", "Revit Elements") { }
  }
}
