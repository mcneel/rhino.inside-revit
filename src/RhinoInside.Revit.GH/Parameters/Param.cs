using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class Param<T> : GH_Param<T>
    where T : class, IGH_Goo
  {
    protected override sealed Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                              ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);

    public virtual void SetInitCode(string code) => NickName = code;

    protected Param(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory, GH_ParamAccess.item)
    { }
  }
}
