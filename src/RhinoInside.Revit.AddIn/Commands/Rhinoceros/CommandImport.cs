using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eto.Forms;

using Autodesk.Revit.Attributes;
using ARDB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#if REVIT_2018
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif

using Rhino.Geometry;
using Rhino.FileIO;
using Rhino.DocObjects;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public class CommandImport : RhinoCommand
  {
    public static string CommandName => "Import\n3DM";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandImport, NeedsActiveDocument<Availability>>
      (
        name: CommandName,
        iconName: "Ribbon.Rhinoceros.Import-3DM.png",
        tooltip: "Imports geometry from 3DM file to a Revit model or family",
        url : "reference/rir-interface#rhinoceros-panel"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    #region Categories
    static Dictionary<string, ARDB.Category> GetCategoriesByName(ARDB.Document doc)
    {
      return doc.OwnerFamily.FamilyCategory.SubCategories.
        OfType<ARDB.Category>().
        ToDictionary(x => x.Name, x => x);
    }

    static ARDB.ElementId ImportLayer
    (
      ARDB.Document doc,
      File3dm model,
      Layer layer,
      Dictionary<string, ARDB.Category> categories,
      Dictionary<string, ARDB.Material> materials
    )
    {
      var id = ARDB.ElementId.InvalidElementId;

      if (layer.HasName && layer.Name is string layerName)
      {
        layerName = ElementNaming.MakeValidName(layerName);

        if (categories.TryGetValue(layerName, out var category)) id = category.Id;
        else
        {
          var familyCategory = doc.OwnerFamily.FamilyCategory;
          if (familyCategory.CanAddSubcategory)
          {
            if (doc.Settings.Categories.NewSubcategory(familyCategory, layerName) is ARDB.Category subCategory)
            {
              subCategory.LineColor = layer.Color.ToColor();

              var modelMaterial = layer.RenderMaterialIndex >= 0 ? model.AllMaterials.FindIndex(layer.RenderMaterialIndex) : default;
              if (modelMaterial is object)
              {
                if (doc.GetElement(ImportMaterial(doc, modelMaterial, materials)) is ARDB.Material material)
                  subCategory.Material = material;
              }

              categories.Add(layerName, subCategory);
              id = subCategory.Id;
            }
          }
        }
      }

      return id;
    }
    #endregion

    #region Materials
    static Dictionary<string, ARDB.Material> GetMaterialsByName(ARDB.Document doc)
    {
      var collector = new ARDB.FilteredElementCollector(doc);
      return collector.OfClass(typeof(ARDB.Material)).OfType<ARDB.Material>().
        GroupBy(x => x.Name).
        ToDictionary(x => x.Key, x => x.First());
    }

    static string GenericAssetName(Autodesk.Revit.ApplicationServices.LanguageType language)
    {
      switch (language)
      {
        case Autodesk.Revit.ApplicationServices.LanguageType.English_USA:           return "Generic";
        case Autodesk.Revit.ApplicationServices.LanguageType.German:                return "Generisch";
        case Autodesk.Revit.ApplicationServices.LanguageType.Spanish:               return "Genérico";
        case Autodesk.Revit.ApplicationServices.LanguageType.French:                return "Générique";
        case Autodesk.Revit.ApplicationServices.LanguageType.Italian:               return "Generico";
        case Autodesk.Revit.ApplicationServices.LanguageType.Dutch:                 return "Allgemeine";
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Simplified:    return "常规";
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Traditional:   return "常規";
        case Autodesk.Revit.ApplicationServices.LanguageType.Japanese:              return "一般";
        case Autodesk.Revit.ApplicationServices.LanguageType.Korean:                return "일반";
        case Autodesk.Revit.ApplicationServices.LanguageType.Russian:               return "общий";
        case Autodesk.Revit.ApplicationServices.LanguageType.Czech:                 return "Obecný";
        case Autodesk.Revit.ApplicationServices.LanguageType.Polish:                return "Rodzajowy";
        case Autodesk.Revit.ApplicationServices.LanguageType.Hungarian:             return "Generikus";
        case Autodesk.Revit.ApplicationServices.LanguageType.Brazilian_Portuguese:  return "Genérico";
        #if REVIT_2018
        case Autodesk.Revit.ApplicationServices.LanguageType.English_GB:            return "Generic";
        #endif
      }

      return null;
    }

    static string GenericAssetName() => GenericAssetName(Revit.ActiveUIApplication.Application.Language) ?? "Generic";

    static ARDB.AppearanceAssetElement GetGenericAppearanceAssetElement(ARDB.Document doc)
    {
      var applicationLanguage = Revit.ActiveUIApplication.Application.Language;
      var languages = Enumerable.Repeat(applicationLanguage, 1).
      Concat
      (
        Enum.GetValues(typeof(Autodesk.Revit.ApplicationServices.LanguageType)).
        Cast<Autodesk.Revit.ApplicationServices.LanguageType>().
        Where(lang => lang != applicationLanguage && lang != Autodesk.Revit.ApplicationServices.LanguageType.Unknown)
      );

      foreach (var lang in languages)
      {
        if (ARDB.AppearanceAssetElement.GetAppearanceAssetElementByName(doc, GenericAssetName(lang)) is ARDB.AppearanceAssetElement assetElement)
          return assetElement;
      }

      return null;
    }

    static ARDB.ElementId ImportMaterial
    (
      ARDB.Document doc,
      Rhino.Render.RenderMaterial mat
    )
    {
      string name = ElementNaming.MakeValidName(mat.Name ?? mat.Id.ToString());
      var appearanceAssetId = ARDB.ElementId.InvalidElementId;

#if REVIT_2018
      if (ARDB.AppearanceAssetElement.GetAppearanceAssetElementByName(doc, name) is ARDB.AppearanceAssetElement appearanceAssetElement)
        appearanceAssetId = appearanceAssetElement.Id;
      else
      {
        appearanceAssetElement = GetGenericAppearanceAssetElement(doc);
        if (appearanceAssetElement is null)
        {
          var assets = Revit.ActiveUIApplication.Application.GetAssets(AssetType.Appearance);
          foreach (var asset in assets)
          {
            if (asset.Name == GenericAssetName())
            {
              appearanceAssetElement = ARDB.AppearanceAssetElement.Create(doc, name, asset);
              appearanceAssetId = appearanceAssetElement.Id;
              break;
            }
          }
        }
        else
        {
          appearanceAssetElement = appearanceAssetElement.Duplicate(name);
          appearanceAssetId = appearanceAssetElement.Id;
        }

        if (appearanceAssetId != ARDB.ElementId.InvalidElementId)
        {
          using (var editScope = new AppearanceAssetEditScope(doc))
          {
            var editableAsset = editScope.Start(appearanceAssetId);

            //var category = editableAsset.FindByName("category") as AssetPropertyString;
            //category.Value = $":{mat.Category.FirstCharUpper()}";

            var description = editableAsset.FindByName("description") as AssetPropertyString;
            description.Value = mat.Notes ?? string.Empty;

            var keyword = editableAsset.FindByName("keyword") as AssetPropertyString;
            {
              string tags = string.Empty;
              foreach (var tag in (mat.Tags ?? string.Empty).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                tags += $":{tag.Replace(':', ';')}";
              keyword.Value = tags;
            }

            if (mat.SmellsLikeMetal || mat.SmellsLikeTexturedMetal)
            {
              var generic_self_illum_luminance = editableAsset.FindByName(Generic.GenericIsMetal) as AssetPropertyBoolean;
              generic_self_illum_luminance.Value = true;
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Diffuse, out Rhino.Display.Color4f diffuse))
            {
              var generic_diffuse = editableAsset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
              generic_diffuse.SetValueAsDoubles(new double[] { diffuse.R, diffuse.G, diffuse.B, diffuse.A });
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Transparency, out double transparency))
            {
              var generic_transparency = editableAsset.FindByName(Generic.GenericTransparency) as AssetPropertyDouble;
              generic_transparency.Value = transparency;

              if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.TransparencyColor, out Rhino.Display.Color4f transparencyColor))
              {
                diffuse = diffuse.BlendTo((float) transparency, transparencyColor);

                var generic_diffuse = editableAsset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
                generic_diffuse.SetValueAsDoubles(new double[] { diffuse.R, diffuse.G, diffuse.B, diffuse.A });
              }
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Ior, out double ior))
            {
              var generic_refraction_index = editableAsset.FindByName(Generic.GenericRefractionIndex) as AssetPropertyDouble;
              generic_refraction_index.Value = ior;
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Shine, out double shine))
            {
              if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Specular, out Rhino.Display.Color4f specularColor))
              {
                var generic_reflectivity_at_0deg = editableAsset.FindByName(Generic.GenericReflectivityAt0deg) as AssetPropertyDouble;
                generic_reflectivity_at_0deg.Value = shine * specularColor.L;
              }
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Reflectivity, out double reflectivity))
            {
              if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.ReflectivityColor, out Rhino.Display.Color4f reflectivityColor))
              {
                var generic_reflectivity_at_90deg = editableAsset.FindByName(Generic.GenericReflectivityAt90deg) as AssetPropertyDouble;
                generic_reflectivity_at_90deg.Value = reflectivity * reflectivityColor.L;

                if (mat.Fields.TryGetValue("fresnel-enabled", out bool fresnel_enabled) && !fresnel_enabled)
                {
                  diffuse = diffuse.BlendTo((float) reflectivity, reflectivityColor);
                  var generic_diffuse = editableAsset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
                  generic_diffuse.SetValueAsDoubles(new double[] { diffuse.R, diffuse.G, diffuse.B, diffuse.A });
                }
              }
            }

            if (mat.Fields.TryGetValue("polish-amount", out double polish_amount))
            {
              var generic_glossiness = editableAsset.FindByName(Generic.GenericGlossiness) as AssetPropertyDouble;
              generic_glossiness.Value = polish_amount;
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Emission, out Rhino.Display.Color4f emission))
            {
              var generic_self_illum_filter_map = editableAsset.FindByName(Generic.GenericSelfIllumFilterMap) as AssetPropertyDoubleArray4d;
              generic_self_illum_filter_map.SetValueAsDoubles(new double[] { emission.R, emission.G, emission.B, emission.A });
            }

            if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.DisableLighting, out bool self_illum))
            {
              var generic_self_illum_luminance = editableAsset.FindByName(Generic.GenericSelfIllumLuminance) as AssetPropertyDouble;
              generic_self_illum_luminance.Value = self_illum ? 200000 : 0.0;
            }

            editScope.Commit(false);
          }
        }
      }
