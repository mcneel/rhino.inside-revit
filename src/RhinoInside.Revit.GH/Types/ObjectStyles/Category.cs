using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Render;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;
using OS = System.Environment;
#if RHINO_8
using Grasshopper.Rhinoceros;
using Grasshopper.Rhinoceros.Model;
using Grasshopper.Rhinoceros.Drafting;
using Grasshopper.Rhinoceros.Render;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Category")]
  public sealed class Category : Element, Bake.IGH_BakeAwareElement
  {
    #region IGH_Goo
    public override bool IsValid => (Id?.TryGetBuiltInCategory(out var _) == true) || base.IsValid;
    public override object ScriptVariable() => APIObject;

    public sealed override bool ConvertFrom(object source)
    {
      if (base.ConvertFrom(source))
        return true;

      if (source is IGH_Goo goo)
      {
        switch (source)
        {
          case CategoryId catId:        source = (ARDB.BuiltInCategory) catId.Value; break;
          default:                      source = goo.ScriptVariable(); break;
        }
      }

      var document = Revit.ActiveDBDocument;
      var categoryId = ElementIdExtension.Invalid;

      switch (source)
      {
        case null:                      return false;
        case int i:                     categoryId = ElementIdExtension.FromValue(i); break;
        case ARDB.BuiltInCategory bic:  categoryId = new ARDB.ElementId(bic); break;
        case ARDB.ElementId id:         categoryId = id; break;
        case ARDB.Category c:           SetValue(c.Document(), c.Id); return true;
        case string n:
          if (!DBXS.CategoryId.TryParse(n, null, out var cid)) return false;
          categoryId = new ARDB.ElementId(cid);

          break;
      }

      if (categoryId.TryGetBuiltInCategory(out var _))
      {
        SetValue(document, categoryId);
        return true;
      }

      return false;
    }

    public override bool ConvertTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Category)))
      {
        target = (Q) (object) APIObject;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(CategoryId)))
      {
        var categoryId = new CategoryId();
        if (APIObject.Id.TryGetBuiltInCategory(out var bic))
        {
          categoryId.Value = bic;
          target = (Q) (object) categoryId;
          return true;
        }
        else
        {
          target = (Q) (object) default(Q);
          return false;
        }
      }

#if RHINO_8
      if (typeof(Q).IsAssignableFrom(typeof(ModelLayer)))
      {
        target = (Q) (object) ToModelContent(new Dictionary<ARDB.ElementId, ModelContent>());
        return true;
      }
