using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel
{
  static class GH_ComponentParamServerExtension
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

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        T val = default;
        if (DA.GetData(index, ref val))
          value = val;
        else
          value = null;

        return true;
      }

      value = default;
      return false;
    }
    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value)
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        DA.GetData(index, ref value);
        return true;
      }

      return false;
    }

    public static bool TryGetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T?[] values)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = Activator.CreateInstance(typeof(List<>).MakeGenericType(param.Type)) as ICollection;
        dynamic da = DA;
        if (da.GetDataList(index, goos))
        {
          values = new T?[goos.Count];

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo is object && goo.CastTo<T>(out var data))
              values[i] = data;

            i++;
          }

          return true;
        }
      }

      values = default;
      return false;
    }
    public static bool TryGetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T[] values)
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = Activator.CreateInstance(typeof(List<>).MakeGenericType(param.Type)) as ICollection;
        dynamic da = DA;
        if (da.GetDataList(index, goos))
        {
          values = new T[goos.Count];
          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo is object && goo.CastTo<T>(out var data))
            {
              values[i] = data;
            }
            else if (TIsGoo)
            {
              var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
              if (TGoo.CastFrom(goo))
                values[i] = (T) TGoo;
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return false;
    }

    public static bool TrySetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<T?> value)
      where T : struct
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (index >= 0)
      {
        var nullable = value();
        if (nullable.HasValue)
          return DA.SetData(index, nullable.Value);
        else
          return DA.SetData(index, null);
      }

      return false;
    }
    public static bool TrySetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<T> value)
    {
      var index = parameters.Output.IndexOf(name, out var _);
      return index >= 0 && DA.SetData(index, value());
    }

    public static bool TrySetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<IEnumerable<T?>> list)
      where T : struct
    {
      var index = parameters.Output.IndexOf(name, out var _);
      return index >= 0 && DA.SetDataList(index, list().Select(x => x.HasValue ? (object) x.Value : null));
    }
    public static bool TrySetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<IEnumerable<T>> list)
    {
      var index = parameters.Output.IndexOf(name, out var _);
      return index >= 0 && DA.SetDataList(index, list());
    }
  }
}
