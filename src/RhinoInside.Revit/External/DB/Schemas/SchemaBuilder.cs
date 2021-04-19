using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RhinoInside.Revit.External.DB.Schemas
{
#if DEBUG
  static class SchemaBuilder
  {
    public static void BuildSchemas()
    {
#if REVIT_2021 && !REVIT_2022
      BuildEnum
      (
        typeof(SpecType),
        typeof(Autodesk.Revit.DB.SpecTypeId).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId) && x.Name != "Custom").
          ToDictionary(x => x.Name, x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId),
        x => Autodesk.Revit.DB.UnitUtils.GetUnitType(x)
      );

      BuildSchema
      (
        typeof(UnitType),
        Autodesk.Revit.DB.UnitUtils.GetAllUnits(),
        x => Autodesk.Revit.DB.UnitUtils.GetDisplayUnitType(x)
      );

      BuildSchema
      (
        typeof(UnitSymbol),
        typeof(Autodesk.Revit.DB.SymbolTypeId).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId) && x.Name != "Custom").
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId),
        x => Autodesk.Revit.DB.UnitUtils.GetUnitSymbolType(x)
      );
#endif

#if REVIT_2022
      BuildLabels
      (
        typeof(DataType),
        (
          typeof(Autodesk.Revit.DB.SpecTypeId).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (
          typeof(Autodesk.Revit.DB.SpecTypeId.Int).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (
          typeof(Autodesk.Revit.DB.SpecTypeId.Reference).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (
          typeof(Autodesk.Revit.DB.SpecTypeId.Boolean).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (
          typeof(Autodesk.Revit.DB.SpecTypeId.String).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (Autodesk.Revit.DB.UnitUtils.GetAllUnits()).Concat
        (
          typeof(Autodesk.Revit.DB.SymbolTypeId).
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
          Select(x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId)
        ).Concat
        (Autodesk.Revit.DB.UnitUtils.GetAllDisciplines()).Concat
        (Autodesk.Revit.DB.ParameterUtils.GetAllBuiltInParameters()).Concat
        (Autodesk.Revit.DB.ParameterUtils.GetAllBuiltInGroups()).Concat
        (
          Enum.GetValues(typeof(Autodesk.Revit.DB.BuiltInCategory)).
          Cast<Autodesk.Revit.DB.BuiltInCategory>().
          Where(x => Autodesk.Revit.DB.Category.IsBuiltInCategoryValid(x)).
          Select(x => Autodesk.Revit.DB.Category.GetBuiltInCategoryTypeId(x))
        ),
        x =>
        {
          if (Autodesk.Revit.DB.UnitUtils.IsMeasurableSpec(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForSpec(x); } catch { }

          else if (Autodesk.Revit.DB.UnitUtils.IsUnit(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForUnit(x); } catch { }

          else if (Autodesk.Revit.DB.UnitUtils.IsSymbol(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForSymbol(x); } catch { }

          else if (x.TypeId.StartsWith("autodesk.spec.discipline") || x.TypeId.StartsWith("autodesk.spec:discipline"))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForDiscipline(x); } catch { }

          else if (Autodesk.Revit.DB.ParameterUtils.IsBuiltInParameter(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForBuiltInParameter(x); } catch { }

          else if (Autodesk.Revit.DB.ParameterUtils.IsBuiltInGroup(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelForGroup(x); } catch { }

          else if (Autodesk.Revit.DB.Category.IsBuiltInCategory(x))
            try { return Autodesk.Revit.DB.LabelUtils.GetLabelFor(Autodesk.Revit.DB.Category.GetBuiltInCategory(x)); } catch { }

          return string.Empty;
        }
      );

      return;

      BuildSchema(typeof(SpecType), "Measurable", typeof(Autodesk.Revit.DB.SpecTypeId));
      BuildSchema(typeof(SpecType), "Int", typeof(Autodesk.Revit.DB.SpecTypeId.Int));
      BuildSchema(typeof(SpecType), "Boolean", typeof(Autodesk.Revit.DB.SpecTypeId.Boolean));
      BuildSchema(typeof(SpecType), "String", typeof(Autodesk.Revit.DB.SpecTypeId.String));
      BuildSchema(typeof(SpecType), "Reference", typeof(Autodesk.Revit.DB.SpecTypeId.Reference));

      BuildSchema(typeof(DisciplineType), typeof(Autodesk.Revit.DB.DisciplineTypeId));

      BuildSchema
      (
        typeof(ParameterGroup),
        Autodesk.Revit.DB.ParameterUtils.GetAllBuiltInGroups(),
        x => Autodesk.Revit.DB.ParameterUtils.GetBuiltInParameterGroup(x)
      );

      BuildSchema
      (
        typeof(ParameterId),
        Autodesk.Revit.DB.ParameterUtils.GetAllBuiltInParameters(),
        x => Autodesk.Revit.DB.ParameterUtils.GetBuiltInParameter(x)
      );

      BuildSchema
      (
        typeof(CategoryId),
        Enum.GetValues(typeof(Autodesk.Revit.DB.BuiltInCategory)).
        OfType<Autodesk.Revit.DB.BuiltInCategory>().
        Where(x => Autodesk.Revit.DB.Category.IsBuiltInCategoryValid(x)).
        Select(x => Autodesk.Revit.DB.Category.GetBuiltInCategoryTypeId(x)),
        x => Autodesk.Revit.DB.Category.GetBuiltInCategory(x)
      );
#endif
    }

#if REVIT_2021
    static void BuildSchema(Type type, Type typeId)
    {
      string ValuesPath = Path.Combine(AddIn.SourceCodePath, "External", "DB", "Schemas", $"{type.Name}.Values.cs");

      var items = typeId.
                  GetProperties(BindingFlags.Public | BindingFlags.Static).
                  Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
                  ToDictionary(x => x.Name, x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId);

      using (var stream = new FileStream(ValuesPath, FileMode.Create))
      {
        using (var writer = new StreamWriter(stream))
        using (var text = new IndentedTextWriter(writer, "  "))
        {
          text.WriteLine("using Autodesk.Revit.DB;");
          text.WriteLine();
          text.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
          text.WriteLine("{");
          text.Indent++;

          text.WriteLine($"public partial class {type.Name}");
          text.WriteLine("{");
          text.Indent++;

          if (items.TryGetValue("Custom", out var custom))
          {
            text.WriteLine($"public static {type.Name} Custom => new {type.Name}(\"{custom.TypeId}\");");
            text.WriteLine();
          }

          foreach (var item in items.OrderBy(x => x.Key))
          {
            if (item.Key == "Custom") continue;
            text.WriteLine($"public static {type.Name} {item.Key} => new {type.Name}(\"{item.Value.TypeId}\");");
          }

          text.Indent--;
          text.WriteLine("}");

          text.Indent--;
          text.WriteLine("}");
        }

        stream.Close();
      }
    }

    static void BuildSchema(Type type, string subType, Type typeId)
    {
      var items = typeId.
                  GetProperties(BindingFlags.Public | BindingFlags.Static).
                  Where(x => x.PropertyType == typeof(Autodesk.Revit.DB.ForgeTypeId)).
                  ToDictionary(x => x.Name, x => x.GetValue(null) as Autodesk.Revit.DB.ForgeTypeId);

      // Values
      {
        string ValuesPath = Path.Combine(AddIn.SourceCodePath, "External", "DB", "Schemas", $"{type.Name}.{subType}.cs");

        using (var stream = new FileStream(ValuesPath, FileMode.Create))
        {
          using (var writer = new StreamWriter(stream))
          using (var text = new IndentedTextWriter(writer, "  "))
          {
            text.WriteLine("using Autodesk.Revit.DB;");
            text.WriteLine();
            text.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
            text.WriteLine("{");
            text.Indent++;

            text.WriteLine($"public partial class {type.Name}");
            text.WriteLine("{");
            text.Indent++;

            text.WriteLine($"public static class {subType}");
            text.WriteLine("{");
            text.Indent++;

            foreach (var item in items.OrderBy(x => x.Key))
            {
              if (item.Key == "Custom") continue;
              text.WriteLine($"public static {type.Name} {item.Key} => new {type.Name}(\"{item.Value.TypeId}\");");
            }

            text.Indent--;
            text.WriteLine("}");

            text.Indent--;
            text.WriteLine("}");

            text.Indent--;
            text.WriteLine("}");
          }

          stream.Close();
        }
      }
    }

    public delegate string Label(Autodesk.Revit.DB.ForgeTypeId input);
    public delegate TOutput Code<out TOutput>(Autodesk.Revit.DB.ForgeTypeId input);
    static void BuildSchema<TEnum>
    (
      Type type,
      IEnumerable<Autodesk.Revit.DB.ForgeTypeId> values,
      Code<TEnum> code
    )
      where TEnum : struct, Enum
    {
      string PascalCase(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1).Replace('.', '_');

      var items = values.
                  ToDictionary(x => PascalCase(((DataType) x).Name), x => x);

      // Values
      {
        string ValuesPath = Path.Combine(AddIn.SourceCodePath, "External", "DB", "Schemas", $"{type.Name}.Values.cs");
        using (var stream = new FileStream(ValuesPath, FileMode.Create))
        {
          using (var writer = new StreamWriter(stream))
          using (var text = new IndentedTextWriter(writer, "  "))
          {
            text.WriteLine("using System.Collections.Generic;");
            text.WriteLine("using Autodesk.Revit.DB;");
            text.WriteLine();
            text.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
            text.WriteLine("{");
            text.Indent++;

            text.WriteLine($"public partial class {type.Name}");
            text.WriteLine("{");
            text.Indent++;

            if (items.TryGetValue("Custom", out var custom))
            {
              text.WriteLine($"public static {type.Name} Custom => new {type.Name}(\"{custom.TypeId}\");");
              text.WriteLine();
            }

            foreach (var item in items.OrderBy(x => x.Key))
            {
              if (item.Key == "Custom") continue;
              text.WriteLine($"public static {type.Name} {item.Key} => new {type.Name}(\"{item.Value.TypeId}\");");
            }

            text.Indent--;
            text.WriteLine("}");

            text.Indent--;
            text.WriteLine("}");
          }

          stream.Close();
        }
      }

      BuildEnum(type, items, code);
    }

    static void BuildEnum<TEnum>
    (
      Type type,
      IDictionary<string, Autodesk.Revit.DB.ForgeTypeId> items,
      Code<TEnum> code
    )
      where TEnum : struct, Enum
    {
      string ValuesPath = Path.Combine(AddIn.SourceCodePath, "External", "DB", "Schemas", $"{type.Name}.Enum.cs");
      using (var stream = new FileStream(ValuesPath, FileMode.Create))
      {
        using (var writer = new StreamWriter(stream))
        using (var text = new IndentedTextWriter(writer, "  "))
        {
          text.WriteLine("using System.Collections.Generic;");
          text.WriteLine("using Autodesk.Revit.DB;");
          text.WriteLine();
          text.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
          text.WriteLine("{");
          text.Indent++;

          text.WriteLine($"public partial class {type.Name}");
          text.WriteLine("{");
          text.Indent++;

          text.WriteLine($"static readonly Dictionary<{type.Name}, int> map = new Dictionary<{type.Name}, int>()");
          text.WriteLine("{");
          text.Indent++;

          foreach (var item in items.OrderBy(x => code(x.Value)))
          {
            var enumValue = code(item.Value);

            text.WriteLine($"{{ {item.Key}, {Enum.Format(typeof(TEnum), enumValue, "D")} }}, // {enumValue}");
          }

          text.Indent--;
          text.WriteLine("};");

          text.Indent--;
          text.WriteLine("}");

          text.Indent--;
          text.WriteLine("}");
        }

        stream.Close();
      }
    }

    static void BuildLabels
    (
      Type type,
      IEnumerable<Autodesk.Revit.DB.ForgeTypeId> items,
      Label label
    )
    {
      string ValuesPath = Path.Combine(AddIn.SourceCodePath, "External", "DB", "Schemas", $"{type.Name}.Label.cs");
      using (var stream = new FileStream(ValuesPath, FileMode.Create))
      {
        using (var writer = new StreamWriter(stream))
        using (var text = new IndentedTextWriter(writer, "  "))
        {
          text.WriteLine("using System.Collections.Generic;");
          text.WriteLine("using Autodesk.Revit.DB;");
          text.WriteLine();
          text.WriteLine("namespace RhinoInside.Revit.External.DB.Schemas");
          text.WriteLine("{");
          text.Indent++;

          text.WriteLine($"public partial class {type.Name}");
          text.WriteLine("{");
          text.Indent++;

          text.WriteLine("public string Label => labels.TryGetValue(FullName, out var label) ? label : string.Empty;");
          text.WriteLine();

          text.WriteLine($"static readonly Dictionary<string, string> labels = new Dictionary<string, string>()");
          text.WriteLine("{");
          text.Indent++;

          foreach (var item in items.OrderBy(x => x.TypeId))
          {
            var key = ((DataType) item).FullName;
            var value = label(item);

            text.WriteLine($"{{ \"{key}\", \"{value}\" }},");
          }

          text.Indent--;
          text.WriteLine("};");

          text.Indent--;
          text.WriteLine("}");

          text.Indent--;
          text.WriteLine("}");
        }

        stream.Close();
      }
    }
#endif
  }
#endif
}
