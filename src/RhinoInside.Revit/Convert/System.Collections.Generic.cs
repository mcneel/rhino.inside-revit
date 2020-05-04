using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.Convert.System.Collections.Generic
{
  public static class IListConverter
  {
    public static List<TOutput> ConvertAll<TInput, TOutput>(this IList<TInput> input, Converter<TInput, TOutput> converter)
    {
      var count = input.Count;
      var output = new List<TOutput>(count);

      for(int i = 0; i < count; ++i)
        output[i] = converter(input[i]);

      return output;
    }
  }

  public static class IEnumerableConverter
  {
    public static IEnumerable<TOutput> Convert<TInput, TOutput>(this IEnumerable<TInput> input, Converter<TInput, TOutput> converter)
    {
      foreach (var item in input)
        yield return converter(item);
    }
  }
}