#endif

      return appearanceAssetId;
    }

    static ARDB.ElementId ImportMaterial
    (
      ARDB.Document doc,
      Material mat,
      Dictionary<string, ARDB.Material> materials
    )
    {
      var id = ARDB.ElementId.InvalidElementId;

      if(mat.HasName && mat.Name is string materialName)
      {
        materialName = ElementNaming.MakeValidName(materialName);

        if (materials.TryGetValue(materialName, out var material)) id = material.Id;
        else
        {
          id = ARDB.Material.Create(doc, materialName);
          var newMaterial = doc.GetElement(id) as ARDB.Material;

          newMaterial.Color         = mat.PreviewColor.ToColor();
          newMaterial.Shininess     = (int) Math.Round(mat.Shine / Material.MaxShine * 128.0);
          newMaterial.Smoothness    = (int) Math.Round(mat.Reflectivity * 100.0);
          newMaterial.Transparency  = (int) Math.Round(mat.Transparency * 100.0);
          newMaterial.AppearanceAssetId = ImportMaterial(doc, mat.RenderMaterial);

          materials.Add(materialName, newMaterial);
        }
      }

      return id;
    }
    #endregion

    #region Symbols
    static Dictionary<string, ARDB.FamilySymbol> GetSymbolsByName(ARDB.Document doc)
    {
      var collector = new ARDB.FilteredElementCollector(doc);
      return collector.OfCategoryId(doc.OwnerFamily.FamilyCategoryId).
        OfClass(typeof(ARDB.FamilySymbol)).OfType<ARDB.FamilySymbol>().
        GroupBy(x => x.Name).
        ToDictionary(x => x.Key, x => x.First());
    }

    static ARDB.ElementId ImportBlock
    (
      ImportContext context,
      File3dm model,
      InstanceDefinition block
    )
    {
      var id = ARDB.ElementId.InvalidElementId;
      if (block.HasName && block.Name is string blockName)
      {
        blockName = ElementNaming.MakeValidName(blockName);
        if (context.Symbols.TryGetValue(blockName, out var symbol)) id = symbol.Id;
        else
        {
          
        }
      }

      return id;
    }
    #endregion

    static Point3d ImportPlacement(File3dm model, ARDB.ImportPlacement placement)
    {
      switch (placement)
      {
        case ARDB.ImportPlacement.Site:
          return model.Settings.ModelBasepoint;

        case ARDB.ImportPlacement.Origin:
          return Point3d.Origin;

        case ARDB.ImportPlacement.Centered:
          throw new NotImplementedException();

        case ARDB.ImportPlacement.Shared:
          throw new NotImplementedException();
      }

      throw new NotImplementedException();
    }

    static ARDB.XYZ ImportPlacement(ARDB.Document doc, ARDB.ImportPlacement placement)
    {
      switch (placement)
      {
        case ARDB.ImportPlacement.Site:
          return BasePointExtension.GetProjectBasePoint(doc).GetPosition();

        case ARDB.ImportPlacement.Origin:
          return ARDB.XYZ.Zero;

        case ARDB.ImportPlacement.Centered:
          throw new NotImplementedException();

        case ARDB.ImportPlacement.Shared:
          return BasePointExtension.GetSurveyPoint(doc).GetPosition();
      }

      throw new NotImplementedException();
    }

    #region Project
    static IList<ARDB.GeometryObject> ImportObject
    (
      ARDB.Document doc,
      File3dm model,
      GeometryBase geometry,
      ObjectAttributes attributes,
      Dictionary<string, ARDB.Material> materials,
      double scaleFactor,
      Vector3d translationVector,
      bool visibleLayersOnly
    )
    {
      var layer = model.AllLayers.FindIndex(attributes.LayerIndex);
      if (visibleLayersOnly || layer?.IsVisible == true)
      {
        using (var ctx = GeometryEncoder.Context.Push(doc))
        {
          switch (attributes.MaterialSource)
          {
            case ObjectMaterialSource.MaterialFromObject:
              {
                var modelMaterial = attributes.MaterialIndex < 0 ? Material.DefaultMaterial : model.AllMaterials.FindIndex(attributes.MaterialIndex);
                ctx.MaterialId = ImportMaterial(doc, modelMaterial, materials);
                break;
              }
            case ObjectMaterialSource.MaterialFromLayer:
              {
                var modelLayer = model.AllLayers.FindIndex(attributes.LayerIndex);
                var modelMaterial = modelLayer.RenderMaterialIndex < 0 ? Material.DefaultMaterial : model.AllMaterials.FindIndex(modelLayer.RenderMaterialIndex);
                ctx.MaterialId = ImportMaterial(doc, modelMaterial, materials);
                break;
              }
          }

          if (translationVector != Vector3d.Zero)
            geometry.Translate(translationVector);

          if (geometry is InstanceReferenceGeometry instance)
          {
            if (model.AllInstanceDefinitions.FindId(instance.ParentIdefId) is InstanceDefinitionGeometry definition)
            {
              var definitionId = definition.Id.ToString();
              var library = ARDB.DirectShapeLibrary.GetDirectShapeLibrary(doc);
              if (!library.Contains(definitionId))
              {
                var GNodes = definition.GetObjectIds(). // Get idef object ids
                  Select(x => model.Objects.FindId(x)). // Find object instance
                  OfType<File3dmObject>().              // Skip missing objects
                  SelectMany(x => ImportObject(doc, model, x.Geometry, x.Attributes, materials, scaleFactor, Vector3d.Zero, visibleLayersOnly));

                library.AddDefinition(definitionId, GNodes.ToArray());
              }

              return ARDB.DirectShape.CreateGeometryInstance(doc, definitionId, instance.Xform.ToTransform(scaleFactor));
            }
          }
          else return geometry.ToShape(scaleFactor);
        }
      }

      return new ARDB.GeometryObject[0];
    }

    static Result Import3DMFileToProject
    (
      ARDB.Document doc,
      string filePath,
      ARDB.ImportPlacement placement,
      bool visibleLayersOnly,
      ARDB.ElementId categoryId,
      ARDB.WorksetId worksetId,
      string familyName,
      string typeName
    )
    {
      try
      {
        ARDB.DirectShapeLibrary.GetDirectShapeLibrary(doc).Reset();

        using (var model = OpenModel(filePath, out var scaleFactor))
        {
          using (var trans = new ARDB.Transaction(doc, "Import 3D Model"))
          {
            if (trans.Start() == ARDB.TransactionStatus.Started)
            {
              var materials = GetMaterialsByName(doc);

              if (string.IsNullOrEmpty(typeName))
                typeName = Path.GetFileName(filePath);

              var type = default(ARDB.DirectShapeType);
              {
                using (var collector = new ARDB.FilteredElementCollector(doc))
                {
                  var typeCollector = collector.WhereElementIsElementType().
                    WhereElementIsKindOf(typeof(ARDB.DirectShapeType)).
                    OfCategoryId(categoryId).
                    WhereParameterEqualsTo(ARDB.BuiltInParameter.ALL_MODEL_FAMILY_NAME, familyName).
                    WhereParameterEqualsTo(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME, typeName);

                  type = typeCollector.FirstElement() as ARDB.DirectShapeType;
                  if (type is null)
                  {
                    type = ARDB.DirectShapeType.Create(doc, typeName, categoryId);
#if REVIT_2022
                    if (!string.IsNullOrEmpty(familyName))
                      type.SetFamilyName(familyName);
#endif
                  }
                  else
                  {
                    type.SetShape(new ARDB.GeometryObject[0]);
                  }
                }
              }

              // Fill type geometry
              foreach (var obj in model.Objects.Where(x => !x.Attributes.IsInstanceDefinitionObject && x.Attributes.Space == ActiveSpace.ModelSpace))
              {
                if (!obj.Attributes.Visible)
                  continue;

                if (visibleLayersOnly && model.AllLayers.FindIndex(obj.Attributes.LayerIndex)?.IsVisible != true)
                  continue;

                var geometryList = ImportObject
                (
                  doc,
                  model,
                  obj.Geometry,
                  obj.Attributes,
                  materials,
                  scaleFactor,
                  Point3d.Origin - model.Settings.ModelBasepoint,
                  visibleLayersOnly
                ).ToArray();

                if (geometryList?.Length > 0)
                {
                  try { type.AppendShape(geometryList); }
                  catch (Autodesk.Revit.Exceptions.ArgumentException) { }
                }
              }

              var ds = ARDB.DirectShape.CreateElement(doc, type.Category.Id);
              ds.SetTypeId(type.Id);

              var library = ARDB.DirectShapeLibrary.GetDirectShapeLibrary(doc);
              if (!library.ContainsType(type.UniqueId))
                library.AddDefinitionType(type.UniqueId, type.Id);

              var transform = ARDB.Transform.CreateTranslation(model.Settings.ModelBasepoint.ToXYZ(scaleFactor));
              ds.SetShape(ARDB.DirectShape.CreateGeometryInstance(doc, type.UniqueId, transform));

              if (doc.IsWorkshared)
              {
                if (ds.GetParameter(ParameterId.ElemPartitionParam) is ARDB.Parameter worksetParam)
                  worksetParam.Set(worksetId.IntegerValue);
              }

              {
                var from = ImportPlacement(model, placement).ToXYZ(scaleFactor);
                var to = ImportPlacement(doc, placement);
                var placementTranslation = to - from;

                if (!placementTranslation.IsZeroLength())
                  ARDB.ElementTransformUtils.MoveElement(doc, ds.Id, placementTranslation);
              }

              if (trans.Commit() == ARDB.TransactionStatus.Committed)
              {
                var elements = new ARDB.ElementId[] { ds.Id };
                Revit.ActiveUIDocument.Selection.SetElementIds(elements);
                Revit.ActiveUIDocument.ShowElements(elements);

                return Result.Succeeded;
              }
            }
          }
        }
      }
      finally
      {
        ARDB.DirectShapeLibrary.GetDirectShapeLibrary(doc).Reset();
      }

      return Result.Failed;
    }
    #endregion

    #region Family
    class ImportContext
    {
      public double Scale;
      public Transform Transform;
      public bool VisibleLayersOnly;

      public readonly ARDB.Document Document;
      public readonly Dictionary<string, ARDB.Category> Categories;
      public readonly Dictionary<string, ARDB.Material> Materials;
      public readonly Dictionary<string, ARDB.FamilySymbol> Symbols;

      public ImportContext(ARDB.Document document)
      {
        Document = document;
        Categories = GetCategoriesByName(document);
        Materials = GetMaterialsByName(document);
        Symbols = GetSymbolsByName(document);
        Scale = 1.0;
        Transform = Transform.Identity;
        VisibleLayersOnly = false;
      }

      public ImportContext(ImportContext context, Transform transform)
      {
        Document = context.Document;
        Categories = context.Categories;
        Materials = context.Materials;
        Symbols = context.Symbols;
        Scale = context.Scale;
        Transform = context.Transform * transform;
        VisibleLayersOnly = context.VisibleLayersOnly;
      }
    }

    static void ImportObjectsToFamily
    (
      ImportContext context,
      List<ARDB.ElementId> elements,
      File3dm model,
      IEnumerable<File3dmObject> objects
    )
    {
      foreach (var obj in objects)
      {
        if (!obj.Attributes.Visible)
          continue;

        var layer = model.AllLayers.FindIndex(obj.Attributes.LayerIndex);
        if (context.VisibleLayersOnly && layer?.IsVisible != true)
          continue;

        var geometry = default(GeometryBase);
        switch (obj.Geometry)
        {
          case null: continue;
          case InstanceReferenceGeometry instance:

            if (model.AllInstanceDefinitions.FindId(instance.ParentIdefId) is InstanceDefinitionGeometry definition)
            {
              ImportObjectsToFamily
              (
                new ImportContext(context, instance.Xform),
                elements,
                model,
                definition.GetObjectIds().Select(x => model.Objects.FindId(x))
              );
            }

            continue;

          case Extrusion extrusion: geometry = extrusion.ToBrep(); break;
          case SubD subD: geometry = subD.ToBrep(SubDToBrepOptions.Default); break;
          default: geometry = obj.Geometry.Duplicate(); break;
        }

        if (geometry is null)
          continue;

        if (!geometry.Transform(context.Transform))
          continue;

        try
        {
          switch (geometry)
          {
            case Point point:
              if (context.Document.OwnerFamily.IsConceptualMassFamily)
              {
                var referncePoint = context.Document.FamilyCreate.NewReferencePoint(point.Location.ToXYZ(context.Scale));
                elements.Add(referncePoint.Id);
              }
              break;

            case Curve curve:
              if (curve.TryGetPlane(out var plane, GeometryTolerance.Internal.VertexTolerance / context.Scale))
              {
                var sketchPlane = ARDB.SketchPlane.Create(context.Document, plane.ToPlane(context.Scale));
                foreach (var crv in curve.ToCurveMany(context.Scale))
                {
                  var modelCurve = context.Document.FamilyCreate.NewModelCurve(crv, sketchPlane);
                  elements.Add(modelCurve.Id);

                  {
                    var subCategoryId = ImportLayer(context.Document, model, layer, context.Categories, context.Materials);
                    if (ARDB.Category.GetCategory(context.Document, subCategoryId) is ARDB.Category subCategory)
                    {
                      var familyGraphicsStyle = subCategory?.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection);

                      if (familyGraphicsStyle is object)
                        modelCurve.Subcategory = familyGraphicsStyle;
                    }
                  }
                }
              }
              else if (ARDB.DirectShape.IsSupportedDocument(context.Document) && ARDB.DirectShape.IsValidCategoryId(context.Document.OwnerFamily.FamilyCategory.Id, context.Document))
              {
                var subCategoryId = ImportLayer(context.Document, model, layer, context.Categories, context.Materials);
                var shape = curve.ToShape();
                if (shape.Length > 0)
                {
                  var ds = ARDB.DirectShape.CreateElement(context.Document, context.Document.OwnerFamily.FamilyCategory.Id);
                  ds.SetShape(shape);
                  elements.Add(ds.Id);
                }
              }
              break;

            case Brep brep:
              if (brep.ToSolid(context.Scale) is ARDB.Solid solid)
              {
                if (ARDB.FreeFormElement.Create(context.Document, solid) is ARDB.FreeFormElement freeForm)
                {
                  elements.Add(freeForm.Id);

                  {
                    var categoryId = ImportLayer(context.Document, model, layer, context.Categories, context.Materials);
                    if (categoryId != ARDB.ElementId.InvalidElementId)
                      freeForm.GetParameter(ParameterId.FamilyElemSubcategory).Set(categoryId);
                  }

                  if (obj.Attributes.MaterialSource == ObjectMaterialSource.MaterialFromObject)
                  {
                    if (model.AllMaterials.FindIndex(obj.Attributes.MaterialIndex) is Material material)
                    {
                      var categoryId = ImportMaterial(context.Document, material, context.Materials);
                      if (categoryId != ARDB.ElementId.InvalidElementId)
                        freeForm.GetParameter(ParameterId.MaterialIdParam).Set(categoryId);
                    }
                  }
                }
              }
              break;

            case InstanceReferenceGeometry instance:

              break;
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }
    }

    static Result Import3DMFileToFamily
    (
      ARDB.Document doc,
      string filePath,
      ARDB.ImportPlacement placement,
      bool visibleLayersOnly
    )
    {
      using (var model = OpenModel(filePath, out var scaleFactor))
      {
        var translationVector = Point3d.Origin - ImportPlacement(model, placement);

        using (var trans = new ARDB.Transaction(doc, "Import 3D Model"))
        {
          if (trans.Start() == ARDB.TransactionStatus.Started)
          {
            var materials = GetMaterialsByName(doc);
            var categories = GetCategoriesByName(doc);
            var elements = new List<ARDB.ElementId>();

            var view3D = default(ARDB.View);
            using (var collector = new ARDB.FilteredElementCollector(doc))
            {
              var elementCollector = collector.OfClass(typeof(ARDB.View3D));
              view3D = elementCollector.Cast<ARDB.View3D>().FirstOrDefault(x => x.Name == "{3D}");
            }

            if (view3D is object)
            {
              foreach (var cplane in model.AllNamedConstructionPlanes)
              {
                var plane = cplane.Plane;
                var bubbleEnd = plane.Origin.ToXYZ(scaleFactor);
                var freeEnd = (plane.Origin + plane.XAxis).ToXYZ(scaleFactor);
                var cutVec = plane.YAxis.ToXYZ();

                var refrencePlane = doc.FamilyCreate.NewReferencePlane(bubbleEnd, freeEnd, cutVec, view3D);
                refrencePlane.Name = cplane.Name;
                refrencePlane.Maximize3DExtents();
              }
            }

            var context = new ImportContext(doc)
            {
              Scale = scaleFactor,
              Transform = Transform.Translation(translationVector),
              VisibleLayersOnly = visibleLayersOnly
            };

            var objects = model.Objects.Where(x => !x.Attributes.IsInstanceDefinitionObject && x.Attributes.Space == ActiveSpace.ModelSpace);
            ImportObjectsToFamily(context, elements, model, objects);

            if (trans.Commit() == ARDB.TransactionStatus.Committed)
            {
              Revit.ActiveUIDocument.Selection.SetElementIds(elements);
              Revit.ActiveUIDocument.ShowElements(elements);

              return Result.Succeeded;
            }
          }
        }
      }

      return Result.Failed;
    }
    #endregion

    static File3dm OpenModel(string filePath, out double scaleFactor)
    {
      try
      {
        var model = File3dm.Read(filePath);
        scaleFactor = UnitScale.Convert(1.0, (UnitScale) model.Settings.ModelUnitSystem, UnitScale.Internal);

        if (!(External.DB.NumericTolerance.Delta < scaleFactor && scaleFactor < double.PositiveInfinity))
          throw new External.FailException($"Model '{Path.GetFileName(filePath)}' has an unsupported model unit system.\n - Model Unit System = {model.Settings.ModelUnitSystem}.");

        return model;
      }
      catch (Exception e) { throw new External.FailException(e.Message); }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ARDB.ElementSet elements)
    {
      var doc = data.Application.ActiveUIDocument.Document;
      if(!doc.IsFamilyDocument && !ARDB.DirectShape.IsSupportedDocument(doc))
      {
        message = "Active document doesnt't support DirectShape functionality.";
        return Result.Failed;
      }

      using (var options = new Forms.ImportOptionsDialog(data.Application))
      {
        switch (options.ShowModal())
        {
          case DialogResult.Ok:
            if (doc.IsFamilyDocument)
            {
              return Import3DMFileToFamily
              (
                doc,
                options.FileName,
                options.Placement,
                options.VisibleLayersOnly
              );
            }
            else
            {
              if (!ARDB.DirectShape.IsValidCategoryId(options.CategoryId, doc))
              {
                message = "DirectShape functionality doesnt't support selected category on the active document.";
                return Result.Failed;
              }

              return Import3DMFileToProject
              (
                doc,
                options.FileName,
                options.Placement,
                options.VisibleLayersOnly,
                options.CategoryId,
                options.WorksetId,
                options.FamilyName,
                options.TypeName
              );
            }

          case DialogResult.Cancel: return Result.Cancelled;
        }
      }

      return Result.Failed;
    }
  }
}
