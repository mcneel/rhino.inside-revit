using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.11")]
  public class AddImageType : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("09BD0AA8-52D7-4C2B-B7B1-C66304C939AF");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddImageType() : base
    (
      name: "Add ImageType",
      nickname: "ImageType",
      description: "Given the path, it adds an image type to the given View",
      category: "Revit",
      subCategory: "Element"
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
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the image",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "ImageType";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

#if REVIT_2022
      ReconstructElement<ARDB.ImageType>
      (
        doc.Value, _Output_, imgInstance =>
        {
          // Input
          if (!Params.GetData(DA, "Path", out string path)) return null;

          // Compute
          imgInstance = Reconstruct(imgInstance, doc.Value, path);

          DA.SetData(_Output_, imgInstance);
          return imgInstance;
        }
      );
#endif
    }

    bool Reuse(ARDB.ImageType imageType, ARDB.Document doc, string path)
    {
      if (imageType is null) return false;
      if (imageType.Path != path) return false;

      return true;
    }

    ARDB.ImageType Reconstruct(ARDB.ImageType imageType, ARDB.Document doc, string path)
    {
      if (!Reuse(imageType, doc, path))
      {
        imageType = ARDB.ImageType.Create(doc, new ARDB.ImageTypeOptions(path, false, ARDB.ImageTypeSource.Import));
      }
      return imageType;
    }
  }
}
