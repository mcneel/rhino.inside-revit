using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.Convert.System.Collections.Generic
{
  public static class ArrayConverter
  {
    /// <summary>
    /// Converts an array of one type to an array of another type.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements of the source array.</typeparam>
    /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
    /// <param name="input">The one-dimensional, zero-based <see cref="Array"/> to convert to a target type.</param>
    /// <param name="converter">A <see cref="Converter{TInput, TOutput}"/> that converts each element from one type to another type.</param>
    /// <returns>An array of the target type containing the converted elements from the source array.</returns>
    public static TOutput[] ConvertAll<TInput, TOutput>(this TInput[] input, Converter<TInput, TOutput> converter)
    {
      var count = input.Length;
      var output = new TOutput[count];

      for (int i = 0; i < count; ++i)
        output[i] = converter(input[i]);

      return output;
    }
  }

  public static class IListConverter
  {
    /// <summary>
    /// Converts an IList of one type to an array of another type.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements of the source IList.</typeparam>
    /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
    /// <param name="input">The <see cref="IList{T}"/> to convert to a target type.</param>
    /// <param name="converter">A <see cref="Converter{TInput, TOutput}"/> that converts each element from one type to another type.</param>
    /// <returns>An array of the target type containing the converted elements from the source IList.</returns>
    public static TOutput[] ConvertAll<TInput, TOutput>(this IList<TInput> input, Converter<TInput, TOutput> converter)
    {
      var count = input.Count;
      var output = new TOutput[count];

      for (int i = 0; i < count; ++i)
        output[i] = converter(input[i]);

      return output;
    }
  }

  public static class ICollectionConverter
  {
    /// <summary>
    /// Converts an ICollection of one type to a List of another type.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements of the source ICollection.</typeparam>
    /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
    /// <param name="input">The <see cref="ICollection{T}"/> to convert to a target type.</param>
    /// <param name="converter">A <see cref="Converter{TInput, TOutput}"/> that converts each element from one type to another type.</param>
    /// <returns>An array of the target type containing the converted elements from the source ICollection.</returns>
    public static TOutput[] ConvertAll<TInput, TOutput>(this ICollection<TInput> input, Converter<TInput, TOutput> converter)
    {
      var count = input.Count;
      var output = new TOutput[count];

      int index = 0;
      foreach(var item in input)
        output[index++] = converter(item);

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
