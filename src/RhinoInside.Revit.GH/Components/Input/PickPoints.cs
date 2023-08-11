using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.UI.Selection;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Input
{
  [ComponentVersion(introduced: "1.17")]
  public class PickPoints : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("7D45EC0D-F531-478E-B2A0-657678C0C6FD");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public PickPoints() : base
    (
      name: "Pick Points",
      nickname: "P-Pick",
      description: string.Empty,
      category: "Revit",
      subCategory: "Input"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Plane were the points should be picked",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Plane were the points where picked",
          Hidden = true,
        }
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Description = "Picked points",
          Hidden = true,
          Access = GH_ParamAccess.list
        }
      ),
    };

    readonly GH_Structure<GH_Plane> Planes = new GH_Structure<GH_Plane>();
    readonly GH_Structure<GH_Point> Points = new GH_Structure<GH_Point>();

    readonly SortedList<ARDB.Document, ARDB.TransactionGroup> TransactionGroups = new SortedList<ARDB.Document, ARDB.TransactionGroup>();

    private void StartTransactionGroup(ARDB.Document document)
    {
      if (!TransactionGroups.ContainsKey(document))
      {
        var group = new ARDB.TransactionGroup(document, Name);
        group.Start();

        TransactionGroups.Add(document, group);
      }
    }

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      TransactionGroups.Clear();

      if ((Attributes as ButtonAttributes).Pressed)
      {
        Planes.Clear();
        Points.Clear();
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var _Plane_ = Params.IndexOfOutputParam("Plane");
      var _Points_ = Params.IndexOfOutputParam("Points");

      var pl_path = DA.ParameterTargetPath(_Plane_);
      var pt_path = DA.ParameterTargetPath(_Points_);

      if ((Attributes as ButtonAttributes).Pressed)
      {
        if (Revit.ActiveUIDocument is ARUI.UIDocument uiDocument)
        {
          if (uiDocument.ActiveGraphicalView.SketchPlane is ARDB.SketchPlane sketchPlane)
          {
            StartTransactionGroup(uiDocument.Document);

            using (var scope = uiDocument.Document.CommitScope())
            {
              bool setSketchGrid = Params.GetData(DA, "Plane", out Plane? plane);
              if (setSketchGrid)
              {
                uiDocument.ActiveGraphicalView.SketchPlane = ARDB.SketchPlane.Create(uiDocument.Document, plane.Value.ToPlane());
                uiDocument.ActiveGraphicalView.ShowActiveWorkPlane();
              }

              // Orient sketch grid
              if
              (
                setSketchGrid &&
                uiDocument.ActiveGraphicalView.TryGetSketchGridSurface(out var name, out var surface, out var _, out var _) &&
                surface is ARDB.Plane surfacePlane &&
                uiDocument.Document.GetElement(uiDocument.ActiveGraphicalView.GetSketchGridId()) is ARDB.Element sketchGrid
              )
              {
                ElementLocation.SetLocation
                (
                  sketchGrid,
                  plane.Value.Origin.ToXYZ(),
                  (UnitXYZ) plane.Value.XAxis.ToXYZ(),
                  (UnitXYZ) plane.Value.YAxis.ToXYZ(),
                  x => (surfacePlane.Origin, (UnitXYZ) surfacePlane.XVec, (UnitXYZ) surfacePlane.YVec),
                  out var _
                );
              }

              scope.Commit();
            }

            Planes.Append
            (
              new GH_Plane(uiDocument.ActiveGraphicalView.SketchPlane.GetPlane().ToPlane()),
              pl_path
            );

            if (uiDocument.PickPoints(out var points) == ARUI.Result.Succeeded)
            {
              Points.AppendRange
              (
                points.Select(x => new GH_Point(x.ToPoint3d())),
                pt_path
              );
            }
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no active Revit document");
      }

      Params.TrySetData
      (
        DA, "Plane",
        () =>
        {
          var index = DA.ParameterTargetIndex(_Plane_);
          var list = Planes.get_Branch(pl_path) ?? Array.Empty<GH_Plane>();
          return index < list.Count ? list[index] : default;
        }
      );

      Params.TrySetDataList
      (
        DA, "Points",
        () =>
        (Points.get_Branch(pt_path)?.OfType<GH_Point>() ?? Array.Empty<GH_Point>())
      );
    }

    protected override void AfterSolveInstance()
    {
      foreach (var group in TransactionGroups)
        group.Value.RollBack();

      base.AfterSolveInstance();
    }

    #region Display
    public override BoundingBox ClippingBox => base.ClippingBox;
    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      if (Params.Output<Param_Plane>("Planes") is Param_Plane planes)
        planes.DrawViewportWires(args);

      if (Params.Output<Param_Point>("Points") is Param_Point points)
      {
        if (Attributes.Selected)
        {
          var xform = args.Display.Viewport.GetTransform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Screen);
          var data = points.VolatileData;
          foreach (var path in data.Paths)
          {
            var pts = new List<Point3d>();
            var branch = data.get_Branch(path);
            for (int i = 0; i < branch.Count; i++)
            {
              if (branch[i] is GH_Point point)
              {
                pts.Add(point.Value);
                var pt = xform * point.Value;
                args.Display.Draw2dText($"{path} ({i})", args.WireColour_Selected, new Point2d(pt.X, pt.Y - 24), true, 24);
              }
            }

            args.Display.DrawPatternedPolyline(pts, args.WireColour_Selected, 0x00003333, args.DefaultCurveThickness, false);
          }
        }

        points.DrawViewportWires(args);
      }
    }
    #endregion

    #region UI
    private class ButtonAttributes : ExpireButtonAttributes
    {
      public ButtonAttributes(ZuiComponent owner) : base(owner) { }

      protected override string DisplayText => Owner.Name;

      protected override bool Visible => true;
    }

    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader)) return false;

      Planes.Clear();
      if (reader.FindChunk(nameof(Views)) is GH_IReader planes)
        Planes.Read(planes);

      Points.Clear();
      if (reader.FindChunk(nameof(Points)) is GH_IReader points)
        Points.Read(points);

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer)) return false;

      if (Planes.Branches.Count > 0)
        Planes.Write(writer.CreateChunk(nameof(Planes)));

      if (Points.Branches.Count > 0)
        Points.Write(writer.CreateChunk(nameof(Points)));

      return true;
    }
    #endregion
  }
}
