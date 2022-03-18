using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using Kernel.Attributes;

  public class AdaptiveComponentByPoints : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E8DDC0E4-97E9-4659-9945-E8C77114273D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

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
      ARDB.Document document,

      [Description("New Adaptive Component element")]
      ref ARDB.FamilyInstance component,

      IList<Rhino.Geometry.Point3d> points,

      ARDB.FamilySymbol type
    )
    {
      var adaptivePoints = points.ConvertAll(GeometryEncoder.ToXYZ);

      if (!type.IsActive)
        type.Activate();

      // Type
      ChangeElementTypeId(ref component, type.Id);

      if (component is object && ARDB.AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(component))
      {
        var adaptivePointIds = ARDB.AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(component);
        if (adaptivePointIds.Count == adaptivePoints.Length)
        {
          int index = 0;
          foreach (var vertex in adaptivePointIds.Select(id => document.GetElement(id)).Cast<ARDB.ReferencePoint>())
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
          throw new Exceptions.RuntimeErrorException("Failed to create Family Instance element.");

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM
        };

        ReplaceElement(ref component, document.GetElement(newElementIds.First()) as ARDB.FamilyInstance, parametersMask);
      }
    }
  }
}
