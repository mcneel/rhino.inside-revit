using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Render;
using RhinoInside.Revit.Convert.Render;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Appearance Asset")]
  public sealed class AppearanceAssetElement : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.AppearanceAssetElement);
    public new ARDB.AppearanceAssetElement Value => base.Value as ARDB.AppearanceAssetElement;

    public AppearanceAssetElement() { }
    public AppearanceAssetElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public AppearanceAssetElement(ARDB.AppearanceAssetElement asset) : base(asset) { }

    public override bool ConvertTo<Q>(out Q target)
    {
      if (base.ConvertTo<Q>(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(Grasshopper.Kernel.Types.GH_Material)))
      {
        if (Value?.ToRenderMaterial(RhinoDoc.ActiveDoc) is RenderMaterial renderMaterial)
          target = (Q) (object) new Grasshopper.Kernel.Types.GH_Material(renderMaterial);

        return true;
      }
      return false;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.AppearanceAssetElement appearance)
      {
        // 2. Check if already exist
        var target = doc.RenderMaterials.FindName(appearance.Name);

        // 3. Update if necessary
        if (target is null || overwrite)
        {
          var source = appearance.ToRenderMaterial(doc);
          if (target is object)
          {
            target.BeginChange(RenderContent.ChangeContexts.Program);
            if (!target.Replace(source)) target.MatchData(source);
            target.EndChange();
          }
          else if (doc.RenderMaterials.Add(source))
          {
            target = source;
          }
        }

        if (target is object)
        {
          idMap.Add(Id, (guid = target.Id));
          return true;
        }
      }

      guid = Guid.Empty;
      return false;
    }
    #endregion
  }
}
