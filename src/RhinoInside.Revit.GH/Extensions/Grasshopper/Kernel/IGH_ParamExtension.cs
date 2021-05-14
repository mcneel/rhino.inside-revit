using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Types;

namespace Grasshopper.Kernel
{
  static class IGH_DocumentObjectExtension
  {
    public static IGH_DocumentObject GetTopLevelObject(this IGH_DocumentObject docObject)
    {
      var top = docObject.Attributes.GetTopLevel.DocObject;
      var document = top.OnPingDocument();
      while (document.Owner is object)
      {
        top = document.Owner as IGH_ActiveObject;
        document = document.Owner.OwnerDocument();
      }

      return top;
    }
  }

  static class IGH_ParamExtension
  {
    public static void AddVolatileDataTree<T1, T2>(this IGH_Param param, IGH_Structure structure, Converter<T1, T2> converter)
    where T1 : IGH_Goo
    where T2 : IGH_Goo
    {
      for (int p = 0; p < structure.PathCount; ++p)
      {
        var path = structure.get_Path(p);
        var srcBranch = structure.get_Branch(path);

        var data = srcBranch.As<T1>().Select(x => x == null ? default : converter(x));
        param.AddVolatileDataList(path, data);
      }
    }

    public static bool ConnectNewObject(this IGH_Param param, IGH_DocumentObject obj)
    {
      if (obj is null)
        return false;

      if (param.Kind == GH_ParamKind.unknown)
        return false;

      var document = param.OnPingDocument();
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

      float offsetX = param.Kind == GH_ParamKind.input ? -(obj.Attributes.Bounds.X + obj.Attributes.Bounds.Width) - 94 : -(obj.Attributes.Bounds.X) + 100.0f;

      if (obj is IGH_Param)
        obj.Attributes.Pivot = new System.Drawing.PointF(param.Attributes.Pivot.X + offsetX, param.Attributes.Pivot.Y - obj.Attributes.Bounds.Height / 2);
      else if (obj is IGH_Component)
        obj.Attributes.Pivot = new System.Drawing.PointF(param.Attributes.Pivot.X + offsetX, param.Attributes.Pivot.Y);

      obj.Attributes.ExpireLayout();

      document.AddObject(obj, false);
      document.UndoUtil.RecordAddObjectEvent($"Add {obj.Name}", obj);

      if (param.Kind == GH_ParamKind.input)
      {
        if (obj is IGH_Param parameter)
        {
          param.AddSource(parameter);
        }
        else if (obj is IGH_Component component)
        {
          var selfType = param.Type;
          foreach (var output in component.Params.Output)
          {
            if (output.GetType() == param.GetType() || output.Type.IsAssignableFrom(selfType))
            {
              param.AddSource(output);
              break;
            }
          }
        }
      }
      else
      {
        if (obj is IGH_Param parameter)
        {
          parameter.AddSource(param);
        }
        else if (obj is IGH_Component component)
        {
          var selfType = param.Type;
          foreach (var input in component.Params.Input)
          {
            if (input.GetType() == param.GetType() || input.Type.IsAssignableFrom(selfType))
            {
              input.AddSource(param);
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
        var RiR = new Guid("1FA46AFC-7B70-D4EB-C77A-D6DF5E36BA5C");
        var components = new List<IGH_Component>();
        var paramType = param.Type;

        foreach (var proxy in Instances.ComponentServer.ObjectProxies)
        {
          if (proxy.Obsolete) continue;
          if (!proxy.SDKCompliant) continue;
          if (proxy.Kind != GH_ObjectType.CompiledObject) continue;
          if (proxy.Exposure != GH_Exposure.primary && proxy.Exposure != GH_Exposure.secondary) continue;
          if (proxy.LibraryGuid != RiR) continue;

          if (typeof(IGH_Component).IsAssignableFrom(proxy.Type))
          {
            try
            {
              if (proxy.CreateInstance() is IGH_Component component)
              {
                foreach (var input in component.Params.Input)
                {
                  if (input.Type == typeof(IGH_Goo) || input.Type == typeof(IGH_GeometricGoo))
                    continue;

                  if (input.GetType() == param.GetType() || input.Type.IsAssignableFrom(paramType))
                  {
                    components.Add(component);
                    break;
                  }
                }
              }
            }
            catch { }
          }
        }

        var connect = GH_DocumentObject.Menu_AppendItem(menu, "Connect") as ToolStripMenuItem;

        var panedComponentId = new Guid("{59E0B89A-E487-49f8-BAB8-B5BAB16BE14C}");
        var panel = GH_DocumentObject.Menu_AppendItem(connect.DropDown, "Panel", eventHandler, Instances.ComponentServer.EmitObjectIcon(panedComponentId));
        panel.Tag = panedComponentId;

        var valueSetComponentId = new Guid("{AFB12752-3ACB-4ACF-8102-16982A69CDAE}");
        var picker = GH_DocumentObject.Menu_AppendItem(connect.DropDown, "Value Set Picker", eventHandler, Instances.ComponentServer.EmitObjectIcon(valueSetComponentId));
        picker.Tag = valueSetComponentId;

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

          if (param.ConnectNewObject(obj))
            obj.ExpireSolution(true);
        }
      };

      Menu_AppendConnect(param, menu, DefaultConnectMenuHandler);
    }
  }
}
