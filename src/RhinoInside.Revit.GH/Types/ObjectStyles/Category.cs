using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Category")]
  public class Category : Element, Bake.IGH_BakeAwareElement
  {
    #region IGH_Goo
    public override bool IsValid => (Id?.TryGetBuiltInCategory(out var _) == true) || base.IsValid;
    public override object ScriptVariable() => APIObject;

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      if (source is IGH_Goo goo)
      {
        switch (source)
        {
          case GraphicsStyle style:     source = style.Value; break;
          case Element element:         SetValue(element.Document, element.Category.Id); return element.IsValid;
          case CategoryId catId:        source = (ARDB.BuiltInCategory) catId.Value; break;
          default:                      source = goo.ScriptVariable(); break;
        }
      }

      var document = Revit.ActiveDBDocument;
      var categoryId = ARDB.ElementId.InvalidElementId;

      switch (source)
      {
        case int i:                     categoryId = ElementIdExtension.FromValue(i); break;
        case ARDB.BuiltInCategory bic:  categoryId = new ARDB.ElementId(bic); break;
        case ARDB.ElementId id:         categoryId = id; break;
        case ARDB.Category c:           SetValue(c.Document(), c.Id); return true;
        case ARDB.GraphicsStyle s:      SetValue(s.Document, s.GraphicsStyleCategory.Id); return true;
        case ARDB.Family f:             SetValue(f.Document, f.FamilyCategoryId); return true;
        case ARDB.Element e:
          if(e.Category is ARDB.Category category)
            SetValue(e.Document, category.Id);
          else
            SetValue(default, ARDB.ElementId.InvalidElementId);

          return true;
        case string n:
          if (DBXS.CategoryId.IsCategoryId(n))
          {
            categoryId = new ARDB.ElementId(new DBXS.CategoryId(n));
            break;
          }

          return false;
      }

      if (categoryId.TryGetBuiltInCategory(out var _))
      {
        SetValue(document, categoryId);
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
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

      return base.CastTo(out target);
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
    public override string DisplayName => FullName ?? base.DisplayName;

    internal ARDB.Category APIObject => IsReferencedDataLoaded ? Document.GetCategory(Id) : default;

    protected override void ResetValue()
    {
      _FullName = default;
      _CategoryType = default;
      _IsTagCategory = default;
      _IsSubcategory = default;
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
      // Only cache values that can not change.
      if (Id.IsBuiltInId()) _FullName = value?.FullName();
      _CategoryType = value?.CategoryType;
      _IsTagCategory = value?.IsTagCategory;
      _IsSubcategory = value?.Parent is object;
      _CanAddSubcategory = value?.CanAddSubcategory;
      _AllowsBoundParameters = value?.AllowsBoundParameters;
      _HasMaterialQuantities = value?.HasMaterialQuantities;
      _IsCuttable = value?.IsCuttable;
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

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    // Weights in mm.
    // Almost equal to Model at 1:100, Perspective and Annotation default line weights.
    static readonly double[] PlotWeights = new double[]
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

    static double ToPlotWeight(int? value)
    {
      if (!value.HasValue) return -1.0;

      if (0 < value.Value && value.Value < PlotWeights.Length)
        return PlotWeights[value.Value];

      return 0.0;
    }

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

      const string RootLayerName = "Revit";
      var PS = Layer.PathSeparator;

      if (APIObject is ARDB.Category category)
      {
        var fullLayerName = category.Parent is null ?
          $"{RootLayerName}{PS}{category.CategoryType}{PS}{category.Name}" :
          $"{RootLayerName}{PS}{category.CategoryType}{PS}{category.Parent.Name}{PS}{category.Name}";

        // 2. Check if already exist
        var index = doc.Layers.FindByFullPath(fullLayerName, -1);
        var layer = index < 0 ?
          Layer.GetDefaultLayerProperties() :
          doc.Layers[index];

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (index < 0)
          {
            // Create Root Layer
            new Category(category.Parent).BakeElement(idMap, false, doc, att, out var parentGuid);

            // Create Category Type Layer
            if (category.Parent is null)
            {
              if (Types.CategoryType.NamedValues.TryGetValue((int) category.CategoryType, out var typeName))
              {
                var type = doc.Layers.FindByFullPath($"{RootLayerName}::{category.CategoryType}", -1);
                if (type < 0)
                {
                  var typeLayer = Layer.GetDefaultLayerProperties();
                  typeLayer.ParentLayerId = parentGuid;
                  typeLayer.Name = typeName;
                  type = doc.Layers.Add(typeLayer);
                }

                parentGuid = doc.Layers[type].Id;
              }
            }

            layer.ParentLayerId = parentGuid;
            layer.Name = category.Name;
            layer.IsExpanded = false;
          }

          // Color
          {
            var lineColor = category.LineColor.ToColor();
            layer.Color = lineColor.IsEmpty ? System.Drawing.Color.Black : lineColor;
          }

          // Linetype
          {
            var linetypeIndex = -1;
            if (ProjectionLinePattern is LinePatternElement linePattern)
            {
              if (linePattern.BakeElement(idMap, false, doc, att, out var linetypeGuid))
                linetypeIndex = doc.Linetypes.FindId(linetypeGuid).Index;
            }
            layer.LinetypeIndex = linetypeIndex;
          }

          // Print Width
          {
            layer.PlotWeight = category.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_InvisibleLines ?
              -1.0 : // No Plot
              ToPlotWeight(ProjectionLineWeight);
          }

          // Material
          {
            var materialIndex = -1;
            if (Material is Material material)
            {
              if (material.BakeElement(idMap, false, doc, att, out var materialGuid))
                materialIndex = doc.Materials.FindId(materialGuid).Index;
            }
            layer.RenderMaterialIndex = materialIndex;
          }

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

            case ARDB.BuiltInCategory.OST_LightingFixtureSource:
              layer.Color = System.Drawing.Color.FromArgb(35, layer.Color);
              layer.IsVisible = false;
              break;
          }

          if (index < 0) layer = doc.Layers[doc.Layers.Add(layer)];
          else if (overwrite) doc.Layers.Modify(layer, index, true);
        }

        idMap.Add(Id, guid = layer.Id);
        return true;
      }
      else
      {
        var index = doc.Layers.FindByFullPath(RootLayerName, -1);
        if (index < 0)
        {
          var layer = Layer.GetDefaultLayerProperties();
          {
            layer.Name = RootLayerName;
          }
          index = doc.Layers.Add(layer);
        }

        guid = doc.Layers[index].Id;
        return true;
      }
    }
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
      get => FullName?.Split('\\').Last() ?? base.Nomen;
      set
      {
        base.Nomen = value;
        _FullName = null;
      }
    }

    string _FullName;
    public string FullName => _FullName ?? (Id.IsBuiltInId() ? _FullName = APIObject?.FullName() : APIObject?.FullName());

    ARDB.CategoryType? _CategoryType;
    public ARDB.CategoryType CategoryType => _CategoryType ?? (_CategoryType = (APIObject?.CategoryType ?? ARDB.CategoryType.Invalid)).Value;

    public Category Parent => _IsSubcategory == false ? null : FromCategory(APIObject?.Parent);

    public IEnumerable<Category> SubCategories => APIObject?.SubCategories?.Cast<ARDB.Category>().Select(FromCategory);

    bool? _IsTagCategory;
    public bool? IsTagCategory => _IsTagCategory ?? (_IsTagCategory = APIObject?.IsTagCategory);

    bool? _IsSubcategory;
    public bool? IsSubcategory => _IsSubcategory ?? (_IsSubcategory = APIObject?.Parent is object);

    bool? _CanAddSubcategory;
    public bool? CanAddSubcategory => _CanAddSubcategory ?? (_CanAddSubcategory = APIObject?.CanAddSubcategory);

    bool? _AllowsBoundParameters;
    public bool? AllowsBoundParameters => _AllowsBoundParameters ?? (_AllowsBoundParameters = APIObject?.AllowsBoundParameters);

    bool? _HasMaterialQuantities;
    public bool? HasMaterialQuantities => _HasMaterialQuantities ?? (_HasMaterialQuantities = APIObject?.HasMaterialQuantities);

    bool? _IsCuttable;
    public bool? IsCuttable => _IsCuttable ?? (_IsCuttable = APIObject?.IsCuttable);
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
  public class GraphicsStyle : Element
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

    public override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      if (source is Category category)
      {
        if (category.APIObject.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle style)
        {
          SetValue(style.Document, style.Id);
          return true;
        }
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GraphicsStyle)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(out target);
    }
  }
}
