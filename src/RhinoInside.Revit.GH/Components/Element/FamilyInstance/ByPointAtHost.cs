using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Kernel.Attributes;

  public class FamilyInstanceByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("0C642D7D-897B-479E-8668-91E09222D7B9");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public FamilyInstanceByLocation () : base
    (
      "Add Component (Location)", "CompLoca",
      "Given its location, it reconstructs a Component element into the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FamilyInstance(), "Component", "C", "New Component element", GH_ParamAccess.item);
    }

    void ReconstructFamilyInstanceByLocation
    (
      DB.Document doc,
      ref DB.FamilyInstance element,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Plane location,
      DB.FamilySymbol type,
      Optional<DB.Level> level,
      [Optional] DB.Element host
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location), "Location is not valid.");

      SolveOptionalLevel(doc, location.Origin, ref level, out var _);

      if (host is null && type.Family.FamilyPlacementType == DB.FamilyPlacementType.OneLevelBasedHosted)
        ThrowArgumentException(nameof(host), $"This family requires a host.");

      if (!type.IsActive)
        type.Activate();

      {
        var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>();

        if (host is null && type.Family.FamilyPlacementType == DB.FamilyPlacementType.WorkPlaneBased)
        {
          creationData.Add
          (
            new Autodesk.Revit.Creation.FamilyInstanceCreationData
            (
              location.Origin.ToXYZ(),
              type,
              DB.SketchPlane.Create(doc, location.ToPlane()),
              level.Value,
              DB.Structure.StructuralType.NonStructural
            )
          );
        }
        else
        {
          creationData.Add
          (
           new Autodesk.Revit.Creation.FamilyInstanceCreationData
           (
             location.Origin.ToXYZ(),
             type,
             host,
             level.Value,
             DB.Structure.StructuralType.NonStructural
           )
         );
        }

        var newElementIds = doc.IsFamilyDocument ?
                            doc.FamilyCreate.NewFamilyInstances2(creationData) :
                            doc.Create.NewFamilyInstances2(creationData);

        if (newElementIds.Count != 1)
        {
          doc.Delete(newElementIds);
          throw new InvalidOperationException();
        }

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.FAMILY_LEVEL_PARAM,
          DB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
          DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
        };

        ReplaceElement(ref element, doc.GetElement(newElementIds.First()) as DB.FamilyInstance, parametersMask);
        doc.Regenerate();
        element.Pinned = false;
      }

      element?.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
    }
  }
}
