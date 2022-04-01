using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Components
{
  public class BrepIsSolid : GH_Component
  {
    public override Guid ComponentGuid => new Guid("ACF07D2E-7204-430D-8352-13AF35E08365");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public BrepIsSolid()
    : base("Is Solid", "Is Solid", "Test whether a Brep is solid, and it's orientation", "Surface", "Analysis")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", "Brep to check for its solid orientation", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("Solid", "S", "Wether or not brep is solid", GH_ParamAccess.item);
      manager.AddIntegerParameter("Orientation", "O", "Open=0, OutWard=1, Inward=-1", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Brep brep = null;
      if (!DA.GetData(0, ref brep))
        return;

      var orientation = brep?.SolidOrientation ?? BrepSolidOrientation.Unknown;
      if (orientation == BrepSolidOrientation.Unknown)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to determine if brep is solid or not");
        return;
      }

      var _Solid_ = Params.IndexOfOutputParam("Solid");
      if (_Solid_ >= 0)
        DA.SetData(_Solid_, orientation != 0);

      var _Orientation_ = Params.IndexOfOutputParam("Orientation");
      if (_Orientation_ >= 0)
        DA.SetData(_Orientation_, orientation);
     }
  }

  public class BrepSetOrientation : GH_Component
  {
    public override Guid ComponentGuid => new Guid("9FB0E42C-BF47-418E-8A69-21D1927C66C0");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public BrepSetOrientation()
    : base("Set Orientation", "Set Orientation", "Sets a Brep solid orientation", "Surface", "Util")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", "Brep to set its solid orientation", GH_ParamAccess.item);
      var param = manager[manager.AddIntegerParameter("Orientation", "O", "Outward=1, Inward=-1", GH_ParamAccess.item)];

      if (param is Param_Integer orientation)
      {
        orientation.AddNamedValue("Outward", (int) BrepSolidOrientation.Outward);
        orientation.AddNamedValue("Inward", (int) BrepSolidOrientation.Inward);
      }
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", string.Empty, GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Brep brep = null;
      if (!DA.GetData(0, ref brep))
        return;

      var orientationValue = (int) BrepSolidOrientation.Unknown;
      if (!DA.GetData(1, ref orientationValue))
        return;

      var orientation = (BrepSolidOrientation) orientationValue;
      if (orientation == BrepSolidOrientation.Unknown)
        return;

      if (brep.SolidOrientation != BrepSolidOrientation.None)
      {
        if (orientation != brep.SolidOrientation)
          brep.Flip();
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Brep is not solid");

      DA.SetData(0, brep);
    }
  }

  [ComponentVersion(introduced: "1.7")]
  public class BoundingFrustum : Component
  {
    public override Guid ComponentGuid => new Guid("B687E1B0-E817-4149-9EC9-FE122CA369DE");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public BoundingFrustum() :
      base("Bounding Frustum", "BFrustum", "Solve transformed geometry bounding frustum.", "Surface", "Primitive")
    {
      ValuesChanged();
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddGeometryParameter("Content", "C", "Geometry to contain", GH_ParamAccess.list);

      if (manager[manager.AddTransformParameter("Transform", "T", "BoundingBox transform", GH_ParamAccess.item)] is Param_Transform transform)
        transform.SetPersistentData(new GH_Transform(Transform.Identity));

      manager.HideParameter(1);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("Frustum", "F", "Bounding frustum in world coordinates", GH_ParamAccess.list);
      manager.AddBoxParameter("Box", "B", "Bounding box in transform coordinates", GH_ParamAccess.list);
      manager.HideParameter(1);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var geometry = new List<IGH_GeometricGoo>();
      if (!DA.GetDataList(0, geometry)) return;
      if (geometry.Count == 0) return;

      var transform = Transform.Identity;
      if (!DA.GetData(1, ref transform)) return;
      if (!transform.IsValid) return;

      var boxes = new List<BoundingBox?>();
      if (GetValue("UnionBox", false))
      {
        var box = BoundingBox.Empty;
        foreach (var g in geometry)
        {
          if (g is null) continue;

          var bbox = g.GetBoundingBox(transform);
          if (!bbox.IsValid) continue;

          box.Union(bbox);
        }

        boxes.Add(box.IsValid ? box : default(BoundingBox?));
      }
      else
      {
        foreach (var g in geometry)
          boxes.Add(g?.GetBoundingBox(transform));
      }

      var hasInverse = transform.TryGetInverse(out var inverse);

      var frustums = new List<IGH_GeometricGoo>();
      foreach (var bbox in boxes)
      {
        if (bbox.HasValue && bbox.Value.IsValid && hasInverse)
        {
          var absoluteTolerance = DocumentTolerance();
          var box = new Box(bbox.Value);
          var size = new Vector3d(box.X.Length, box.Y.Length, box.Z.Length);

          if (size.EpsilonEquals(Vector3d.Zero, absoluteTolerance))
          {
            var point = new Point3d(box.X.Mid, box.Y.Mid, box.Z.Mid);
            point.Transform(inverse);
            frustums.Add(new GH_Point(point));
          }
          else if (size.EpsilonEquals(Vector3d.XAxis * size.X, absoluteTolerance))
          {
            var line = new Line(new Point3d(box.X.T0, box.Y.Mid, box.Z.Mid), new Point3d(box.X.T1, box.Y.Mid, box.Z.Mid));
            var curve = new LineCurve(line, box.X.T0, box.X.T1);
            curve.Transform(inverse);
            frustums.Add(new GH_Curve(curve));
          }
          else if (size.EpsilonEquals(Vector3d.YAxis * size.Y, absoluteTolerance))
          {
            var line = new Line(new Point3d(box.X.Mid, box.Y.T0, box.Z.Mid), new Point3d(box.X.Mid, box.Y.T1, box.Z.Mid));
            var curve = new LineCurve(line, box.Y.T0, box.Y.T1);
            curve.Transform(inverse);
            frustums.Add(new GH_Curve(curve));
          }
          else if (size.EpsilonEquals(Vector3d.ZAxis * size.Z, absoluteTolerance))
          {
            var line = new Line(new Point3d(box.X.Mid, box.Y.Mid, box.Z.T0), new Point3d(box.X.Mid, box.Y.Mid, box.Z.T1));
            var curve = new LineCurve(line, box.Z.T0, box.Z.T1);
            curve.Transform(inverse);
            frustums.Add(new GH_Curve(curve));
          }
          else
          {
            var breps = new List<Brep>();

            if (size.Z > absoluteTolerance && size.Y > absoluteTolerance)
            {
              var leftPlane = new Plane(box.Plane.Origin, box.Plane.ZAxis, box.Plane.YAxis);
              leftPlane.Translate(box.Plane.XAxis * box.X.T0);
              var left = Brep.CreateFromSurface(new PlaneSurface(leftPlane, box.Z, box.Y));
              left.Flip();
              breps.Add(left);

              var rightPlane = new Plane(box.Plane.Origin, box.Plane.ZAxis, box.Plane.YAxis);
              rightPlane.Translate(box.Plane.XAxis * box.X.T1);
              var right = Brep.CreateFromSurface(new PlaneSurface(rightPlane, box.Z, box.Y));
              breps.Add(right);
            }

            if (size.X > absoluteTolerance && size.Z > absoluteTolerance)
            {
              var bottomPlane = new Plane(box.Plane.Origin, box.Plane.XAxis, box.Plane.ZAxis);
              bottomPlane.Translate(box.Plane.YAxis * box.Y.T0);
              var bottom = Brep.CreateFromSurface(new PlaneSurface(bottomPlane, box.X, box.Z));
              bottom.Flip();
              breps.Add(bottom);

              var topPlane = new Plane(box.Plane.Origin, box.Plane.XAxis, box.Plane.ZAxis);
              topPlane.Translate(box.Plane.YAxis * box.Y.T1);
              var top = Brep.CreateFromSurface(new PlaneSurface(topPlane, box.X, box.Z));
              breps.Add(top);
            }

            if (size.X > absoluteTolerance && size.Y > absoluteTolerance)
            {
              var farPlane = box.Plane;
              farPlane.Translate(farPlane.Normal * box.Z.T0);
              var far = Brep.CreateFromSurface(new PlaneSurface(farPlane, box.X, box.Y));
              far.Flip();
              breps.Add(far);

              var nearPlane = box.Plane;
              nearPlane.Translate(nearPlane.Normal * box.Z.T1);
              var near = Brep.CreateFromSurface(new PlaneSurface(nearPlane, box.X, box.Y));
              breps.Add(near);
            }

            var brep = default(Brep);
            if (breps.Count > 2)
            {
              var joined = Brep.JoinBreps(breps, absoluteTolerance);
              switch (joined.Length)
              {
                case 1: brep = joined[0]; break;
                default: brep = Brep.MergeBreps(joined, Rhino.RhinoMath.UnsetValue); break;
              }
            }
            else brep = Brep.MergeBreps(breps, Rhino.RhinoMath.UnsetValue);

            if (brep is object)
            {
              brep.Transform(inverse);
              frustums.Add(new GH_Brep(brep));
            }
            else frustums.Add(null);
          }
        }
        else frustums.Add(null);
      }

      if (GetValue("UnionBox", false))
      {
        DA.SetData(0, frustums.FirstOr(null));
        DA.SetData(1, boxes.FirstOr(null));
      }
      else
      {
        DA.SetDataList(0, frustums);
        DA.SetDataList(1, boxes);
      }
    }

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var item = Menu_AppendItem(menu, "Union Box", Menu_UnionBoxClicked, true, GetValue("UnionBox", false));
      item.ToolTipText = "When checked, a single box for all items in each list is returned";
    }

    void Menu_UnionBoxClicked(object sender, EventArgs args)
    {
      if (GetValue("UnionBox", false))
        RecordUndoEvent("Union Box");
      else
        RecordUndoEvent("Per Object Box");

      SetValue("UnionBox", !GetValue("UnionBox", false));
      ExpireSolution(true);
    }

    protected override void ValuesChanged()
    {
      base.ValuesChanged();

      if (GetValue("UnionBox", false))
      {
        Message = "Union Box";
        Params.Output[0].Access = GH_ParamAccess.item;
        Params.Output[1].Access = GH_ParamAccess.item;
      }
      else
      {
        Message = "Per Object";
        Params.Output[0].Access = GH_ParamAccess.list;
        Params.Output[1].Access = GH_ParamAccess.list;
      }
    }
    #endregion
  }
}
