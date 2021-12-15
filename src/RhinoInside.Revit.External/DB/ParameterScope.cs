using System;

namespace RhinoInside.Revit.External.DB
{
  [Flags]
  public enum ParameterScope
  {
    Unknown,
    Instance = 1,
    Type     = 2,
    Global   = 4
  }
}
