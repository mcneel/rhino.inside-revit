using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel
{
  interface IGH_RuntimeContract
  {
    bool RequiresFailed
    (
      IGH_DataAccess access,
      int index,
      object value,
      string message = default
    );
    //bool Ensures<T>(IGH_DataAccess dataAccess, int index, Func<bool> predicate);
    //bool Assert<T>(IGH_DataAccess dataAccess, int index, Func<bool> predicate);
  }

  static class GH_ComponentParamServerExtension
  {
    static bool Requires<T>
    (
      IGH_Component component,
      IGH_DataAccess access, int index,
      T value, Predicate<T> predicate
    )
    {
      if (predicate(value)) return true;

      if (component is IGH_RuntimeContract contract)
        return contract.RequiresFailed(access, index, value);

      component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Input parameter '{component.Params.Input[index].NickName}' collected invalid data.");
      return false;
    }

    internal static int IndexOf(this IList<IGH_Param> list, string name, out IGH_Param value)
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

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value, Predicate<T> predicate)
      where T : struct
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void && !param.VolatileData.IsEmpty)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        return Requires(parameters.Owner, DA, index, value.Value, predicate);
      }

      return true;
    }

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value, Predicate<T> predicate)
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void && !param.VolatileData.IsEmpty)
      {
        if (!DA.GetData(index, ref value)) return false;
        return Requires(parameters.Owner, DA, index, value, predicate);
      }

      return true;
    }

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value, Predicate<T> predicate)
      where T : struct
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        return Requires(parameters.Owner, DA, index, value.Value, predicate);
      }

      return false;
    }

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value, Predicate<T> predicate)
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) return false;
        return Requires(parameters.Owner, DA, index, value, predicate);
      }

      return false;
    }

    public static bool TryGetDataList<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out IList<T?> values)
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void && !param.VolatileData.IsEmpty)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T?[goos.Count];

          int i = 0;
          foreach (var goo in goos)
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              else if (goo.CastTo<T>(out data)) values[i] = data;
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
      if (param?.DataType > GH_ParamData.@void && !param.VolatileData.IsEmpty)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos)
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
          foreach (var goo in goos)
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
          foreach (var goo in goos)
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

    internal static IEnumerable<TSource> TakeWhileIsNotEscapeKeyDown<TSource>
      (this IEnumerable<TSource> source, IGH_DocumentObject documentObject)
    {
      if (source is null) throw new ArgumentNullException(nameof(source));
      if (documentObject is null) throw new ArgumentNullException(nameof(documentObject));

      foreach (TSource element in source)
      {
        if (GH_Document.IsEscapeKeyDown())
        {
          documentObject.OnPingDocument()?.RequestAbortSolution();
          break;
        }
        yield return element;
      }
    }
  }
}
