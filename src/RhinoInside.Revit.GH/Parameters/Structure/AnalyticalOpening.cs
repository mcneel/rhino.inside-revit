using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2023
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AnalyticalOpening : GraphicalElement<Types.AnalyticalOpening, ARDB_AnalyticalOpening>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("9DAD352A-2E88-4375-BB02-802C8990C9D6");

    public AnalyticalOpening() : base("Analytical Opening", "Analytical Opening", "Contains a collection of Revit analytical openings", "Params", "Revit Elements") { }
  }
}
