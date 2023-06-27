using System;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Numerical;
  using Convert.Geometry;
  using Convert.Units;
  using External.DB;
  using External.DB.Extensions;
  using static External.DB.Extensions.BoundingBoxXYZExtension;

  public class ViewFrame : GH_GeometricGoo<ViewportInfo>, IGH_PreviewData
  {
    public ViewFrame() { }
    public ViewFrame(ViewportInfo info) : base(info) { }
    public ViewFrame(ViewportInfo info, string title) : base(info) => Title = title;

    public ViewFrame(ViewFrame other)
    {
      if (other is null) throw new ArgumentNullException(nameof(other));

      ReferenceID = other.ReferenceID;
      if (other.Value is object) Value = new ViewportInfo(other.Value);
      Title = other.Title;
      BoundEnabled = (bool[,]) other.BoundEnabled.Clone();
      Bound = (Interval[]) other.Bound.Clone();
    }

    public override bool IsValid => Value is object;

    public override string TypeName => "View Frame";

    public override string TypeDescription => "View frame";

    public string Title { get; set; } = string.Empty;

    [Flags]
    enum BoundPlane
    {
      None = 0,
      Left = 1 << 0,
      Right = 1 << 1,
      Bottom = 1 << 2,
      Top = 1 << 3,
      Far = 1 << 4,
      Near = 1 << 5,
    }

    BoundPlane PlaneEnabled
    {
      get => (BoundEnabled[AxisX, BoundsMin] ? BoundPlane.Left   : default) |
             (BoundEnabled[AxisX, BoundsMax] ? BoundPlane.Right  : default) |
             (BoundEnabled[AxisY, BoundsMin] ? BoundPlane.Bottom : default) |
             (BoundEnabled[AxisY, BoundsMax] ? BoundPlane.Top    : default) |
             (BoundEnabled[AxisZ, BoundsMin] ? BoundPlane.Far    : default) |
             (BoundEnabled[AxisZ, BoundsMax] ? BoundPlane.Near   : default);

      set
      {
        BoundEnabled[AxisX, BoundsMin] = value.HasFlag(BoundPlane.Left);
        BoundEnabled[AxisX, BoundsMax] = value.HasFlag(BoundPlane.Right);
        BoundEnabled[AxisY, BoundsMin] = value.HasFlag(BoundPlane.Bottom);
        BoundEnabled[AxisY, BoundsMax] = value.HasFlag(BoundPlane.Top);
        BoundEnabled[AxisZ, BoundsMin] = value.HasFlag(BoundPlane.Far);
        BoundEnabled[AxisZ, BoundsMax] = value.HasFlag(BoundPlane.Near);
      }
    }

    public bool[,] BoundEnabled { get; set; } = BoundEnabledNone;
    static bool[,] BoundEnabledNone => new bool[,]
    {
      { false, false },
      { false, false },
      { false, false },
    };
    static bool[,] BoundEnabledPlanar => new bool[,]
    {
      { true, true },
      { true, true },
      { false, false },
    };
    static bool[,] BoundEnabledBox => new bool[,]
    {
      { true, true },
      { true, true },
      { true, true },
    };

    public Interval[] Bound { get; set; } = BoundDefault;
    static Interval[] BoundDefault => new Interval[]
    {
      new Interval(double.NaN, double.NaN),
      new Interval(double.NaN, double.NaN),
      new Interval(double.NaN, double.NaN),
    };

    public override object ScriptVariable() => ToBoundingBoxXYZ();

    public override string ToString()
    {
      if (Value == null)
        return "Null View Frame";

      if (!Value.IsValid)
        return "Invalid View Frame";

      if (!string.IsNullOrWhiteSpace(Title))
        return Title;

      if (Value.IsTwoPointPerspectiveProjection)
        return "Two-point perspective";

      if (Value.IsPerspectiveProjection)
      {
        Value.GetCameraAngles(out var _, out var _, out var h);
        return $"Perspective ({Math.Round(Value.Camera35mmLensLength, 0)}mm. | {Math.Round(h * 2.0 * 180.0 / Math.PI, 0)}°)";
      }

      if (Value.IsParallelProjection)
      {
        switch (Value.CameraDirection.IsParallelTo(Vector3d.ZAxis))
        {
          case -1: return "Top view";
          case +1: return "Bottom view";
        }
        switch (Value.CameraDirection.IsParallelTo(Vector3d.YAxis))
        {
          case -1: return "Back view";
          case +1: return "Front view";
        }
        switch (Value.CameraDirection.IsParallelTo(Vector3d.XAxis))
        {
          case -1: return "Right view";
          case +1: return "Left view";
        }

        return "Parallel";
      }

      return "Unrecogized projection";
    }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case GH_Goo<ViewportInfo> info: source = info.Value; break;
        case IGH_Goo goo: source = goo.ScriptVariable(); break;
      }

      switch (source)
      {
        case Point3d point: source = new Plane(point, Vector3d.XAxis, Vector3d.YAxis);  break;
      }

      switch (source)
      {
        case Vector3d vector:
        {
          var radius = vector.Length;
          var target = vector.Length;
          var near = double.Epsilon;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(Point3d.Origin);
          vport.SetCameraDirection(-vector);
          vport.SetCameraUp(Vector3d.CrossProduct(vector, vector.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance)));
          vport.TargetPoint = Point3d.Origin - vector;

          if (vport.SetFrustum(-radius, +radius, -radius, +radius, near, target))
          {
            BoundEnabled = BoundEnabledNone;
            Bound[AxisX] = new Interval(-radius, +radius);
            Bound[AxisY] = new Interval(-radius, +radius);
            Bound[AxisZ] = new Interval(-target, +target);
            Value = vport;
            return true;
          }
          else return false;
        }

        case Line line:
        {
          var radius = line.Length / 3.0;
          var target = line.Length;
          var near = double.Epsilon;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(line.From);
          vport.SetCameraDirection(line.Direction);
          vport.SetCameraUp(Vector3d.CrossProduct(-line.Direction, -line.Direction.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance)));
          vport.TargetPoint = line.To;

          if (vport.SetFrustum(-radius, +radius, -radius * 3.0 / 4.0, +radius * 3.0 / 4.0, near, target))
          {
            BoundEnabled = BoundEnabledNone;
            Bound[AxisX] = new Interval(-radius, +radius);
            Bound[AxisY] = new Interval(-radius * 3.0 / 4.0, +radius * 3.0 / 4.0);
            Bound[AxisZ] = new Interval(-target, 0.0);
            Value = vport;
            return true;
          }
          else return false;
        }

        case Plane plane:
        {
          var radius = 30 * Revit.ModelUnits;
          var width = radius * 2;
          var height = radius * 2;

          var target = Math.Sqrt(width * width + height * height);
          var near = double.Epsilon;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(plane.Origin);
          vport.SetCameraDirection(-plane.ZAxis);
          vport.SetCameraUp(plane.YAxis);
          vport.TargetPoint = plane.Origin - plane.ZAxis * (target * 0.5);

          if (vport.SetFrustum(-radius, +radius, -radius, +radius, near, target * 0.5))
          {
            BoundEnabled = BoundEnabledNone;
            Bound[AxisX] = new Interval(-radius, +radius);
            Bound[AxisY] = new Interval(-radius, +radius);
            Bound[AxisZ] = new Interval(target * -0.5, target * +0.5);
            Value = vport;
            return true;
          }
          else return false;
        }

        case Box box:
        {
          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(box.Plane.Origin);
          vport.SetCameraDirection(-box.Plane.ZAxis);
          vport.SetCameraUp(box.Plane.YAxis);

          var near = Math.Max(double.Epsilon, -box.Z.T0);
          var far  = Math.Max(near + double.Epsilon, -box.Z.T1);
          if (far == near) far += 0.001;// double.Epsilon;

          if (vport.SetFrustum(box.X.T0, box.X.T1, box.Y.T0, box.Y.T1, near, far))
          {
            vport.SetScreenPortFromFrustum((UnitScale.GetModelScale(RhinoDoc.ActiveDoc) / UnitScale.Inches).Ratio.Quotient);

            BoundEnabled = BoundEnabledBox;
            Bound[AxisX] = box.X;
            Bound[AxisY] = box.Y;
            Bound[AxisZ] = box.Z;

            Value = vport;
            return true;
          }
          else return false;
        }

        case Rectangle3d rect:
        {
          var width = rect.Width;
          var height = rect.Height;

          var target = Math.Sqrt(width * width + height * height);
          //var near = 0.5 * Math.Sqrt(3.0) * rect.Width;
          var near = double.Epsilon;
          var plane = rect.Plane;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(plane.Origin /*+ nearPlane.ZAxis * near*/);
          vport.SetCameraDirection(-plane.ZAxis /** -(near + target)*/);
          vport.SetCameraUp(plane.YAxis);
          vport.TargetPoint = plane.Origin - plane.ZAxis * (target * 0.5);

          if (vport.SetFrustum(rect.X.T0, rect.X.T1, rect.Y.T0, rect.Y.T1, near, near + target))
          {
            BoundEnabled = BoundEnabledPlanar;
            Bound[AxisX] = rect.X;
            Bound[AxisY] = rect.Y;
            Bound[AxisZ] = new Interval(target * -0.5, 0.0);
            Value = vport;
            return true;
          }
          else return false;
        }

        case Circle circle:
        {
          var plane = circle.Plane;
          var radius = circle.Radius;
          var width = radius * 2.0;
          var height = radius * 2.0;

          var target = Math.Sqrt(width * width + height * height);
          var near = double.Epsilon;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(plane.Origin);
          vport.SetCameraDirection(-plane.ZAxis);
          vport.SetCameraUp(plane.YAxis);
          vport.TargetPoint = plane.Origin - plane.ZAxis * (target * 0.5);

          if (vport.SetFrustum(-radius, +radius, -radius, +radius, near, target))
          {
            BoundEnabled = BoundEnabledPlanar;
            Bound[AxisX] = new Interval(-radius, +radius);
            Bound[AxisY] = new Interval(-radius, +radius);
            Bound[AxisZ] = new Interval(target * -0.5, 0.0);
            Value = vport;
            return true;
          }
          else return false;
        }

        case ViewportInfo vport:

          Value = new ViewportInfo(vport);
          if (!Value.IsParallelProjection)
          {
            var near = 0.1 * Revit.ModelUnits;
            var rect = Value.GetFrustumRectangle(near);
            Value.SetFrustum(rect.X.T0, rect.X.T1, rect.Y.T0, rect.Y.T1, near, Value.FrustumFar);
          }

          BoundEnabled = BoundEnabledNone;
          Bound = BoundDefault;
          return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (!IsValid)
        return false;

      if (typeof(ViewportInfo).IsAssignableFrom(typeof(Q)))
      {
        target = (Q) (object) Value;
        return true;
      }
      else if (target is GH_Goo<ViewportInfo> info)
      {
        info.Value = Value is null ? null : new ViewportInfo(Value);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Interval)))
      {
        target = (Q) (object) new GH_Interval(new Interval(-Value.FrustumNear, -Value.FrustumFar));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var intervalU = new Interval(Value.FrustumLeft, Value.FrustumRight);
        var intervalV = new Interval(Value.FrustumBottom, Value.FrustumTop);
        target = (Q) (object) new GH_Interval2D(new UVInterval(intervalU, intervalV));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        target = (Q) (object) new GH_Point(Value.CameraLocation);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        target = (Q) (object) new GH_Vector(Value.CameraDirection);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(Plane)))
      {
        target = (Q) (object) new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        target = (Q) (object) new GH_Transform(Value.GetXform(CoordinateSystem.World, CoordinateSystem.Camera));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Rectangle)))
      {
        var corners = Value.GetNearPlaneCorners();
        var rectangle = new Rectangle3d(Value.FrustumNearPlane, corners[0], corners[3]);
        target = (Q) (object) new GH_Rectangle(rectangle);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        var box = new Box
        (
          new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY),
          new Interval(Value.FrustumLeft, Value.FrustumRight),
          new Interval(Value.FrustumBottom, Value.FrustumTop),
          new Interval(-Value.FrustumFar, -Value.FrustumNear)
        );
        target = (Q) (object) new GH_Box(box);
        return true;
      }

      return false;
    }

    public Point3d Min => new Point3d
    (
      Arithmetic.Min( Value.FrustumLeft,   Bound[AxisX].T0),
      Arithmetic.Min( Value.FrustumBottom, Bound[AxisY].T0),
      Arithmetic.Min(-Value.FrustumFar,    Bound[AxisZ].T0)
    );

    public Point3d Max => new Point3d
    (
      Arithmetic.Max( Value.FrustumRight,  Bound[AxisX].T1),
      Arithmetic.Max( Value.FrustumTop,    Bound[AxisY].T1),
      Arithmetic.Max(-Value.FrustumNear,   Bound[AxisZ].T1)
    );

    internal ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(bool ensurePositiveY = false)
    {
      if (Value is null) return null;

      var vport = Value;

      if (ensurePositiveY && vport.CameraY.Z < 0.0)
      {
        var positiveY = new ViewFrame(this);
        if (positiveY.Transform(Rhino.Geometry.Transform.Rotation(-Math.PI, vport.CameraZ, vport.CameraLocation)) is ViewFrame transformed)
          return transformed.ToBoundingBoxXYZ();

        return null;
      }

      var transform = ARDB.Transform.Identity;
      {
        transform.Origin = vport.CameraLocation.ToXYZ();
        transform.BasisX = vport.CameraX.ToXYZ();
        transform.BasisY = vport.CameraY.ToXYZ();
        transform.BasisZ = vport.CameraZ.ToXYZ();
      }

      var box = new ARDB.BoundingBoxXYZ()
      {
        Transform = transform,
        Min = Min.ToXYZ(),
        Max = Max.ToXYZ(),
      };

      for(int axis = AxisX; axis <= AxisZ; ++axis)
      for(int bound = BoundsMin; bound <= BoundsMax; ++bound)
          box.set_BoundEnabled(bound, axis, BoundEnabled[axis, bound]);

      return box;
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (IsValid)
          return new BoundingBox(Value.GetFramePlaneCorners(Value.TargetDistance(true)));

        return BoundingBox.Empty;
      }
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value?.IsValidCamera is true)
      {
        args.Pipeline.DrawDirectionArrow(Value.CameraLocation, Value.CameraX, System.Drawing.Color.Red);
        args.Pipeline.DrawDirectionArrow(Value.CameraLocation, Value.CameraY, System.Drawing.Color.Green);
        args.Pipeline.DrawDirectionArrow(Value.CameraLocation, Value.CameraZ, System.Drawing.Color.Blue);
      }

      if (Value?.IsValidFrustum is true)
      {
        var targetDistance = Value.TargetDistance(true);
        if (targetDistance == RhinoMath.UnsetValue) return;

        int NoClipPattern = 0x0007FF0;
        var pN = Value.GetNearPlaneCorners();
        var pF = Value.GetFarPlaneCorners();
        var pC   = Value.GetFramePlaneCorners(Value.FrustumNear, Bound[AxisX], Bound[AxisY]);
        var pMin = Value.GetFramePlaneCorners(-Bound[AxisZ].T0,  Bound[AxisX], Bound[AxisY]);
        var pMax = Value.GetFramePlaneCorners(-Bound[AxisZ].T1,  Bound[AxisX], Bound[AxisY]);

        // Direction
        var cameraDirection = new Line(Value.CameraLocation, -Value.CameraZ * targetDistance);
        {
          args.Pipeline.DrawLineNoClip(cameraDirection.From, cameraDirection.To, args.Color, args.Thickness);
          args.Pipeline.DrawArrow(cameraDirection, args.Color, args.Pipeline.DpiScale * 15, 0);
        }

        // Near Plane
        {
          args.Pipeline.DrawDottedLine(pN[0], pN[1], args.Color);
          args.Pipeline.DrawDottedLine(pN[1], pN[3], args.Color);
          args.Pipeline.DrawDottedLine(pN[3], pN[2], args.Color);
          args.Pipeline.DrawDottedLine(pN[2], pN[0], args.Color);
        }

        // Far Plane
        {
          args.Pipeline.DrawDottedLine(pF[0], pF[1], args.Color);
          args.Pipeline.DrawDottedLine(pF[1], pF[3], args.Color);
          args.Pipeline.DrawDottedLine(pF[3], pF[2], args.Color);
          args.Pipeline.DrawDottedLine(pF[2], pF[0], args.Color);
        }

        // Frustum Near - Far
        {
          args.Pipeline.DrawDottedLine(pN[0], pF[0], args.Color);
          args.Pipeline.DrawDottedLine(pN[1], pF[1], args.Color);
          args.Pipeline.DrawDottedLine(pN[2], pF[2], args.Color);
          args.Pipeline.DrawDottedLine(pN[3], pF[3], args.Color);
        }

        // Crop Far Plane
        {
          if (BoundEnabled[AxisZ, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[0], pMin[1], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[0], pMin[1], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[1], pMin[3], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[1], pMin[3], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[3], pMin[2], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[3], pMin[2], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[2], pMin[0], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[2], pMin[0], args.Color, NoClipPattern, args.Thickness);
        }


        // Crop Near Plane
        {
          if (BoundEnabled[AxisZ, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[0], pMax[1], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMax[0], pMax[1], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[1], pMax[3], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMax[1], pMax[3], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[3], pMax[2], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMax[3], pMax[2], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisZ, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[2], pMax[0], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMax[2], pMax[0], args.Color, NoClipPattern, args.Thickness);
        }

        // Crop Far - Near
        {
          args.Pipeline.DrawDottedLine(pC[0], pMax[0], args.Color);
          args.Pipeline.DrawDottedLine(pC[1], pMax[1], args.Color);
          args.Pipeline.DrawDottedLine(pC[2], pMax[2], args.Color);
          args.Pipeline.DrawDottedLine(pC[3], pMax[3], args.Color);

          if (BoundEnabled[AxisX, BoundsMin] || BoundEnabled[AxisY, BoundsMin])
            args.Pipeline.DrawLineNoClip(pMin[0], pMax[0], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[0], pMax[0], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisX, BoundsMin] || BoundEnabled[AxisY, BoundsMin])
            args.Pipeline.DrawLineNoClip(pMin[1], pMax[1], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[1], pMax[1], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisX, BoundsMin] || BoundEnabled[AxisY, BoundsMax])
            args.Pipeline.DrawLineNoClip(pMin[2], pMax[2], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[2], pMax[2], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisX, BoundsMax] || BoundEnabled[AxisY, BoundsMax])
            args.Pipeline.DrawLineNoClip(pMin[3], pMax[3], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pMin[3], pMax[3], args.Color, NoClipPattern, args.Thickness);
        }

        // Crop Box - View Plane
        {
          if (BoundEnabled[AxisX, BoundsMin]) args.Pipeline.DrawLineNoClip(pC[2], pC[0], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pC[2], pC[0], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisX, BoundsMax]) args.Pipeline.DrawLineNoClip(pC[1], pC[3], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pC[1], pC[3], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisY, BoundsMin]) args.Pipeline.DrawLineNoClip(pC[0], pC[1], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pC[0], pC[1], args.Color, NoClipPattern, args.Thickness);

          if (BoundEnabled[AxisY, BoundsMax]) args.Pipeline.DrawLineNoClip(pC[3], pC[2], args.Color, args.Thickness);
          else args.Pipeline.DrawPatternedLine(pC[3], pC[2], args.Color, NoClipPattern, args.Thickness);
        }

        // Crop Box - Far Plane
        if (BoundEnabled[AxisZ, BoundsMin])
        {
          if (BoundEnabled[AxisX, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[2], pMin[0], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMin[2], pMin[0], args.Color);

          if (BoundEnabled[AxisX, BoundsMax]) args.Pipeline.DrawLineNoClip(pMin[1], pMin[3], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMin[1], pMin[3], args.Color);

          if (BoundEnabled[AxisY, BoundsMin]) args.Pipeline.DrawLineNoClip(pMin[0], pMin[1], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMin[0], pMin[1], args.Color);

          if (BoundEnabled[AxisY, BoundsMax]) args.Pipeline.DrawLineNoClip(pMin[3], pMin[2], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMin[3], pMin[2], args.Color);
        }

        // Crop Box - Near Plane
        if (BoundEnabled[AxisZ, BoundsMax])
        {
          if (BoundEnabled[AxisX, BoundsMin]) args.Pipeline.DrawLineNoClip(pMax[2], pMax[0], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMax[2], pMax[0], args.Color);

          if (BoundEnabled[AxisX, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[1], pMax[3], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMax[1], pMax[3], args.Color);

          if (BoundEnabled[AxisY, BoundsMin]) args.Pipeline.DrawLineNoClip(pMax[0], pMax[1], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMax[0], pMax[1], args.Color);

          if (BoundEnabled[AxisY, BoundsMax]) args.Pipeline.DrawLineNoClip(pMax[3], pMax[2], args.Color, args.Thickness);
          else args.Pipeline.DrawDottedLine(pMax[3], pMax[2], args.Color);
        }
      }
    }
    #endregion

    #region IGH_GeometricGoo
    public override Guid ReferenceID { get; set; } = Guid.Empty;

    public override void ClearCaches()
    {
      if (IsReferencedGeometry)
      {
        Title = string.Empty;
        BoundEnabled = BoundEnabledNone;
        Bound = BoundDefault;
      }

      base.ClearCaches();
    }

    public override bool IsGeometryLoaded => m_value is object;

    public override bool LoadGeometry(RhinoDoc doc)
    {
      if (ReferenceID != Guid.Empty)
      {
        if (doc.Views.Find(ReferenceID) is RhinoView rhinoView)
        {
          Value = new ViewportInfo(rhinoView.MainViewport);
          Title = rhinoView.MainViewport.Name;
          return true;
        }
      }

      return false;
    }

    public override IGH_GeometricGoo DuplicateGeometry() => new ViewFrame(this);

    public override BoundingBox Boundingbox
    {
      get
      {
        if (Value == null)
          return BoundingBox.Empty;

        var near = Value.GetNearPlaneCorners();
        var far = Value.GetFarPlaneCorners();

        var boxNear = new BoundingBox(near);
        var boxFar = new BoundingBox(far);
        var boxAll = BoundingBox.Union(boxNear, boxFar);

        boxAll.Union(Value.CameraLocation);

        return boxAll;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (IsValid)
      {
        var bbox = BoundingBox.Empty;

        foreach (var point in Value.GetNearPlaneCorners())
        {
          point.Transform(xform);
          bbox.Union(point);
        }

        foreach (var point in Value.GetFarPlaneCorners())
        {
          point.Transform(xform);
          bbox.Union(point);
        }

        //{
        //  var point = Value.CameraLocation;
        //  point.Transform(xform);
        //  bbox.Union(point);
        //}

        return bbox;
      }

      return BoundingBox.Unset;
    }

    public override IGH_GeometricGoo Transform(Transform xform)
    {
      if (Value.IsValid && Value.TransformCamera(xform))
        return this;

      return default;
    }

    public override IGH_GeometricGoo Morph(SpaceMorph xmorph) => default;
    #endregion

    #region GH_ISerializable
    public override bool Write(GH_IWriter writer)
    {
      writer.SetGuid("RefID", ReferenceID);
      if (!string.IsNullOrWhiteSpace(Title)) writer.SetString("Title", Title);

      if (PlaneEnabled != BoundPlane.None) writer.SetInt32("PlaneEnabled", (int) PlaneEnabled);
      writer.SetInterval1D("Bound", AxisX, new GH_IO.Types.GH_Interval1D(Bound[AxisX].T0, Bound[AxisX].T1));
      writer.SetInterval1D("Bound", AxisY, new GH_IO.Types.GH_Interval1D(Bound[AxisY].T0, Bound[AxisY].T1));
      writer.SetInterval1D("Bound", AxisZ, new GH_IO.Types.GH_Interval1D(Bound[AxisZ].T0, Bound[AxisZ].T1));

      if (ReferenceID == Guid.Empty && m_value is object)
      {
        var data = GH_Convert.CommonObjectToByteArray(m_value);
        if (data is object)
          writer.SetByteArray("ON_Data", data);
      }

      return true;
    }

    public override bool Read(GH_IReader reader)
    {
      ClearCaches();
      ReferenceID = Guid.Empty;
      Value = null;

      var title = string.Empty;
      reader.TryGetString("Title", ref title);
      Title = title;

      var planeEnabled = (int) BoundPlane.None;
      reader.TryGetInt32("PlaneEnabled", ref planeEnabled);
      PlaneEnabled = (BoundPlane) planeEnabled;

      var boundX = reader.GetInterval1D("Bound", AxisX); Bound[AxisX] = new Interval(boundX.a, boundX.b);
      var boundY = reader.GetInterval1D("Bound", AxisY); Bound[AxisY] = new Interval(boundY.a, boundY.b);
      var boundZ = reader.GetInterval1D("Bound", AxisZ); Bound[AxisZ] = new Interval(boundZ.a, boundZ.b);

      ReferenceID = reader.GetGuid("RefID");
      if (reader.ItemExists("ON_Data"))
      {
        var data = reader.GetByteArray("ON_Data");
        m_value = GH_Convert.ByteArrayToCommonObject<ViewportInfo>(data);
      }

      if (ReferenceID != Guid.Empty) LoadGeometry();

      return true;
    }
    #endregion
  }
}
