using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  public class SpatialElement : GraphicalElementT<Types.SpatialElement, ARDB.SpatialElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("8774ACF3-7B77-474F-B12B-03D4CBBC3C15");
    protected override string IconTag => string.Empty;

    public SpatialElement() : base
    (
      name: "Spatial Element",
      nickname: "Spatial Element",
      description: "Contains a collection of Revit spatial elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Surface" }
    );
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class AreaElement : GraphicalElementT<Types.AreaElement, ARDB.Area>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("66AAAE96-BA85-4DC7-A188-AC213FAD3176");

    public AreaElement() : base
    (
      name: "Area",
      nickname: "Area",
      description: "Contains a collection of Revit area elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Append("Surface");

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var AreaId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Area);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(AreaId),
        Revit.ActiveUIApplication.CanPostCommand(AreaId),
        false
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class RoomElement : GraphicalElementT<Types.RoomElement, ARDB.Architecture.Room>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1E6825B6-4A7A-44EA-BC70-A9A110963E17");

    public RoomElement() : base
    (
      name: "Room",
      nickname: "Room",
      description: "Contains a collection of Revit room elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Surface", "Brep" }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var RoomId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Room);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(RoomId),
        Revit.ActiveUIApplication.CanPostCommand(RoomId),
        false
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class SpaceElement : GraphicalElementT<Types.SpaceElement, ARDB.Mechanical.Space>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("30473B1D-6226-45CE-90A7-5F8E1E1DCBE3");

    public SpaceElement() : base
    (
      name: "Space",
      nickname: "Space",
      description: "Contains a collection of Revit space elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Surface", "Brep" }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var SpaceId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Space);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(SpaceId),
        Revit.ActiveUIApplication.CanPostCommand(SpaceId),
        false
      );
    }
    #endregion
  }
}
