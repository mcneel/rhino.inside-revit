using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class DatumPlaneExtension
  {
    static PlaneEquation GetPlaneEquation(this DatumPlane datum)
    {
      switch (datum)
      {
        case Level level:
          return new PlaneEquation(UnitXYZ.BasisZ, -level.ProjectElevation);

        case Grid grid:
          if (grid.IsCurved) return default;
          var curve = grid.Curve;
          var start = curve.GetEndPoint(CurveEnd.Start);
          var end = curve.GetEndPoint(CurveEnd.End);
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var right = axis.ToUnitXYZ().Right();
          return new PlaneEquation(origin, -right);

        case ReferencePlane referencePlane:
          var plane = referencePlane.GetPlane();
          return new PlaneEquation(plane.Origin, (UnitXYZ) plane.Normal);

        case DatumPlane _:
          return default;
      }

      throw new NotImplementedException($"{nameof(GetPlaneEquation)} is not implemented for {datum.GetType()}");
    }

    public static SketchPlane GetSketchPlane(this DatumPlane datum, bool ensureSketchPlane = false)
    {
      if (ensureSketchPlane && !datum.Document.IsModifiable)
        throw new InvalidOperationException($"There is no started transaction in course on {nameof(datum)} document.");

      using (var collector = new FilteredElementCollector(datum.Document).OfClass(typeof(SketchPlane)))
      {
        var minDistance = double.PositiveInfinity;
        var closestSketchPlane = default(SketchPlane);
        var comparer = GeometryObjectEqualityComparer.Default;
        var datumEquation = GetPlaneEquation(datum);
        var datuName = datum.Name;

        foreach (var sketchPlane in collector.Cast<SketchPlane>())
        {
          if (!sketchPlane.IsSuitableForModelElements) continue;
          if (sketchPlane.Name != datuName) continue;
          using (var plane = sketchPlane.GetPlane())
          {
            var equation = new PlaneEquation(plane.Origin, (UnitXYZ) plane.Normal);

            if (!equation.AlmostEquals(datumEquation))
              continue;

            var distance = Math.Abs(equation.Offset - datumEquation.Offset);
            if (distance < minDistance)
            {
              minDistance = distance;
              closestSketchPlane = sketchPlane;
            }
          }
        }

        if (comparer.Equals(minDistance, 0.0))
          return closestSketchPlane;
      }

      return ensureSketchPlane ? SketchPlane.Create(datum.Document, datum.Id) : default;
    }
  }
}
