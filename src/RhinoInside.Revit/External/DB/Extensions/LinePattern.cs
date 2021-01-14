using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class LinePatternExtension
  {
    public static bool IsValid(this BuiltInLinePattern value)
    {
      return value == BuiltInLinePattern.Solid;
    }
  }
}
