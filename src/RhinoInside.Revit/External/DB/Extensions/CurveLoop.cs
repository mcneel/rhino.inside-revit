using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
#if !REVIT_2020
  public static class CurveLoopExtension
  {
    public static int NumberOfCurves(this CurveLoop curveLoop)
    {
      return curveLoop.Count();
    }
  }
#endif
}
