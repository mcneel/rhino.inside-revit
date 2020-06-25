namespace System.Collections.Generic
{
  internal static class ListExtensions
  {
    public static void AddCollection<T>(this List<T> list, ICollection<T> collection)
    {
      if (list.Capacity < list.Count + collection.Count)
        list.Capacity = list.Count + collection.Count;

      list.AddRange(collection);
    }

    public static void AddRange<T>(this List<T> list, IEnumerable<T> collection, int collectionCount)
    {
      if (list.Capacity < list.Count + collectionCount)
        list.Capacity = list.Count + collectionCount;

      list.AddRange(collection);
    }
  }
}
