using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2", updated: "1.5")]
  public class AssemblyByMembers : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("6915b697-f10d-4bc8-8faa-f25438f393a8");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AssemblyByMembers() : base
    (
      name: "Assemble Elements",
      nickname: "Assemble",
      description: "Create a new assembly instance",
      category: "Revit",
      subCategory: "Assembly"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Members",
          NickName = "M",
          Description = "Elements to be members of the new assembly",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Naming Category",
          NickName = "C",
          Description = "Category that drives the default naming scheme for the assembly instance",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template assembly",
          Optional = true
        },
        ParamRelevance.Occasional
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
          if (!Params.GetDataList(DA, "Members", out IList<Types.GraphicalElement> members)) return null;
          var memberIds = members.Where(x => x.IsValid && x.Document.Equals(doc.Value)).Select(x => x.Id).ToArray();

          Params.TryGetData(DA, "Naming Category", out Types.Category category, x => x.IsValid);
          var categoryId = category?.Id ?? doc.Value.GetElement(memberIds[0]).Category.Id;

          Params.TryGetData(DA, "Template", out ARDB.AssemblyInstance template);

          // Compute
          StartTransaction(doc.Value);
          {
            assembly = Reconstruct(assembly, doc.Value, memberIds, categoryId, template);
          }

          DA.SetData(_Assembly_, assembly);
          return assembly;
        }
      );
    }

    bool Reuse(ARDB.AssemblyInstance assembly, IList<ARDB.ElementId> members, ARDB.ElementId categoryId, ARDB.AssemblyInstance template)
    {
      if (assembly is null) return false;

      assembly.SetMemberIds(members);
      assembly.NamingCategoryId = categoryId;

      assembly.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.AssemblyInstance Create(ARDB.Document doc, IList<ARDB.ElementId> members, ARDB.ElementId categoryId, ARDB.AssemblyInstance template)
    {
      var assembly = ARDB.AssemblyInstance.Create(doc, members, categoryId);
      assembly.CopyParametersFrom(template, ExcludeUniqueProperties);
      return assembly;
    }

    ARDB.AssemblyInstance Reconstruct(ARDB.AssemblyInstance assembly, ARDB.Document doc, IList<ARDB.ElementId> members, ARDB.ElementId categoryId, ARDB.AssemblyInstance template)
    {
      if (!Reuse(assembly, members, categoryId, template))
      {
        assembly = assembly.ReplaceElement
        (
          Create(doc, members, categoryId, template),
          ExcludeUniqueProperties
        );
      }

      return assembly;
    }
  }
}
