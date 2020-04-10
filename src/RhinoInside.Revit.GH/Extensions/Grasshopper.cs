using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RhinoInside.Revit.GH.Types;

namespace Grasshopper.Kernel.Extensions
{
  static partial class Extension
  {
    #region IGH_Param
    public static bool ConnectNewObject(this IGH_Param self, IGH_DocumentObject obj)
    {
      if (obj is null)
        return false;

      if (self.Kind == GH_ParamKind.unknown)
        return false;

      var document = self.OnPingDocument();
      if (document is null)
        return false;

      obj.CreateAttributes();
      if (CentralSettings.CanvasFullNames)
      {
        var atts = new List<IGH_Attributes>();
        obj.Attributes.AppendToAttributeTree(atts);
        foreach (var att in atts)
          att.DocObject.NickName = att.DocObject.Name;
      }

      obj.NewInstanceGuid();
      obj.Attributes.Pivot = default;
      obj.Attributes.PerformLayout();

      float offsetX = self.Kind == GH_ParamKind.input ? -(obj.Attributes.Bounds.X + obj.Attributes.Bounds.Width) - 94 : -(obj.Attributes.Bounds.X) + 100.0f;

      if (obj is IGH_Param)
        obj.Attributes.Pivot = new System.Drawing.PointF(self.Attributes.Pivot.X + offsetX, self.Attributes.Pivot.Y - obj.Attributes.Bounds.Height / 2);
      else if (obj is IGH_Component)
        obj.Attributes.Pivot = new System.Drawing.PointF(self.Attributes.Pivot.X + offsetX, self.Attributes.Pivot.Y);

      obj.Attributes.ExpireLayout();

      document.AddObject(obj, false);
      document.UndoUtil.RecordAddObjectEvent($"Add {obj.Name}", obj);

      if (self.Kind == GH_ParamKind.input)
      {
        if (obj is IGH_Param param)
        {
          self.AddSource(param);
        }
        else if (obj is IGH_Component component)
        {
          var selfType = self.Type;
          foreach (var output in component.Params.Output.Where(i => typeof(IGH_ElementId).IsAssignableFrom(i.Type)))
          {
            if (output.GetType() == self.GetType() || output.Type.IsAssignableFrom(selfType))
            {
              self.AddSource(output);
              break;
            }
          }
        }
      }
      else
      {
        if (obj is IGH_Param param)
        {
          param.AddSource(self);
        }
        else if (obj is IGH_Component component)
        {
          var selfType = self.Type;
          foreach (var input in component.Params.Input.Where(i => typeof(IGH_ElementId).IsAssignableFrom(i.Type)))
          {
            if (input.GetType() == self.GetType() || input.Type.IsAssignableFrom(selfType))
            {
              input.AddSource(self);
              break;
            }
          }
        }
      }

      return true;
    }

    public static void Menu_AppendConnect(this IGH_Param param, ToolStripDropDown menu, EventHandler eventHandler)
    {
      if ((param.Kind == GH_ParamKind.floating || param.Kind == GH_ParamKind.output) && param.Recipients.Count == 0)
      {
        var components = new List<IGH_Component>();
        var paramType = param.Type;

        foreach (var proxy in Instances.ComponentServer.ObjectProxies.Where(x => !x.Obsolete && x.Exposure != GH_Exposure.hidden && x.Exposure < GH_Exposure.tertiary))
        {
          if (typeof(IGH_Component).IsAssignableFrom(proxy.Type))
          {
            var obj = proxy.CreateInstance() as IGH_Component;
            foreach (var input in obj.Params.Input.Where(i => typeof(IGH_ElementId).IsAssignableFrom(i.Type)))
            {
              if (input.GetType() == param.GetType() || input.Type.IsAssignableFrom(paramType))
              {
                components.Add(obj);
                break;
              }
            }
          }
        }

        var connect = GH_DocumentObject.Menu_AppendItem(menu, "Connect") as ToolStripMenuItem;

        var panedComponentId = new Guid("{59E0B89A-E487-49f8-BAB8-B5BAB16BE14C}");
        var panel = GH_DocumentObject.Menu_AppendItem(connect.DropDown, "Panel", eventHandler, Instances.ComponentServer.EmitObjectIcon(panedComponentId));
        panel.Tag = panedComponentId;

        var picker = GH_DocumentObject.Menu_AppendItem(connect.DropDown, "Value Set Picker", eventHandler, Instances.ComponentServer.EmitObjectIcon(External.Special.ValueSetPicker.ComponentClassGuid));
        picker.Tag = External.Special.ValueSetPicker.ComponentClassGuid;

        if (components.Count > 0)
        {
          GH_DocumentObject.Menu_AppendSeparator(connect.DropDown);
          var maxComponents = CentralSettings.CanvasMaxSearchResults;
          maxComponents = Math.Min(maxComponents, 30);
          maxComponents = Math.Max(maxComponents, 3);

          int count = 0;
          foreach (var componentGroup in components.GroupBy(x => x.Exposure).OrderBy(x => x.Key))
          {
            foreach (var component in componentGroup.OrderBy(x => x.Category).OrderBy(x => x.SubCategory).OrderBy(x => x.Name))
            {
              var item = GH_DocumentObject.Menu_AppendItem(connect.DropDown, component.Name, eventHandler, component.Icon_24x24);
              item.Tag = component.ComponentGuid;

              if (count >= maxComponents)
                break;
            }

            if (count >= maxComponents)
              break;
          }
        }
      }
    }

    public static void Menu_AppendConnect(this IGH_Param param, ToolStripDropDown menu)
    {
      EventHandler DefaultConnectMenuHandler = (sender, e) =>
      {
        if (sender is ToolStripMenuItem item && item.Tag is Guid componentGuid)
        {
          var obj = Instances.ComponentServer.EmitObject(componentGuid);
          if (obj is null)
            return;

          if(param.ConnectNewObject(obj))
            obj.ExpireSolution(true);
        }
      };

      Menu_AppendConnect(param, menu, DefaultConnectMenuHandler);
    }
    #endregion
  }
}
