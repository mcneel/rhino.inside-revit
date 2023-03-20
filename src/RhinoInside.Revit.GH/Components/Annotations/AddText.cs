using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8", updated: "1.12")]
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
          Description = "View to add the text",
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place the text",
        }
      ),
      new ParamDefinition
      (
        new Param_Angle
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Base line text rotation",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Content",
          NickName = "C",
          Description = "Text content",
        }.SetDefaultVale("ABC")
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Width",
          NickName = "W",
          Description = "Width of the text in paper space",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.HorizontalTextAlignment>
        {
          Name = "Horizontal Align",
          NickName = "HA",
          Description = "Horizontal text alignment",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.VerticalTextAlignment>
        {
          Name = "Vertical Align",
          NickName = "VA",
          Description = "Vertical text alignment",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given text",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_TextNotes
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.TextElement()
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
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      ReconstructElement<ARDB.TextNote>
      (
        view.Document, _Output_, textNote =>
        {
          // Input
          if (!view.Value.IsGraphicalView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support text notes creation", view);
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;
          if (!Params.GetData(DA, "Content", out string text)) return null;
          if (!Params.TryGetData(DA, "Width", out double? width)) return null;
          if (!Params.TryGetData(DA, "Horizontal Align", out ARDB.HorizontalTextAlignment? horizontalAlignment)) return null;
          if (!Params.TryGetData(DA, "Vertical Align", out ARDB.VerticalTextAlignment? verticalAlignment)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.TextNoteType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.TextNoteType)) return null;

          if (rotation.HasValue && Params.Input<Param_Number>("Rotation")?.UseDegrees == true)
            rotation = Rhino.RhinoMath.ToRadians(rotation.Value);

          width = GeometryEncoder.ToInternalLength(width ?? double.NaN);
          var min = ARDB.TextElement.GetMinimumAllowedWidth(view.Document, type.Id);
          var max = ARDB.TextElement.GetMaximumAllowedWidth(view.Document, type.Id);

          if (width == 0.0) width = double.NaN;
          else if (width < min)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Minimum allowed width for type '{type.Name}' is {GeometryDecoder.ToModelLength(min)} {GH_Format.RhinoUnitSymbol()}");
            width = min;
          }
          else if(width > max)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Maximum allowed width for type '{type.Name}' is {GeometryDecoder.ToModelLength(max)} {GH_Format.RhinoUnitSymbol()}");
            width = max;
          }

          var viewPlane = view.Location;
          if (view.Value.ViewType != ARDB.ViewType.ThreeD)
            point = viewPlane.ClosestPoint(point.Value);

          // Compute
          textNote = Reconstruct
          (
            textNote,
            view.Value,
            point.Value.ToXYZ(),
            rotation ?? 0.0,
            text,
            horizontalAlignment ?? ARDB.HorizontalTextAlignment.Center,
            verticalAlignment ?? ARDB.VerticalTextAlignment.Middle,
            width.Value,
            type
          );

          DA.SetData(_Output_, textNote);
          return textNote;
        }
      );
    }

    bool Reuse
    (
      ARDB.TextNote textNote, ARDB.View view,
      ARDB.XYZ point, double rotation,
      string text,
      ARDB.HorizontalTextAlignment horizontalAlignment,
      ARDB.VerticalTextAlignment verticalAlignment,
      double width,
      ARDB.TextNoteType type
    )
    {
      if (textNote is null) return false;
      if (textNote.OwnerViewId != view.Id) return false;
      if (textNote.GetTypeId() != type.Id) textNote.ChangeTypeId(type.Id);
      if (textNote.IsTextWrappingActive && double.IsNaN(width)) return false;

      if (textNote.LeaderLeftAttachment != ARDB.LeaderAtachement.Midpoint)
        textNote.LeaderLeftAttachment = ARDB.LeaderAtachement.Midpoint;
      if (textNote.LeaderRightAttachment != ARDB.LeaderAtachement.Midpoint)
        textNote.LeaderRightAttachment = ARDB.LeaderAtachement.Midpoint;

      if (textNote.HorizontalAlignment != horizontalAlignment)
        textNote.HorizontalAlignment = horizontalAlignment;
#if REVIT_2019
      if (textNote.VerticalAlignment != verticalAlignment)
        textNote.VerticalAlignment = verticalAlignment;
#endif

      if (!textNote.Coord.AlmostEqualPoints(point))
        textNote.Coord = point;

      var currentRotation = view.RightDirection.AngleOnPlaneTo(textNote.BaseDirection, view.ViewDirection);
      if (!GeometryTolerance.Internal.AlmostEqualAngles(currentRotation, rotation))
      {
        var pinned = textNote.Pinned;
        textNote.Pinned = false;
        using (var axis = ARDB.Line.CreateUnbound(textNote.Coord, view.ViewDirection))
          ARDB.ElementTransformUtils.RotateElement(textNote.Document, textNote.Id, axis, rotation - currentRotation);
        textNote.Pinned = pinned;
      }

      if (text[text.Length - 1] != '\r')
        text += '\r';

      if (textNote.Text != text)
        textNote.Text = text;

      if (!double.IsNaN(width) && textNote.Width != width)
        textNote.Width = width;
      
      return true;
    }

    ARDB.TextNote Create
    (
      ARDB.View view,
      ARDB.XYZ point, double rotation,
      string text,
      ARDB.HorizontalTextAlignment horizontalAlignment,
      ARDB.VerticalTextAlignment verticalAlignment,
      double width,
      ARDB.TextNoteType type
    )
    {
      using (var opts = new ARDB.TextNoteOptions(type.Id))
      {
        opts.HorizontalAlignment = horizontalAlignment;
#if REVIT_2019
        opts.VerticalAlignment = verticalAlignment;
#endif
        opts.Rotation = rotation;

        if (double.IsNaN(width))
          return ARDB.TextNote.Create(view.Document, view.Id, point, text, opts);
        else
          return ARDB.TextNote.Create(view.Document, view.Id, point, width, text, opts);
      }
    }

    ARDB.TextNote Reconstruct
    (
      ARDB.TextNote textNote,
      ARDB.View view,
      ARDB.XYZ point,
      double rotation,
      string text,
      ARDB.HorizontalTextAlignment horizontalAlignment,
      ARDB.VerticalTextAlignment verticalAlignment,
      double width,
      ARDB.TextNoteType type
    )
    {
      if (!Reuse(textNote, view, point, rotation, text, horizontalAlignment, verticalAlignment, width, type))
        textNote = Create(view, point, rotation, text, horizontalAlignment, verticalAlignment, width, type);

      return textNote;
    }
  }
}

