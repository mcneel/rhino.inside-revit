using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Grasshopper.Kernel;
  using RhinoInside.Revit.External.DB.Extensions;

  [Kernel.Attributes.Name("Dimension")]
  public class SpotDimension : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.SpotDimension);
    public static explicit operator ARDB.SpotDimension(SpotDimension value) => value?.Value;
    public new ARDB.SpotDimension Value => base.Value as ARDB.SpotDimension;

    public SpotDimension() { }
    public SpotDimension(ARDB.SpotDimension spotDimension) : base(spotDimension) { }

  }
}
