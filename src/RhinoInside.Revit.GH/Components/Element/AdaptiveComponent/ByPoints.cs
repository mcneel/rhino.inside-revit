using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AdaptiveComponentByPoints : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E8DDC0E4-97E9-4659-9945-E8C77114273D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AdaptiveComponentByPoints() : base
    (
      "Add Component (Adaptive)", "CompAdap",
      "Given a collection of Points, it adds an AdaptiveComponent element to the active Revit document",
      "Revit", "Build"
    )
    { }

    void ReconstructAdaptiveComponentByPoints
    (
      DB.Document doc,

      [Description("New Adaptive Component element")]
      ref DB.FamilyInstance component,

      IList<Rhino.Geometry.Point3d> points,
      DB.FamilySymbol type
    )
    {
      var adaptivePoints = points.ConvertAll(GeometryEncoder.ToXYZ);

      if (!type.IsActive)
        type.Activate();

      // Type
      ChangeElementTypeId(ref component, type.Id);

      if (component is object && DB.AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(component))
      {
        var adaptivePointIds = DB.AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(component);
        if (adaptivePointIds.Count == adaptivePoints.Count)
        {
          int index = 0;
          foreach (var vertex in adaptivePointIds.Select(id => doc.GetElement(id)).Cast<DB.ReferencePoint>())
            vertex.Position = adaptivePoints[index++];

          return;
        }
      }

      {
        var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>
        {
          Revit.ActiveUIApplication.Application.Create.NewFamilyInstanceCreationData(type, adaptivePoints)
        };

        var newElementIds = doc.IsFamilyDocument ?
                            doc.FamilyCreate.NewFamilyInstances2( creationData ) :
                            doc.Create.NewFamilyInstances2( creationData );

        if (newElementIds.Count != 1)
        {
          doc.Delete(newElementIds);
          throw new InvalidOperationException();
        }

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM
        };

        ReplaceElement(ref component, doc.GetElement(newElementIds.First()) as DB.FamilyInstance, parametersMask);
      }
    }
  }
}