#endif

      return base.ConvertTo(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new Category owner => base.owner as Category;

      public Proxy(Category c) : base(c) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => !owner.IsReferencedData || owner.Document is object;
      public override string FormatInstance()
      {
        if (owner.IsReferencedData && owner.IsReferencedDataLoaded)
          return owner.DisplayName;

        return base.FormatInstance();
      }
      public override bool FromString(string str)
      {
        var doc = owner.Document ?? Revit.ActiveUIDocument.Document;

        if (Enum.TryParse(str, out ARDB.BuiltInCategory builtInCategory) && builtInCategory.IsValid())
          owner.SetValue(doc, new ARDB.ElementId(builtInCategory));
        else if (str == string.Empty)
          owner.SetValue(default, new ARDB.ElementId(ARDB.BuiltInCategory.INVALID));
        else if (doc is object)
        {
          foreach (var category in doc.GetCategories())
          {
            if (category.FullName() == str)
            {
              owner.SetValue(doc, category.Id);
              break;
            }
          }
        }
        else
          return false;

        owner.UnloadReferencedData();
        return owner.LoadReferencedData();
      }

      #region Misc
      protected override bool IsValidId(ARDB.Document doc, ARDB.ElementId id) => id.IsCategoryId(doc);
      public override Type ObjectType => IsBuiltIn ? typeof(ARDB.BuiltInCategory) : base.ObjectType;

      [System.ComponentModel.Description("Category BuiltIn Id.")]
      public ARDB.BuiltInCategory? BuiltInId => owner.Id.TryGetBuiltInCategory(out var bic) ? bic : default;

      [System.ComponentModel.Description("Category Schema Id.")]
      public DBXS.CategoryId SchemaId => owner.Id.TryGetBuiltInCategory(out var bic) ? (DBXS.CategoryId) bic : default;
      #endregion

      #region Category
      const string Category = "Category";

      ARDB.Category category => owner.APIObject;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Parent category of this category.")]
      public string Parent => category?.Parent?.Name;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Category can have project parameters.")]
      public bool? AllowsParameters => category?.AllowsBoundParameters;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Identifies if the category is associated with a type of tag for a different category.")]
      public bool? IsTag => category?.IsTagCategory;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Material of the category.")]
      public string Material => category?.Material?.Name;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Identifies if elements of the category are able to report what materials they contain in what quantities.")]
      public bool? HasMaterialQuantities => category?.HasMaterialQuantities;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Category type of this category.")]
      public ARDB.CategoryType? CategoryType => category?.CategoryType;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Indicates if the category is cuttable or not.")]
      public bool? IsCuttable => category?.IsCuttable;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("The color of lines shown for elements of this category.")]
      public System.Drawing.Color LineColor => category?.LineColor.ToColor() ?? System.Drawing.Color.Empty;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region DocumentObject
    internal ARDB.Category APIObject => IsReferencedDataLoaded ?
      (IsLinked ? ReferenceDocument : Document).GetCategory(Id) : default;

    protected override void ResetValue()
    {
      _FullName = default;
      _CategoryDiscipline = default;
      _CategoryType = default;
      _IsTagCategory = default;
      _IsSubcategory = default;
      _IsVisibleInUI = default;
      _CanAddSubcategory = default;
      _AllowsBoundParameters = default;
      _HasMaterialQuantities = default;
      _IsCuttable = default;

      base.ResetValue();
    }

    protected override bool SetValue(ARDB.Element element)
    {
      if (DocumentExtension.AsCategory(element) is ARDB.Category)
        return base.SetValue(element);

      return false;
    }
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => APIObject is ARDB.Category category ?
      !category.IsReadOnly && !Document.IsLinked : default(bool?);
    #endregion

    public Category() : base() { }
    public Category(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Category(ARDB.Category value) : base(value.Document(), value?.Id ?? ARDB.ElementId.InvalidElementId)
    {
      if (value is null) return;

      // Only cache values that can not change.
      if (Id.IsBuiltInId()) _FullName = value.FullName();
      _CategoryDiscipline = value.CategoryDiscipline();
      _CategoryType = value.CategoryType;
      _IsTagCategory = value.IsTagCategory;
      _IsSubcategory = value.Parent is object;
      _IsVisibleInUI = value.IsVisibleInUI();
      _CanAddSubcategory = value.CanAddSubcategory;
      _AllowsBoundParameters = value.AllowsBoundParameters;
      _HasMaterialQuantities = value.HasMaterialQuantities;
      _IsCuttable = value.IsCuttable;
    }

    public static Category FromCategory(ARDB.Category category)
    {
      if (category is null)
        return null;

      return new Category(category);
    }

    public static new Category FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id.IsCategoryId(doc))
        return new Category(doc, id);

      return null;
    }

    #region LineWeight
    // Weights in mm.
    // Almost equal to Model at 1:100, Perspective and Annotation default line weights.
    static readonly double[] LineWeights = new double[]
    {
      0.0,
      0.1,
      0.18,
      0.25,
      0.35,
      0.5,
      0.7,
      1.0,
      1.4,
      2.0,
      2.8,
      4.0,
      5.0,
      6.0,
      7.0,
      8.0,
      10.0
    };

    static double ToLineWeight(int? value)
    {
      if (!value.HasValue) return -1.0;

      if (0 < value.Value && value.Value < LineWeights.Length)
        return LineWeights[value.Value];

      return 0.0;
    }
    #endregion

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

      if (APIObject is ARDB.Category category)
      {
        var elementPath = GetElementPath().ToArray();

        // 2. Check if already exist
        var index = doc.Layers.FindByFullPath(string.Join("::", elementPath), -1);
        var layer = index >= 0 && index < doc.Layers.Count ? doc.Layers[index] : null;

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (index < 0)
          {
            // Bake parent layers and create current one
            var name = default(string);
            var parentId = Guid.Empty;
            {
              if (Parent is Category parent)
              {
                parent.BakeElement(idMap, overwrite: true, doc, default, out parentId);
                name = elementPath.LastOrDefault();
              }
              else
              {
                var added = false;
                foreach (var token in elementPath.TakeButLast(out name))
                {
                  if (!added && doc.Manifest.FindName(token, ModelComponentType.Layer, parentId) is Layer current)
                  {
                    parentId = current.Id;
                  }
                  else
                  {
                    current = new Layer
                    {
                      Index = -1,
                      IsExpanded = false,
                      ParentLayerId = parentId,
                      Name = token,
                      Color = System.Drawing.Color.FromArgb(23, 107, 254),
                    };
                    parentId = doc.Layers[doc.Layers.Add(current)].Id;
                    added = true;
                  }
                }
              }

              layer = new Layer
              {
                Index = -1,
                Name = name,
                IsExpanded = false,
                ParentLayerId = parentId
              };
            }
          }

          // Linetype
          {
            layer.LinetypeIndex = ProjectionLinePattern.BakeElement(idMap, false, doc, att, out var linetypeGuid) ?
                                  doc.Linetypes.FindId(linetypeGuid).Index :
                                  -1; // Continuous
          }

          // Print Width
          {
            layer.PlotWeight = category.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_InvisibleLines ?
              -1.0 : // No Plot
              ToLineWeight(ProjectionLineWeight);
          }

          // LineColor
          {
            var lineColor = category.LineColor.ToColor();
            layer.PlotColor = lineColor.IsEmpty ? System.Drawing.Color.Black : lineColor;
          }

          // Color
          {
            if (category.Material is ARDB.Material material)
            {
              var color = category.Material.Color.ToColor();
              var alpha = 255 - (int) (category.Material.Transparency * 2.55);
              layer.Color = System.Drawing.Color.FromArgb(alpha, color);
            }
            else layer.Color = System.Drawing.Color.FromArgb(0x7F, 0x7F, 0x7F);
          }

          // Material
          if (category.CategoryType != ARDB.CategoryType.Annotation)
          {
            var materialIndex = -1;
            {
              var material = new Material(category.Material);
              if (material.BakeElement(idMap, false, doc, att, out var renderMaterialInstanceId))
              {
                materialIndex = layer.RenderMaterialIndex;
                var layerMaterial = materialIndex >= 0 && materialIndex < doc.Materials.Count ? doc.Materials[materialIndex] : null;
                if (layerMaterial?.IsDeleted is true || layerMaterial?.IsReference is true) layerMaterial = null;
                if (layerMaterial?.RenderMaterialInstanceId != renderMaterialInstanceId)
                {
                  layerMaterial = doc.RenderMaterials.Find(renderMaterialInstanceId).ToMaterial(RenderTexture.TextureGeneration.Allow);
                  layerMaterial.RenderMaterialInstanceId = renderMaterialInstanceId;

                  if (materialIndex < 0) materialIndex = doc.Materials.Add(layerMaterial);
                  else doc.Materials.Modify(layerMaterial, materialIndex, quiet: true);
                }
              }
            }
            layer.RenderMaterialIndex = materialIndex;
          }

#if RHINO_8
          // Section style
          if (ToSectionStyle() is SectionStyle sectionStyle)
          {
            var material = Material;
            if (!material.IsEmpty)
            {
              sectionStyle.HatchPatternColor = material.CutForegroundPatternColor ?? System.Drawing.Color.Black;
              if (material.CutForegroundPattern?.BakeElement(idMap, overwrite: false, doc, null, out var patternId) is true)
                sectionStyle.HatchIndex = doc.HatchPatterns.FindId(patternId).Index;

              if (material.CutBackgroundPattern.Value?.GetFillPattern()?.IsSolidFill is true)
              {
                sectionStyle.BackgroundFillColor = material.CutBackgroundPatternColor ?? System.Drawing.Color.White;
                sectionStyle.BackgroundFillMode = SectionBackgroundFillMode.SolidColor;
              }
              else
              {
                sectionStyle.BackgroundFillMode = SectionBackgroundFillMode.Viewport;
              }
            }

            layer.SetCustomSectionStyle(sectionStyle);
          }
          else if (layer.GetCustomSectionStyle() is object)
          {
            layer.RemoveCustomSectionStyle();
          }
#endif

          // Some hardcoded tweaks…
          switch (Id.ToBuiltInCategory())
          {
            case ARDB.BuiltInCategory.OST_Views:
              layer.IsLocked = true;
              break;

            case ARDB.BuiltInCategory.OST_Levels:
            case ARDB.BuiltInCategory.OST_Grids:
              layer.Color = System.Drawing.Color.FromArgb(35, layer.Color);
              layer.IsLocked = true;
              break;

            case ARDB.BuiltInCategory.OST_VolumeOfInterest:
              layer.PlotWeight = -1.0;
#if RHINO_8
              layer.PerViewportIsVisibleInNewDetails = false;
#endif
              break;

            case ARDB.BuiltInCategory.OST_GridChains:
              layer.Color = System.Drawing.Color.FromArgb(35, layer.Color);
              break;

            case ARDB.BuiltInCategory.OST_LightingFixtureSource:
              layer.IsVisible = false;
              break;
          }

          if (index < 0) layer = doc.Layers[doc.Layers.Add(layer)];
          else if (overwrite) doc.Layers.Modify(layer, index, true);
        }

        if (layer is object)
        {
          idMap.Add(Id, guid = layer.Id);
          return true;
        }
      }

      guid = default;
      return false;
    }
    #endregion

    #region ModelContent
    private IEnumerable<string> GetElementPath()
    {
      if (APIObject is ARDB.Category category)
      {
        var parent = Parent;
        if (parent is object)
        {
          foreach (var token in parent.GetElementPath())
            yield return token;
        }
        else
        {
          yield return "Revit";

          if (category.Id.TryGetBuiltInCategory(out var bic) && bic != ARDB.BuiltInCategory.OST_ImportObjectStyles)
          {
            if (category.IsTagCategory)
            {
              yield return "Tags";
            }
            else if (Types.CategoryType.NamedValues.TryGetValue((int) category.CategoryType, out var typeName))
            {
              yield return typeName;
            }
            else
            {
              yield return "~";
            }
          }
          else yield return "Imports";
        }

        if (category.Id.IsBuiltInId() || parent is object)
          yield return Nomen;
        else
          yield return $"<{Nomen}>";
      }
    }

    protected override string ElementPath => string.Join("::", GetElementPath());

