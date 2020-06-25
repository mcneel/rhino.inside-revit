using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class InstanceExtension
  {
    public static void GetLocation(this Instance instance, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      var transform = instance.GetTransform();
      origin = transform.Origin;
      basisX = transform.BasisX;
      basisY = transform.BasisY;

      switch (instance.Location)
      {
        case LocationPoint pointLocation:
          origin = pointLocation.Point;
          break;

        case LocationCurve curveLocation:
          var start = curveLocation.Curve.Evaluate(0.0, normalized: true);
          var end = curveLocation.Curve.Evaluate(1.0, normalized: true);
          var axis = end - start;
          var perp = transform.BasisZ.CrossProduct(transform.BasisX);

          origin = start + (axis * 0.5);
          basisX = axis.Normalize();
          basisY = perp.Normalize();
          break;
      }
    }

    public static void SetLocation(this Instance element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY)
    {
      element.GetLocation(out var origin, out var basisX, out var basisY);
      var basisZ = basisX.CrossProduct(basisY);

      var newBasisZ = newBasisX.CrossProduct(newBasisY);
      {
        if (!basisZ.IsParallelTo(newBasisZ))
        {
          var axisDirection = basisZ.CrossProduct(newBasisZ);
          double angle = basisZ.AngleTo(newBasisZ);

          using (var axis = Line.CreateUnbound(origin, axisDirection))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

          element.GetLocation(out origin, out basisX, out basisY);
          basisZ = basisX.CrossProduct(basisY);
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
  }
}
