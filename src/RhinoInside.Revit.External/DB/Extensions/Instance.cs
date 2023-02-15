using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementLocation
  {
    #region Element
    delegate void LocationGetter<T>(T element, out XYZ origin, out XYZ basisX, out XYZ basisY) where T : Element;

    static void SetLocation<T>(this T element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY, LocationGetter<T> GetLocation) where T : Element =>
      SetLocation(element, newOrigin, newBasisX, newBasisY, GetLocation, out var _);

    static void SetLocation<T>(this T element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY, LocationGetter<T> GetLocation, out bool modified)
      where T : Element
    {
      modified = false;
      var pinned = element.Pinned;

      try
      {
        GetLocation(element, out var origin, out var basisX, out var basisY);
        var basisZ = basisX.CrossProduct(basisY);

        var newBasisZ = newBasisX.CrossProduct(newBasisY);
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

        if (!basisX.IsAlmostEqualTo(newBasisX))
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
    #endregion

    #region Instance
    public static void GetLocation(this Instance instance, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      using (var transform = instance.GetTransform())
      {
        // Value Overrides
        switch (instance.Location)
        {
          case LocationPoint pointLocation:
            origin = pointLocation.Point;
            basisX = transform.BasisX.Normalize(0D);
            basisY = transform.BasisY.Normalize(0D);
            return;

          case LocationCurve curveLocation:
            if (curveLocation.Curve.TryGetLocation(out origin, out basisX, out basisY))
            {
              if (instance is FamilyInstance familyInstance)
              {
                basisY = familyInstance.HandOrientation.CrossProduct(familyInstance.FacingOrientation).CrossProduct(basisX);

                if (familyInstance.Mirrored)
                  basisY = -basisY;
              }

              return;
            }
            break;
        }

        // Default values
        origin = transform.Origin;
        basisX = transform.BasisX.Normalize(0D);
        basisY = transform.BasisY.Normalize(0D);
      }
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY)
    {
      ElementLocation.SetLocation(element, newOrigin, newBasisX, newBasisY, GetLocation);
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, double newAngle)
    {
      var modified = false;
      var pinned = element.Pinned;

      try
      {
        element.GetLocation(out var origin, out var basisX, out var basisY);

        // Set Origin
        {
          var translation = newOrigin - origin;
          if (translation.GetLength() > element.Document.Application.VertexTolerance)
          {
            element.Pinned = false;
            modified = true;
            ElementTransformUtils.MoveElement(element.Document, element.Id, translation);
          }
        }

        // Set Rotation
        {
          var normal = basisX.CrossProduct(basisY);
          var right = normal.PerpVector();
          var rotation = newAngle - basisX.AngleOnPlaneTo(right, normal);
          if (rotation > element.Document.Application.AngleTolerance)
          {
            element.Pinned = false;
            modified = true;

            using (var axis = Line.CreateUnbound(newOrigin, normal))
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

    #region ElevationMarker
    public static void GetLocation(this ElevationMarker mark, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      origin = basisX = basisY = default;

      var viewX = mark.Document.GetElement(mark.GetViewId(0)) as View;
      var viewY = mark.Document.GetElement(mark.GetViewId(1)) as View;

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
            viewX = mark.CreateElevation(mark.Document, plan.Id, 0);
            lineX = Line.CreateUnbound(new XYZ(viewX.Origin.X, viewX.Origin.Y, 0.0), viewX.RightDirection);
          }

          if (lineY is null)
          {
            viewY = mark.CreateElevation(mark.Document, plan.Id, 1);
            lineY = Line.CreateUnbound(new XYZ(viewY.Origin.X, viewY.Origin.Y, 0.0), viewY.RightDirection);
          }
        }
      }

      if (lineX.Intersect(lineY, out var result) == SetComparisonResult.Disjoint)
      {
        if (result.Size == 1)
        {
          origin = result.get_Item(0).XYZPoint;
          basisX = lineX.Direction;
          basisY = lineY.Direction;
        }
      }
    }

    public static void SetLocation(this ElevationMarker mark, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY)
    {
      ElementLocation.SetLocation(mark, newOrigin, newBasisX, newBasisY, GetLocation);
    }
    #endregion
  }
}
