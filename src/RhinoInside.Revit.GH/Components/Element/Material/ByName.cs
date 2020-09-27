using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class MaterialByName : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("3AEDBA3C-1B77-4C52-A7FC-7CA7095F730E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public MaterialByName() : base
    (
      name: "Create Material",
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
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Material Name",
          Access = GH_ParamAccess.item,
        }
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Material",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Material",
          NickName = "M",
          Description = "Material",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if(!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var name = default(string);
      if (!DA.GetData("Name", ref name) || name == string.Empty)
        return;

      // Query for an existing material with the requested name
      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        material = collector.OfClass(typeof(DB.Material)).
          WhereParameterEqualsTo(DB.BuiltInParameter.MATERIAL_NAME, name).
          FirstElement() as DB.Material;
      }

      if (material is null)
      {
        // Create new material
        StartTransaction(doc);

        // Try to duplicate template
        var template = default(DB.Material);
        if (DA.GetData("Template", ref template))
        {
          if (template.Document.Equals(doc))
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
              DB.Transform.Identity,
              new DB.CopyPasteOptions()
            );

            material = ids.Select(x => doc.GetElement(x)).
              OfType<DB.Material>().
              FirstOrDefault();
          }
        }

        if(material is null)
          material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;
      }

      DA.SetData("Material", material);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Since 2020-09-24")]
  public class MaterialByName : TransactionComponent
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

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Override", "O", "Override Material", GH_ParamAccess.item, false);
      manager[manager.AddColourParameter("Color", "C", "Material color", GH_ParamAccess.item, System.Drawing.Color.White)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Material", "M", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var doc = Revit.ActiveDBDocument;
      if (doc is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to access the active Revit doument");
        return;
      }

      var overrideMaterial = false;
      if (!DA.GetData("Override", ref overrideMaterial))
        return;

      var name = string.Empty;
      if (!DA.GetData("Name", ref name))
        return;

      var color = System.Drawing.Color.Empty;
      DA.GetData("Color", ref color);

      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        material = collector.OfClass(typeof(DB.Material)).
               WhereParameterEqualsTo(DB.BuiltInParameter.MATERIAL_NAME, name).
               FirstElement() as DB.Material;
      }

      bool materialIsNew = material is null;
      if (materialIsNew)
        material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;

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
