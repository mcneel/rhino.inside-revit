using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material.Obsolete
{
  [Obsolete("Since 2020-09-25")]
  public class MaterialByColor : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("273FF43D-B771-4EB7-A66D-5DA5F7F2731E");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;

    public MaterialByColor() : base
    (
      name: "Create Material (Color)",
      nickname: "Material",
      description: "Quickly create a new Revit material from color",
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
        relevance: ParamVisibility.Voluntary
      ),
      ParamDefinition.Create<Param_Colour>
      (
        name: "Color",
        nickname: "C",
        description: "Material color",
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

      var color = System.Drawing.Color.Empty;
      if (!DA.GetData("Color", ref color)) return;

      StartTransaction(doc.Value);

      string name = color.A == 255 ?
                    $"RGB {color.R} {color.G} {color.B}" :
                    $"RGB {color.R} {color.G} {color.B} {color.A}";

      // Query for an existing material with the requested name
      var material = default(DB.Material);
      using (var collector = new DB.FilteredElementCollector(doc.Value))
      {
        material = collector.OfClass(typeof(DB.Material)).
        WhereParameterEqualsTo(DB.BuiltInParameter.MATERIAL_NAME, name).
        FirstElement() as DB.Material;
      }

      if (material is null)
        material = doc.Value.GetElement(DB.Material.Create(doc.Value, name)) as DB.Material;

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
