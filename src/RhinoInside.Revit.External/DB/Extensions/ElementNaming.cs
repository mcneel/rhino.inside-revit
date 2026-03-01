using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementNaming
  {
    /// <summary>
    /// Identifies if the input <paramref name="name"/> is valid for use as an Element name in Revit.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsValidName(string name) => NamingUtils.IsValidName(name);

    public static bool IsValidNameCharacter(char character) => NamingUtils.IsValidName(character.ToString());
    public static string InvalidCharacters => "\\:{}[]|;<>?`~";

    public static string MakeValidName(string name)
    {
      var builder = new System.Text.StringBuilder();

      foreach(var c in name)
      {
        switch(c)
        {
          case '\\': builder.Append('⧵'); break;
          case ':': builder.Append('∶'); break;
          case '{': builder.Append('❴'); break;
          case '}': builder.Append('❵'); break;
          case '[': builder.Append('［'); break;
          case ']': builder.Append('］'); break;
          case '|': builder.Append('∣'); break;
          case ';': builder.Append(';'); break;
          case '<': builder.Append('‹'); break;
          case '>': builder.Append('›'); break;
          case '?': builder.Append('¿'); break;
          case '`': builder.Append('｀'); break;
          case '~': builder.Append('∼'); break;
          default:
            if (IsValidNameCharacter(c)) builder.Append(c);
            else builder.Append('�');
            break;
        }
      }

      return builder.ToString();
    }

    internal static string Tooltip(this Element element)
    {
      var tokens = new List<string>(3);
      if (element.Category is Category category)
      {
        if (category.Parent is Category parent)
          tokens.Add(parent.Name);
        tokens.Add(category.Name);
      }

      if (element.Document.GetElement(element.GetTypeId()) is ElementType type)
      {
        tokens.Add(UI.HostedApplication.Active.InvokeInHostContext(() => type.FamilyName));
      }

      tokens.Add(element.Name);

      return string.Join(" : ", tokens);
    }

    struct ElementNameComparer : IComparer<string>
    {
      public int Compare(string x, string y) => NamingUtils.CompareNames(x, y);
    }

    /// <summary>
    /// Compares two Element name string for equality using Revit's comparison rules.
    /// </summary>
    public static readonly IEqualityComparer<string> NameEqualityComparer = StringComparer.Ordinal;

    /// <summary>
    /// Compares two Element name strings using Revit's comparison rules.
    /// </summary>
    public static readonly IComparer<string> NameComparer = default(ElementNameComparer);

    /// <summary>
    /// <see cref="System.StringComparison"/> used on <see cref="Autodesk.Revit.DB.NamingUtils"/> for equality comparsion.
    /// </summary>
    /// <remarks>
    /// Revit UI is not consitent some places uses a case sensitive comparsion.
    /// </remarks>
    public static readonly StringComparison ComparisonType = StringComparison.Ordinal;

    #region Nomen

    static bool HasNomen(this Type type)
    {
      if (typeof(Family).IsAssignableFrom(type))
        return true;

      if (typeof(ElementType).IsAssignableFrom(type))
        return true;

      if (typeof(ParameterElement).IsAssignableFrom(type))
        return true;

      if (typeof(GraphicsStyle).IsAssignableFrom(type))
        return true;

      if (typeof(LinePatternElement).IsAssignableFrom(type))
        return true;

      if (typeof(FillPatternElement).IsAssignableFrom(type))
        return true;

      if (typeof(AppearanceAssetElement).IsAssignableFrom(type))
        return true;

      if (typeof(StructuralAsset).IsAssignableFrom(type))
        return true;

      if (typeof(ThermalAsset).IsAssignableFrom(type))
        return true;

      return false;
    }

    public static bool HasNomen(this Element element)
    {
      if (element is null) return false;
      ElementExtension.Unproxy(ref element);

      if (HasNomen(element.GetType()))
        return true;

      if (GetNomenParameter(element) != BuiltInParameter.INVALID)
        return true;

      if (element.GetTypeId() != ElementIdExtension.Invalid)
        return false;

      return true;
    }

    // `Element.Name` does not always access the true denomination of the element.
    //
    // In cases like `ViewSheet` the true denomination is the "Sheet Number" parameter.
    // Denomination is used here as the element property that identifies it univocally on the UI.
    // Is the property that produce a "Name" collision in case is duplicated.
    //
    // In other cases like 'Design Options' the Name parameter may come decorated
    // this makes `Element.Name` not useful for searching or comparing namesake elements.
    // Nomen is undecorated in this case.

    public static bool CanBeRenominated(this Element element)
    {
      if (!HasNomen(element))
        return false;

      var document = element.Document;
      if (document.IsLinked) return false;

      // At that point let's be empiric…
      using (document.RollBackScope())
      {
        try
        {
          var guid = Guid.NewGuid().ToString("N");
          element.Name = guid;
          return element.Name == guid;
        }
        catch
        {
          return false;
        }
      }
    }

    public static bool IsNomenInUse(this Element element, string name)
    {
      if (element is null) return false;
      ElementExtension.Unproxy(ref element);

      var nomen = element.GetNomen(out var nomenParameter);
      using (element.Document.RollBackScope())
      {
        try { element.SetNomen(nomenParameter, name); }
        // The caller should see this exception to know this element can not be renamed.
        //catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { return true; }
      }

      return nomen == name;
    }

    public static bool SetIncrementalNomen(this Element element, string prefix)
    {
      var prefixed = DocumentExtension.TryParseNomenId(element.Name, out var p, out var _);
      if (!prefixed || prefix != p)
      {
        var categoryId = element.Category is Category category &&
          category.Id.TryGetBuiltInCategory(out var builtInCategory) ?
          builtInCategory : default(BuiltInCategory?);

        var nextName = element.Document.NextIncrementalNomen
        (
          prefix,
          element.GetType(),
          element is ElementType type ?
          type.FamilyName :
          element is View view ? view.ViewType.ToString() :
          default,
          categoryId
        );

        if (nextName != element.GetNomen(out var nomenParameter))
        {
          element.SetNomen(nomenParameter, nextName);
          return true;
        }
      }

      return false;
    }

    internal static BuiltInParameter GetNomenParameter(Type type)
    {
      // `DB.Family` parameter `ALL_MODEL_FAMILY_NAME` use to be `null`.
      //
      // if (typeof(Family).IsAssignableFrom(type))
      //   return BuiltInParameter.ALL_MODEL_FAMILY_NAME;

      if (typeof(ElementType).IsAssignableFrom(type))
        return BuiltInParameter.ALL_MODEL_TYPE_NAME;

      if (typeof(DatumPlane).IsAssignableFrom(type))
        return BuiltInParameter.DATUM_TEXT;

      if (typeof(ViewSheet).IsAssignableFrom(type))
        return BuiltInParameter.SHEET_NUMBER;

      if (typeof(View).IsAssignableFrom(type))
        return BuiltInParameter.VIEW_NAME;

      if (typeof(Viewport).IsAssignableFrom(type))
        return BuiltInParameter.VIEWPORT_VIEW_NAME;

      if (typeof(PropertySetElement).IsAssignableFrom(type))
        return BuiltInParameter.PROPERTY_SET_NAME;

      if (typeof(Material).IsAssignableFrom(type))
        return BuiltInParameter.MATERIAL_NAME;

      if (typeof(DesignOption).IsAssignableFrom(type))
        return BuiltInParameter.OPTION_NAME;

      if (typeof(Phase).IsAssignableFrom(type))
        return BuiltInParameter.PHASE_NAME;

      if (typeof(AreaScheme).IsAssignableFrom(type))
        return BuiltInParameter.AREA_SCHEME_NAME;

      if (typeof(SpatialElement).IsAssignableFrom(type))
        return BuiltInParameter.ROOM_NUMBER;

      if (typeof(RevitLinkInstance).IsAssignableFrom(type))
        return BuiltInParameter.RVT_LINK_INSTANCE_NAME;

      // BuiltInParameter.IMPORT_SYMBOL_NAME is tagged as "Name" in te UI,
      // but in fact is the type-name not the instance name.
      //if (typeof(ImportInstance).IsAssignableFrom(type))
      //  return BuiltInParameter.IMPORT_SYMBOL_NAME;

      return BuiltInParameter.INVALID;
    }

    static BuiltInParameter GetNomenParameter(Element element)
    {
      ElementExtension.Unproxy(ref element);

      var builtInParameter = GetNomenParameter(element.GetType());
      if (builtInParameter != BuiltInParameter.INVALID) return builtInParameter;

      if (element.Category is Category category)
      {
        if (category.Id.TryGetBuiltInCategory(out var builtInCategory) == true)
        {
          switch (builtInCategory)
          {
            case BuiltInCategory.OST_DesignOptionSets: return BuiltInParameter.OPTION_SET_NAME;
            case BuiltInCategory.OST_VolumeOfInterest: return BuiltInParameter.VOLUME_OF_INTEREST_NAME;
          }
        }
      }

      return BuiltInParameter.INVALID;
    }

    public static string GetNomen(this Element element) =>
      GetNomen(element, out var _);

    internal static string GetNomen(this Element element, out BuiltInParameter nomenParameter)
    {
      if ((nomenParameter = GetNomenParameter(element)) != BuiltInParameter.INVALID)
        return element.get_Parameter(nomenParameter).AsString();
      else
        return element.Name;
    }

    internal static string GetNomen(this Element element, BuiltInParameter nomenParameter)
    {
      if (nomenParameter != BuiltInParameter.INVALID)
        return element.get_Parameter(nomenParameter).AsString();
      else
        return element.Name;
    }

    internal static void SetNomen(this Element element, BuiltInParameter nomenParameter, string name)
    {
      if (nomenParameter != BuiltInParameter.INVALID && !(element is ElementType))
      {
        if (element.get_Parameter(nomenParameter) is Parameter parameter)
        {
          if (parameter.IsReadOnly)
            throw new InvalidOperationException($"Element '{element.Tooltip()}' parameter {parameter.Definition?.Name} is read-only. {{{element.Id.ToValue()}}}");

          parameter.Update(name);
        }
      }
      else if (element.GetTypeId() != ElementIdExtension.Invalid)
      {
        // These elements do not support user-specified naming (Nomen).
        name = null;
      }
      else if (element.Name != name)
      {
        try { element.Name = name; }
        catch { }
      }

      if (element.Name != name)
        throw new InvalidOperationException($"Element '{element.Tooltip()}' does not support assignment of a user-specified name. {{{element.Id.ToValue()}}}");
    }

    public static void SetNomen(this Element element, string nomen) =>
      SetNomen(element, GetNomenParameter(element), nomen);

    public static bool SwapNomenWith(this Element element, Element other, out BuiltInParameter nomenParameter)
    {
      if (!element.Document.IsEquivalent(other.Document))
        throw new InvalidOperationException($"{nameof(element)} document '{element.Document.Title}' doesn't match with {nameof(other)} document '{element.Document.Title}'");

      var elementNomen = element.GetNomen(out var elementParameter);
      if (elementParameter != BuiltInParameter.INVALID)
      {
        if (!element.Id.Equals(other.Id))
        {
          if (element.GetType() != other.GetType())
            throw new InvalidOperationException($"{nameof(element)} type {element.GetType()} doesn't match with {nameof(other)} type {other.GetType()}");

          var otherNomen = other.GetNomen(out var otherParameter);
          if (elementParameter != otherParameter)
            throw new InvalidOperationException($"{nameof(element)} nomen parameter {elementParameter} doesn't match with {nameof(other)} nomen parameter {otherParameter}");

          other.SetNomen(otherParameter, Guid.NewGuid().ToString());
          element.SetNomen(elementParameter, otherNomen);
          other.SetNomen(otherParameter, elementNomen);
        }

        nomenParameter = elementParameter;
        return true;
      }

      nomenParameter = BuiltInParameter.INVALID;
      return false;
    }
    #endregion

  }
}
