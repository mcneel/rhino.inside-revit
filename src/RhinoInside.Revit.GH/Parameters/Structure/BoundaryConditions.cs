using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.27")]
  public class BoundaryConditions : GraphicalElement<Types.BoundaryConditions, ARDB.Structure.BoundaryConditions>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("DEE191F4-9D73-46C7-9BD0-EBBBD5B8D4A6");

    public BoundaryConditions() : base("Boundary Conditions", "Boundary Conditions", "Contains a collection of Revit Boundary Conditions", "Params", "Revit Elements") { }
  }

  //[ComponentVersion(introduced: "1.27")]
  //public class PointBoundaryConditions : GraphicalElement<Types.PointBoundaryConditions, ARDB.Structure.BoundaryConditions>
  //{
  //  public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
  //  public override Guid ComponentGuid => new Guid("475B48F4-D900-43EB-BD48-B9B683291EFF");

  //  public PointBoundaryConditions() : base("Point Boundary Conditions", " Point Boundary Conditions", "Contains a collection of Revit Point Boundary Conditions", "Params", "Revit Elements") { }
  //}

  //[ComponentVersion(introduced: "1.27")]
  //public class LineBoundaryConditions : GraphicalElement<Types.LineBoundaryConditions, ARDB.Structure.BoundaryConditions>
  //{
  //  public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
  //  public override Guid ComponentGuid => new Guid("0B3DD84D-A8F1-425D-B37C-00986EA35D34");

  //  public LineBoundaryConditions() : base("Line Boundary Conditions", " Line Boundary Conditions", "Contains a collection of Revit Line Boundary Conditions", "Params", "Revit Elements") { }
  //}

  //[ComponentVersion(introduced: "1.27")]
  //public class AreaBoundaryConditions : GraphicalElement<Types.AreaBoundaryConditions, ARDB.Structure.BoundaryConditions>
  //{
  //  public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
  //  public override Guid ComponentGuid => new Guid("8FB5CB17-3A19-4FE7-A210-70B6DF88A8A5");

  //  public AreaBoundaryConditions() : base("Area Boundary Conditions", "Area Boundary Conditions", "Contains a collection of Revit Area Boundary Conditions", "Params", "Revit Elements") { }
  //}
}
