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

    public static T Input<T>(this GH_ComponentParamServer parameters, string name)
      where T : class, IGH_Param
    {
      if (IndexOf(parameters.Input, name, out var value) >= 0)
        return value as T;

      return default;
    }

    public static T Output<T>(this GH_ComponentParamServer parameters, string name)
      where T : class, IGH_Param
    {
      if (IndexOf(parameters.Output, name, out var value) >= 0)
        return value as T;

      return default;
    }

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value)
      where T : struct => TryGetData(parameters, DA, name, out value, x => true);

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value)
      where T : class => TryGetData(parameters, DA, name, out value, x => true);

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value)
      where T : class => GetData(parameters, DA, name, out value, x => true);

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value)
      where T : struct => GetData(parameters, DA, name, out value, x => true);

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value, Func<T, bool> validate)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        T val = default;
        if (!DA.GetData(index, ref val)) { value = default; return false; }
        if (!validate(val))
        {
          parameters.Owner.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{name} value is not valid.");
          value = default;
          return false;
        }

        value = val;
        return true;
      }

      value = default;
      return true;
    }

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value, Func<T, bool> validate)
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) return false;
        if (!validate(value))
        {
          parameters.Owner.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{name} value is not valid.");
          return false;
        }
      }

      return true;
    }

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value, Func<T, bool> validate)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        T val = default;
        if (!DA.GetData(index, ref val)) { value = default; return false; }
        if (!validate(val))
        {
          parameters.Owner.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{name} value is not valid.");
          value = default;
          return false;
        }

        value = val;
        return true;
      }

      value = default;
      return false;
    }

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value, Func<T, bool> validate)
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) return false;
        if (!validate(value))
        {
          parameters.Owner.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Input parameter {name} collected some invalid data.");
          return false;
        }

        return true;
      }

      return false;
    }

    public static bool TryGetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out IList<T?> values)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T?[goos.Count];

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              if (goo.CastTo<T>(out data)) values[i] = data;
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return true;
    }
    public static bool TryGetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out IList<T> values)
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              if (goo.CastTo(out data)) values[i] = data;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T) TGoo;
              }
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return true;
    }

    public static bool GetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out IList<T?> values)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T?[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo != null)
            {
              if (goo is T?) values[i] = (T?) goo;
              else if (goo.CastTo(out T data)) values[i] = data;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T) TGoo;
              }
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return false;
    }

    public static bool GetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out IList<T> values)
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos.Cast<IGH_Goo>())
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              else if (goo.CastTo(out data)) values[i] = data;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T) TGoo;
              }
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
