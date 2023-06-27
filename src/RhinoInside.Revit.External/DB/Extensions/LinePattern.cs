using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{

  public static class LinePatternExtension
  {
    public static bool IsValid(this BuiltInLinePattern value)
    {
      return value == BuiltInLinePattern.Solid;
    }

    public static bool UpdateSegments(this LinePattern pattern, IList<LinePatternSegment> lineSegs)
    {
      bool update = false;
      var segments = pattern.GetSegments();
      var count = segments.Count;
      if (count == lineSegs.Count)
      {
        var tol = Numerical.Tolerance.Default;
        for (int s = 0; s < count && !update; ++s)
        {
          if (segments[s].Type != lineSegs[s].Type || !tol.Equals(segments[s].Length, lineSegs[s].Length))
            update = true;
        }
      }
      else update = true;

      if (update)
        pattern.SetSegments(lineSegs);

      return update;
    }
  }
}
