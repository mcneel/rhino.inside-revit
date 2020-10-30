using System;
using System.Collections.Generic;

namespace Grasshopper.Kernel
{
  static class IGH_DataAccessExtension
  {
    static int IndexOf(this IList<IGH_Param> list, string name, out IGH_Param value)
    {
      value = default;
      int index = 0;
      for (; index < list.Count; ++index)
      {
        var item = list[index];
        if (item.Name == name)
        {
          value = item;
          return index;
        }
      }

      return -1;
    }

    public static bool TryGetData<T>(this IGH_DataAccess DA, IList<IGH_Param> parameters, string name, out T value)
    {
      value = default;

      var index = parameters.IndexOf(name, out var param);
      return param?.DataType > GH_ParamData.@void && DA.GetData(index, ref value);
    }

    public static bool TryGetDataList<T>(this IGH_DataAccess DA, IList<IGH_Param> parameters, string name, out List<T> list)
    {
      list = new List<T>();

      var index = parameters.IndexOf(name, out var param);
      return param?.DataType > GH_ParamData.@void && DA.GetDataList(index, list);
    }

    public static bool TrySetData<T>(this IGH_DataAccess DA, IList<IGH_Param> parameters, string name, Func<T> value)
    {
      var index = parameters.IndexOf(name, out var _);
      return index >= 0 && DA.SetData(index, value());
    }

    public static bool TrySetDataList<T>(this IGH_DataAccess DA, IList<IGH_Param> parameters, string name, Func<IEnumerable<T>> list)
    {
      var index = parameters.IndexOf(name, out var _);
      return index >= 0 && DA.SetDataList(index, list());
    }
  }
}
