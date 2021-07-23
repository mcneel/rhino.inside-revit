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
      if (!id.StartsWith("autodesk.spec:discipline") && !id.StartsWith("autodesk.spec.discipline"))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }
  }
}
