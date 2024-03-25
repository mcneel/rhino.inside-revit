using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static partial class BuiltInCategoryExtension
  {
    static Document _HiddenInUIBuiltInCategoriesDocument;
    static BuiltInCategory[] _HiddenInUIBuiltInCategories;

    /// <summary>
    /// Set of hidden <see cref="Autodesk.Revit.DB.BuiltInCategory"/> enum values.
    /// </summary>
    /// <param name="document"></param>
    public static IReadOnlyCollection<BuiltInCategory> GetHiddenInUIBuiltInCategories(Document document)
    {
      if (!document.IsEquivalent(_HiddenInUIBuiltInCategoriesDocument))
      {
        _HiddenInUIBuiltInCategories = BuiltInCategories.Where(x => document.GetCategory(x)?.IsVisibleInUI() != true).ToArray();
        _HiddenInUIBuiltInCategoriesDocument = document;
      }

      return _HiddenInUIBuiltInCategories;
    }

    /// <summary>
    /// Checks if a <see cref="Autodesk.Revit.DB.BuiltInCategory"/> is valid.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInCategory value)
    {
      if (-3000000 < (int) value && (int) value < -2000000)
        return _BuiltInCategories.Contains(value);

      return false;
    }

    public static string Name(this BuiltInCategory value, bool localized = false)
    {
#if REVIT_2020
      if (localized && value.IsValid()) return LabelUtils.GetLabelFor(value);
#endif
      if (Definitions.TryGetValue(value, out var definition)) return definition.Name;

      return string.Empty;
    }

    public static string FullName(this BuiltInCategory value, bool localized = false)
    {
      if (!Definitions.TryGetValue(value, out var definition)) return string.Empty;
      if (!definition.Parent.IsValid()) return definition.Id.Name(localized);
      return $"{definition.Parent.FullName(localized)}\\{definition.Id.Name(localized)}";
    }

    public static CategoryType CategoryType(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.CategoryType : Autodesk.Revit.DB.CategoryType.Invalid;
    public static bool IsTagCategory(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsTagCategory : false;

    public static BuiltInCategory Parent(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.Parent : Autodesk.Revit.DB.BuiltInCategory.INVALID;
    public static bool CanAddSubcategory(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.CanAddSubcategory : false;

    public static bool AllowsBoundParameters(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.AllowsBoundParameters : false;
    public static bool HasMaterialQuantities(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.HasMaterialQuantities : false;
    public static bool IsCuttable(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsCuttable : false;
    public static bool IsVisibleInUI(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsVisibleInUI : false;
    public static CategoryDiscipline CategoryDiscipline(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.Discipline : DB.CategoryDiscipline.None;

    internal partial struct Definition
    {
      public Definition
      (
        string fullName,
        CategoryType categoryType,
        bool isTagCategory,
        BuiltInCategory parent,
        BuiltInCategory id,
        bool canAddSubcategory,
        bool allowsBoundParameters,
        bool hasMaterialQuantities,
        bool isCuttable,
        bool isVisibleInUI,
        DB.CategoryDiscipline discipline = DB.CategoryDiscipline.None
      )
      {
        Name = fullName;

        CategoryType = categoryType;
        IsTagCategory = isTagCategory;

        Parent = parent;
        Id = id;
        CanAddSubcategory = canAddSubcategory;

        AllowsBoundParameters = allowsBoundParameters;
        HasMaterialQuantities = hasMaterialQuantities;
        IsCuttable = isCuttable;
        IsVisibleInUI = isVisibleInUI;
        Discipline = discipline;
      }

      public readonly string Name;

      public readonly CategoryType CategoryType;
      public readonly bool IsTagCategory;

      public readonly BuiltInCategory Parent;
      public readonly BuiltInCategory Id;
      public readonly bool CanAddSubcategory;

      public readonly bool AllowsBoundParameters;
      public readonly bool HasMaterialQuantities;
      public readonly bool IsCuttable;
      public readonly bool IsVisibleInUI;

      public CategoryDiscipline Discipline;

#if DEBUG
      static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
      static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());

      #region CategoryDefinition
      static Definition ToDefinition(BuiltInCategory value, Document document)
      {
        var category = document.GetCategory(value);

        return new Definition
        (
          fullName: category?.Name ?? string.Empty,
          categoryType: category?.CategoryType ?? CategoryType.Invalid,
          isTagCategory: category?.IsTagCategory ?? false,
          parent: category.Parent?.Id.ToBuiltInCategory() ?? BuiltInCategory.INVALID,
          id: value,
          canAddSubcategory: category?.CanAddSubcategory ?? false,
          allowsBoundParameters: category?.AllowsBoundParameters ?? false,
          hasMaterialQuantities: category?.HasMaterialQuantities ?? false,
          isCuttable: category?.IsCuttable ?? false,
          isVisibleInUI: category?.IsVisibleInUI() ?? false
        );
      }

      static IEnumerable<Definition> GetDefinitions(Document document)
      {
        return BuiltInCategories.Select(bic => ToDefinition(bic, document)).
          OrderBy(x => x.CategoryType).
          ThenBy(x => x.IsTagCategory).
          ThenBy(x => (x.Parent.IsValid() ? x.Parent : x.Id).ToString()).
          ThenBy(x => x.Parent.IsValid()).
          ThenBy(x => x.Id.ToString()).
          ToList();
      }

      static void WriteDefinitions(IEnumerable<Definition> definitions, string versionNumber)
      {
        var path = Path.Combine
        (
          SourceCodePath,
          "..",
          "Schemas",
          versionNumber,
          "BuiltInCategory.cs"
        );

        using (var writer = new System.CodeDom.Compiler.IndentedTextWriter(File.CreateText(path), "  "))
        {
          writer.WriteLine("using System.Collections.Generic;");
          writer.WriteLine("using System.Linq;");
          writer.WriteLine("using Autodesk.Revit.DB;");
          writer.WriteLine();
          writer.WriteLine("namespace RhinoInside.Revit.External.DB.Extensions");
          writer.WriteLine("{");
          writer.Indent++;

          writer.WriteLine("using BIC = BuiltInCategory;");
          writer.WriteLine("using CT = CategoryType;");
          writer.WriteLine("using CD = CategoryDiscipline;");
          writer.WriteLine();

          writer.WriteLine("public partial class BuiltInCategoryExtension");
          writer.WriteLine("{");
          writer.Indent++;
          writer.WriteLine("static readonly Definition[] _Definitions = new Definition[]");
          writer.WriteLine("{");
          writer.Indent++;

          foreach (var ct in Enum.GetValues(typeof(CategoryType)).Cast<CategoryType>().Skip(1))
          {
            writer.WriteLine($"#region {ct}");

            foreach (var d in definitions.Where(x => x.CategoryType == ct))
            {
              writer.Write($"new Definition(");
              if (d.Parent.IsValid()) writer.Write("  ");
              writer.Write($"{$"\"{d.Name}\", ",-64}");
              if (!d.Parent.IsValid()) writer.Write("  ");
              writer.Write($"{$"CT.{d.CategoryType}, ",-20}");
              writer.Write($"{$"{d.IsTagCategory.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"/*{(int) d.Parent,9}*/ BIC.{d.Parent}, ",-50}");
              writer.Write($"{$"/*{(int) d.Id,9}*/ BIC.{d.Id}, ",-67}");
              writer.Write($"{$"{d.CanAddSubcategory.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.AllowsBoundParameters.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.HasMaterialQuantities.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.IsCuttable.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.IsVisibleInUI.ToString().ToLower()}, ",-7}");
              writer.Write($"(CD) {$"{(int)d.Discipline,-2}"}");
              writer.WriteLine($"),");
            }

            writer.WriteLine($"#endregion");
            writer.WriteLine();
          }

          writer.Indent--;
          writer.WriteLine("};");

          writer.WriteLine();
          writer.WriteLine("private static readonly Dictionary<BIC, Definition> Definitions = _Definitions.ToDictionary(x => x.Id);");

          writer.Indent--;
          writer.WriteLine("}");

          writer.Indent--;
          writer.WriteLine("}");
          writer.Close();
        }
      }

      static ViewPlan FindView(Document document, CategoryDiscipline discipline)
      {
        using (var collector = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)))
          return collector.Where(v => v.Name == discipline.ToString()).FirstOrDefault() as ViewPlan;
      }

      internal static void UpdateDefinitions(Document document)
      {
        var definitions = GetDefinitions(document).ToArray();

        // Update Category Discipline on parent categories
        foreach (var discipline in Enum.GetValues(typeof(CategoryDiscipline)).Cast<CategoryDiscipline>())
        {
          if (FindView(document, discipline) is View view)
          {
            for (int d = 0; d < definitions.Length; ++d)
            {
              var definition = definitions[d];
              if (definition.Parent.IsValid()) continue;
              var categoryId = new ElementId(definition.Id);
              if (!view.CanCategoryBeHidden(categoryId)) continue;
              if (view.GetCategoryHidden(categoryId)) continue;
              definition.Discipline |= discipline;
              definitions[d] = definition;
            }
          }
        }

        // Update childs
        var parents = definitions.ToDictionary(d => d.Id);
        for (int d = 0; d < definitions.Length; ++d)
        {
          var definition = definitions[d];
          if (!definition.Parent.IsValid()) continue;
          definition.Discipline |= parents[definition.Parent].Discipline;
          definitions[d] = definition;
        }

        WriteDefinitions(definitions, document.Application.VersionNumber);
      }
      #endregion

      #region CategoryId
      static string FirstCharUppper(string value)
      {
        return value[0].ToString().ToUpper() + value.Substring(1);
      }

      static void WriteCategoryIdValues(IEnumerable<Definition> definitions, string versionNumber)
      {
#if REVIT_2022
        var path = Path.Combine
        (
          SourceCodePath,
          "..",
          "Schemas",
          versionNumber,
          "CategoryId.cs"
        );

        using (var writer = new System.CodeDom.Compiler.IndentedTextWriter(File.CreateText(path), "  "))
        {
          writer.WriteLine("using Autodesk.Revit.DB;");

          writer.WriteLine();
          writer.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
          writer.WriteLine("{");
          writer.Indent++;

          writer.WriteLine("using BIC = BuiltInCategory;");
          writer.WriteLine("using CID = CategoryId;");
          writer.WriteLine();

          writer.WriteLine("public partial class CategoryId");
          writer.WriteLine("{");
          writer.Indent++;

          foreach (var ct in Enum.GetValues(typeof(CategoryType)).Cast<CategoryType>().Skip(1))
          {
            writer.WriteLine($"#region {ct}");

            foreach
            (
              var categoryId in definitions.
              Where(x => x.CategoryType == ct).
              Select(x => new Schemas.CategoryId(x.Id, Category.GetBuiltInCategoryTypeId(x.Id).TypeId)).
              OrderBy(x => x.Name)
            )
            {
              writer.WriteLine($"public static CID {FirstCharUppper(categoryId.Name), -50} {{ get; }} = new CID(BIC.{(BuiltInCategory) categoryId, -50}, \"{categoryId}\");");
            }

            writer.WriteLine($"#endregion");
            writer.WriteLine();
          }


          writer.Indent--;
          writer.WriteLine("}");

          writer.Indent--;
          writer.WriteLine("}");
          writer.Close();
        }
#else
        foreach (var bic in BuiltInCategories)
        {
          if(BuiltInCategory.INVALID == (Schemas.CategoryId) bic)
            Debug.WriteLine($"{bic}");
        }
#endif
      }

      internal static void UpdateCategoryIdValues(Document document)
      {
        WriteCategoryIdValues(GetDefinitions(document), document.Application.VersionNumber);
      }
      #endregion
#endif
    }
  }
}
