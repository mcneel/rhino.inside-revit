using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class DimensionExtension
  {
    public static bool GetHasLeader(this Dimension dimension)
    {
#if REVIT_2021
      return dimension.HasLeader;
#else
      return dimension.get_Parameter(BuiltInParameter.DIM_LEADER).AsBoolean();      
#endif
    }

    public static void SetHasLeader(this Dimension dimension, bool value)
    {
#if REVIT_2021
      dimension.HasLeader = value;
#else
      dimension.get_Parameter(BuiltInParameter.DIM_LEADER).Set(value);
#endif
    }

    public static Curve GetBoundedCurve(this Dimension dimension)
    {
      if (dimension.Curve is Curve curve)
      {
        try
        {
          if (!curve.IsBound)
          {
            var segments = dimension.Segments.Cast<DimensionSegment>().Where(x => x.Value.HasValue).ToArray();
            if (segments.Length > 0)
            {
              var first = segments.First();
              var start = curve.Project(first.Origin);

              var last = segments.Last();
              var end = curve.Project(last.Origin);

              curve.MakeBound(start.Parameter - first.Value.Value * 0.5, end.Parameter + last.Value.Value * 0.5);
            }
            else if (dimension.Value.HasValue)
            {
              if (dimension.Curve.Project(dimension.Origin) is IntersectionResult result)
              {
                var startParameter = dimension.Value.Value * -0.5;
                var endParameter = dimension.Value.Value * +0.5;
                curve.MakeBound(result.Parameter + startParameter, result.Parameter + endParameter);
              }
            }
          }
        }
        catch { }

        return curve;
      }

      return null;
    }
  }
}
