using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Category")]
  public class Category : Element
  {
    #region IGH_Goo
    protected override Type ScriptVariableType => typeof(DB.Category);
    override public object ScriptVariable() => Value;

    public override sealed bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var categoryId = DB.ElementId.InvalidElementId;

      if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
          source = element.Document?.GetCategory(element.Id);
        else
          source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int i:               categoryId = new DB.ElementId(i); break;
        case DB.ElementId id:     categoryId = id; break;
        case DB.Category c:       SetValue(c.Document(), c.Id); return true;
        case DB.GraphicsStyle s:  SetValue(s.Document, s.GraphicsStyleCategory.Id); return true;
        case DB.Family f:         SetValue(f.Document, f.FamilyCategoryId); return true;
        case DB.Element e:        if(e.Category is DB.Category category)
                                  {
                                    SetValue(e.Document, category.Id);
                                    return true;
                                  }
          break;
      }

      if (categoryId.TryGetBuiltInCategory(out var _))
      {
        SetValue(Revit.ActiveDBDocument, categoryId);
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Category)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new Category owner => base.owner as Category;

      public Proxy(Category c) : base(c) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => !owner.IsReferencedElement || owner.Document is object;
      public override string FormatInstance()
      {
        if (owner.IsReferencedElement && owner.IsElementLoaded)
          return owner.DisplayName;

        return base.FormatInstance();
      }
      public override bool FromString(string str)
      {
        var doc = owner.Document ?? Revit.ActiveUIDocument.Document;

        if (Enum.TryParse(str, out DB.BuiltInCategory builtInCategory))
          owner.SetValue(doc, new DB.ElementId(builtInCategory));
        else if (str == string.Empty)
          owner.SetValue(default, new DB.ElementId(DB.BuiltInCategory.INVALID));
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

        owner.UnloadElement();
        return owner.LoadElement();
      }

      #region Misc
      protected override bool IsValidId(DB.Document doc, DB.ElementId id) => id.IsCategoryId(doc);
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInCategory) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn category Id.")]
      public DB.BuiltInCategory? BuiltInId => owner.Id.TryGetBuiltInCategory(out var bic) ? bic : default;
      #endregion

      #region Category
      const string Category = "Category";

      DB.Category category => owner.Value;

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
      public DB.CategoryType? CategoryType => category?.CategoryType;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Indicates if the category is cuttable or not.")]
      public bool? IsCuttable => category?.IsCuttable;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("The color of lines shown for elements of this category.")]
      public System.Drawing.Color LineColor => category?.LineColor.ToColor() ?? System.Drawing.Color.Empty;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region DocumentObject
    DB.Category category = default;
    public new DB.Category Value
    {
      get
      {
        if (category is null && IsElementLoaded)
          category = Document.GetCategory(Id);

        return category;
      }
    }

    public override string DisplayName
    {
      get
      {
        if (Value is DB.Category category)
          return category.FullName();

        return base.DisplayName;
      }
    }
    #endregion

    #region IGH_ElementId
    protected override void ResetValue()
    {
      category = default;

      base.ResetValue();
    }

    public override bool LoadElement()
    {
      if (IsReferencedElement && !IsElementLoaded)
      {
        Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc);
        doc.TryGetCategoryId(UniqueID, out var id);

        SetValue(doc, id);
      }

      return IsElementLoaded;
    }
    #endregion

    public Category() : base() { }
    public Category(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Category(DB.Category value) : base(value.Document(), value.Id) => category = value;

    protected override bool SetValue(DB.Element element)
    {
      if (DocumentExtension.AsCategory(element) is DB.Category cat)
      {
        SetValue(element.Document, element.Id);
        category = cat;
        return true;
      }

      return false;
    }

    public static Category FromCategory(DB.Category category)
    {
      if (category is null)
        return null;

      return new Category(category);
    }

    new public static Category FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsCategoryId(doc))
        return new Category(doc, id);

      return null;
    }

    #region Properties
    public override string Name
    {
      get => Value?.Name;
      set
      {
        if (value is object && Value is DB.Category category && category.Name != value)
        {
          if (Id.IsBuiltInId())
            throw new InvalidOperationException($"BuiltIn category '{category.FullName()}' does not support assignment of a user-specified name.");

          base.Name = value;
        }
      }
    }

    public System.Drawing.Color? LineColor
    {
      get => Value?.LineColor.ToColor();
      set
      {
        if (value is object && Value is DB.Category category)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != category.LineColor)
              category.LineColor = color;
          }
        }
      }
    }

    public Material Material
    {
      get => Value is DB.Category category ? new Material(category.Material) : default;
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(Material));
          if ((category.Material?.Id ?? DB.ElementId.InvalidElementId) != value.Id)
            category.Material = value.Value;
        }
      }
    }

    public int? ProjectionLineWeight
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle _)
            return category.GetLineWeight(DB.GraphicsStyleType.Projection);
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle _)
          {
            if (category.GetLineWeight(DB.GraphicsStyleType.Projection) != value)
              category.SetLineWeight(value.Value, DB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public int? CutLineWeight
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle _)
            return category.GetLineWeight(DB.GraphicsStyleType.Cut);
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle _)
          {
            if (category.GetLineWeight(DB.GraphicsStyleType.Cut) != value)
              category.SetLineWeight(value.Value, DB.GraphicsStyleType.Cut);
          }
        }
      }
    }

    public LinePatternElement ProjectionLinePattern
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(DB.GraphicsStyleType.Projection));
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(ProjectionLinePattern));
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle style)
          {
            if (category.GetLinePatternId(DB.GraphicsStyleType.Projection) != value.Id)
              category.SetLinePatternId(value.Id, DB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public LinePatternElement CutLinePattern
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(DB.GraphicsStyleType.Cut));
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(CutLinePattern));
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle style)
          {
            if (category.GetLinePatternId(DB.GraphicsStyleType.Cut) != value.Id)
              category.SetLinePatternId(value.Id, DB.GraphicsStyleType.Cut);
          }
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Graphics Style")]
  public class GraphicsStyle : Element
  {
    protected override Type ScriptVariableType => typeof(DB.GraphicsStyle);
    public new DB.GraphicsStyle Value => base.Value as DB.GraphicsStyle;
    public static explicit operator DB.GraphicsStyle(GraphicsStyle value) => value?.Value;

    public GraphicsStyle() { }
    public GraphicsStyle(DB.GraphicsStyle graphicsStyle) : base(graphicsStyle) { }

    public override string DisplayName
    {
      get
      {
        var graphicsStyle = (DB.GraphicsStyle) this;
        if (graphicsStyle is object)
        {
          var tip = string.Empty;
          if (graphicsStyle.GraphicsStyleCategory.Parent is DB.Category parent)
            tip = $"{parent.Name} : ";

          switch (graphicsStyle.GraphicsStyleType)
          {
            case DB.GraphicsStyleType.Projection: return $"{tip}{graphicsStyle.Name} [projection]";
            case DB.GraphicsStyleType.Cut:        return $"{tip}{graphicsStyle.Name} [cut]";
          }
        }

        return base.DisplayName;
      }
    }
  }
}
