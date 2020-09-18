using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FlipUnflipElement : TransactionComponent
  {
    public override Guid ComponentGuid => new Guid("b0f068d0-f160-4f80-86b9-67b7830f1cc2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "FE";

    public FlipUnflipElement()
    : base(
      name: "Flip/Unflip Element",
      nickname: "FE",
      description: "Flips/Unflips facing/orientation of hand of elements from Revit document",
      category: "Revit",
      subCategory: "Element"
      )
    {
    }

    // Note:
    // FromRoomToRoom, Workplane are also options but API coverage
    // is pretty bad. Flipped status can not be checked either
    // These two are not implemented
    public enum FlipTarget { Any, Facing, Hand }

    private FlipTarget _target = FlipTarget.Any;

    public FlipTarget Target
    {
      get { return _target; }
      set
      {
        _target = value;
        Message = _target > FlipTarget.Any ? _target.ToString() : null;
        ExpireSolution(true);
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendItem(menu, "Just Flip", Menu_SetFlipAny, true, FlipTarget.Any == Target);
      Menu_AppendItem(menu, "Flip Hand", Menu_SetFlipHand, true, FlipTarget.Hand == Target);
      Menu_AppendItem(menu, "Flip Facing / Orientation", Menu_SetFlipFacing, true, FlipTarget.Facing == Target);
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Element",
        nickname: "E",
        description: "Element to query for its identity",
        access: GH_ParamAccess.item
      );

      manager[manager.AddBooleanParameter(
        name: "Flip",
        nickname: "F",
        description: "Flipped or not (defaults to false)",
        access: GH_ParamAccess.item
      )].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Element",
        nickname: "E",
        description: "Element to query for its identity",
        access: GH_ParamAccess.item
      );

      manager.AddBooleanParameter(
        name: "Flip",
        nickname: "F",
        description: "Flipped or not (defaults to false)",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      bool flipState = false;
      bool apply = DA.GetData("Flip", ref flipState);

      bool flipped = false;
      switch (element)
      {
        // flipping family instances (facing or hand)
        case DB.FamilyInstance famInst:
          if (FlipTarget.Facing == _target && famInst.CanFlipFacing)
          {
            if (apply && ((flipState && !famInst.FacingFlipped)
                 || (!flipState && famInst.FacingFlipped)))
              famInst.flipFacing();
            flipped = famInst.FacingFlipped;
          }
          else if (FlipTarget.Hand == _target && famInst.CanFlipHand)
          {
            if (apply && ((flipState && !famInst.HandFlipped)
                 || (!flipState && famInst.HandFlipped)))
              famInst.flipHand();
            flipped = famInst.HandFlipped;
          }
          break;

        // flipping option is only available on some elements
        // need to reflect over and test the availability
        case DB.Element elmntInst:
          var flippMethod = elmntInst.GetType().GetMethod("Flip");
          var flippedProp = elmntInst.GetType().GetProperty("Flipped");
          if (flippMethod != null && flippedProp != null)
          {
            bool alreadyFlipped = (bool) flippedProp.GetValue(elmntInst);
            if (apply && ((flipState && !alreadyFlipped)
                 || (!flipState && alreadyFlipped)))
              flippMethod.Invoke(elmntInst, new object[] { });
            flipped = (bool) flippedProp.GetValue(elmntInst);
          }
          break;
      }

      DA.SetData("Element", element);
      DA.SetData("Flip", flipped);
    }

    void Menu_SetFlipAny(object sender, EventArgs e)
    {
      Target = FlipTarget.Any;
      RecordUndoEvent($"Change Element Flip {Target}");
    }

    void Menu_SetFlipFacing(object sender, EventArgs e)
    {
      Target = FlipTarget.Facing;
      RecordUndoEvent($"Change Element Flip {Target}");
    }

    void Menu_SetFlipHand(object sender, EventArgs e)
    {
      Target = FlipTarget.Hand;
      RecordUndoEvent($"Change Element Flip {Target}");
    }

    public override bool Write(GH_IO.Serialization.GH_IWriter writer)
    {
      writer.SetInt32("FlipTarget", (int) Target);
      return base.Write(writer);
    }

    public override bool Read(GH_IO.Serialization.GH_IReader reader)
    {
      Target = (FlipTarget) reader.GetInt32("FlipTarget");
      return base.Read(reader);
    }
  }
}
