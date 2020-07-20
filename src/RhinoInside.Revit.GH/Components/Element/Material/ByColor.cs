using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class MaterialByColor : TransactionComponent
  {
    public override Guid ComponentGuid => new Guid("273FF43D-B771-4EB7-A66D-5DA5F7F2731E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "C";

    public MaterialByColor()
    : base("Add Color Material", "Material", "Quickly create a new Revit material from color", "Revit", "Material")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddColourParameter("Color", "C", "Material color", GH_ParamAccess.item, System.Drawing.Color.White);
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

      var color = default(System.Drawing.Color);
      if (!DA.GetData("Color", ref color))
        return;

      string name = color.A == 255 ?
                    $"RGB {color.R} {color.G} {color.B}" :
                    $"RGB {color.R} {color.G} {color.B} {color.A}";

      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Material)))
        material = collector.Where(x => x.Name == name).Cast<DB.Material>().FirstOrDefault();

      bool materialIsNew = material is null;
      if (materialIsNew)
        material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;

      if (material.MaterialClass != "RGB")
        material.MaterialClass = "RGB";

      if (material.MaterialCategory != "RGB")
        material.MaterialCategory = "RGB";

      var newColor = color.ToColor();
      if (newColor.Red != material.Color.Red || newColor.Green != material.Color.Green || newColor.Blue != material.Color.Blue)
        material.Color = newColor;

      var newTransparency = (int) Math.Round((255 - color.A) * 100.0 / 255.0);
      if(material.Transparency != newTransparency)
        material.Transparency = newTransparency;

      var newShininess = (int) Math.Round(0.5 * 128.0);
      if (newShininess != material.Shininess)
        material.Shininess = newShininess;

      DA.SetData("Material", material);
    }
  }
}
