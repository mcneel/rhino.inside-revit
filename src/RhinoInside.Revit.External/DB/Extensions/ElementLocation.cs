using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementLocation
  {
    #region Element
    internal delegate void LocationGetter<T>(T element, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY) where T : Element;

    internal static void SetLocation<T>(this T element, XYZ newOrigin, UnitXYZ newBasisX, UnitXYZ newBasisY, LocationGetter<T> GetLocation, out bool modified)
      where T : Element
    {
      if (!UnitXYZ.Orthonormal(newBasisX, newBasisY, out var newBasisZ))
        throw new System.ArgumentException("Location basis is not Orthonormal");

      modified = false;
      var pinned = element.Pinned;

      try
      {
        GetLocation(element, out var origin, out var basisX, out var basisY);
        UnitXYZ.Orthonormal(basisX, basisY, out var basisZ);

        if (!basisZ.IsCodirectionalTo(newBasisZ))
        {
          var axisDirection = basisZ.CrossProduct(newBasisZ);
          if (axisDirection.IsZeroLength()) axisDirection = basisY;

          element.Pinned = false;
          using (var axis = Line.CreateUnbound(origin, axisDirection))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, basisZ.AngleTo(newBasisZ));
          modified = true;

          GetLocation(element, out origin, out basisX, out basisY);
        }

        if (!basisX.AlmostEquals(newBasisX))
        {
          element.Pinned = false;
          using (var axis = Line.CreateUnbound(origin, newBasisZ))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, basisX.AngleOnPlaneTo(newBasisX, newBasisZ));
          modified = true;

          GetLocation(element, out origin, out basisX, out basisY);
        }

        {
          var trans = newOrigin - origin;
          if (!trans.IsZeroLength())
          {
            element.Pinned = false;
            ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
            modified = true;
          }
        }
      }
      finally
      {
        if (element.Pinned != pinned)
          element.Pinned = pinned;
      }
    }

    internal static void SetLocation<T>(this T element, XYZ newOrigin, UnitXYZ newBasisX, LocationGetter<T> GetLocation, out bool modified)
      where T : Element
    {
      modified = false;
      var pinned = element.Pinned;

      try
      {
        GetLocation(element, out var origin, out var basisX, out var basisY);
        UnitXYZ.Orthonormal(basisX, basisY, out var basisZ);

        newBasisX = (new PlaneEquation(origin, basisZ).Project(origin + newBasisX) - origin).ToUnitXYZ();
        if (newBasisX && !basisX.AlmostEquals(newBasisX))
        {
          element.Pinned = false;
          using (var axis = Line.CreateUnbound(origin, basisZ))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, basisX.AngleOnPlaneTo(newBasisX, basisZ));
          modified = true;

          GetLocation(element, out origin, out basisX, out basisY);
        }

        {
          var trans = newOrigin - origin;
          if (!trans.IsZeroLength())
          {
            element.Pinned = false;
            ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
            modified = true;
          }
        }
      }
      finally
      {
        if (element.Pinned != pinned)
          element.Pinned = pinned;
      }
    }

    internal static void SetLocation<T>(this T element, XYZ newOrigin, double newAngle, LocationGetter<T> GetLocation, out bool modified) where T : Element
    {
      modified = false;
      var pinned = element.Pinned;

      try
      {
        GetLocation(element, out var origin, out var basisX, out var basisY);

        // Set Origin
        {
          var translation = newOrigin - origin;
          if (translation.IsZeroLength())
          {
            element.Pinned = false;
            modified = true;
            ElementTransformUtils.MoveElement(element.Document, element.Id, translation);
          }
        }

        // Set Rotation
        if (UnitXYZ.Orthonormal(basisX, basisY, out var basisZ))
        {
          var right = basisZ.Right();
          var rotation = newAngle - basisX.AngleOnPlaneTo(right, basisZ);
          if (rotation > element.Document.Application.AngleTolerance)
          {
            element.Pinned = false;
            modified = true;

            using (var axis = Line.CreateUnbound(newOrigin, basisZ))
              ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotation);
          }
        }
      }
      finally
      {
        if (modified)
          element.Pinned = pinned;
      }
    }
    #endregion

    #region SketchPlane
    public static void GetLocation(this SketchPlane sketchPlane, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY)
    {
      using (var plane = sketchPlane.GetPlane())
      {
        origin = plane.Origin;
        basisX = (UnitXYZ) plane.XVec;
        basisY = (UnitXYZ) plane.YVec;
      }
    }

    public static void SetLocation(this SketchPlane sketchPlane, XYZ newOrigin, UnitXYZ newBasisX, UnitXYZ newBasisY)
    {
      ElementLocation.SetLocation(sketchPlane, newOrigin, newBasisX, newBasisY, GetLocation, out var _);
    }
    #endregion

    #region ReferencePlane
    public static void GetLocation(this ReferencePlane referencePlane, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY)
    {
      using (var plane = referencePlane.GetPlane())
      {
        origin = plane.Origin;
        basisX = (UnitXYZ) plane.XVec;
        basisY = (UnitXYZ) plane.YVec;
      }
    }

    public static void SetLocation(this ReferencePlane referencePlane, XYZ newOrigin, UnitXYZ newBasisX, UnitXYZ newBasisY)
    {
      ElementLocation.SetLocation(referencePlane, newOrigin, newBasisX, newBasisY, GetLocation, out var _);
    }
    #endregion

    #region Instance
    public static void GetLocation(this Instance instance, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY)
    {
      using (var transform = instance.GetTransform())
      {
        // Value Overrides
        switch (instance.Location)
        {
          case LocationPoint pointLocation:
            origin = pointLocation.Point;
            basisX = transform.BasisX.ToUnitXYZ();
            basisY = transform.BasisY.ToUnitXYZ();
            return;

          case LocationCurve curveLocation:
            if (curveLocation.Curve.TryGetLocation(out origin, out basisX, out basisY))
              return;

            break;
        }

        // Default values
        origin = transform.Origin;
        basisX = transform.BasisX.ToUnitXYZ();
        basisY = transform.BasisY.ToUnitXYZ();
      }
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, UnitXYZ newBasisX, UnitXYZ newBasisY)
    {
      ElementLocation.SetLocation(element, newOrigin, newBasisX, newBasisY, GetLocation, out var _);
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, UnitXYZ newBasisX)
    {
      ElementLocation.SetLocation(element, newOrigin, newBasisX, GetLocation, out var _);
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, double newAngle)
    {
      ElementLocation.SetLocation(element, newOrigin, newAngle, GetLocation, out var _);
    }
    #endregion

    #region ElevationMarker
    public static void GetLocation(this ElevationMarker mark, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY)
    {
      origin = basisX = basisY = default;

      var viewX = mark.Document.GetElement(mark.GetViewId(1)) as View;
      var viewY = mark.Document.GetElement(mark.GetViewId(0)) as View;

      var lineX = viewX is object ? Line.CreateUnbound(new XYZ(viewX.Origin.X, viewX.Origin.Y, 0.0), viewX.RightDirection) : default;
      var lineY = viewY is object ? Line.CreateUnbound(new XYZ(viewY.Origin.X, viewY.Origin.Y, 0.0), viewY.RightDirection) : default;

      if (lineX is null || lineY is null)
      {
        using (mark.Document.RollBackScope())
        using (mark.Document.RollBackScope()) // We need a SubTransaction here to avoid changes on the document.
        {
          var level = Level.Create(mark.Document, 0.0);
          var plan = ViewPlan.Create(mark.Document, mark.Document.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeFloorPlan), level.Id);

          if (lineX is null)
          {
            viewX = mark.CreateElevation(mark.Document, plan.Id, 1);
            lineX = Line.CreateUnbound(new XYZ(viewX.Origin.X, viewX.Origin.Y, 0.0), viewX.RightDirection);
          }

          if (lineY is null)
          {
            viewY = mark.CreateElevation(mark.Document, plan.Id, 0);
            lineY = Line.CreateUnbound(new XYZ(viewY.Origin.X, viewY.Origin.Y, 0.0), viewY.RightDirection);
          }
        }
      }

      if (lineX.Intersect(lineY, out var result) == SetComparisonResult.Overlap)
      {
        if (result.Size == 1)
        {
          origin = result.get_Item(0).XYZPoint;
          basisX = (UnitXYZ) lineX.Direction;
          basisY = (UnitXYZ) lineY.Direction;
        }
      }
    }

    public static void SetLocation(this ElevationMarker mark, XYZ newOrigin, UnitXYZ newBasisX, UnitXYZ newBasisY)
    {
      ElementLocation.SetLocation(mark, newOrigin, newBasisX, newBasisY, GetLocation, out var _);
    }
    #endregion
  }
}
