using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros;
using Grasshopper.Rhinoceros.Model;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Drafting;
using Grasshopper.Rhinoceros.Render;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.DocObjects;
  using Convert.Geometry;
  using External.DB.Extensions;

  partial class GeometricElement
  {
    #region ModelContent
#if RHINO_8
    readonly struct ModelAttributes
    {
      public static readonly ObjectDisplay DisplayByLayer = new ObjectDisplay.Attributes() { Color = ObjectDisplayColor.Value.ByLayer };
      public static readonly ObjectDrafting DraftingByLayer = new ObjectDrafting.Attributes() { Color = ObjectDraftingColor.Value.ByLayer };
      public static readonly ObjectRender RenderByLayer = new ObjectRender.Attributes() { Material = ObjectRenderMaterial.Value.ByLayer };

      public static readonly ObjectDisplay DisplayByParent = new ObjectDisplay.Attributes() { Color = ObjectDisplayColor.Value.ByParent };
      public static readonly ObjectDrafting DraftingByParent = new ObjectDrafting.Attributes() { Color = ObjectDraftingColor.Value.ByParent };
      public static readonly ObjectRender RenderByParent = new ObjectRender.Attributes() { Material = ObjectRenderMaterial.Value.ByParent };

      public static readonly ObjectDisplay DisplayByMaterial = new ObjectDisplay.Attributes() { Color = ObjectDisplayColor.Value.ByMaterial };
    }

    internal static ModelInstanceDefinition ToModelInstanceDefinition
    (
      IDictionary<ARDB.ElementId, ModelContent> idMap,
      Transform transform,
      ARDB.Element element,
      ARDB.GeometryElement geometryElement
    )
    {
      if (idMap.TryGetValue(element.Id, out var modelContent))
        return modelContent as ModelInstanceDefinition;

      var geometryElementContent = geometryElement?.Where(x => !x.IsEmpty()).ToArray() ?? Array.Empty<ARDB.GeometryObject>();
      if (geometryElementContent.Length == 0)
        return null;

      if
      (
        geometryElementContent.Length == 1 &&
        geometryElementContent[0] is ARDB.GeometryInstance geometryInstance &&
        geometryInstance.GetSymbol() is ARDB.ElementType symbol
      )
      {
        // Special case to simplify ARDB.FamilyInstance elements.
        var instanceTransform = geometryInstance.Transform.ToTransform() * transform;
        if (instanceTransform.IsIdentity)
          return ToModelInstanceDefinition(idMap, instanceTransform, symbol, geometryInstance.SymbolGeometry);
      }

      var definition = new ModelInstanceDefinition.Attributes()
      {
        Path = NameConverter.EscapeName(element, out var description),
        Notes = description
      };

      GeometryDecoder.UpdateGraphicAttributes(geometryElement);

      bool identity = transform.IsIdentity;
      var objects = new List<ModelObject.Attributes>(geometryElementContent.Length);
      foreach (var g in geometryElementContent)
      {
        using (GeometryDecoder.Context.Push())
        {
          GeometryDecoder.UpdateGraphicAttributes(g);

          var shaded = false;
          var geo = default(IGH_GeometricGoo);
          switch (g)
          {
            case ARDB.Point point:
              var pointGeometry = point.Coord.ToPoint3d();
              if (!identity) pointGeometry.Transform(transform);
              geo = new GH_Point(pointGeometry);
              break;

            case ARDB.PolyLine pline:
              var plineGeometry = pline.ToPolylineCurve();
              if (!identity) plineGeometry.Transform(transform);
              geo = new GH_Curve(plineGeometry);
              break;

            case ARDB.Curve curve:
              var curveGeometry = curve.ToCurve();
              if (!identity) curveGeometry.Transform(transform);
              geo = new GH_Curve(curveGeometry);
              break;

            case ARDB.Mesh mesh:
              var meshGeometry = mesh.ToMesh();
              if (!identity) meshGeometry.Transform(transform);
              geo = new GH_Mesh(meshGeometry);
              shaded = true;
              break;

            case ARDB.Solid solid:
              var solidGeometry = solid.ToBrep();
              if (!identity) solidGeometry.Transform(transform);
              if (solidGeometry.TryGetExtrusion(out var extrusion)) geo = new GH_Extrusion(extrusion);
              else if (solidGeometry.Faces.Count == 1) geo = new GH_Surface(solidGeometry);
              else geo = new GH_Brep(solidGeometry);
              shaded = true;
              break;

            case ARDB.GeometryInstance instance:
              using (GeometryDecoder.Context.Push())
              {
                if (ToModelInstanceDefinition(idMap, Transform.Identity, instance.GetSymbol(), instance.SymbolGeometry) is ModelInstanceDefinition idef)
                  geo = new GH_InstanceReference(new InstanceReferenceGeometry(Guid.Empty, transform * instance.Transform.ToTransform()), idef);
              }
              break;
          }

          if (geo is null) continue;

          var geometry = ModelObject.Cast(geo).ToAttributes();

          var context = GeometryDecoder.Context.Peek;
          if (context.Category is ARDB.Category category)
            geometry.Layer = new Category(category).ToModelContent(idMap) as ModelLayer;

          if (g is ARDB.GeometryInstance)
          {
            geometry.Display = ModelAttributes.DisplayByLayer;
            geometry.Drafting = ModelAttributes.DraftingByLayer;
            geometry.Render = ModelAttributes.RenderByLayer;
          }
          else
          {
            geometry.Display = ModelAttributes.DisplayByLayer;
            geometry.Drafting = ModelAttributes.DraftingByParent;
            geometry.Render = ModelAttributes.RenderByParent;

            if (shaded)
            {

              if (context.FaceMaterialId?.Length > 0)
              {
                bool hasPerFaceMaterials = false;
                for (int f = 1; f < context.FaceMaterialId.Length && !hasPerFaceMaterials; ++f)
                  hasPerFaceMaterials |= context.FaceMaterialId[f] != context.FaceMaterialId[f - 1];

                if (!hasPerFaceMaterials)
                {
                  if (context.FaceMaterialId[0].IsValid())
                  {
                    var faceMaterial = new Material(element.Document, context.FaceMaterialId[0]);
                    var faceModelMaterial = faceMaterial.ToModelContent(idMap) as ModelRenderMaterial;
                    geometry.Render = new ObjectRender.Attributes() { Material = faceModelMaterial };
                    //geometry.Display = ModelAttributes.DisplayByMaterial;
                  }
                }
              }
            }
          }

          objects.Add(geometry);
        }
      }

      definition.Objects = objects.Select(x => x.ToModelData() as ModelObject).ToArray();

      var modelInstanceDefinition = definition.ToModelData() as ModelInstanceDefinition;
      idMap.Add(element.Id, modelInstanceDefinition);
      return modelInstanceDefinition;
    }

    internal override ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (Value is ARDB.Element element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var geometry = element.GetGeometry(options))
          {
            using (var context = GeometryDecoder.Context.Push())
            {
              context.Element = element;
              context.Category = element.Category;
              context.Material = geometry?.MaterialElement;

              var location = Location;
              if (ToModelInstanceDefinition(idMap, Transform.PlaneToPlane(location, Plane.WorldXY), element, geometry) is ModelInstanceDefinition definition)
              {
                var elementToWorld = Transform.PlaneToPlane(Plane.WorldXY, TransformTo(location));
                var attributes = ModelObject.Cast(new GH_InstanceReference(new InstanceReferenceGeometry(Guid.Empty, elementToWorld), definition)).ToAttributes();
                attributes.Name = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                attributes.Url = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;
                attributes.Layer = Category.ToModelContent(idMap) as ModelLayer;
                attributes.Frame = location;
                if (geometry?.MaterialElement is object)
                {
                  var material = new Material(geometry.MaterialElement);
                  var modelMaterial = material.ToModelContent(idMap) as ModelRenderMaterial;
                  attributes.Render = new ObjectRender.Attributes() { Material = modelMaterial };
                }

                modelContent = attributes.ToModelData() as ModelContent;
                //idMap.Add(Id, modelContent);
                return modelContent;
              }
            }
          }
        }
      }

      return null;
    }
#endif
    #endregion

  }
}
