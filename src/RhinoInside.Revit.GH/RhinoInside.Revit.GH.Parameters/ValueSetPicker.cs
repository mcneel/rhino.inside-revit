using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ValueSetPicker : ValueSet
  {
    public static readonly Guid ComponentClassGuid = new Guid("AFB12752-3ACB-4ACF-8102-16982A69CDAE");
    public override Guid ComponentGuid => ComponentClassGuid;

    public ValueSetPicker() { }

    class GooComparer : IEqualityComparer<IGH_Goo>
    {
      static bool IsEquatable(Type value)
      {
        for (var type = value; type is object; type = type.BaseType)
        {
          var IEquatableT = typeof(IEquatable<>).MakeGenericType(type);
          if (IEquatableT.IsAssignableFrom(value))
            return true;
        }

        return false;
      }

      public static bool IsComparable(IGH_Goo goo)
      {
        return IsEquatable(goo.GetType()) ||
        goo is IGH_GeometricGoo geometry ||
        goo is IGH_QuickCast ||
        goo is GH_StructurePath ||
        goo is GH_Culture ||
        (
          goo?.ScriptVariable() is object obj &&
          (
            obj is ValueType ||
            obj is IComparable
          )
        );
      }

      public bool Equals(IGH_Goo x, IGH_Goo y)
      {
        if (x.GetType() is Type typeX && y.GetType() is Type typeY && typeX == typeY)
        {
          if (IsEquatable(typeX))
          {
            dynamic dynX = x, dynY = y;
            return dynX.Equals(dynY);
          }
        }

        if (x is IGH_QuickCast qcX && y is IGH_QuickCast qcY)
          return qcX.QC_CompareTo(qcY) == 0;

        if (x is IGH_GeometricGoo geoX && y is IGH_GeometricGoo geoY)
        {
          if (geoX.IsReferencedGeometry || geoY.IsReferencedGeometry)
            return geoX.ReferenceID == geoY.ReferenceID;

          if (geoX.ScriptVariable() is Rhino.Geometry.GeometryBase geometryX && geoY.ScriptVariable() is Rhino.Geometry.GeometryBase geometryY)
            return Rhino.Geometry.GeometryBase.GeometryEquals(geometryX, geometryY);
        }

        if (x is GH_StructurePath pathX && y is GH_StructurePath pathY)
          return pathX.Value == pathX.Value;

        if (x is GH_Culture cultureX && y is GH_Culture cultureY)
          return cultureX.Value == cultureY.Value;

        if (x.ScriptVariable() is object objX && y.ScriptVariable() is object objY)
        {
          if (objX is ValueType valueX && objY is ValueType valueY)
            return valueX.Equals(valueY);

          if (objX is IComparable comparableX && objY is IComparable comparableY)
            return comparableX.CompareTo(comparableY) == 0;
        }

        return false;
      }

      public int GetHashCode(IGH_Goo obj)
      {
        if (IsEquatable(obj.GetType()))
          return obj.GetHashCode();

        if (obj is IGH_GeometricGoo geo && geo.IsReferencedGeometry)
          return geo.ReferenceID.GetHashCode();

        if (obj is IGH_QuickCast qc)
          return qc.QC_Hash();

        if (obj is GH_StructurePath path)
          return path.Value.GetHashCode();

        if (obj is GH_Culture culture)
          return culture.Value.LCID;

        if (obj.ScriptVariable() is object o)
        {
          if (o is ValueType value)
            return value.GetHashCode();

          if (o is IComparable comparable)
            return comparable.GetHashCode();
        }

        return 0;
      }
    }

    public override void ProcessData()
    {
      int dataCount = VolatileDataCount;
      int nonComparableCount = 0;
      var goosSet = new HashSet<IGH_Goo>(VolatileData.AllData(true).
          Where(x =>
          {
            if (GooComparer.IsComparable(x))
              return true;

            nonComparableCount++;
            return false;
          })
          , new GooComparer());

      if (nonComparableCount > 0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{nonComparableCount} null or non comparable elements filtered.");

      var duplicatedCount = dataCount - nonComparableCount - goosSet.Count;
      if (duplicatedCount > 0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{duplicatedCount} duplicated elements filtered.");

      if (DataType == GH_ParamData.local)
      {
        ListItems = goosSet.Select(goo => new ListItem(goo, true)).
                    Where(x => string.IsNullOrEmpty(NickName) || x.Name.IsSymbolNameLike(NickName)).
                    ToList();
      }
      else if (DataType == GH_ParamData.remote)
      {
        var selectSet = new HashSet<IGH_Goo>(PersistentData.Where(x => GooComparer.IsComparable(x)), new GooComparer());
        ListItems = goosSet.Select(goo => new ListItem(goo, selectSet.Contains(goo))).
                    Where(x => string.IsNullOrEmpty(NickName) || x.Name.IsSymbolNameLike(NickName)).
                    ToList();
      }
      else
      {
        ListItems.Clear();
      }
    }
  }
}
