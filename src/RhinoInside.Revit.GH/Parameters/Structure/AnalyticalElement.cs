using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalElement;
#else
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalModel;
#endif

  [ComponentVersion(introduced: "1.16")]
  public class AnalyticalElement : GraphicalElement<Types.AnalyticalElement, ARDB_Structure_AnalyticalElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("CEC912FA-C0D3-47C2-A3A6-52A8B8EA4476");

    public AnalyticalElement() : base("Analytical Element", "Analytical Element", "Contains a collection of Revit analytical elements", "Params", "Revit Elements") { }

  }
}