#if RHINO_8
    internal ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (APIObject is ARDB.Category category)
      {
        var attributes = new ModelLayer.Attributes();

        // Path
        {
          attributes.Path = ElementPath;
        }

        // Tags
        {
          var root = category.Parent ?? category;
          if (root.Id.IsBuiltInId())
          {
            var disciplines = CategoryDiscipline;
            attributes.Tags = new ModelTags(Enum.GetValues(typeof(ERDB.CategoryDiscipline)).Cast<ERDB.CategoryDiscipline>().Where(x => x != ERDB.CategoryDiscipline.None && disciplines.HasFlag(x)).Select(x => x.ToString()));
          }
        }

        // Linetype
        {
          attributes.Linetype = ProjectionLinePattern?.ToModelContent(idMap) as ModelLinetype ?? ModelLinetype.Unset;
        }

        // LineWeight
        {
          attributes.LineWeight = category.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_InvisibleLines ?
            -1.0 : // No Plot
            ToLineWeight(ProjectionLineWeight);
        }

        // LineColor
        {
          //var lineColor = category.LineColor.ToColor();
          //attributes.DraftingColor = lineColor.IsEmpty ? System.Drawing.Color.Black : lineColor;
        }

        // Color
        {
          var lineColor = category.LineColor.ToColor();
          attributes.DisplayColor = lineColor.IsEmpty ? System.Drawing.Color.Black : lineColor;
        }

        // Material
        var material = Material;
        {
          attributes.Material = material.ToModelContent(idMap) as ModelRenderMaterial;
        }

        // Some hardcoded tweaks…
        switch (Id.ToBuiltInCategory())
        {
          case ARDB.BuiltInCategory.OST_Views:
            attributes.Locked = true;
            break;

          case ARDB.BuiltInCategory.OST_Levels:
          case ARDB.BuiltInCategory.OST_Grids:
            attributes.DisplayColor = System.Drawing.Color.FromArgb(35, attributes.DisplayColor.Value);
            attributes.Locked = true;
            break;

          case ARDB.BuiltInCategory.OST_LightingFixtureSource:
            attributes.DisplayColor = System.Drawing.Color.FromArgb(35, attributes.DisplayColor.Value);
            attributes.Hidden = true;
            break;
        }

        idMap.Add(Id, modelContent = attributes.ToModelData() as ModelContent);
        return modelContent;
      }

      return null;
    }

    private SectionStyle ToSectionStyle()
    {
      if (APIObject is ARDB.Category category && category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is object)
      {
        var style = new SectionStyle();

        var linetype = CutLinePattern?.ToLinetype();
        {
          if (linetype is null) { linetype = new Linetype(); linetype.SetSegments(new double[] { 1.0 }); }
          linetype.Name = $"{Nomen} [cut]";
          linetype.WidthUnits = Rhino.UnitSystem.Millimeters;
          linetype.Width = ToLineWeight(CutLineWeight);
          style.SetBoundaryLinetype(linetype);
        }

        return style;
      }

      return null;
    }
