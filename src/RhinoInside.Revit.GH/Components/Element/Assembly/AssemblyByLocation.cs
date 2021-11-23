using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2")]
  public class AssemblyByLocation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("26feb2e9-6476-4ba7-a553-1d0300674d1d");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AssemblyByLocation() : base
    (
      name: "Add Assembly (Location)",
      nickname: "Assembly",
      description: "Create a new assembly instance at given location",
      category: "Revit",
      subCategory: "Assembly"
    )
    { }

    static readonly (string name, string nickname, string tip) _Assembly_
      = (name: "Assembly", nickname: "A", tip: "Created assembly instance");

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = $"Location to place the new instance. Point, plane is accepted"
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Assembly Type",
          NickName = "AT",
          Description = $"Assembly to create another instance of and place at given location"
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = _Assembly_.name,
          NickName = _Assembly_.nickname,
          Description = _Assembly_.tip,
        }
      )
    };

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ASSEMBLY_NAMING_CATEGORY,
      ARDB.BuiltInParameter.ASSEMBLY_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // active document
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      var location = default(Plane);
      if (!DA.GetData("Location", ref location))
        return;

      var sourceType = default(ARDB.ElementType);
      if (!DA.GetData("Assembly Type", ref sourceType))
        return;

      // find any tracked sheet
      Params.ReadTrackedElement(_Assembly_.name, doc.Value, out ARDB.AssemblyInstance assembly);

      // update, or create
      StartTransaction(doc.Value);
      {
        assembly = Reconstruct(assembly, doc.Value, sourceType, location);

        Params.WriteTrackedElement(_Assembly_.name, doc.Value, assembly);
        DA.SetData(_Assembly_.name, assembly);
      }
    }

    bool Reuse(ARDB.AssemblyInstance assembly, Plane location)
    {
      if (assembly.Location is ARDB.LocationPoint lp)
      {
        var translate = location.ToPlane().Origin - lp.Point;
        ARDB.ElementTransformUtils.MoveElement(assembly.Document, assembly.Id, translate);
        return true;
      }
      return false;
    }

    ARDB.AssemblyInstance Create(ARDB.Document doc, ARDB.ElementType sourceType, Plane location)
    {
      return ARDB.AssemblyInstance.PlaceInstance(doc, sourceType.Id, location.ToPlane().Origin);
    }

    ARDB.AssemblyInstance Reconstruct(ARDB.AssemblyInstance assembly, ARDB.Document doc, ARDB.ElementType sourceType, Plane location)
    {
      if (assembly is null || !Reuse(assembly, location))
        assembly = assembly.ReplaceElement
        (
          Create(doc, sourceType, location),
          ExcludeUniqueProperties
        );

      return assembly;
    }
  }
}
