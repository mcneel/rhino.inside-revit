using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.Interaction;
using Grasshopper.GUI.HTML;
using Grasshopper.Kernel;
using System.Diagnostics;

///////////////////////////////////////////////////////////////////////////////
//                                                                           //
// NOTE: At some point this code may end up in the Grasshopper code base.    //
//                                                                           //
///////////////////////////////////////////////////////////////////////////////

namespace Grasshopper.External.Special
{
  public class TriggerComponent : GH_ActiveObject, IGH_InstanceGuidDependent
  {
    public TriggerComponent() : base
    (
      sName: "Trigger",
      sAbbreviation: "Trigger",
      sDescription: "Provides a mechanism for updating solutions at user request.",
      sCategory: "Params",
      sSubCategory: "Util"
    )
    { }

    public static Guid TriggerComponentID => new Guid("11B807B5-F088-46C3-9895-693EA3E54DBE");
    public override Guid ComponentGuid => TriggerComponentID;
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override Bitmap Icon => ClassIcon;
    static /*readonly*/ Bitmap ClassIcon => RhinoInside.Revit.ImageBuilder.BuildIcon
    (
      (graphics, bounds) =>
      {
        var iconBounds = new RectangleF(bounds.Location, bounds.Size);
        iconBounds.Inflate(-0.5f, -0.5f);
        using (var capsule = GH_Capsule.CreateCapsule(iconBounds, GH_Palette.Grey))
        {
          capsule.Render(graphics, false, false, false);
          ComponentAttributes.RenderIcon(graphics, iconBounds, Color.Black);
        }
      }
    );

    public override bool IconCapableUI => false;

    public override bool DependsOn(IGH_ActiveObject PotentialSource) => false;
    public override bool IsDataProvider => false;

    HashSet<Guid> Targets { get; set; } = new HashSet<Guid>();

    public int ExpireTargets()
    {
      var count = 0;
      if (OnPingDocument() is GH_Document ghDocument)
      {
        foreach (var target in Targets)
        {
          if (ghDocument.FindObject(target, true) is IGH_DocumentObject docObject)
          {
            docObject.ExpireSolution(false);
            count++;
          }
        }
      }

      return count;
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, "Enabled", new EventHandler(Menu_Enabled), true, !Locked);
      Menu_AppendSeparator(menu);

      ToolStripMenuItem toolStripMenuItem1 = Menu_AppendItem(menu, "Remove Target", null, Targets.Count > 0);
      foreach (var target in Targets)
      {
        if (OnPingDocument().FindObject(target, true) is IGH_DocumentObject ghDocumentObject)
        {
          var item = Menu_AppendItem(toolStripMenuItem1.DropDown, $"{ghDocumentObject.Name} ({ghDocumentObject.NickName})", Menu_RemoveTarget, ghDocumentObject.Icon_24x24);
          item.Tag = target;
        }
      }
    }

