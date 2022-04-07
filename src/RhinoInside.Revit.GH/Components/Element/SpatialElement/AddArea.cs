using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElements
{
  [ComponentVersion(introduced: "1.7")]
  public class AddArea : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("2EE360F3-A023-44C0-8922-1890B7629B4A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddArea() : base
    (
      name: "Add Area",
      nickname: "AddArea",
      description: "Given an internal point, it adds an Area to the given Area Plan",
      category: "Revit",
      subCategory: "Spatial"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = "Area Plan",
          NickName = "AP",
          Description = "Area Plan to add a specific area",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
       (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Internal point to define the area",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
        }
      )
    };

    const string _Output_ = "Area";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Area Plan", out ARDB.ViewPlan viewPlan)) return;

      ReconstructElement<ARDB.Area>
      (
        viewPlan.Document, _Output_, (area) =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;

          // Compute
          var xyz = point.Value.ToXYZ();
          area = Reconstruct(area, viewPlan, new ARDB.UV(xyz.X, xyz.Y));

          DA.SetData(_Output_, area);
          return area;
        }
      );
    }

    bool Reuse(ARDB.Area area, ARDB.ViewPlan viewPlan, ARDB.UV point)
    {
      if (area is null) return false;
      if (area.LevelId != viewPlan.GenLevel.Id) return false;
      if (area.Location is ARDB.LocationPoint locationPoint)
      {
        var target = new ARDB.XYZ(point.U, point.V, viewPlan.GenLevel.ProjectElevation);
        var position = locationPoint.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = area.Pinned;
          area.Pinned = false;
          locationPoint.Move(target - position);
          area.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Area Create(ARDB.ViewPlan viewPlan, ARDB.UV point)
    {
      return viewPlan.Document.Create.NewArea(viewPlan, point);
    }

    ARDB.Area Reconstruct(ARDB.Area area, ARDB.ViewPlan viewPlan, ARDB.UV point)
    {
      if (!Reuse(area, viewPlan, point))
        area = Create(viewPlan, point);

      return area;
    }
  }
}
