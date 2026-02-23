using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.DocObjects;
  using Convert.Geometry;
  using External.DB.Extensions;

  partial class GeometricElement
  {
    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    static System.Drawing.Color EnsureNoBlack(System.Drawing.Color value)
    {
      return (value.R == 0 && value.G == 0 && value.B == 0) ? System.Drawing.Color.FromArgb(value.A, 1, 1, 1) : value;
    }

    static ObjectAttributes CreateObjectAttributes(IDictionary<ARDB.ElementId, Guid> idMap, RhinoDoc doc, ARDB.Document document)
    {
      var context = GeometryDecoder.Context.Peek;
      var attributes = new ObjectAttributes();

      if (context.Category is ARDB.Category category)
      {
        if (new Category(category).BakeElement(idMap, false, doc, default, out var layerGuid))
          attributes.LayerIndex = doc.Layers.FindId(layerGuid).Index;
      }

      if (context.Material is ARDB.Material material)
      {
        if (new Material(material).BakeElement(idMap, false, doc, default, out var materialGuid))
          attributes.RenderMaterial = doc.RenderMaterials.Find(materialGuid);
      }

      return attributes;
    }

    public virtual bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      guid = Guid.Empty;

      // 3. Update if necessary
      if (Value is ARDB.Element element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var geometry = element.GetGeometry(options))
          {
            if (geometry is object)
            {
              using (var context = GeometryDecoder.Context.Push())
              {
                context.Element = element;
                context.Category = element.Category;
                context.Material = element.Category?.Material;

                var location = element.Category is null || element.Category.Parent is object ?
                  Plane.WorldXY :
                  Location;

                var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);
                if (BakeGeometryElement(idMap, overwrite, doc, worldToElement, element, geometry, out var idefIndex))
                {
                  att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
                  att.Space = ActiveSpace.ModelSpace;
                  att.ViewportId = Guid.Empty;
                  att.Name = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                  att.Url = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

                  if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
                    att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

                  guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);

                  // We don't want geometry on the active viewport but on its own.
                  doc.Objects.ModifyAttributes(guid, att, quiet: true);
                }
              }

              if (guid != Guid.Empty)
                return true;
            }
          }
        }
      }

      return false;
    }

    protected internal static bool BakeGeometryElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      Transform transform,
      ARDB.Element element,
      ARDB.GeometryElement geometryElement,
      out int index
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(element.Id, out var guid))
      {
        index = doc.InstanceDefinitions.FindId(guid).Index;
        return true;
      }

      var geometryElementContent = geometryElement.ToArray();
      if (geometryElementContent.Length < 1)
      {
        index = -1;
        return false;
      }
      else if
      (
        geometryElementContent.Length == 1 &&
        geometryElementContent[0] is ARDB.GeometryInstance geometryInstance &&
        geometryInstance.GetSymbol() is ARDB.ElementType symbol
      )
      {
        // Special case to simplify ARDB.FamilyInstance elements.
        var instanceTransform = geometryInstance.Transform.ToTransform();
        return BakeGeometryElement(idMap, false, doc, instanceTransform * transform, symbol, geometryInstance.SymbolGeometry, out index);
      }

      // Get a Unique Instance Definition name.
      var idef_name = NameConverter.EscapeName(element, out var idef_description);

      // 2. Check if already exist
      index = doc.InstanceDefinitions.Find(idef_name)?.Index ?? -1;

      // 3. Update if necessary
      if (index < 0 || overwrite)
      {
        bool identity = transform.IsIdentity;

        GeometryDecoder.UpdateGraphicAttributes(geometryElement);

        var geometry = new List<GeometryBase>(geometryElementContent.Length);
        var attributes = new List<ObjectAttributes>(geometryElementContent.Length);

        foreach (var g in geometryElementContent)
        {
          using (GeometryDecoder.Context.Push())
          {
            GeometryDecoder.UpdateGraphicAttributes(g);

            var objectGeometry = default(GeometryBase);
            switch (g)
            {
              case ARDB.Mesh m: if (m.NumTriangles > 0) objectGeometry = m.ToMesh(); break;
              case ARDB.Solid s: if (!s.Faces.IsEmpty) objectGeometry = s.ToBrep(); break;
              case ARDB.Curve c: objectGeometry = c.ToCurve(); break;
              case ARDB.PolyLine p: if (p.NumberOfCoordinates > 0) objectGeometry = p.ToPolylineCurve(); break;
              case ARDB.GeometryInstance i:
                using (GeometryDecoder.Context.Push())
                {
                  if (BakeGeometryElement(idMap, false, doc, Transform.Identity, i.GetSymbol(), i.SymbolGeometry, out var idefIndex))
                    objectGeometry = new InstanceReferenceGeometry(doc.InstanceDefinitions[idefIndex].Id, i.Transform.ToTransform());
                }
                break;
            }

            if (objectGeometry is null) continue;
            if (!identity) objectGeometry.Transform(transform);

            var objectAttributes = CreateObjectAttributes(idMap, doc, element.Document);

            if (objectGeometry is Mesh mesh)
            {
              var context = GeometryDecoder.Context.Peek;
              if (context.FaceMaterialId?.Length == 1 && context.FaceMaterialId[0].IsValid())
              {
                var faceMaterial = new Material(element.Document, context.FaceMaterialId[0]);
                if (faceMaterial.BakeElement(idMap, false, doc, null, out var materialId))
                {
                  objectAttributes.RenderMaterial = doc.RenderMaterials.Find(materialId);
                  objectAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                  objectAttributes.ObjectColor = faceMaterial.ObjectColor;
                }
              }
            }
            else if (objectGeometry is Brep brep)
            {
              // In case objectGeometry is a Brep and has different materials per face.
              var context = GeometryDecoder.Context.Peek;
              if (context.FaceMaterialId?.Length > 0)
              {
                bool hasPerFaceMaterials = false;
                {
                  for (int f = 1; f < context.FaceMaterialId.Length && !hasPerFaceMaterials; ++f)
                    hasPerFaceMaterials |= context.FaceMaterialId[f] != context.FaceMaterialId[f - 1];
                }

                if (hasPerFaceMaterials)
                {
                  var layer = doc.Layers[objectAttributes.LayerIndex];

                  if (objectAttributes.MaterialIndex < 0) objectAttributes.MaterialIndex = doc.Materials.Add();
                  var objectMaterial = doc.Materials[objectAttributes.MaterialIndex];
                  if (layer.RenderMaterial is RenderMaterial layerMaterial)
                  {
                    layerMaterial.SimulateMaterial(ref objectMaterial, RenderTexture.TextureGeneration.Allow);
                    objectMaterial.RenderMaterialInstanceId = layerMaterial.Id;
                  }
                  else objectMaterial.RenderMaterialInstanceId = default;
                  objectMaterial.ClearMaterialChannels();

                  foreach (var face in brep.Faces)
                  {
                    var faceMaterialId = context.FaceMaterialId[face.SurfaceIndex];
                    if (faceMaterialId != (context.Material?.Id ?? ARDB.ElementId.InvalidElementId))
                    {
                      var faceMaterial = new Material(element.Document, faceMaterialId);
                      if (faceMaterial.BakeSharedMaterial(idMap, false, doc, out var materialGuid))
                      {
                        face.MaterialChannelIndex = objectMaterial.MaterialChannelIndexFromId(materialGuid, true);
                      }
                    }
                    else
                    {
                      var materialIndex = layer.RenderMaterialIndex;
                      if (doc.Materials[materialIndex] is Rhino.DocObjects.Material material)
                      {
                        face.MaterialChannelIndex = objectMaterial.MaterialChannelIndexFromId(material.Id, true);
                        face.PerFaceColor = EnsureNoBlack(material.DiffuseColor);
                      }
                      else
                      {
                        face.ClearMaterialChannelIndex();
                      }
                    }
                  }

                  doc.Materials.Modify(objectMaterial, objectAttributes.MaterialIndex, quiet: true);
                  objectAttributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                }
                else if (context.FaceMaterialId[0].IsValid())
                {
                  var objectMaterial = new Material(element.Document, context.FaceMaterialId[0]);
                  if (objectMaterial.BakeElement(idMap, false, doc, null, out var materialId))
                  {
                    objectAttributes.RenderMaterial = doc.RenderMaterials.Find(materialId);
                    objectAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                    objectAttributes.ObjectColor = objectMaterial.ObjectColor;
                  }
                }

                // If we don't have per-face materials let's try to convert it to an extrusion.
                if (!hasPerFaceMaterials && brep.TryGetExtrusion(out var extrusion) is true)
                  objectGeometry = extrusion;
              }
            }

            geometry.Add(objectGeometry);
            attributes.Add(objectAttributes);
          }
        }

        if (index < 0) index = doc.InstanceDefinitions.Add(idef_name, idef_description, Point3d.Origin, geometry, attributes);
        else if (!doc.InstanceDefinitions.ModifyGeometry(index, geometry, attributes)) index = -1;

        if (index >= 0) idMap[element.Id] = doc.InstanceDefinitions[index].Id;
      }

      return index >= 0;
    }
    #endregion
  }
}
