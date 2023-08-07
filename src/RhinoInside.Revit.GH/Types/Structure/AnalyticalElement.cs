using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalElement;
#else
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalModel;
#endif

  [Kernel.Attributes.Name("Analytical Element")]
  public class AnalyticalElement : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalElement);
    public new ARDB_Structure_AnalyticalElement Value => base.Value as ARDB_Structure_AnalyticalElement;

    public AnalyticalElement() { }
    public AnalyticalElement(ARDB_Structure_AnalyticalElement element) : base(element)
    {
      var p = element.HasPhases();
    }
  }
}
