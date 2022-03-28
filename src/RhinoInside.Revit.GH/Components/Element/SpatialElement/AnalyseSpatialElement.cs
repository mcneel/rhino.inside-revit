using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.SpatialElement
{
  public class AnalyzeSpatialElement : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("43E7B783-6DA8-483D-9DB4-6FD72F1F0B0F");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ASP";

    public AnalyzeSpatialElement() : base
    (
      name: "Analyze Spatial Element",
      nickname: "A-SE",
      description: "Analyze given spatial element",
      category: "Revit",
      subCategory: "Room & Area"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = "Spatial Element",
          NickName = "SE",
          Description = "Spatial element to analyze.",
          Access = GH_ParamAccess.item
        }
      )
    };
      
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Height",
          NickName = "H",
          Description = "Height of the given spatial element",
        }
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Area",
          NickName = "A",
          Description = "Area of the given spatial element",
        }
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Perimeter",
          NickName = "P",
          Description = "Perimeter of the given spatial element",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level of the given spatial element",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.SpatialElement spatialElement = default;
      if (!DA.GetData("Spatial Element", ref spatialElement))
        return;

      DA.SetData("Height", spatialElement?.get_Parameter(ARDB.BuiltInParameter.ROOM_HEIGHT).AsGoo());
      DA.SetData("Area", spatialElement?.get_Parameter(ARDB.BuiltInParameter.ROOM_AREA).AsGoo());
      DA.SetData("Perimeter", spatialElement?.get_Parameter(ARDB.BuiltInParameter.ROOM_PERIMETER).AsGoo());
      DA.SetData("Level", spatialElement?.Level);
    }
  }
}
