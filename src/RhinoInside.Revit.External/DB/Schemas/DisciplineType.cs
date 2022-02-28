using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.UnitGroup
  /// </summary>
  public partial class DisciplineType : DataType
  {
    static readonly DisciplineType empty = new DisciplineType();
    public static new DisciplineType Empty => empty;

    public DisciplineType() { }
    public DisciplineType(string id) : base(id)
    {
      if (!IsDisciplineType(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsDisciplineType(string id)
    {
      return id.StartsWith("autodesk.spec:discipline") || id.StartsWith("autodesk.spec.discipline");
    }

#if REVIT_2021
    public static implicit operator DisciplineType(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new DisciplineType(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(DisciplineType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif
  }
}
