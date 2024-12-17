using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2023
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AnalyticalPanel : GraphicalElement<Types.AnalyticalPanel, ARDB_AnalyticalPanel>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("04B8EAC3-E81F-4D14-A65B-EB867EF2577E");
    protected override string IconTag => "AP";

    public AnalyticalPanel() : base("Analytical Panel", "Analytical Panel", "Contains a collection of Revit analytical panels", "Params", "Revit Elements") { }
  }
}
