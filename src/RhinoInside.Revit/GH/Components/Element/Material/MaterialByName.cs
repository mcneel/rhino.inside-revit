using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class MaterialByName : TransactionComponent
  {
    public override Guid ComponentGuid => new Guid("0D9F07E2-3A21-4E85-96CC-BC0E6A607AF1");
    protected override string IconTag => "N";

    public MaterialByName()
    : base("Material.ByName", "ByName", string.Empty, "Revit", "Materials")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBooleanParameter("Override", "O", "Override Material", GH_ParamAccess.item, false);

      manager.AddTextParameter("Name", "N", string.Empty, GH_ParamAccess.item);
      manager[manager.AddColourParameter("Color", "C", "Material color", GH_ParamAccess.item, System.Drawing.Color.White)].Optional = true;
      //manager[manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_OGLShader(), "Shader", "S", "Material shading attributes", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Material", "M", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var doc = Revit.ActiveDBDocument;

      var overrideMaterial = false;
      if (!DA.GetData("Override", ref overrideMaterial))
        return;

      var name = string.Empty;
      if (!DA.GetData("Name", ref name))
        return;

      var color = System.Drawing.Color.Empty;
      DA.GetData("Color", ref color);

      //var shader = default(GH_Material);
      //if (!DA.GetData("Shader", ref shader))
      //  return;

      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Material)))
        material = collector.Where(x => x.Name == name).Cast<DB.Material>().FirstOrDefault();

      bool materialIsNew = material is null;
      if (materialIsNew)
        material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;

      if (materialIsNew || overrideMaterial)
      {
        material.UseRenderAppearanceForShading = color.IsEmpty;
        if (!color.IsEmpty)
        {
          var newColor = color.ToHost();
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
