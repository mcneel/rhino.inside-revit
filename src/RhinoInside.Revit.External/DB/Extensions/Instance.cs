using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class InstanceExtension
  {
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
      element.GetLocation(out var origin, out var basisX, out var basisY);
      var basisZ = basisX.CrossProduct(basisY);

      var newBasisZ = newBasisX.CrossProduct(newBasisY);
      {
        if (!basisZ.IsCodirectionalTo(newBasisZ))
        {
          var axisDirection = basisZ.CrossProduct(newBasisZ);
          if (axisDirection.IsZeroLength()) axisDirection = basisY;
          double angle = basisZ.AngleTo(newBasisZ);

          using (var axis = Line.CreateUnbound(origin, axisDirection))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

          element.GetLocation(out origin, out basisX, out basisY);

        }

        if (!basisX.IsAlmostEqualTo(newBasisX))
        {
          double angle = basisX.AngleOnPlaneTo(newBasisX, newBasisZ);
          using (var axis = Line.CreateUnbound(origin, newBasisZ))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
        }

        {
          var trans = newOrigin - origin;
          if (!trans.IsZeroLength())
            ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
        }
      }
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
  }
}
