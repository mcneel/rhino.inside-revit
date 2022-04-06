using System;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit
{
  internal static class Operator
  {
    public enum CompareMethod
    {
      Nothing,
      Equals,
      StartsWith, // <
      EndsWith,   // >
      Contains,   // ?
      Wildcard,   // :
      Regex,      // ;
    }

    public static CompareMethod CompareMethodFromPattern(string pattern)
    {
      bool not = false;
      return CompareMethodFromPattern(ref pattern, ref not);
    }

    public static CompareMethod CompareMethodFromPattern(ref string pattern, ref bool not)
    {
      if (pattern is null)
        return CompareMethod.Nothing;

      if (pattern == string.Empty)
        return CompareMethod.Equals;

      switch (pattern[0])
      {
        case '~': not = !not; pattern = pattern.Substring(1); return CompareMethodFromPattern(ref pattern, ref not);
        case '<': pattern = pattern.Substring(1); return pattern == string.Empty ? CompareMethod.Equals : CompareMethod.StartsWith;
        case '>': pattern = pattern.Substring(1); return pattern == string.Empty ? CompareMethod.Equals : CompareMethod.EndsWith;
        case '?': pattern = pattern.Substring(1); return pattern == string.Empty ? CompareMethod.Equals : CompareMethod.Contains;
        case ':': pattern = pattern.Substring(1); return pattern == string.Empty ? CompareMethod.Equals : CompareMethod.Wildcard;
        case ';': pattern = pattern.Substring(1); return pattern == string.Empty ? CompareMethod.Equals : CompareMethod.Regex;
        default: return CompareMethod.Equals;
      }
    }

    public static bool IsSymbolNameLike(this string source, string pattern)
    {
      if (pattern is null)
        return true;

      bool not = false;
      switch (CompareMethodFromPattern(ref pattern, ref not))
      {
        case CompareMethod.Nothing:     return not ^ false;
        case CompareMethod.Equals:      return not ^ string.Equals(source, pattern, ElementNameComparer.StringComparison);
        case CompareMethod.StartsWith:  return not ^ source.StartsWith(pattern, ElementNameComparer.StringComparison);
        case CompareMethod.EndsWith:    return not ^ source.EndsWith(pattern, ElementNameComparer.StringComparison);
        case CompareMethod.Contains:    return not ^ (source.IndexOf(pattern, ElementNameComparer.StringComparison) >= 0);
        case CompareMethod.Wildcard:    return not ^ Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(source, pattern, Microsoft.VisualBasic.CompareMethod.Text);
        case CompareMethod.Regex: var regex = new System.Text.RegularExpressions.Regex(pattern); return not ^ regex.IsMatch(source);
      }

      return false;
    }
  }
}
