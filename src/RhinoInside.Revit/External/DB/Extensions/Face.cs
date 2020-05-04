using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FaceExtension
  {
    public static bool OrientationMatchesSurface(this Face face)
    {
#if REVIT_2018
      return face.OrientationMatchesSurfaceOrientation;
#else
      return true;
#endif
    }

#if !REVIT_2018
    public static Surface GetSurface(this Face face)
    {
      switch(face)
      {
        case PlanarFace planarFace:
          return Plane.CreateByOriginAndBasis(planarFace.Origin, planarFace.XVector, planarFace.YVector);

        case ConicalFace conicalFace:
        {
          var basisX = conicalFace.get_Radius(0).Normalize();
          var basisY = conicalFace.get_Radius(1).Normalize();
          var basisZ = conicalFace.Axis.Normalize();
          return ConicalSurface.Create(new Frame(conicalFace.Origin, basisX, basisY, basisZ), conicalFace.HalfAngle);
        }

        case CylindricalFace cylindricalFace:
        {
          double radius = cylindricalFace.get_Radius(0).GetLength();
          var basisX = cylindricalFace.get_Radius(0).Normalize();
          var basisY = cylindricalFace.get_Radius(1).Normalize();
          var basisZ = cylindricalFace.Axis.Normalize();
          return CylindricalSurface.Create(new Frame(cylindricalFace.Origin, basisX, basisY, basisZ), radius);
        }

        case RevolvedFace revolvedFace:
        {
          var ECStoWCS = new Transform(Transform.Identity)
          {
            Origin = revolvedFace.Origin,
            BasisX = revolvedFace.get_Radius(0).Normalize(),
            BasisY = revolvedFace.get_Radius(1).Normalize(),
            BasisZ = revolvedFace.Axis.Normalize()
          };

          var profileInWCS = revolvedFace.Curve.CreateTransformed(ECStoWCS);

          return RevolvedSurface.Create(new Frame(ECStoWCS.Origin, ECStoWCS.BasisX, ECStoWCS.BasisY, ECStoWCS.BasisZ), profileInWCS);
        }
        case RuledFace ruledFace:
        {
          var profileCurve0 = ruledFace.get_Curve(0);
          var profileCurve1 = ruledFace.get_Curve(1);
          return RuledSurface.Create(profileCurve0, profileCurve1);
        }
      }

      return null;
    }

    public static Curve GetProfileCurveInWorldCoordinates(this RevolvedSurface revolvedSurface)
    {
      var profileCurve = revolvedSurface.GetProfileCurve();
      var ECStoWCS = new Transform(Transform.Identity)
      {
        Origin = revolvedSurface.Origin,
        BasisX = revolvedSurface.XDir.Normalize(),
        BasisY = revolvedSurface.YDir.Normalize(),
        BasisZ = revolvedSurface.Axis.Normalize()
      };

      return profileCurve.CreateTransformed(ECStoWCS);
    }

    public static bool HasFirstProfilePoint(this RuledSurface ruledSurface)
    {
      return ruledSurface.GetFirstProfilePoint() is object;
    }

    public static bool HasSecondProfilePoint(this RuledSurface ruledSurface)
    {
      return ruledSurface.GetSecondProfilePoint() is object;
    }
#endif
  }
}
