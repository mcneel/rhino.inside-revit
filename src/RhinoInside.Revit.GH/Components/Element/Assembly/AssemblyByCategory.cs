using System;
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  public class AssemblyByCategory : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("6915b697-f10d-4bc8-8faa-f25438f393a8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AssemblyByCategory() : base
    (
      name: "Add Assembly",
      nickname: "Assembly",
      description: "Create a new assembly instance",
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
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = $"Category of the new assembly"
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Members",
          NickName = "M",
          Description = $"Elements to be members of the new assembly",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = $"Name of the new assembly",
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
          Description = $"Template sheet (only sheet parameters are copied)",
          Optional = true
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

    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.ASSEMBLY_NAMING_CATEGORY,
      DB.BuiltInParameter.ASSEMBLY_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // active document
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      var category = default(DB.Category);
      if (!DA.GetData("Category", ref category))
        return;

      var members = new List<DB.Element>();
      if (!DA.GetDataList("Members", members))
        return;

      Params.TryGetData(DA, "Name", out string name);
      Params.TryGetData(DA, "Template", out DB.AssemblyInstance template);

      // find any tracked sheet
      Params.ReadTrackedElement(_Assembly_.name, doc.Value, out DB.AssemblyInstance assembly);

      // update, or create
      StartTransaction(doc.Value);
      {
        assembly = Reconstruct(assembly, doc.Value, new AssemblyHandler(category, members)
        {
          Name = name,
          Template = template
        });

        Params.WriteTrackedElement(_Assembly_.name, doc.Value, assembly);
        DA.SetData(_Assembly_.name, assembly);
      }
    }

    bool Reuse(DB.AssemblyInstance assembly, AssemblyHandler data)
    {
      //bool rejected;

      //// if categories are different, do not use
      //rejected = assembly.NamingCategoryId is DB.ElementId categoryId
      //    && !categoryId.Equals(data.CategoryId);

      //if (rejected)
      //{
      //  // let's change the sheet number so other sheets can be created with same id
      //  data.ExpireAssembly(assembly);
      //  return false;
      //}

      assembly.CopyParametersFrom(data.Template, ExcludeUniqueProperties);
      data.UpdateAssembly(assembly);
      return true;
    }

    DB.AssemblyInstance Create(DB.Document doc, AssemblyHandler data)
    {
      var assembly = data.CreateAssembly(doc);
      assembly.CopyParametersFrom(data.Template, ExcludeUniqueProperties);
      return assembly;
    }

    DB.AssemblyInstance Reconstruct(DB.AssemblyInstance assembly, DB.Document doc, AssemblyHandler data)
    {
      if (assembly is null || !Reuse(assembly, data))
        assembly = assembly.ReplaceElement
        (
          Create(doc, data),
          ExcludeUniqueProperties
        );

      return assembly;
    }
  }
}
