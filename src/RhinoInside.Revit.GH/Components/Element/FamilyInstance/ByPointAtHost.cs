using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
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
      Rhino.Geometry.Plane location,
      DB.FamilySymbol type,
      Optional<DB.Level> level,
      [Optional] DB.Element host
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location), "Should be a valid point or plane.");

      SolveOptionalLevel(doc, location.Origin, ref level, out var bbox);

      if (host == null && type.Family.FamilyPlacementType == DB.FamilyPlacementType.OneLevelBasedHosted)
        ThrowArgumentException(nameof(host), $"This family requires a host.");

      if (!type.IsActive)
        type.Activate();

      ChangeElementTypeId(ref element, type.Id);

      bool hasSameHost = false;
      if (element is DB.FamilyInstance)
      {
        if (element.Host is DB.Element elementHost)
          hasSameHost = elementHost.Id == (host?.Id ?? DB.ElementId.InvalidElementId);
        else using (var freeHostName = element.get_Parameter(DB.BuiltInParameter.INSTANCE_FREE_HOST_PARAM))
          hasSameHost = !(freeHostName is null);
      }

      if(hasSameHost)
      {
        element.Pinned = false;

        if (element.LevelId != level.Value.Id)
        {
          using (var levelParam = element.get_Parameter(DB.BuiltInParameter.FAMILY_LEVEL_PARAM))
          {
            levelParam.Set(level.Value.Id);
            doc.Regenerate();
          }
        }
      }
      else
      {
        var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>()
        {
          new Autodesk.Revit.Creation.FamilyInstanceCreationData
          (
            location.Origin.ToXYZ(),
            type,
            host,
            level.Value,
            DB.Structure.StructuralType.NonStructural
          )
        };

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
          DB.BuiltInParameter.FAMILY_LEVEL_PARAM
        };

        ReplaceElement(ref element, doc.GetElement(newElementIds.First()) as DB.FamilyInstance, parametersMask);
        doc.Regenerate();
      }

      element?.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
    }
  }
}
