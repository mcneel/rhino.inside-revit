using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
  public class MaterialByName : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("3AEDBA3C-1B77-4C52-A7FC-7CA7095F730E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public MaterialByName() : base
    (
      name: "Add Material",
      nickname: "Material",
      description: "Create a Revit material by name",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Material Name",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Material",
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
        new Parameters.Material()
        {
          Name = _Material_,
          NickName = _Material_.Substring(0, 1),
          Description = $"Output {_Material_}",
        }
      ),
    };

    const string _Material_ = "Material";
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.MATERIAL_NAME
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      Params.TryGetData(DA, "Template", out DB.Material template);

      // Previous Output
      Params.ReadTrackedElement(_Material_, doc.Value, out DB.Material material);

      StartTransaction(doc.Value);
      {
        material = Reconstruct(material, doc.Value, name, template);

        Params.WriteTrackedElement(_Material_, doc.Value, material);
        DA.SetData(_Material_, material);
      }
    }

    bool Reuse(DB.Material material, string name, DB.Material template)
    {
      if (material is null) return false;
      if (name is object) material.Name = name;

      material.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    DB.Material Create(DB.Document doc, string name, DB.Material template)
    {
      var material = default(DB.Material);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? _Material_;

        name = doc.GetNamesakeElements
        (
          typeof(DB.Material), name, categoryId: DB.BuiltInCategory.OST_Materials
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template is object)
      {
        if (doc.Equals(template.Document))
        {
          material = template.Duplicate(name);
        }
        else
        {
          var ids = DB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new DB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          material = ids.Select(x => doc.GetElement(x)).OfType<DB.Material>().FirstOrDefault();
          material.Name = name;
        }
      }

      if (material is null)
        material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;

      return material;
    }

    DB.Material Reconstruct(DB.Material material, DB.Document doc, string name, DB.Material template)
    {
      if (!Reuse(material, name, template))
      {
        material = material.ReplaceElement
        (
          Create(doc, name, template),
          ExcludeUniqueProperties
        );
      }

      return material;
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Since 2020-09-24")]
  public class MaterialByName : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("0D9F07E2-3A21-4E85-96CC-BC0E6A607AF1");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;

    public MaterialByName() : base
    (
      name: "Add Material",
      nickname: "Material",
      description: "Create a new Revit material by name and color",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>
      (
        name: "Document",
        nickname: "DOC",
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Param_String>
      (
        name: "Name",
        nickname: "N"
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Override",
        nickname: "O",
        description: "Override Material",
        defaultValue: false
      ),
      ParamDefinition.Create<Param_Colour>
      (
        name: "Color",
        nickname: "C",
        description: "Material color",
        optional: true,
        defaultValue: System.Drawing.Color.White
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
        ParamDefinition.Create<Parameters.Material>
        (
          name: "Material",
          nickname: "M"
        ),
      };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      var overrideMaterial = false;
      if (!DA.GetData("Override", ref overrideMaterial)) return;

      var name = string.Empty;
      if (!DA.GetData("Name", ref name)) return;

      var color = System.Drawing.Color.Empty;
      DA.GetData("Color", ref color);

      // Query for an existing material with the requested name
      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc.Value))
      {
        material = collector.OfClass(typeof(DB.Material)).
                WhereParameterEqualsTo(DB.BuiltInParameter.MATERIAL_NAME, name).
                FirstElement() as DB.Material;
      }

      StartTransaction(doc.Value);

      bool materialIsNew = material is null;
      if (materialIsNew)
        material = doc.Value.GetElement(DB.Material.Create(doc.Value, name)) as DB.Material;

      if (materialIsNew || overrideMaterial)
      {
        material.UseRenderAppearanceForShading = color.IsEmpty;
        if (!color.IsEmpty)
        {
          var newColor = color.ToColor();
          if (newColor.Red != material.Color.Red || newColor.Green != material.Color.Green || newColor.Blue != material.Color.Blue)
            material.Color = newColor;

          var newTransparency = (int) Math.Round((255 - color.A) * 100.0 / 255.0);
          if (material.Transparency != newTransparency)
            material.Transparency = newTransparency;

          var newShininess = (int) Math.Round(0.5 * 128.0);
          if (newShininess != material.Shininess)
            material.Shininess = newShininess;
        }
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Material '{name}' already exist!");

      DA.SetData("Material", material);
    }
  }
}
