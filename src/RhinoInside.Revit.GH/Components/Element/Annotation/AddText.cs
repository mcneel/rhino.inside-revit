using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
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
        new Param_Number
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Base line text rotation",
          Optional = true,
          AngleParameter = true,
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
        view.Document, _Output_, textNote =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;
          if (!Params.GetData(DA, "Content", out string text)) return null;
          if (!Params.TryGetData(DA, "Width", out double? width)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.TextNoteType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.TextNoteType)) return null;

          if (rotation.HasValue && Params.Input<Param_Number>("Rotation")?.UseDegrees == true)
            rotation = Rhino.RhinoMath.ToRadians(rotation.Value);

          if
          (
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support text notes creation", view);

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

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.RightDirection.ToVector3d(), view.UpDirection.ToVector3d());
          if (view.ViewType != ARDB.ViewType.ThreeD)
            point = viewPlane.ClosestPoint(point.Value);

          // Compute
          textNote = Reconstruct
          (
            textNote,
            view,
            point.Value.ToXYZ(),
            rotation ?? 0.0,
            text,
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
      string text, double width,
      ARDB.TextNoteType type
    )
    {
      if (textNote is null) return false;
      if (textNote.OwnerViewId != view.Id) return false;
      if (textNote.GetTypeId() != type.Id) textNote.ChangeTypeId(type.Id);
      if (textNote.IsTextWrappingActive && double.IsNaN(width)) return false;

      if (!textNote.Coord.IsAlmostEqualTo(point))
        textNote.Coord = point;

      var currentRotation = textNote.BaseDirection.AngleOnPlaneTo(view.RightDirection, view.ViewDirection);
      if (!GeometryTolerance.Internal.AlmostEqualAngles(currentRotation, rotation))
      {
        var pinned = textNote.Pinned;
        textNote.Pinned = false;
        using (var axis = ARDB.Line.CreateUnbound(textNote.Coord, -view.ViewDirection))
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
      string text, double width,
      ARDB.TextNoteType type
    )
    {
      using (var opts = new ARDB.TextNoteOptions(type.Id))
      {
        opts.HorizontalAlignment = ARDB.HorizontalTextAlignment.Center;
#if REVIT_2019
        opts.VerticalAlignment = ARDB.VerticalTextAlignment.Middle;
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
      double width,
      ARDB.TextNoteType type
    )
    {
      if (!Reuse(textNote, view, point, rotation, text, width, type))
        textNote = Create(view, point, rotation, text, width, type);

      if (textNote.LeaderLeftAttachment != ARDB.LeaderAtachement.Midpoint)
        textNote.LeaderLeftAttachment = ARDB.LeaderAtachement.Midpoint;
      if (textNote.LeaderRightAttachment != ARDB.LeaderAtachement.Midpoint)
        textNote.LeaderRightAttachment = ARDB.LeaderAtachement.Midpoint;
      if (textNote.HorizontalAlignment != ARDB.HorizontalTextAlignment.Center)
        textNote.HorizontalAlignment = ARDB.HorizontalTextAlignment.Center;
#if REVIT_2019
      if (textNote.VerticalAlignment != ARDB.VerticalTextAlignment.Middle)
        textNote.VerticalAlignment = ARDB.VerticalTextAlignment.Middle;
#endif

      return textNote;
    }
  }
}

