using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  [ComponentVersion(introduced: "1.16")]
  public class AddScheduleSheetInstance : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("1CAAFC26-D456-4EA2-84A1-FD123B934B5F");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AddScheduleSheetInstance() : base
    (
      name: "Add Schedule Graphics",
      nickname: "Schedule Graphics",
      description: "Given a point and a view, it adds a schedule on a sheet",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = "Sheet",
          NickName = "S",
          Description = "Sheet where to place the new schedule"
        }
      ),
      new ParamDefinition
      (
        new Parameters.ViewSchedule()
        {
          Name = "Schedule",
          NickName = "SC",
          Description = "Schedule view to place on the sheet"
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Schedule center point on sheet",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Graphics_,
          NickName = _Graphics_.Substring(0, 1),
          Description = $"Output Schedule {_Graphics_}",
        }
      )
    };

    protected const string _Graphics_ = "Graphics";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out Types.ViewSheet sheet)) return;

      ReconstructElement<ARDB.ScheduleSheetInstance>
      (
        sheet.Document, _Graphics_, viewport =>
        {
          // Input
          if (!Params.GetData(DA, "Schedule", out ARDB.ViewSchedule view)) return null;
          if (!Params.TryGetData(DA, "Point", out Point3d? point)) return null;

          if (view.IsTitleblockRevisionSchedule)
            throw new Exceptions.RuntimeArgumentException("Sheet", $"{view.GetElementNomen()} is not a Schedule that can be added to a sheet.");

          if (point is null)
          {
            if (viewport is object) point = viewport.GetBoundingBoxXYZ().GetCenter().ToPoint3d();
            else
            {
              var outline = sheet.GetOutline(Rhino.DocObjects.ActiveSpace.ModelSpace);
              point = new Point3d(outline.U.Mid, outline.V.Mid, 0.0);
            }
          }
          else point = new Point3d(point.Value.X, point.Value.Y, 0.0);

          // Compute
          viewport = Reconstruct(viewport, sheet.Value, view, point.Value.ToXYZ());

          DA.SetData(_Graphics_, viewport);
          return viewport;
        }
      );
    }

    bool Reuse(ARDB.ScheduleSheetInstance viewport, ARDB.ViewSheet sheet, ARDB.View view)
    {
      if (viewport is null) return false;
      if (viewport.OwnerViewId != sheet.Id) return false;
      if (viewport.ScheduleId != view.Id) return false;

      return true;
    }

    ARDB.ScheduleSheetInstance Create(ARDB.ViewSheet sheet, ARDB.View view)
    {
      return ARDB.ScheduleSheetInstance.Create(sheet.Document, sheet.Id, view.Id, XYZExtension.Zero);
    }

    ARDB.ScheduleSheetInstance Reconstruct(ARDB.ScheduleSheetInstance viewport, ARDB.ViewSheet sheet, ARDB.View view, ARDB.XYZ point)
    {
      if (!Reuse(viewport, sheet, view))
        viewport = Create(sheet, view);

      var center = viewport.GetBoundingBoxXYZ().GetCenter();
      if (!center.AlmostEqualPoints(point))
      {
        var pinned = viewport.Pinned;
        try
        {
          viewport.Pinned = false;
          viewport.Location.Move(point - center);
        }
        finally { viewport.Pinned = pinned; }
      }

      return viewport;
    }
  }
}
