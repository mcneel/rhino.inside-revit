using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class TransformExtension
  {
    public static void Deconstruct
    (
      this Transform transform,
      out XYZ origin, out XYZ basisX, out XYZ basisY, out XYZ basisZ
    )
    {
      origin = transform.Origin;
      basisX = transform.BasisX;
      basisY = transform.BasisY;
      basisZ = transform.BasisZ;
    }

    public static bool TryGetInverse(this Transform transform, out Transform inverse)
    {
      if (DefaultTolerance < transform.Determinant)
      {
        try { inverse = transform.Inverse; return true; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      inverse = Transform.Identity;
      return false;
    }

    public static void GetCoordSystem
    (
      this Transform transform,
      out XYZ origin, out XYZ basisX, out XYZ basisY, out XYZ basisZ
    )
    {
      origin = transform.Origin;
      basisX = transform.BasisX;
      basisY = transform.BasisY;
      basisZ = transform.BasisZ;
    }

    public static void SetCoordSystem
    (
      this Transform transform,
      XYZ origin, XYZ basisX, XYZ basisY, XYZ basisZ
    )
    {
      transform.Origin = origin;
      transform.BasisX = basisX;
      transform.BasisY = basisY;
      transform.BasisZ = basisZ;
    }

    public static void SetToAlignCoordSystem
    (
      this Transform transform,
      XYZ origin0, XYZ basisX0, XYZ basisY0, XYZ basisZ0,
      XYZ origin1, XYZ basisX1, XYZ basisY1, XYZ basisZ1
    )
    {
      var from = Transform.Identity;
      from.BasisX = new XYZ(basisX0.X, basisY0.X, basisZ0.X);
      from.BasisY = new XYZ(basisX0.Y, basisY0.Y, basisZ0.Y);
      from.BasisZ = new XYZ(basisX0.Z, basisY0.Z, basisZ0.Z);
      from.Origin = from.OfPoint(-origin0);

      var to = Transform.Identity;
      to.BasisX = basisX1;
      to.BasisY = basisY1;
      to.BasisZ = basisZ1;
      to.Origin = origin1;

      var planeToPlane = to * from;

      transform.Origin = planeToPlane.Origin;
      transform.BasisX = planeToPlane.BasisX;
      transform.BasisY = planeToPlane.BasisY;
      transform.BasisZ = planeToPlane.BasisZ;
    }
  }
}
