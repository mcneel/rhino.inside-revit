using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;


namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SolidExtension
  {
    /// <summary>
    /// Identifies if the solid is watertight. Is watertight if all edges are shared by two faces.
    /// </summary>
    /// <param name="solid"></param>
    /// <returns>It true, the solid is watertight and defines a closed volume.</returns>
    public static bool IsWatertight(this Solid solid)
    {
      foreach (var face in solid.Faces.Cast<Face>())
      {
        foreach (var loop in face.EdgeLoops.Cast<EdgeArray>())
        {
          foreach (var edge in loop.Cast<Edge>())
          {
            if (edge.GetFace(1) is null) return false;
          }
        }
      }

      return !solid.Faces.IsEmpty;
    }

    /// <summary>
    /// Identifies if the solid is watertight. Is watertight if all edges are shared by two faces.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="brepType"></param>
    /// <returns>It true, the solid is watertight and defines a closed volume.</returns>
    public static bool IsWatertight(this Solid solid, out BRepType brepType)
    {
      brepType = BRepType.OpenShell;
      var volume = solid.Volume;
      var watertight = IsWatertight(solid);

      if(watertight || System.Math.Abs(volume) > 1e-9)
        brepType = volume < 0.0 ? BRepType.Void : BRepType.Solid;

      return watertight;
    }

    public static bool TryGetNakedEdges(this Solid solid, out List<Edge> nakedEdges)
    {
      nakedEdges = default;
      foreach (var face in solid.Faces.Cast<Face>())
      {
        foreach (var loop in face.EdgeLoops.Cast<EdgeArray>())
        {
          foreach (var edge in loop.Cast<Edge>())
          {
            if (edge.GetFace(1) is null)
            {
              if (nakedEdges is null) nakedEdges = new List<Edge>();
              nakedEdges.Add(edge);
            }
          }
        }
      }

      return nakedEdges is object;
    }
  }
}
