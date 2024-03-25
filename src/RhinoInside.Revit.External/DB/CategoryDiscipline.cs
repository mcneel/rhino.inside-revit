using System;

namespace RhinoInside.Revit.External.DB
{
  [Flags]
  public enum CategoryDiscipline
  {
    None = 0,
    Architecture = 1,
    Structure = 2,
    Mechanical = 4,
    Electrical = 8,
    Piping = 16,
    Infrastructure = 32,
  }
}
