using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel.Parameters
{
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class Param_ColorRGBA : GH_PersistentParam<GH_ColorRGBA>
  {
    public override Guid ComponentGuid => new Guid("DF028C15-D188-42B6-B50A-2EF7A5D5B4F0");
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    static readonly Guid ColourParamComponentGuid = new Guid("203A91C3-287A-43b6-A9C5-EBB96240A650");
    protected override Bitmap Icon => Instances.ComponentServer.EmitObjectIcon(ColourParamComponentGuid);

    public Param_ColorRGBA() : base("Colour", "Col", "Contains a collection of RGB colours defined by 4 double floating point values.", "Params", "Primitive") { }

    protected override GH_ColorRGBA PreferredCast(object data)
    {
      if (data is Rhino.Display.ColorRGBA color)
        return new GH_ColorRGBA(color);

      return null;
    } 

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<GH_ColorRGBA> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref GH_ColorRGBA value) => GH_GetterResult.cancel;
  }
}
