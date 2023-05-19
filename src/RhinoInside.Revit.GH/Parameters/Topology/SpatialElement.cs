using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class SpatialElement : GraphicalElement<Types.SpatialElement, ARDB.SpatialElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("8774ACF3-7B77-474F-B12B-03D4CBBC3C15");
    protected override string IconTag => string.Empty;

    public SpatialElement() : base
    (
      name: "Spatial Element",
      nickname: "Spatial Element",
      description: "Contains a collection of Revit spatial elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Surface" }
    );
    #endregion
  }
}
