using System;
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

    #region GetData
    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value)
      where T : class => GetData(parameters, DA, name, out value, x => true);

    public static bool GetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value)
      where T : struct => GetData(parameters, DA, name, out value, x => true);

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

    public static bool GetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T value)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : class => GetData(parameters, DA, name, reference, out value, x => true);

    public static bool GetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T? value)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct => GetData(parameters, DA, name, reference, out value, x => true);

    public static bool GetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T? value, Predicate<T> predicate)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) return false;
        value = reference?.TransformFrom(value.Value) ?? value;
        return Requires(parameters.Owner, DA, index, value.Value, predicate);
      }

      return false;
    }

    public static bool GetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T value, Predicate<T> predicate)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) return false;
        value = reference?.TransformFrom(value) ?? value;
        return Requires(parameters.Owner, DA, index, value, predicate);
      }

      return false;
    }
    #endregion

    #region TryGetData
    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value)
      where T : struct => TryGetData(parameters, DA, name, out value, x => true);

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T value)
      where T : class => TryGetData(parameters, DA, name, out value, x => true);

    public static bool TryGetData<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, out T? value, Predicate<T> predicate)
      where T : struct
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
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
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        return Requires(parameters.Owner, DA, index, value, predicate);
      }

      return true;
    }

    public static bool TryGetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T? value)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct => TryGetData(parameters, DA, name, reference, out value, x => true);

    public static bool TryGetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T value)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : class => TryGetData(parameters, DA, name, reference, out value, x => true);

    public static bool TryGetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T? value, Predicate<T> predicate)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        value = reference?.TransformFrom(value) ?? value;
        return Requires(parameters.Owner, DA, index, value.Value, predicate);
      }

      return true;
    }

    public static bool TryGetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out T value, Predicate<T> predicate)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : class
    {
      value = default;

      var index = parameters.Input.IndexOf(name, out var param);
      if (param?.DataType > GH_ParamData.@void)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        value = reference?.TransformFrom(value) ?? value;
        return Requires(parameters.Owner, DA, index, value, predicate);
      }

      return true;
    }
    #endregion

    #region GetDataList
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

    public static bool GetDataList<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out IList<T?> values)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (reference is object && param?.DataType > GH_ParamData.@void)
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
              if (goo is T?) values[i] = reference.TransformFrom((T) goo);
              else if (goo.CastTo(out T data)) values[i] = reference.TransformFrom(data);
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = reference.TransformFrom((T) TGoo);
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

    public static bool GetDataList<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, out IList<T> values)
      where R : RhinoInside.Revit.GH.Types.Reference
    {
      var index = parameters.Input.IndexOf(name, out var param);
      if (reference is object && param?.DataType > GH_ParamData.@void)
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
              if (goo is T data) values[i] = reference.TransformFrom(data);
              else if (goo.CastTo(out data)) values[i] = reference.TransformFrom(data);
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo) Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = reference.TransformFrom((T) TGoo);
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
    #endregion

    #region TryGetDataList
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
              else if (goo.CastTo(out data)) values[i] = data;
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
    #endregion

    #region TrySetData
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

    public static bool TrySetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, Func<R, T?> value)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (index >= 0)
      {
        var nullable = value(reference);
        if (nullable.HasValue)
          return DA.SetData(index, reference.TransformTo(nullable.Value));
        else
          return DA.SetData(index, null);
      }

      return false;
    }
    public static bool TrySetData<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, Func<R, T> value)
      where R : RhinoInside.Revit.GH.Types.Reference
    {
      var index = parameters.Output.IndexOf(name, out var _);
      return index >= 0 && DA.SetData(index, reference.TransformTo(value(reference)));
    }
    #endregion

    #region TrySetDataList
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

    public static bool TrySetDataList<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, Func<R, IEnumerable<T?>> list)
      where R : RhinoInside.Revit.GH.Types.Reference
      where T : struct
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (reference is null) return false;
      return index >= 0 && DA.SetDataList(index, list(reference).Select(x => x.HasValue ? (object) reference.TransformTo(x.Value) : null));
    }
    public static bool TrySetDataList<R, T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, R reference, Func<R, IEnumerable<T>> list)
      where R : RhinoInside.Revit.GH.Types.Reference
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (reference is null) return false;
      return index >= 0 && DA.SetDataList(index, list(reference).Select(reference.TransformTo));
    }
    #endregion

    #region TrySetDataTree
    readonly struct DataTree<T> : Data.IGH_DataTree
    {
      readonly Data.GH_Path Path;
      readonly IEnumerable<IEnumerable<T>> Enumerable;
      public DataTree(Data.GH_Path path, IEnumerable<IEnumerable<T>> enumerable)
      {
        Path = path;
        Enumerable = enumerable;
      }

      bool Data.IGH_DataTree.MergeWithParameter(IGH_Param param)
      {
        if (param is null)
          return false;

        if (Enumerable is null)
          return param.AddVolatileDataList(Path.AppendElement(0), default);

        int index = 0;
        foreach (var enumerable in Enumerable)
          param.AddVolatileDataList(Path.AppendElement(index++), enumerable);

        return true;
      }
    }

    public static bool TrySetDataTree<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<IEnumerable<IEnumerable<T?>>> tree)
      where T : struct
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (index < 0) return false;

      return DA.SetDataTree(index, new DataTree<T?>(DA.ParameterTargetPath(index).AppendElement(DA.ParameterTargetIndex(index)), tree()));
    }

    public static bool TrySetDataTree<T>(this GH_ComponentParamServer parameters, IGH_DataAccess DA, string name, Func<IEnumerable<IEnumerable<T>>> tree)
    {
      var index = parameters.Output.IndexOf(name, out var _);
      if (index < 0) return false;

      return DA.SetDataTree(index, new DataTree<T>(DA.ParameterTargetPath(index).AppendElement(DA.ParameterTargetIndex(index)), tree()));
    }
    #endregion

    internal static IEnumerable<TSource> TakeWhileIsNotEscapeKeyDown<TSource>
    (this IEnumerable<TSource> source, RhinoInside.Revit.GH.AbortToken token)
    {
      return TakeWhileNotAborted(source, token);
    }

    internal static IEnumerable<TSource> TakeWhileNotAborted<TSource>
      (this IEnumerable<TSource> source, RhinoInside.Revit.GH.AbortToken token)
    {
      if (source is null) throw new ArgumentNullException(nameof(source));
      if (!token.CanBeAborted) return source;
      return TakeWhileNotAborted(source, token.Lambda);
    }

    static IEnumerable<TSource> TakeWhileNotAborted<TSource>
      (this IEnumerable<TSource> source, Func<bool> isAbortRequested)
    {
      if (source is null) throw new ArgumentNullException(nameof(source));
      if (isAbortRequested is null) throw new ArgumentNullException(nameof(isAbortRequested));

      foreach (var item in source)
      {
        if (isAbortRequested())
          break;

        yield return item;
      }
    }
  }
}
