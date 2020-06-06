using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyNew : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("82523911-309F-4A66-A4B9-CF21E0AC250E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "N";

    public FamilyNew() : base
    (
      name: "New Family",
      nickname: "New",
      description: "Creates a new Family from a template.",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam
      (
        CreateDocumentParam(),
        ParamVisibility.Voluntary
      ),
      ParamDefinition.FromParam
      (
        new Param_FilePath()
        {
          Name = "Template",
          NickName = "T",
          Access = GH_ParamAccess.item,
          Optional = true,
          FileFilter = "Family Template Files (*.rft)|*.rft"
        }
      ),
      ParamDefinition.FromParam
      (
        new Param_Boolean()
        {
          Name = "Override Family",
          NickName = "OF",
          Description = "Override Family",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Binding,
        defaultValue: false
      ),
      ParamDefinition.FromParam
      (
        new Param_Boolean()
        {
          Name = "Override Parameters",
          NickName = "OP",
          Description = "Override Parameters",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Binding,
        defaultValue: false
      ),
      ParamDefinition.FromParam
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Family Name",
          Access = GH_ParamAccess.item
        }
      ),
      ParamDefinition.FromParam
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Family Category",
          Access = GH_ParamAccess.item,
          Optional = true
        }
      ),
      ParamDefinition.FromParam
      (
        new Param_Geometry()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Family Geometry",
          Access = GH_ParamAccess.list,
          Optional = true
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.FromParam
      (
        new Parameters.Family()
        {
          Name = "Family",
          NickName = "F",
          Description = string.Empty,
          Access = GH_ParamAccess.item
        }
      )
    };

    public static Dictionary<string, DB.ElementId> GetMaterialIdsByName(DB.Document doc)
    {
      var collector = new DB.FilteredElementCollector(doc);
      return collector.OfClass(typeof(DB.Material)).OfType<DB.Material>().
        GroupBy(x => x.Name).
        ToDictionary(x => x.Key, x => x.First().Id);
    }

    static Rhino.Geometry.GeometryBase AsGeometryBase(IGH_GeometricGoo obj)
    {
      var scriptVariable = obj?.ScriptVariable();
      switch (scriptVariable)
      {
        case Rhino.Geometry.Point3d point: return new Rhino.Geometry.Point(point);
        case Rhino.Geometry.Line line: return new Rhino.Geometry.LineCurve(line);
        case Rhino.Geometry.Rectangle3d rect: return rect.ToNurbsCurve();
        case Rhino.Geometry.Arc arc: return new Rhino.Geometry.ArcCurve(arc);
        case Rhino.Geometry.Circle circle: return new Rhino.Geometry.ArcCurve(circle);
        case Rhino.Geometry.Ellipse ellipse: return ellipse.ToNurbsCurve();
        case Rhino.Geometry.Box box: return box.ToBrep();
      }

      return (scriptVariable as Rhino.Geometry.GeometryBase)?.DuplicateShallow();
    }

    class PlaneComparer : IComparer<KeyValuePair<double[], DB.SketchPlane>>
    {
      public static PlaneComparer Instance = new PlaneComparer();

      int IComparer<KeyValuePair<double[], DB.SketchPlane>>.Compare(KeyValuePair<double[], DB.SketchPlane> x, KeyValuePair<double[], DB.SketchPlane> y)
      {
        var abcdX = x.Key;
        var abcdY = y.Key;

        const double tol = Rhino.RhinoMath.ZeroTolerance;

        var d = abcdX[3] - abcdY[3];
        if (d < -tol) return -1;
        if (d > +tol) return +1;

        var c = abcdX[2] - abcdY[2];
        if (c < -tol) return -1;
        if (c > +tol) return +1;

        var b = abcdX[1] - abcdY[1];
        if (b < -tol) return -1;
        if (b > +tol) return +1;

        var a = abcdX[0] - abcdY[0];
        if (a < -tol) return -1;
        if (a > +tol) return +1;

        return 0;
      }
    }

    DB.Category MapCategory(DB.Document project, DB.Document family, DB.ElementId categoryId, bool createIfNotExist = false)
    {
      if (-3000000 < categoryId.IntegerValue && categoryId.IntegerValue < -2000000)
        return DB.Category.GetCategory(family, categoryId);

      try
      {
        if (DB.Category.GetCategory(project, categoryId) is DB.Category category)
        {
          if (family.OwnerFamily.FamilyCategory.SubCategories.Contains(category.Name) && family.OwnerFamily.FamilyCategory.SubCategories.get_Item(category.Name) is DB.Category subCategory)
            return subCategory;

          if (createIfNotExist)
            return family.Settings.Categories.NewSubcategory(family.OwnerFamily.FamilyCategory, category.Name);
        }
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

      return null;
    }

    DB.GraphicsStyle MapGraphicsStyle(DB.Document project, DB.Document family, DB.ElementId graphicsStyleId, bool createIfNotExist = false)
    {
      try
      {
        if (project.GetElement(graphicsStyleId) is DB.GraphicsStyle graphicsStyle)
        {
          if (family.OwnerFamily.FamilyCategory.SubCategories.Contains(graphicsStyle.GraphicsStyleCategory.Name) && family.OwnerFamily.FamilyCategory.SubCategories.get_Item(graphicsStyle.GraphicsStyleCategory.Name) is DB.Category subCategory)
            return subCategory.GetGraphicsStyle(graphicsStyle.GraphicsStyleType);

          if (createIfNotExist)
            return family.Settings.Categories.NewSubcategory(family.OwnerFamily.FamilyCategory, graphicsStyle.GraphicsStyleCategory.Name).
                   GetGraphicsStyle(graphicsStyle.GraphicsStyleType);
        }
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

      return null;
    }

    static DB.ElementId MapMaterial(DB.Document project, DB.Document family, DB.ElementId materialId, bool createIfNotExist = false)
    {
      if (project.GetElement(materialId) is DB.Material material)
      {
        using (var collector = new DB.FilteredElementCollector(family).OfClass(typeof(DB.Material)))
        {
          if (collector.Cast<DB.Material>().Where(x => x.Name == material.Name).FirstOrDefault() is DB.Material familyMaterial)
            return familyMaterial.Id;
        }

        if (createIfNotExist)
          return DB.Material.Create(family, material.Name);
      }

      return DB.ElementId.InvalidElementId;
    }

    class DeleteElementEnumerator<T> : IEnumerator<T> where T : DB.Element
    {
      readonly IEnumerator<T> enumerator;
      public DeleteElementEnumerator(IEnumerable<T> e) { enumerator = e.GetEnumerator(); }
      readonly List<DB.Element> elementsToDelete = new List<DB.Element>();

      public void Dispose()
      {
        while (MoveNext()) ;

        foreach (var element in elementsToDelete)
          element.Document.Delete(element.Id);

        enumerator.Dispose();
        DeleteCurrent = false;
      }

      public bool DeleteCurrent;
      public T Current => DeleteCurrent ? enumerator.Current : null;
      object IEnumerator.Current => Current;
      void IEnumerator.Reset() { enumerator.Reset(); DeleteCurrent = false; }
      public bool MoveNext()
      {
        if (DeleteCurrent)
          elementsToDelete.Add(Current);

        return DeleteCurrent = enumerator.MoveNext();
      }
    }

    bool Add
    (
      DB.Document doc,
      DB.Document familyDoc,
      Rhino.Geometry.Brep brep,
      DeleteElementEnumerator<DB.GenericForm> forms
    )
    {
      forms.MoveNext();
      if (brep.ToSolid() is DB.Solid solid)
      {
        if (forms.Current is DB.FreeFormElement freeForm)
        {
          freeForm.UpdateSolidGeometry(solid);
          forms.DeleteCurrent = false;
        }
        else freeForm = DB.FreeFormElement.Create(familyDoc, solid);

        brep.GetUserBoolean(DB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), out var cutting);
        freeForm.get_Parameter(DB.BuiltInParameter.ELEMENT_IS_CUTTING).Set(cutting ? 1 : 0);

        if (!cutting)
        {
          DB.Category familySubCategory = null;
          if
          (
            brep.GetUserElementId(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), out var subCategoryId) &&
            DB.Category.GetCategory(doc, subCategoryId) is DB.Category subCategory
          )
          {
            if (subCategory.Parent.Id == familyDoc.OwnerFamily.FamilyCategory.Id)
            {
              familySubCategory = MapCategory(doc, familyDoc, subCategoryId, true);
            }
            else
            {
              if (subCategory.Parent is null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{subCategory.Name}' is not subcategory of '{familyDoc.OwnerFamily.FamilyCategory.Name}'");
              else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{subCategory.Parent.Name} : {subCategory.Name}' is not subcategory of '{familyDoc.OwnerFamily.FamilyCategory.Name}'");
            }
          }

          if (familySubCategory is null)
            freeForm.get_Parameter(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY).Set(DB.ElementId.InvalidElementId);
          else
            freeForm.Subcategory = familySubCategory;

          brep.GetUserBoolean(DB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), out var visible, true);
          freeForm.get_Parameter(DB.BuiltInParameter.IS_VISIBLE_PARAM).Set(visible ? 1 : 0);

          brep.GetUserInteger(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), out var visibility, 57406);
          freeForm.get_Parameter(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM).Set(visibility);

          brep.GetUserElementId(DB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), out var materialId);
          var familyMaterialId = MapMaterial(doc, familyDoc, materialId, true);
          freeForm.get_Parameter(DB.BuiltInParameter.MATERIAL_ID_PARAM).Set(familyMaterialId);
        }

        return cutting;
      }

      return false;
    }

    void Add
    (
      DB.Document doc,
      DB.Document familyDoc,
      Rhino.Geometry.Curve curve,
      List<KeyValuePair<double[], DB.SketchPlane>> planesSet,
      DeleteElementEnumerator<DB.CurveElement> curves
    )
    {
      if (curve.TryGetPlane(out var plane))
      {
        var abcd = plane.GetPlaneEquation();
        int index = planesSet.BinarySearch(new KeyValuePair<double[], DB.SketchPlane>(abcd, null), PlaneComparer.Instance);
        if (index < 0)
        {
          var entry = new KeyValuePair<double[], DB.SketchPlane>(abcd, DB.SketchPlane.Create(familyDoc, plane.ToPlane()));
          index = ~index;
          planesSet.Insert(index, entry);
        }
        var sketchPlane = planesSet[index].Value;

        DB.GraphicsStyle familyGraphicsStyle = null;
        {
          DB.Category familySubCategory = null;
          if
          (
            curve.GetUserElementId(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), out var subCategoryId) &&
            DB.Category.GetCategory(doc, subCategoryId) is DB.Category subCategory
          )
          {
            if (subCategoryId == familyDoc.OwnerFamily.FamilyCategory.Id)
            {
              familySubCategory = MapCategory(doc, familyDoc, subCategoryId, true);
            }
            else if (subCategory?.Parent?.Id == familyDoc.OwnerFamily.FamilyCategory.Id)
            {
              familySubCategory = MapCategory(doc, familyDoc, subCategoryId, true);
            }
            else
            {
              if (subCategory.Parent is null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{subCategory.Name}' is not subcategory of '{familyDoc.OwnerFamily.FamilyCategory.Name}'");
              else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{subCategory.Parent.Name} : {subCategory.Name}' is not subcategory of '{familyDoc.OwnerFamily.FamilyCategory.Name}'");
            }
          }

          curve.GetUserEnum(DB.BuiltInParameter.FAMILY_CURVE_GSTYLE_PLUS_INVISIBLE.ToString(), out var graphicsStyleType, DB.GraphicsStyleType.Projection);

          familyGraphicsStyle = familySubCategory?.GetGraphicsStyle(graphicsStyleType);
        }

        curve.GetUserBoolean(DB.BuiltInParameter.MODEL_OR_SYMBOLIC.ToString(), out var symbolic);
        curve.GetUserBoolean(DB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), out var visible, true);
        curve.GetUserInteger(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), out var visibility, 57406);

        foreach (var c in curve.ToCurveMany())
        {
          curves.MoveNext();

          if (symbolic)
          {
            if (curves.Current is DB.SymbolicCurve symbolicCurve && symbolicCurve.GeometryCurve.IsSameKindAs(c))
            {
              symbolicCurve.SetSketchPlaneAndCurve(sketchPlane, c);
              curves.DeleteCurrent = false;
            }
            else symbolicCurve = familyDoc.FamilyCreate.NewSymbolicCurve(c, sketchPlane);

            symbolicCurve.get_Parameter(DB.BuiltInParameter.IS_VISIBLE_PARAM).Set(visible ? 1 : 0);
            symbolicCurve.get_Parameter(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM).Set(visibility);

            if (familyGraphicsStyle is object)
              symbolicCurve.Subcategory = familyGraphicsStyle;
          }
          else
          {
            if (curves.Current is DB.ModelCurve modelCurve && modelCurve.GeometryCurve.IsSameKindAs(c))
            {
              modelCurve.SetSketchPlaneAndCurve(sketchPlane, c);
              curves.DeleteCurrent = false;
            }
            else modelCurve = familyDoc.FamilyCreate.NewModelCurve(c, sketchPlane);

            modelCurve.get_Parameter(DB.BuiltInParameter.IS_VISIBLE_PARAM).Set(visible ? 1 : 0);
            modelCurve.get_Parameter(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM).Set(visibility);

            if (familyGraphicsStyle is object)
              modelCurve.Subcategory = familyGraphicsStyle;
          }
        }
      }
    }

    void Add
    (
      DB.Document doc,
      DB.Document familyDoc,
      IEnumerable<Rhino.Geometry.Curve> loops,
      DB.HostObject host,
      DeleteElementEnumerator<DB.Opening> openings
    )
    {
      var profile = loops.SelectMany(x => x.ToCurveMany()).ToCurveArray();
      var opening = familyDoc.FamilyCreate.NewOpening(host, profile);
    }

    static string GetFamilyTemplateFileName(DB.ElementId categoryId, Autodesk.Revit.ApplicationServices.LanguageType language)
    {
      if (categoryId.TryGetBuiltInCategory(out var builtInCategory))
      {
        if (builtInCategory == DB.BuiltInCategory.OST_Mass)
        {
          switch (language)
          {
            case Autodesk.Revit.ApplicationServices.LanguageType.English_USA: return @"Conceptual Mass\Metric Mass";
            case Autodesk.Revit.ApplicationServices.LanguageType.German: return @"Entwurfskörper\M_Körper";
            case Autodesk.Revit.ApplicationServices.LanguageType.Spanish: return @"Masas conceptuales\Masa métrica";
            case Autodesk.Revit.ApplicationServices.LanguageType.French: return @"Volume conceptuel\Volume métrique";
            case Autodesk.Revit.ApplicationServices.LanguageType.Italian: return @"Massa concettuale\Massa metrica";
            case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Simplified: return @"概念体量\公制体量";
            case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Traditional: return @"概念量體\公制量體";
            case Autodesk.Revit.ApplicationServices.LanguageType.Japanese: return @"コンセプト マス\マス(メートル単位)";
            case Autodesk.Revit.ApplicationServices.LanguageType.Korean: return @"개념 질량\미터법 질량";
            case Autodesk.Revit.ApplicationServices.LanguageType.Russian: return @"Концептуальный формообразующий элемент\Метрическая система, формообразующий элемент";
            case Autodesk.Revit.ApplicationServices.LanguageType.Czech: return null;
            case Autodesk.Revit.ApplicationServices.LanguageType.Polish: return @"Bryła koncepcyjna\Bryła (metryczna)";
            case Autodesk.Revit.ApplicationServices.LanguageType.Hungarian: return null;
            case Autodesk.Revit.ApplicationServices.LanguageType.Brazilian_Portuguese: return @"Massa conceitual\Massa métrica";
#if REVIT_2018
            case Autodesk.Revit.ApplicationServices.LanguageType.English_GB: return @"Conceptual Mass\Mass";
#endif
          }

          return null;
        }
      }

      switch (language)
      {
        case Autodesk.Revit.ApplicationServices.LanguageType.English_USA: return @"Metric Generic Model";
        case Autodesk.Revit.ApplicationServices.LanguageType.German: return @"Allgemeines Modell";
        case Autodesk.Revit.ApplicationServices.LanguageType.Spanish: return @"Modelo genérico métrico";
        case Autodesk.Revit.ApplicationServices.LanguageType.French: return @"Modèle générique métrique";
        case Autodesk.Revit.ApplicationServices.LanguageType.Italian: return @"Modello generico metrico";
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Simplified: return @"公制常规模型";
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Traditional: return @"公制常规模型";
        case Autodesk.Revit.ApplicationServices.LanguageType.Japanese: return @"一般モデル(メートル単位)";
        case Autodesk.Revit.ApplicationServices.LanguageType.Korean: return @"미터법 일반 모델";
        case Autodesk.Revit.ApplicationServices.LanguageType.Russian: return @"Метрическая система, типовая модель";
        case Autodesk.Revit.ApplicationServices.LanguageType.Czech: return @"Obecný model";
        case Autodesk.Revit.ApplicationServices.LanguageType.Polish: return @"Model ogólny (metryczny)";
        case Autodesk.Revit.ApplicationServices.LanguageType.Hungarian: return null;
        case Autodesk.Revit.ApplicationServices.LanguageType.Brazilian_Portuguese: return @"Modelo genérico métrico";
#if REVIT_2018
        case Autodesk.Revit.ApplicationServices.LanguageType.English_GB: return @"Generic Model";
#endif
      }

      return null;
    }

    static string GetFamilyTemplateFilePath(DB.ElementId categoryId, Autodesk.Revit.ApplicationServices.Application app)
    {
      string fileName = GetFamilyTemplateFileName(categoryId, app.Language);
      var templateFilePath = fileName is null ? string.Empty : Path.Combine(app.FamilyTemplatePath, $"{fileName}.rft");

      if (File.Exists(templateFilePath))
        return templateFilePath;

      // Emergency template file path
      fileName = GetFamilyTemplateFileName(categoryId, Autodesk.Revit.ApplicationServices.LanguageType.English_USA);
      return Path.Combine
      (
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)),
        "Autodesk",
        $"RVT {app.VersionNumber}",
        "Family Templates",
        "English",
        $"{fileName}.rft"
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      var overrideFamily = false;
      if (!DA.GetData("Override Family", ref overrideFamily))
        return;

      var overrideParameters = false;
      if (!DA.GetData("Override Parameters", ref overrideParameters))
        return;

      var name = string.Empty;
      if (!DA.GetData("Name", ref name))
        return;

      var categoryId = DB.ElementId.InvalidElementId;
      DA.GetData("Category", ref categoryId);
      var updateCategory = categoryId != DB.ElementId.InvalidElementId;

      var geometry = new List<IGH_GeometricGoo>();
      var updateGeometry = !(!DA.GetDataList("Geometry", geometry) && Params.Input[Params.IndexOfInputParam("Geometry")].SourceCount == 0);

      var family = default(DB.Family);
      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Family)))
        family = collector.Cast<DB.Family>().Where(x => x.Name == name).FirstOrDefault();

      bool familyIsNew = family is null;

      var templatePath = string.Empty;
      if (familyIsNew)
      {
        if (!DA.GetData("Template", ref templatePath))
          templatePath = GetFamilyTemplateFilePath(categoryId, doc.Application);

        if (!Path.HasExtension(templatePath))
          templatePath += ".rft";

        if (!Path.IsPathRooted(templatePath))
          templatePath = Path.Combine(doc.Application.FamilyTemplatePath, templatePath);
      }
      else
      {
        updateCategory &= family.FamilyCategory.Id != categoryId;
      }

      if (familyIsNew || (overrideFamily && (updateCategory || updateGeometry)))
      {
        try
        {
          if
          (
            (
              familyIsNew ?
              doc.Application.NewFamilyDocument(templatePath) :
              doc.EditFamily(family)
            )
            is var familyDoc
          )
          {
            try
            {
              using (var transaction = NewTransaction(familyDoc))
              {
                transaction.Start(Name);

                if (updateCategory && familyDoc.OwnerFamily.FamilyCategoryId != categoryId)
                {
                  try { familyDoc.OwnerFamily.FamilyCategoryId = categoryId; }
                  catch (Autodesk.Revit.Exceptions.ArgumentException e)
                  {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                    return;
                  }
                }

                if (updateGeometry)
                {
                  using (var forms = new DeleteElementEnumerator<DB.GenericForm>(new DB.FilteredElementCollector(familyDoc).OfClass(typeof(DB.GenericForm)).Cast<DB.GenericForm>().ToArray()))
                  using (var curves = new DeleteElementEnumerator<DB.CurveElement>(new DB.FilteredElementCollector(familyDoc).OfClass(typeof(DB.CurveElement)).Cast<DB.CurveElement>().Where(x => x.Category.Id.IntegerValue != (int) DB.BuiltInCategory.OST_SketchLines).ToArray()))
                  using (var openings = new DeleteElementEnumerator<DB.Opening>(new DB.FilteredElementCollector(familyDoc).OfClass(typeof(DB.Opening)).Cast<DB.Opening>().ToArray()))
                  {
                    bool hasVoids = false;
                    var planesSet = new List<KeyValuePair<double[], DB.SketchPlane>>();
                    var planesSetComparer = new PlaneComparer();
                    var loops = new List<Rhino.Geometry.Curve>();

                    foreach (var geo in geometry.Select(x => AsGeometryBase(x)))
                    {
                      try
                      {
                        if (geo is Rhino.Geometry.Curve loop && geo.GetUserBoolean("IS_OPENING_PARAM", out var opening) && opening)
                        {
                          loops.Add(loop);
                        }
                        else
                        {
                          switch (geo)
                          {
                            case Rhino.Geometry.Brep brep: hasVoids |= Add(doc, familyDoc, brep, forms); break;
                            case Rhino.Geometry.Curve curve: Add(doc, familyDoc, curve, planesSet, curves); break;
                            default:
                              if (geo is object)
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{geo.GetType().Name} is not supported and will be ignored");
                              break;
                          }
                        }
                      }
                      catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
                      {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                      }
                    }

                    if (loops.Count > 0)
                    {
                      using (var hosts = new DB.FilteredElementCollector(familyDoc).OfClass(typeof(DB.HostObject)))
                      {
                        if (hosts.Where(x => x is DB.Wall || x is DB.Ceiling).FirstOrDefault() is DB.HostObject host)
                          Add(doc, familyDoc, loops, host, openings);
                        else
                          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No suitable host object is been found");
                      }
                    }

                    familyDoc.OwnerFamily.get_Parameter(DB.BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS).Set(hasVoids ? 1 : 0);
                  }
                }

                CommitTransaction(familyDoc, transaction);
              }

              family = familyDoc.LoadFamily(doc, new FamilyLoadOptions(overrideFamily, overrideParameters));
            }
            finally
            {
              familyDoc.Release();
            }

            if (familyIsNew)
            {
              using (var transaction = NewTransaction(doc))
              {
                transaction.Start(Name);
                try { family.Name = name; }
                catch (Autodesk.Revit.Exceptions.ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message); }

                if (doc.GetElement(family.GetFamilySymbolIds().First()) is DB.FamilySymbol symbol)
                  symbol.Name = name;

                CommitTransaction(doc, transaction);
              }
            }
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
        }
      }
      else if (!overrideFamily)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Family '{name}' already loaded!");
      }

      DA.SetData("Family", family);
    }
  }
}
