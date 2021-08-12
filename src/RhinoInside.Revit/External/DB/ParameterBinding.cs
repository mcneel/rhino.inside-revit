using System;

namespace RhinoInside.Revit.External.DB
{
  [Flags]
  public enum ParameterBinding
  {
    Unknown,
    Instance = 1,
    Type     = 2,
    Global   = 4
  }
}
