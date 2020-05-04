using System;

namespace RhinoInside.Revit.Convert.System
{
  public static class ArrayConverter
  {
    public static TOutput[] ConvertAll<TInput, TOutput>(this TInput[] input, Converter<TInput, TOutput> converter)
    {
      var count = input.Length;
      var output = new TOutput[count];

      for (int i = 0; i < count; ++i)
        output[i] = converter(input[i]);

      return output;
    }
  }
}
