using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class Param<T> : GH_Param<T>
    where T : class, IGH_Goo
  {
    protected sealed override Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                              ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);

    public virtual void SetInitCode(string code) => NickName = code;

    protected Param(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory, GH_ParamAccess.item)
    { }
  }

  public abstract class ParamWithPreview<T> : Param<T>, IGH_PreviewObject
    where T : class, IGH_Goo
  {
    protected ParamWithPreview(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }
}
