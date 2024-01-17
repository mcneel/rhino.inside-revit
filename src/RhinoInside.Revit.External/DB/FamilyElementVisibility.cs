using System;

namespace RhinoInside.Revit.External.DB
{
  /// <summary>
  /// Enum for values of property <see cref="Autodesk.Revit.DB.BuiltInParameter.GEOM_VISIBILITY_PARAM"/>.
  /// </summary>
  [Flags]
  public enum FamilyElementVisibility
  {
    Model =         1 << 1,

    // Model
    PlanRCPCut =    1 << 2,
    TopBottom =     1 << 3,
    FrontBack =     1 << 4,
    LeftRight =     1 << 5,

    // View direction
    OnlyWhenCut =   1 << 6,

    // Detail level
    Coarse =        1 << 13,
    Medium =        1 << 14,
    Fine =          1 << 15,

    DefaultModel = Model | PlanRCPCut | TopBottom | FrontBack | LeftRight | Coarse | Medium | Fine,
    DefaultDetail = Model | Coarse | Medium | Fine,
    DefaultSymbolic = Coarse | Medium | Fine,
  }
}
