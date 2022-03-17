using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class LevelExtension
  {
    /// <summary>
    /// Set the signed distance along the Z axis from the World XY plane.
    /// </summary>
    /// <remarks>
    /// Tagged as "Internal Origin" in the UI.
    /// </remarks>
    /// <param name="level"></param>
    /// <param name="elevation"></param>
    public static void SetElevation(this Level level, double elevation)
    {
      var offset = level.ProjectElevation - level.Elevation;

      if(level.Elevation != elevation + offset)
        level.Elevation = elevation - offset;
    }

    /// <summary>
    /// Get the signed distance along the Z axis from the World XY plane.
    /// </summary>
    /// <remarks>
    /// Tagged as "Internal Origin" in the UI.
    /// </remarks>
    /// <param name="level"></param>
    /// <returns>Level height in Revit internal units.</returns>
    public static double GetElevation(this Level level)
    {
      return level.ProjectElevation;
    }

    /// <summary>
    /// Get the signed distance along the Z axis from the Project Base point.
    /// </summary>
    /// <param name="level"></param>
    /// <returns>Level elevation in Revit internal units.</returns>
    public static double GetElevationFromProjectBasePoint(this Level level)
    {
      return level.ProjectElevation - GetBasePointLocation(level.Document, ElevationBase.ProjectBasePoint).Z;
    }

    /// <summary>
    /// Get the signed distance along the Z axis from the Shared Coordinate System Origin.
    /// </summary>
    /// <remarks>
    /// Tagged as "Survey Point" in the UI.
    /// </remarks>
    /// <param name="level"></param>
    /// <returns>Level elevation in Revit internal units.</returns>
    public static double GetElevationFromSharedBasePoint(this Level level)
    {
      return level.ProjectElevation - GetBasePointLocation(level.Document, ElevationBase.SurveyPoint).Z;
    }

    internal static XYZ GetBasePointLocation(this Document doc, ElevationBase elevationBase)
    {
      var position = XYZ.Zero;
      switch (elevationBase)
      {
        //case ElevationBase.InternalOrigin:
        //  position = BasePointExtension.GetInternalOriginPoint(doc).GetPosition();
        //  break;
        case ElevationBase.ProjectBasePoint:
          position = BasePointExtension.GetProjectBasePoint(doc).GetPosition();
          break;
        case ElevationBase.SurveyPoint:
          position = BasePointExtension.GetSurveyPoint(doc).GetPosition() - BasePointExtension.GetSurveyPoint(doc).GetSharedPosition();
          break;
      }

      return position;
    }

#if !REVIT_2018
    /// <summary>
    /// Finds the id of the first available associated floor or structural plan view associated with this level.
    /// </summary>
    /// <remarks>
    /// The view id returned is determined by the same rules associated with the Revit
    /// tool "Go to Floor Plan". Many levels may actually have more than one associated
    /// floor plan id and this routine will only return the first one found.
    /// </remarks>
    /// <param name="level"></param>
    /// <returns>InvalidElementId if no associated view is found.</returns>
    /// <seealso cref="ViewExtension.FindAssociatedLevelId(ViewPlan)"/>
    public static ElementId FindAssociatedPlanViewId(this Level level)
    {
      using (var collector = new FilteredElementCollector(level.Document))
      {
        return collector.OfClass(typeof(ViewPlan)).
                WhereParameterEqualsTo(BuiltInParameter.PLAN_VIEW_LEVEL, level.Name).
                FirstElementId();
      }
    }
#endif
  }

  public static class LevelTypeExtension
  {
    public static ElevationBase GetElevationBase(this LevelType levelType)
    {
      return (ElevationBase) levelType.get_Parameter(BuiltInParameter.LEVEL_RELATIVE_BASE_TYPE).AsInteger();
    }
  }
}
