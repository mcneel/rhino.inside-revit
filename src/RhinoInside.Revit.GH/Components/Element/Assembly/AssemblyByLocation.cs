using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2", updated: "1.5")]
  public class AssemblyByLocation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("26FEB2E9-6476-4BA7-A553-1D0300674D1D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    public AssemblyByLocation() : base
    (
      name: "Add Assembly",
      nickname: "Assembly",
      description: "Create a new assembly instance at given location",
      category: "Revit",
      subCategory: "Model"
    )
    { }

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
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = $"Assembly to create another instance of and place at given location",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Assemblies
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = _Assembly_,
          NickName = _Assembly_.Substring(0, 1),
          Description = $"Output {_Assembly_}",
        }
      )
    };

    const string _Assembly_ = "Assembly";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ASSEMBLY_NAMING_CATEGORY,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.AssemblyInstance>
      (
        doc.Value, _Assembly_, (assembly) =>
        {
          // Input
          if (!Params.GetData(DA, "Location", out Plane? location)) return null;
          if (!Params.GetData(DA, "Type", out ARDB.AssemblyType type)) return null;

          // Compute
          StartTransaction(doc.Value);
          {
            assembly = Reconstruct(assembly, doc.Value, location.Value, type);
          }

          DA.SetData(_Assembly_, assembly);
          return assembly;
        }
      );
    }

    bool Reuse(ARDB.AssemblyInstance assembly, ARDB.AssemblyType type)
    {
      if (assembly is null) return false;
      if (type is object && assembly.GetTypeId() != type.Id) assembly.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.AssemblyInstance Reconstruct(ARDB.AssemblyInstance assembly, ARDB.Document doc, Plane location, ARDB.AssemblyType type)
    {
      if (!Reuse(assembly, type))
      {
        assembly = assembly.ReplaceElement
        (
          ARDB.AssemblyInstance.PlaceInstance(doc, type.Id, location.Origin.ToXYZ()),
          ExcludeUniqueProperties
        );
      }

      using (var transform = Transform.PlaneToPlane(Plane.WorldXY, location).ToTransform())
      {
        if (!assembly.GetTransform().AlmostEqual(transform))
        {
          var pinned = assembly.Pinned;
          try
          {
            assembly.Pinned = false;
            assembly.SetTransform(transform);
          }
          finally { assembly.Pinned = pinned; }
        }
      }

      return assembly;
    }
  }
}
