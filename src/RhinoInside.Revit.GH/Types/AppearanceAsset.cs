using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Render;
using RhinoInside.Revit.Convert.Render;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Appearance Asset")]
  public class AppearanceAssetElement : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.AppearanceAssetElement);
    public new DB.AppearanceAssetElement Value => base.Value as DB.AppearanceAssetElement;

    public AppearanceAssetElement() { }
    public AppearanceAssetElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public AppearanceAssetElement(DB.AppearanceAssetElement asset) : base(asset) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
        return true;
#if REVIT_2018
      if (typeof(Q).IsAssignableFrom(typeof(Grasshopper.Kernel.Types.GH_Material)))
      {
        if (RhinoDoc.ActiveDoc is RhinoDoc doc)
        {
          if (Value is DB.AppearanceAssetElement appearance)
          {
            var renderMaterial = RenderMaterial.CreateBasicMaterial(Rhino.DocObjects.Material.DefaultMaterial, doc);
            renderMaterial.Name = appearance.Name;

            using (var asset = appearance.GetRenderingAsset())
              renderMaterial.SimulateRenderingAsset(asset, doc);

            target = (Q) (object) new Grasshopper.Kernel.Types.GH_Material(renderMaterial);
            return true;
          }
        }
      }
#endif
      return false;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
#if REVIT_2018
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is DB.AppearanceAssetElement appearance)
      {
        if (BakeRenderMaterial(overwrite, doc, appearance.Name, out guid))
          idMap.Add(Id, guid);
      }
#else
      guid = Guid.Empty; 
#endif

      return false;
    }

#if REVIT_2018
    internal bool BakeRenderMaterial
    (
      bool overwrite,
      RhinoDoc doc,
      string materialName,
      out Guid guid
    )
    {
      if (Value is DB.AppearanceAssetElement appearance)
      {
        // 2. Check if already exist
        var material = doc.RenderMaterials.Where(x => x.Name == materialName).FirstOrDefault();

        if (material is null)
        {
          material = RenderMaterial.CreateBasicMaterial(Rhino.DocObjects.Material.DefaultMaterial, doc);
          material.Name = materialName;
        }

        if(material.Document is null || overwrite)
        {
          if (material.Document is object)
            material.BeginChange(RenderContent.ChangeContexts.Program);

          using (var asset = appearance.GetRenderingAsset())
            material.SimulateRenderingAsset(asset, doc);

          if (material.Document is object)
            material.EndChange();
          else
            doc.RenderMaterials.Add(material);
        }

        guid = material.Id;
        return true;
      }

      guid = Guid.Empty;
      return false;
    }
#endif
    #endregion
  }
}
