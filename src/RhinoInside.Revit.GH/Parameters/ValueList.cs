using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class GH_ValueList : Grasshopper.Kernel.Special.GH_ValueList
  {
    protected override Bitmap Icon =>
      ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;
  }

  public abstract class ValueList : GH_ValueList, IGH_InitCodeAware
  {
    public override GH_ParamData DataType => GH_ParamData.remote;
    public override GH_ParamKind Kind => GH_ParamKind.floating;

    void IGH_InitCodeAware.SetInitCode(string code) => NickName = code;

    public ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";

      ObjectChanged += OnObjectChanged;

      ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;
      ListItems.Clear();
    }

    private void OnObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
    {
      if (SourceCount == 0)
      {
        if (e.Type == GH_ObjectEventType.Sources)
        {
          //NickName = string.Empty;
          MutableNickName = true;
        }

        if (e.Type == GH_ObjectEventType.NickName)
          ExpireSolution(true);
      }
      else
      {
        if (e.Type == GH_ObjectEventType.Sources)
        {
          NickName = string.Empty;
          MutableNickName = false;
        }
      }
    }

    class ValueListAttributes : Grasshopper.Kernel.Special.GH_ValueListAttributes
    {
      public override bool HasInputGrip => true;
      public override bool AllowMessageBalloon => true;
      public ValueListAttributes(ValueList owner) : base(owner) { }
      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Wires)
        {
          if (Owner.SourceCount > 0)
            RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
        }

        base.Render(canvas, graphics, channel);
      }
      public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (Owner.MutableNickName && e.Button == MouseButtons.Left)
        {
          var objectMenu = new ContextMenuStrip();

          Owner.AppendMenuItems(objectMenu);
          if (objectMenu.Items.Count > 0)
          {
            sender.ActiveInteraction = null;
            objectMenu.Show(sender, e.ControlLocation);
          }

          return GH_ObjectResponse.Handled;
        }

        return GH_ObjectResponse.Ignore;
      }
    }

    public override void CreateAttributes() => m_attributes = new ValueListAttributes(this);

    protected abstract void RefreshList(string FamilyName);

    protected abstract void RefreshList(IEnumerable<IGH_Goo> goos);

    public override void PostProcessData()
    {
      base.PostProcessData();

      if (SourceCount == 0)
        RefreshList(NickName);
      else
        RefreshList(VolatileData.AllData(true));

      // Show elements sorted
      ListItems.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));

      //base.CollectVolatileData_Custom();
      m_data.Clear();

      var path = new GH_Path(0);
      if (SelectedItems.Count == 0)
        m_data.AppendRange(new IGH_Goo[0], path);
      else foreach (var item in SelectedItems)
          m_data.Append(item.Value, path);
    }

    protected override void CollectVolatileData_FromSources()
    {
      base.CollectVolatileData_FromSources();

      NickName = string.Empty;
    }

    protected override void CollectVolatileData_Custom()
    {
      NickName = NickName.Trim();
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new Grasshopper.GUI.HTML.GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>Double click on it and use the name input box to enter a name, alternativelly you can enter a name patter. " +
        @"If a pattern is used, this param list will be filled up with all the objects that match it.</p>" +
        @"<p>Several kind of patterns are supported, the method used depends on the first pattern character:</p>" +
        @"<dl>" +
        @"<dt><b>></b></dt><dd>Starts with</dd>" +
        @"<dt><b><</b></dt><dd>Ends with</dd>" +
        @"<dt><b>?</b></dt><dd>Contains, same as a regular search</dd>" +
        @"<dt><b>:</b></dt><dd>Wildcards, see Microsoft.VisualBasic " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options\">LikeOperator</a></dd>" +
        @"<dt><b>;</b></dt><dd>Regular expresion, see " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference\">here</a> as reference</dd>" +
        @"</dl>",
        ContactURI = AssemblyInfo.ContactURI,
        WebPageURI = AssemblyInfo.WebPageURI
      };

      return nTopic.HtmlFormat();
    }
  }
}
