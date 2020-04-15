namespace RhinoInside.Revit.External.DB
{
  // Revit API does not have an enum for this (eirannejad: 2020-04-15)
  // replace with Revit API enum when implemented
  public enum CurtainMullionSystemFamily
  {
    Unknown          = -1,
    Rectangular      = 0,
    Circular         = 1,
    LCorner          = 2,
    TrapezoidCorner  = 3,
    QuadCorner       = 4,
    VCorner          = 5
  }
}