    private void Menu_RemoveTarget(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem toolStripMenuItem)
      {
        RecordUndoEvent("Remove Target");
        Targets.Remove((Guid) toolStripMenuItem.Tag);

        Instances.InvalidateCanvas();
      }
    }

    private void Menu_Enabled(object sender, EventArgs e)
    {
      Locked = !Locked;

      var count = 0;
      if (OnPingDocument() is GH_Document ghDocument)
      {
        foreach (var target in Targets)
        {
          if (ghDocument.FindObject(target, true) is IGH_ActiveObject activeObject)
          {
            activeObject.Locked = Locked;
            count++;
          }
        }
      }

      if (count > 0)
      {
        if (Locked)
          Instances.InvalidateCanvas();
        else
          ExpireSolution(true);
      }
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new GH_HtmlFormatter()
      {
        Title = "Trigger Object",
        Description =
        @"<p>This component is a special interface object that allows to expire and recompute specific objects.</p>" +
        @"<p>Before a trigger will tell Grasshopper to recompute the solution it will blank certain objects. " +
        @"These are called the targets of the trigger object. You can add a target to a trigger by click + dragging from the right side of the trigger. " +
        @"Drag the wire onto another object, and it will be added to the target list. " +
        @"You can remove objects from the target list by tracing over an existing target wire while holding the Control key." +
        @"<p>By default a new trigger is enabled. You can enable/disable a trigger by double clicking on the object or via the context menu." +
        @"When a trigger is enable/disable all the target objects will be enabled/disabled as well.</p>",
        ContactURI = "https://discourse.mcneel.com/"
      };

      return nTopic.HtmlFormat();
    }

    public override bool Write(GH_IWriter writer)
    {
      if (Targets.Count > 0)
      {
        writer.SetInt32("TargetCount", Targets.Count);

        int index = 0;
        foreach (var target in Targets)
          writer.SetGuid("Target", index, target);
      }

      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      Targets.Clear();

      var targetCount = 0;
      if (reader.TryGetInt32("TargetCount", ref targetCount))
      {
        for (int i = 0; i < targetCount; ++i)
          Targets.Add(reader.GetGuid("Target", i));
      }

      return base.Read(reader);
    }

    public void InstanceGuidsChanged(SortedDictionary<Guid, Guid> map)
    {
      var targets = new HashSet<Guid>();

      foreach (var target in Targets)
      {
        if (map.ContainsKey(target))
          targets.Add(map[target]);
        else
          targets.Add(target);
      }

      Targets = targets;
    }

    public override void CreateAttributes() => Attributes = new ComponentAttributes(this);

    class ComponentAttributes : GH_Attributes<TriggerComponent>
    {
      bool Captured;
      bool ButtonDown;
      RectangleF TextBox;
      RectangleF ButtonBox;
      RectangleF GripBox => new RectangleF(OutputGrip.X, OutputGrip.Y - 6.0f, 6.0f, 12.0f);

      public ComponentAttributes(TriggerComponent owner) : base(owner)
      { }

      public override bool HasInputGrip => false;
      public override bool HasOutputGrip => false;

      public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
      {
        if (TextBox.Contains(GH_Convert.ToPoint(point)))
        {
          base.SetupTooltip(point, e);
        }
        else
        {
          if (!GripBox.Contains(GH_Convert.ToPoint(point)))
            return;

          e.Icon = Owner.Icon_24x24;
          e.Title = "Trigger targets";
          e.Text = "Drag from here to assign Trigger target objects.";
        }
      }

      protected override void Layout()
      {
        Pivot = GH_Convert.ToPoint(Pivot);

        int tagWidth = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.StandardAdjusted);
        int boxWidth = 50;

        TextBox = new RectangleF(Pivot, new Size(tagWidth + 16, 22));
        ButtonBox = new RectangleF(TextBox.Right, TextBox.Top, boxWidth, TextBox.Height);

        Bounds = RectangleF.Union(TextBox, ButtonBox);

        TextBox.Inflate(-2f, -2f);
        ButtonBox.Inflate(-2f, -2f);
      }

      public override bool IsPickRegion(PointF point)
      {
        var bounds = Bounds;
        bounds.Width += 6.0f;

        if (!bounds.Contains(point))
          return false;

        using (var capsule = GH_Capsule.CreateCapsule(bounds, GH_Palette.Grey, 12, 0))
          return capsule.Contains(point);
      }

      public override bool IsPickRegion(RectangleF box, GH_PickBox method)
      {
        switch (method)
        {
          case GH_PickBox.Window:
            return box.Contains(Bounds);

          case GH_PickBox.Crossing:
            if (box.Contains(Bounds))
              return true;

            if (box.IntersectsWith(Bounds))
            {
              using (var roundedRectangle = GH_CapsuleRenderEngine.CreateRoundedRectangle(GH_Convert.ToRectangle(Bounds), 12))
                return SolvePathBoxPick(roundedRectangle, box, 1f, GH_PickBox.Crossing);
            }

            return false;
        }

        return false;
      }

      public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (e.Button == MouseButtons.Left && IsPickRegion(e.CanvasLocation))
        {
          if (!ButtonBox.Contains(e.CanvasLocation))
          {
            Selected = false;
            Owner.Locked = !Owner.Locked;

            if (Owner.OnPingDocument() is GH_Document ghDocument)
            {
              foreach (var target in Owner.Targets)
              {
                if (ghDocument.FindObject(target, true) is IGH_ActiveObject docObject)
                  docObject.Locked = Owner.Locked;
              }

              if (Owner.Locked)
                Instances.InvalidateCanvas();
              else
                Owner.ExpireSolution(true);
            }
          }

          return GH_ObjectResponse.Handled;
        }

        return base.RespondToMouseDoubleClick(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (e.Button == MouseButtons.Left)
        {
          if (GripBox.Contains(e.CanvasLocation))
          {
            Selected = true;
            sender.ActiveInteraction = new TriggerTargetInteraction(sender, e, Owner);
            return GH_ObjectResponse.Handled;
          }

          if (sender.Viewport.Zoom >= 0.5)
          {
            if (ButtonBox.Contains(e.CanvasLocation))
            {
              Captured = true;
              ButtonDown = true;

              if (Owner.OnPingDocument() is GH_Document ghDocument)
              {
                if (Owner.Locked)
                {
                  foreach (var target in Owner.Targets)
                  {
                    if (ghDocument.FindObject(target, true) is IGH_ActiveObject docObject)
                      docObject.Locked = false;
                  }
                }

                if (Owner.ExpireTargets() > 0)
                  Owner.ExpireSolution(true);
              }

              return GH_ObjectResponse.Capture;
            }
          }
        }

        return base.RespondToMouseDown(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (e.Button == MouseButtons.Left)
        {
          ButtonDown = false;
          Instances.InvalidateCanvas();

          if (Captured)
          {
            Captured = false;

            if (Owner.OnPingDocument() is GH_Document ghDocument)
            {
              if (Owner.Locked)
              {
                foreach (var target in Owner.Targets)
                {
                  if (ghDocument.FindObject(target, true) is IGH_ActiveObject docObject)
                    docObject.Locked = true;
                }

                Instances.InvalidateCanvas();
              }
            }

            return GH_ObjectResponse.Release;
          }
        }

        return base.RespondToMouseUp(sender, e);
      }

      public static void RenderIcon(Graphics graphics, RectangleF bounds, Color color)
      {
        var x = (int) (bounds.X + 0.5F * bounds.Width);
        var y = (int) (bounds.Y + 0.5F * bounds.Height);
        var corners = new PointF[]
        {
          new PointF(x + 6.0f, y + 0.0f),
          new PointF(x - 6.0F, y + 7.0F),
          new PointF(x - 6.0F, y - 7.0F),
        };

        using (var edge = new Pen(color, 1.0F))
        {
          edge.LineJoin = LineJoin.Round;
          graphics.FillPolygon(new SolidBrush(Color.FromArgb(150, color)), corners);
          graphics.DrawPolygon(edge, corners);
        }
      }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        switch (channel)
        {
          case GH_CanvasChannel.Wires:
            if (Owner.OnPingDocument() is GH_Document ghDocument)
            {
              var color = Selected ? GH_Skin.wire_selected_a : GH_Skin.wire_default;
              if (Owner.Locked) color = Color.FromArgb(50, color);

              foreach (var target in Owner.Targets)
              {
                if (ghDocument.FindObject(target, true) is IGH_DocumentObject ghDocumentObject)
                {
                  var rectangle = GH_Convert.ToRectangle(ghDocumentObject.Attributes.Bounds);
                  RenderTriggerWire(graphics, OutputGrip, rectangle, color);
                }
              }
            }
            break;

          case GH_CanvasChannel.Objects:

            var bounds = Bounds;
            if (!canvas.Viewport.IsVisible(ref bounds, 10f))
              return;

            using (var capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Grey/*, 12, 0*/))
            {
              capsule.AddOutputGrip(OutputGrip.Y);
              capsule.Render(graphics, Selected, Owner.Locked, true);

              int zoomFade = GH_Canvas.ZoomFadeLow;
              if (zoomFade > 0)
              {
                var textBox = capsule.Box;
                textBox.Inflate(-5, -1);
                textBox.Width -= 20;

                var impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.Grey, Selected, Owner.Locked, true);
                using (var solidBrush = new SolidBrush(Color.FromArgb(zoomFade, impliedStyle.Text)))
                {
                  canvas.SetSmartTextRenderingHint();
                  graphics.DrawString(Owner.NickName, GH_FontServer.StandardAdjusted, solidBrush, TextBox, GH_TextRenderingConstants.CenterCenter);
                }
              }

              using (var buttonCapsule = GH_Capsule.CreateCapsule(ButtonBox, GH_Palette.Black, 1, 9))
              {
                buttonCapsule.RenderEngine.RenderGrips(graphics);
                var style = GH_Skin.palette_black_standard;
                if (ButtonDown)
                {
                  using (var fillDown = new LinearGradientBrush(buttonCapsule.Box, GH_GraphicsUtil.OffsetColour(style.Fill, 0), GH_GraphicsUtil.OffsetColour(style.Fill, 100), LinearGradientMode.Vertical))
                    graphics.FillPath(fillDown, buttonCapsule.OutlineShape);
                }
                else
                {
                  buttonCapsule.RenderEngine.RenderBackground(graphics, canvas.Viewport.Zoom, style);
                  buttonCapsule.RenderEngine.RenderHighlight(graphics);
                }

                buttonCapsule.RenderEngine.RenderOutlines(graphics, canvas.Viewport.Zoom, style);
              }
            }
            break;
        }
      }

      public static void RenderTriggerWire(Graphics g, PointF anchor, RectangleF box, Color col)
      {
        if (box.Contains(anchor))
          return;

        using (var pen = new Pen(col, 5.0f))
        {
          pen.StartCap = LineCap.Round;
          pen.DashCap = DashCap.Round;
          pen.EndCap = LineCap.Round;
          pen.DashPattern = new float[2] { 1f, 0.8f };

          var pt1 = GH_GraphicsUtil.BoxClosestPoint(anchor + new SizeF(20f, 0.0f), box);
          if (Math.Abs(box.Y - anchor.Y) < 1.0)
          {
            g.DrawLine(pen, pt1, anchor);
          }
          else
          {
            var pointFList = new List<PointF>
          {
            new PointF(anchor.X, anchor.Y)
          };

            if (pt1.X < anchor.X + 20.0)
            {
              pointFList.Add(new PointF(anchor.X + 20f, anchor.Y));
              pointFList.Add(new PointF(anchor.X + 20f, pt1.Y));
              pointFList.Add(new PointF(pt1.X, pt1.Y));
            }
            else
            {
              pointFList.Add(new PointF(pt1.X, anchor.Y));
              pointFList.Add(new PointF(pt1.X, pt1.Y));
            }
            pointFList.Reverse();

            using (var path = GH_GDI_Util.FilletPolyline(pointFList.ToArray(), 20f))
              g.DrawPath(pen, path);
          }
        }
      }

      public class TriggerTargetInteraction : GH_AbstractInteraction
      {
        private readonly TriggerComponent Owner;
        private PointF Point = new PointF(float.NaN, float.NaN);
        private IGH_ActiveObject Target;

        public TriggerTargetInteraction(GH_Canvas canvas, GH_CanvasMouseEvent e, TriggerComponent owner)
          : base(canvas, e, true)
        {
          Owner = owner;
          Canvas.StartAutoPan();
          Canvas.CanvasPostPaintObjects += Canvas_PostPaintObjects;
        }

        public override void Destroy()
        {
          Canvas.CanvasPostPaintObjects -= Canvas_PostPaintObjects;
          Canvas.StopAutoPan();
          base.Destroy();
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
          base.RespondToMouseMove(sender, e);

          if (!IsActive)
            return GH_ObjectResponse.Ignore;

          if (Canvas.Document is null)
            return GH_ObjectResponse.Ignore;

          Target = default;
          Point = e.CanvasLocation;

          if (Canvas.Document.FindObject(Point, 10f) is IGH_ActiveObject activeObject && !(activeObject is TriggerComponent))
            Target = activeObject;

          sender.Refresh();
          return GH_ObjectResponse.Handled;
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
          if (Target is object)
          {
            if (Control.ModifierKeys == Keys.Control)
              Owner.Targets.Remove(Target.InstanceGuid);
            else
              Owner.Targets.Add(Target.InstanceGuid);

            Owner.Attributes.ExpireLayout();
          }

          return GH_ObjectResponse.Release;
        }

        public override GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, KeyEventArgs e)
        {
          switch (e.KeyCode)
          {
            case Keys.Cancel:
            case Keys.Escape:
              Target = default;
              return GH_ObjectResponse.Release;
          }

          return GH_ObjectResponse.Ignore;
        }

        private void Canvas_PostPaintObjects(GH_Canvas sender)
        {
          if (float.IsNaN(Point.X) || float.IsNaN(Point.Y))
            return;

          try
          {
            sender.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            var edgeColor = Color.Black;
            var fillColor = Color.FromArgb(100, 255, 255, 255);

            if (Control.ModifierKeys == Keys.Control)
            {
              edgeColor = Color.DarkRed;
              fillColor = Color.FromArgb(100, 255, 0, 0);
            }

            if (Target is object)
            {
              var rectangle = GH_Convert.ToRectangle(Target.Attributes.Bounds);
              rectangle.Inflate(2, 2);

              using (var brush = new SolidBrush(fillColor))
                sender.Graphics.FillRectangle(brush, rectangle);

              using (var pen = new Pen(edgeColor))
                sender.Graphics.DrawRectangle(pen, rectangle);
            }

            var outputGrip = Owner.Attributes.OutputGrip;
            var location = Point;
            if (Target is object)
              location = GH_GraphicsUtil.BoxClosestPoint(outputGrip + new SizeF(20f, 0.0f), Target.Attributes.Bounds);

            RenderTriggerWire(sender.Graphics, outputGrip, new RectangleF(location, new SizeF(0.0f, 0.0f)), edgeColor);
          }
          catch { }
        }
      }
    }
  }
}
