using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddText : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("49ACC84C-793F-40A4-A1BF-2D8BAFBB3604");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddText() : base
    (
      name: "Add Text",
      nickname: "Text",
      description: "Given a content and a point, it adds a text to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific text",
        }
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Text",
          NickName = "T",
          Description = "Text to add to the input view",
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place a specific text",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Width",
          NickName = "W",
          Description = "Width of the text in paper space",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given region"
          // built-in category del type?
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
        }
      )
    };

    const string _Output_ = "Text";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.TextNote>
      (
        view.Document, _Output_, (textNote) =>
        {
          // Input
          if (!Params.GetData(DA, "Text", out string text)) return null;
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Width", out double? width)) return null;
          if (!Params.GetData(DA, "Type", out ARDB.TextNoteType type)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.ThreeD ||
            view.ViewType is ARDB.ViewType.Schedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          // Compute
          textNote = Reconstruct(textNote, view, point.Value.ToXYZ(), text, type.Id, width.HasValue ? width.Value : double.MinValue);

          DA.SetData(_Output_, textNote);
          return textNote;
        }
      );
    }

    bool Reuse(ARDB.TextNote textNote, ARDB.View view, ARDB.XYZ point, string text, ARDB.ElementId typeId, double width)
    {
      if (textNote is null) return false;

      if (textNote.OwnerViewId != view.Id) return false;
      if (textNote.GetTypeId() != typeId) return false;

      var plane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
      var projectedPoint = plane.ClosestPoint(point.ToPoint3d());
      if (!textNote.Coord.IsAlmostEqualTo(projectedPoint.ToXYZ()))
        textNote.Coord = projectedPoint.ToXYZ();

      if (textNote.Text != text)
        textNote.Text = text;

      if (width != double.MinValue)
        if (textNote.Width != width)
          textNote.Width = width;
      
      return true;
    }

    ARDB.TextNote Create(ARDB.View view, ARDB.XYZ point, string text, ARDB.ElementId typeId, double width)
    {
      var opts = new ARDB.TextNoteOptions(typeId);

      if (width == double.MinValue)
        return ARDB.TextNote.Create(view.Document, view.Id, point, text, opts);
      else
        return ARDB.TextNote.Create(view.Document, view.Id, point, width, text, opts);
    }

    ARDB.TextNote Reconstruct(ARDB.TextNote textNote, ARDB.View view, ARDB.XYZ point, string text, ARDB.ElementId typeId, double width)
    {
      if (!Reuse(textNote, view, point, text, typeId, width))
        textNote = Create(view, point, text, typeId, width);

      return textNote;
    }
  }
}

