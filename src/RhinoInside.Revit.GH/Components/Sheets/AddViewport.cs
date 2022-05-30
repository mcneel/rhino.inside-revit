using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  [ComponentVersion(introduced: "1.8")]
  public class AddViewport : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("493035D3-4779-4699-B577-8BC73AFDB78A");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AddViewport() : base
    (
      name: "Add Viewport",
      nickname: "Viewport",
      description: "Given a point and a view, it adds a viewport on a sheet",
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
          Description = "Sheet where to place the new viewport"
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to place on the sheet"
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Viewport center point on sheet",
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
          Name = _Viewport_,
          NickName = _Viewport_.Substring(0, 1),
          Description = $"Output {_Viewport_}",
        }
      )
    };

    protected const string _Viewport_ = "Viewport";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out ARDB.ViewSheet sheet)) return;

      ReconstructElement<ARDB.Viewport>
      (
        sheet.Document, _Viewport_, viewport =>
        {
          // Input
          if (!Params.GetData(DA, "View", out ARDB.View view)) return null;
          if (!Params.TryGetData(DA, "Point", out Point3d? point)) return null;

          if (point is null)
          {
            using (var outline = sheet.Outline)
            {
              var min = outline.Min;
              var max = outline.Max;

              point = new Point3d
              (
                GeometryDecoder.ToModelLength((min.U + max.U) * 0.5),
                GeometryDecoder.ToModelLength((min.V + max.V) * 0.5),
                0.0
              );
            }
          }
          else point = new Point3d
          (
            point.Value.X,
            point.Value.Y,
            0.0
          );

          // Compute
          viewport = Reconstruct(viewport, sheet, view, point.Value.ToXYZ());

          DA.SetData(_Viewport_, viewport);
          return viewport;
        }
      );
    }

    bool Reuse(ARDB.Viewport viewport, ARDB.ViewSheet sheet, ARDB.View view, ARDB.XYZ point)
    {
      if (viewport is null) return false;
      if (viewport.SheetId != sheet.Id) return false;
      if (viewport.ViewId != view.Id) return false;

      if (!viewport.GetBoxCenter().AlmostEquals(point, viewport.Document.Application.VertexTolerance))
        viewport.SetBoxCenter(point);

      return true;
    }

    ARDB.Viewport Create(ARDB.ViewSheet sheet, ARDB.View view, ARDB.XYZ point)
    {
      return ARDB.Viewport.Create(sheet.Document, sheet.Id, view.Id, point);
    }

    ARDB.Viewport Reconstruct(ARDB.Viewport viewport, ARDB.ViewSheet sheet, ARDB.View view, ARDB.XYZ point)
    {
      if (!Reuse(viewport, sheet, view, point))
        viewport = Create(sheet, view, point);

      return viewport;
    }
  }
}
