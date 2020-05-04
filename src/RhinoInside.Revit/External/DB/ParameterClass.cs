using System;

namespace RhinoInside.Revit.External.DB
{
  [Flags]
  public enum ParameterClass
  {
    Any = -1,
    BuiltIn = 1,
    Project = 2,
    Family = 4,
    Shared = 8,
    Global = 16
  }
}