#endif
    #endregion

    #region Properties
    public override string NextIncrementalNomen(string prefix)
    {
      if (APIObject is ARDB.Category category)
      {
        DocumentExtension.TryParseNomenId(prefix, out prefix, out var _);
        var nextName = category.Parent?.SubCategories.
          Cast<ARDB.Category>().
          Select(x => x.Name).
          WhereNomenPrefixedWith(prefix).
          NextNomenOrDefault() ?? $"{prefix} 1";

        return nextName;
      }

      return default;
    }

    public override string Nomen
    {
      get => CategoryNaming.SplitFullName(CompleteNomen, out var _) ?? base.Nomen;
      set
      {
        base.Nomen = value;
        _FullName = null;
      }
    }

    private new ARDB.BuiltInCategory? BuiltInCategory => Id?.ToBuiltInCategory();

    string _FullName;
    public override string CompleteNomen => _FullName ?? (APIObject?.FullName() ?? BuiltInCategory?.FullName(localized: true));

    ERDB.CategoryDiscipline? _CategoryDiscipline;
    public ERDB.CategoryDiscipline CategoryDiscipline => _CategoryDiscipline ??= APIObject?.CategoryDiscipline() ?? BuiltInCategory?.CategoryDiscipline() ?? ERDB.CategoryDiscipline.None;

    ARDB.CategoryType? _CategoryType;
    public ARDB.CategoryType CategoryType => _CategoryType ??= APIObject?.CategoryType ?? BuiltInCategory?.CategoryType() ?? ARDB.CategoryType.Invalid;

    public Category Parent => _IsSubcategory == false ? null :
      APIObject is object ? FromCategory(APIObject.Parent) :
      BuiltInCategory is object ? FromElementId(null, new ARDB.ElementId(BuiltInCategory.Value.Parent())) :
      null;

    public IEnumerable<Category> SubCategories => APIObject?.
      SubCategories?.
      Cast<ARDB.Category>().
      OrderBy(x => x.Id.ToValue()).
      Select(FromCategory);

    bool? _IsTagCategory;
    public bool? IsTagCategory => _IsTagCategory ??= APIObject?.IsTagCategory ?? BuiltInCategory?.IsTagCategory();

    bool? _IsSubcategory;
    public bool? IsSubcategory => _IsSubcategory ??=
    (
      APIObject is object ? APIObject.Parent is object :
      BuiltInCategory is object ? BuiltInCategory.Value.Parent() != ARDB.BuiltInCategory.INVALID :
      default(bool?)
    );

    bool? _IsVisibleInUI;
    public bool? IsVisibleInUI => _IsVisibleInUI ??= APIObject?.IsVisibleInUI() ?? BuiltInCategory?.IsVisibleInUI();

    bool? _CanAddSubcategory;
    public bool? CanAddSubcategory => _CanAddSubcategory ??= APIObject?.CanAddSubcategory ?? BuiltInCategory?.CanAddSubcategory();

    bool? _AllowsBoundParameters;
    public bool? AllowsBoundParameters => _AllowsBoundParameters ??= APIObject?.AllowsBoundParameters ?? BuiltInCategory?.AllowsBoundParameters();

    bool? _HasMaterialQuantities;
    public bool? HasMaterialQuantities => _HasMaterialQuantities ??= APIObject?.HasMaterialQuantities ?? BuiltInCategory?.HasMaterialQuantities();

    bool? _IsCuttable;
    public bool? IsCuttable => _IsCuttable ??= APIObject?.IsCuttable ?? BuiltInCategory?.IsCuttable();
    #endregion

    #region Object Style
    public System.Drawing.Color? LineColor
    {
      get => APIObject?.LineColor.ToColor();
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          if (category.LineColor.ToColor() != value.Value)
            category.LineColor = value.Value.ToColor();
        }
      }
    }

    public Material Material
    {
      get => APIObject is ARDB.Category category ? new Material(category.Material) : default;
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(Material));
          if ((category.Material?.Id ?? ARDB.ElementId.InvalidElementId) != value.Id)
            category.Material = value.Value;
        }
      }
    }

    public int? ProjectionLineWeight
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle _)
            return category.GetLineWeight(ARDB.GraphicsStyleType.Projection);
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle _)
          {
            if (category.GetLineWeight(ARDB.GraphicsStyleType.Projection) != value)
              category.SetLineWeight(value.Value, ARDB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public int? CutLineWeight
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle _)
            return category.GetLineWeight(ARDB.GraphicsStyleType.Cut);
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle _)
          {
            if (category.GetLineWeight(ARDB.GraphicsStyleType.Cut) != value)
              category.SetLineWeight(value.Value, ARDB.GraphicsStyleType.Cut);
          }
        }
      }
    }

    public LinePatternElement ProjectionLinePattern
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(ARDB.GraphicsStyleType.Projection));
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(ProjectionLinePattern));
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle)
          {
            if (category.GetLinePatternId(ARDB.GraphicsStyleType.Projection) != value.Id)
              category.SetLinePatternId(value.Id, ARDB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public LinePatternElement CutLinePattern
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(ARDB.GraphicsStyleType.Cut));
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(CutLinePattern));
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle)
          {
            if (category.GetLinePatternId(ARDB.GraphicsStyleType.Cut) != value.Id)
              category.SetLinePatternId(value.Id, ARDB.GraphicsStyleType.Cut);
          }
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Line Style")]
  public sealed class GraphicsStyle : Element
  {
    protected override Type ValueType => typeof(ARDB.GraphicsStyle);
    public new ARDB.GraphicsStyle Value => base.Value as ARDB.GraphicsStyle;

    public GraphicsStyle() { }
    public GraphicsStyle(ARDB.GraphicsStyle graphicsStyle) : base(graphicsStyle) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.GraphicsStyle style)
        {
          var tip = string.Empty;
          //if (style.GraphicsStyleCategory.Parent is ARDB.Category parent)
          //  tip = $"{parent.Name}\\";

          switch (style.GraphicsStyleType)
          {
            case ARDB.GraphicsStyleType.Projection: return $"{tip}{style.Name} [projection]";
            case ARDB.GraphicsStyleType.Cut:        return $"{tip}{style.Name} [cut]";
          }
        }

        return base.DisplayName;
      }
    }

    public Category GraphicsStyleCategory
    {
      get
      {
        if (Value is ARDB.GraphicsStyle style)
        {
          return style.GraphicsStyleCategory is ARDB.Category category ?
            GetElement(Category.FromCategory(category)) :
            new Category();
        }

        return default;
      }
    }

    public override bool ConvertFrom(object source)
    {
      if (base.ConvertFrom(source))
        return true;

      if (source is Category category)
      {
        if (category.APIObject.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle style)
        {
          SetValue(style.Document, style.Id);
          return true;
        }
      }
      else if (source is GeometryObject geometry)
      {
        if (geometry.Value is ARDB.GeometryObject geometryObject)
        {
          SetValue(geometry.Document, geometryObject.GraphicsStyleId);
          return true;
        }
      }

      return false;
    }

    public override bool ConvertTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GraphicsStyle)))
      {
        target = (Q) (object) Value;
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(Category)))
      {
        target = (Q) (object) GraphicsStyleCategory;
        return true;
      }

#if RHINO_8
      if (typeof(Q).IsAssignableFrom(typeof(ModelLayer)))
      {
        target = (Q) (object) ToModelContent(new Dictionary<ARDB.ElementId, ModelContent>());
        return true;
      }
#endif

      return base.ConvertTo(out target);
    }

    #region ModelContent
#if RHINO_8
    internal ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (Value is ARDB.GraphicsStyle style && style.GraphicsStyleType == ARDB.GraphicsStyleType.Projection)
        return new Category(style.GraphicsStyleCategory).ToModelContent(new Dictionary<ARDB.ElementId, ModelContent>());

      return null;
    }
#endif
    #endregion
  }
}
