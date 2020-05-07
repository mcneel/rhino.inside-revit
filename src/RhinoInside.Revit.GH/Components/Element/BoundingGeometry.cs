using System;
using Grasshopper.Kernel;

using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementBoundingGeometry : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("3396DBC4-0E8F-4402-969A-EF5A0E30E093");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "EBG";

    public ElementBoundingGeometry() : base(
      name: "Element Bounding Geometry",
      nickname: "EBG",
      description: "Bounding geometry of given element",
      category: "Revit",
      subCategory: "Element"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Element",
        nickname: "E",
        description: "Element with complex geometry",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter(
        name: "Bounding Geometry",
        nickname: "BG",
        description: "Element Bounding geometry",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      switch (element)
      {
        case DB.Wall wall:
          // extract the bounding geometry of the wall and set on output
          DA.SetData("Bounding Geometry", wall.ComputeWallBoundingGeometry());
          break;

        // TODO: implement other elements that might have interesting bounding geometries e.g. floors, roofs, ...
      }
    }
  }
}
