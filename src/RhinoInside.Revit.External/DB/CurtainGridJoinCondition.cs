namespace RhinoInside.Revit.External.DB
{
  // Revit API does not have an enum for this (eirannejad: 2020-04-14)
  // replace with Revit API enum when implemented
  public enum CurtainGridJoinCondition
  {
    NotDefined,
    VerticalGridContinuous,
    HorizontalGridContinuous,
    BorderAndVerticalGridContinuous,
    BorderAndHorizontalGridContinuous
  }
}
