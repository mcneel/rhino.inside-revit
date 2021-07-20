using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
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
      name: "Add Component (Adaptive)",
      nickname: "CompAdap",
      description: "Given a collection of Points, it adds an AdaptiveComponent element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    void ReconstructAdaptiveComponentByPoints
    (
      [Optional, NickName("DOC")]
      DB.Document document,

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
        if (adaptivePointIds.Count == adaptivePoints.Length)
        {
          int index = 0;
          foreach (var vertex in adaptivePointIds.Select(id => document.GetElement(id)).Cast<DB.ReferencePoint>())
          {
            var position = adaptivePoints[index++];
            if (!vertex.Position.IsAlmostEqualTo(position))
              vertex.Position = position;
          }

          return;
        }
      }

      {
        var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>
        {
          Revit.ActiveUIApplication.Application.Create.NewFamilyInstanceCreationData(type, adaptivePoints)
        };

        var newElementIds = document.IsFamilyDocument ?
                            document.FamilyCreate.NewFamilyInstances2( creationData ) :
                            document.Create.NewFamilyInstances2( creationData );

        if (newElementIds.Count != 1)
        {
          document.Delete(newElementIds);
          throw new InvalidOperationException();
        }

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM
        };

        ReplaceElement(ref component, document.GetElement(newElementIds.First()) as DB.FamilyInstance, parametersMask);
      }
    }
  }
}
