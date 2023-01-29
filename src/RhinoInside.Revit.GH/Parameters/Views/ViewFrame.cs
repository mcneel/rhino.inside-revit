using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.12")]
  public class ViewFrame : GH_PersistentParam<Types.ViewFrame>, IGH_PreviewObject
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("0CF2E522-D8A2-42E9-AA4F-1D532154F6DD");

    static readonly Guid DataParameterComponentGuid = new Guid("{8EC86459-BF01-4409-BAEE-174D0D2B13D0}");
    static readonly Guid Make2DParallelViewComponentGuid = new Guid("{3FC08088-D75D-43BC-83CC-7A654F156CB7}");
    static readonly Guid ViewportFrameParameterComponentGuid = new Guid("{7069208C-C471-4B82-BAE6-E938F16DACB0}");
    protected override Bitmap Icon
    {
      get
      {
        if (Instances.ComponentServer.EmitObject(Make2DParallelViewComponentGuid) is IGH_Component component)
        {
          foreach (var param in component.Params.Output)
          {
            if (param.ComponentGuid == ViewportFrameParameterComponentGuid)
              return param.Icon_24x24;
          }
        }

        return Instances.ComponentServer.EmitObjectIcon(DataParameterComponentGuid);
      }
    }

    public ViewFrame() : base
    (
      name: "View Frame",
      nickname: "V-Frame",
      description: "Contains a collection of view frames",
      category: "Params",
      subcategory: "Geometry"
    )
    { }

    protected override Types.ViewFrame InstantiateT() => new Types.ViewFrame();
    protected override Types.ViewFrame PreferredCast(object data) => data is ViewportInfo vport ? new Types.ViewFrame(vport) : null;

    #region UI
    protected override GH_GetterResult Prompt_Singular(ref Types.ViewFrame value) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Plural(ref List<Types.ViewFrame> values) => GH_GetterResult.cancel;

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override void Menu_AppendManageCollection(ToolStripDropDown menu) { }
    #endregion

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }

}
