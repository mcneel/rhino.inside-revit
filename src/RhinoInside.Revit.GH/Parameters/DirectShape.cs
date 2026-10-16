using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;
  using External.UI.Extensions;

  public class DirectShape : GraphicalElement<Types.DirectShape, ARDB.DirectShape>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("602F1664-4E94-4033-996B-36E826759E0E");
    protected override string IconTag => "C";

    public DirectShape() : base
    (
      name: "DirectShape",
      nickname: "DirectShape",
      description: "Contains a collection of Revit DirectShape elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    protected override Types.DirectShape InstantiateT() => new Types.DirectShape();
  }

  public class DirectShapeType : ElementType<Types.DirectShapeType, ARDB.DirectShapeType>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("5DCB481A-F056-4557-838E-4970BB3A487A");

    public DirectShapeType() : base
    (
      name: "DirectShape Type",
      nickname: "DSType",
      description: "Contains a collection of Revit DirectShape types",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    protected override Types.DirectShapeType InstantiateT() => new Types.DirectShapeType();
  }
}
