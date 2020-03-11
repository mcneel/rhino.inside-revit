using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Exceptions;
using RhinoInside.Revit.GH;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class TransactionalComponent :
    Component,
    DB.IFailuresPreprocessor,
    DB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected static double LiteralLengthValue(double meters)
    {
      switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
      {
        case Rhino.UnitSystem.None:
        case Rhino.UnitSystem.Inches:
        case Rhino.UnitSystem.Feet:
          return Math.Round(meters * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, Rhino.UnitSystem.Feet))
                 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Feet, Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
        default:
          return meters * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
      }
    }

    public override Rhino.Geometry.BoundingBox ClippingBox
    {
      get
      {
        var clippingBox = Rhino.Geometry.BoundingBox.Empty;

        foreach (var param in Params)
        {
          if (param.SourceCount > 0)
            continue;

          if (param is IGH_PreviewObject previewObject)
          {
            if (!previewObject.Hidden && previewObject.IsPreviewCapable)
              clippingBox.Union(previewObject.ClippingBox);
          }
        }

        return clippingBox;
      }
    }

    protected static void ChangeElementTypeId<T>(ref T element, DB.ElementId elementTypeId) where T : DB.Element
    {
      if (element is object && elementTypeId != element.GetTypeId())
      {
        var doc = element.Document;
        if (element.IsValidType(elementTypeId))
        {
          var newElmentId = element.ChangeTypeId(elementTypeId);
          if (newElmentId != DB.ElementId.InvalidElementId)
            element = (T) doc.GetElement(newElmentId);
        }
        else element = null;
      }
    }

    protected static void ChangeElementType<E, T>(ref E element, Optional<T> elementType) where E : DB.Element where T : DB.ElementType
    {
      if (elementType.HasValue && element is object)
      {
        if (!element.Document.Equals(elementType.Value.Document))
          throw new ArgumentException($"{nameof(ChangeElementType)} failed to assign a type from a diferent document.", nameof(elementType));

        ChangeElementTypeId(ref element, elementType.Value.Id);
      }
    }

    public bool SolveOptionalCategory(ref Optional<DB.Category> category, DB.Document doc, DB.BuiltInCategory builtInCategory, string paramName)
    {
      bool wasMissing = category.IsMissing;

      if (wasMissing)
      {
        if (doc.IsFamilyDocument)
          category = doc.OwnerFamily.FamilyCategory;

        if (category.IsMissing)
        {
          category = Autodesk.Revit.DB.Category.GetCategory(doc, builtInCategory) ??
          throw new ArgumentException("No suitable Category has been found.", paramName);
        }
      }

      else if (category.Value == null)
        throw new ArgumentNullException(paramName);

      return wasMissing;
    }

    public bool SolveOptionalType<T>(ref Optional<T> type, DB.Document doc, DB.ElementTypeGroup group, string paramName) where T : DB.ElementType
    {
      return SolveOptionalType(ref type, doc, group, (document, name) => throw new ArgumentNullException(paramName), paramName);
    }

    public bool SolveOptionalType<T>(ref Optional<T> type, DB.Document doc, DB.ElementTypeGroup group, Func<DB.Document, string, T> recoveryAction, string paramName) where T : DB.ElementType
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = (T) doc.GetElement(doc.GetDefaultElementTypeId(group)) ??
        throw new ArgumentException($"No suitable {group} has been found.", paramName);

      else if (type.Value == null)
        type = (T) recoveryAction.Invoke(doc, paramName);

      return wasMissing;
    }

    public bool SolveOptionalType(ref Optional<DB.FamilySymbol> type, DB.Document doc, DB.BuiltInCategory category, string paramName)
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = doc.GetElement(doc.GetDefaultFamilyTypeId(new DB.ElementId(category))) as DB.FamilySymbol ??
               throw new ArgumentException("No suitable type has been found.", paramName);

      else if (type.Value == null)
        throw new ArgumentNullException(paramName);

      else if (!type.Value.Document.Equals(doc))
        throw new ArgumentException($"{nameof(SolveOptionalType)} failed to assign a type from a diferent document.", nameof(type));

      if (!type.Value.IsActive)
        type.Value.Activate();

      return wasMissing;
    }

    public bool SolveOptionalLevel(DB.Document doc, double elevation, ref Optional<DB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
        level = doc.FindLevelByElevation(elevation) ??
                throw new ArgumentException("No suitable level has been found.", nameof(elevation));

      else if (level.Value == null)
        throw new ArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(level));

      return wasMissing;
    }

    public bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.Point3d point, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = new Rhino.Geometry.BoundingBox(point, point);
      return SolveOptionalLevel(doc, point.IsValid ? point.Z : double.NaN, ref level);
    }

    public bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.Line line, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = line.BoundingBox;
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    public bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.GeometryBase geometry, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = geometry.GetBoundingBox(true);
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    public bool SolveOptionalLevel(DB.Document doc, IEnumerable<Rhino.Geometry.GeometryBase> geometries, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = Rhino.Geometry.BoundingBox.Empty;
      foreach (var geometry in geometries)
        bbox = geometry.GetBoundingBox(true);

      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    public void SolveOptionalLevelsFromBase(DB.Document doc, double elevation, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var b = doc.FindBaseLevelByElevation(elevation, out var t) ??
                t ?? throw new ArgumentException("No suitable base level has been found.", nameof(elevation));

        if (!baseLevel.HasValue)
          baseLevel = b;

        if (!topLevel.HasValue)
          topLevel = t ?? b;
      }

      else if (baseLevel.Value == null)
        throw new ArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new ArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(baseLevel));

      else if (!topLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(topLevel));
    }

    public void SolveOptionalLevelsFromTop(DB.Document doc, double elevation, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var t = doc.FindTopLevelByElevation(elevation, out var b) ??
                b ?? throw new ArgumentException("No suitable top level has been found.", nameof(elevation));

        if (!topLevel.HasValue)
          topLevel = t;

        if (!baseLevel.HasValue)
          baseLevel = b ?? t;
      }

      else if (baseLevel.Value == null)
        throw new ArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new ArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(baseLevel));

      else if (!topLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(topLevel));
    }

    public bool SolveOptionalLevels(DB.Document doc, Rhino.Geometry.Curve curve, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      bool result = true;

      result &= SolveOptionalLevel(doc, Math.Min(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);
      result &= SolveOptionalLevel(doc, Math.Max(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);

      return result;
    }

    #region Reflection
    static Dictionary<Type, Tuple<Type, Type>> ParamTypes = new Dictionary<Type, Tuple<Type, Type>>()
    {
      { typeof(bool),                           Tuple.Create(typeof(Param_Boolean),           typeof(GH_Boolean))         },
      { typeof(int),                            Tuple.Create(typeof(Param_Integer),           typeof(GH_Integer))         },
      { typeof(double),                         Tuple.Create(typeof(Param_Number),            typeof(GH_Number))          },
      { typeof(string),                         Tuple.Create(typeof(Param_String),            typeof(GH_String))          },
      { typeof(Guid),                           Tuple.Create(typeof(Param_Guid),              typeof(GH_Guid))            },
      { typeof(DateTime),                       Tuple.Create(typeof(Param_Time),              typeof(GH_Time))            },

      { typeof(Rhino.Geometry.Transform),       Tuple.Create(typeof(Param_Transform),         typeof(GH_Transform))       },
      { typeof(Rhino.Geometry.Point3d),         Tuple.Create(typeof(Param_Point),             typeof(GH_Point))           },
      { typeof(Rhino.Geometry.Vector3d),        Tuple.Create(typeof(Param_Vector),            typeof(GH_Vector))          },
      { typeof(Rhino.Geometry.Plane),           Tuple.Create(typeof(Param_Plane),             typeof(GH_Plane))          },
      { typeof(Rhino.Geometry.Line),            Tuple.Create(typeof(Param_Line),              typeof(GH_Line))            },
      { typeof(Rhino.Geometry.Arc),             Tuple.Create(typeof(Param_Arc),               typeof(GH_Arc))             },
      { typeof(Rhino.Geometry.Circle),          Tuple.Create(typeof(Param_Circle),            typeof(GH_Circle))          },
      { typeof(Rhino.Geometry.Curve),           Tuple.Create(typeof(Param_Curve),             typeof(GH_Curve))           },
      { typeof(Rhino.Geometry.Surface),         Tuple.Create(typeof(Param_Surface),           typeof(GH_Surface))         },
      { typeof(Rhino.Geometry.Brep),            Tuple.Create(typeof(Param_Brep),              typeof(GH_Brep))            },
//    { typeof(Rhino.Geometry.Extrusion),       Tuple.Create(typeof(Param_Extrusion),         typeof(GH_Extrusion))       },
      { typeof(Rhino.Geometry.Mesh),            Tuple.Create(typeof(Param_Mesh),              typeof(GH_Mesh))            },
      { typeof(Rhino.Geometry.SubD),            Tuple.Create(typeof(Param_SubD),              typeof(GH_SubD))            },

      { typeof(IGH_Goo),                        Tuple.Create(typeof(Param_GenericObject),     typeof(IGH_Goo))            },
      { typeof(IGH_GeometricGoo),               Tuple.Create(typeof(Param_Geometry),          typeof(IGH_GeometricGoo))   },

      { typeof(Autodesk.Revit.DB.Category),     Tuple.Create(typeof(Parameters.Documents.Categories.Category),     typeof(Types.Documents.Categories.Category))     },
      { typeof(Autodesk.Revit.DB.Element),      Tuple.Create(typeof(Parameters.Element),      typeof(Types.Element))      },
      { typeof(Autodesk.Revit.DB.ElementType),  Tuple.Create(typeof(Parameters.Documents.ElementTypes.ElementType),  typeof(Types.Documents.ElementTypes.ElementType))  },
      { typeof(Autodesk.Revit.DB.Material),     Tuple.Create(typeof(Parameters.Elements.Material.Material),     typeof(Types.Elements.Material.Material))     },
      { typeof(Autodesk.Revit.DB.SketchPlane),  Tuple.Create(typeof(Parameters.Elements.SketchPlane.SketchPlane),  typeof(Types.Elements.SketchPlane.SketchPlane))  },
      { typeof(Autodesk.Revit.DB.Level),        Tuple.Create(typeof(Parameters.Elements.Level.Level),        typeof(Types.Elements.Level.Level))        },
      { typeof(Autodesk.Revit.DB.Grid),         Tuple.Create(typeof(Parameters.Elements.Grid.Grid),         typeof(Types.Elements.Grid.Grid))         },
    };

    protected bool TryGetParamTypes(Type type, out Tuple<Type, Type> paramTypes)
    {
      if (type.IsEnum)
      {
        if (!GH_Enumerate.TryGetParamTypes(type, out paramTypes))
          paramTypes = Tuple.Create(typeof(Param_Integer), typeof(GH_Integer));

        return true;
      }

      if (!ParamTypes.TryGetValue(type, out paramTypes))
      {
        if (typeof(Autodesk.Revit.DB.ElementType).IsAssignableFrom(type))
        {
          paramTypes = Tuple.Create(typeof(Parameters.Documents.ElementTypes.ElementType), typeof(Types.Documents.ElementTypes.ElementType));
          return true;
        }

        if (typeof(Autodesk.Revit.DB.Element).IsAssignableFrom(type))
        {
          paramTypes = Tuple.Create(typeof(Parameters.Element), typeof(Types.Element));
          return true;
        }

        return false;
      }

      return true;
    }

    IGH_Param CreateParam(Type argumentType)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return new Param_GenericObject();

      return (IGH_Param) Activator.CreateInstance(paramTypes.Item1);
    }

    IGH_Goo CreateGoo(Type argumentType, object value)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return null;

      return (IGH_Goo) Activator.CreateInstance(paramTypes.Item2, value);
    }

    protected Type GetParameterType(ParameterInfo parameter, out GH_ParamAccess access, out bool optional)
    {
      var parameterType = parameter.ParameterType;
      optional = parameter.IsDefined(typeof(OptionalAttribute), false);
      access = GH_ParamAccess.item;

      var genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;

      if (genericType != null && genericType == typeof(Optional<>))
      {
        optional = true;
        parameterType = parameterType.GetGenericArguments()[0];
        genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;
      }

      if (genericType != null && genericType == typeof(IList<>))
      {
        access = GH_ParamAccess.list;
        parameterType = parameterType.GetGenericArguments()[0];
        genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;
      }

      return parameterType;
    }

    DB.ElementFilter elementFilter = null;
    protected override DB.ElementFilter ElementFilter => elementFilter;

    protected void RegisterInputParams(GH_InputParamManager manager, MethodInfo methodInfo)
    {
      var elementFilterClasses = new List<Type>();

      foreach (var parameter in methodInfo.GetParameters())
      {
        if (parameter.Position < 2)
          continue;

        if (parameter.IsOut || parameter.ParameterType.IsByRef)
          throw new NotImplementedException();

        var parameterType = GetParameterType(parameter, out var access, out var optional);
        var nickname = parameter.Name.First().ToString().ToUpperInvariant();
        var name = nickname + parameter.Name.Substring(1);

        foreach (var nickNameAttribte in parameter.GetCustomAttributes(typeof(NickNameAttribute), false).Cast<NickNameAttribute>())
          nickname = nickNameAttribte.NickName;

        var description = string.Empty;
        foreach (var descriptionAttribute in parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>())
          description = (description.Length > 0) ? $"{description}\r\n{descriptionAttribute.Description}" : descriptionAttribute.Description;

        var param = manager[manager.AddParameter(CreateParam(parameterType), name, nickname, description, access)];

        param.Optional = optional;

        if (parameterType.IsEnum && param is Param_Integer integerParam)
        {
          foreach (var e in Enum.GetValues(parameterType))
            integerParam.AddNamedValue(Enum.GetName(parameterType, e), (int) e);
        }
        else if (parameterType == typeof(Autodesk.Revit.DB.Element) || parameterType.IsSubclassOf(typeof(Autodesk.Revit.DB.Element)))
        {
          elementFilterClasses.Add(parameterType);
        }
        else if (parameterType == typeof(Autodesk.Revit.DB.Category))
        {
          elementFilterClasses.Add(typeof(Autodesk.Revit.DB.Element));
        }
      }

      if (elementFilterClasses.Count > 0 && !elementFilterClasses.Contains(typeof(Autodesk.Revit.DB.Element)))
      {
        elementFilter = (elementFilterClasses.Count == 1) ?
         (DB.ElementFilter) new Autodesk.Revit.DB.ElementClassFilter(elementFilterClasses[0]) :
         (DB.ElementFilter) new Autodesk.Revit.DB.LogicalOrFilter(elementFilterClasses.Select(x => new Autodesk.Revit.DB.ElementClassFilter(x)).ToArray());
      }
    }

    bool GetInputOptionalData<T>(IGH_DataAccess DA, int index, out Optional<T> optional)
    {
      if (GetInputData(DA, index, out T value))
      {
        optional = new Optional<T>(value);
        return true;
      }

      optional = Optional.Missing;
      return false;
    }
    static readonly MethodInfo GetInputOptionalDataInfo = typeof(TransactionalComponent).GetMethod("GetInputOptionalData", BindingFlags.Instance | BindingFlags.NonPublic);

    protected bool GetInputData<T>(IGH_DataAccess DA, int index, out T value)
    {
      if (typeof(T).IsEnum)
      {
        int enumValue = 0;
        if (!DA.GetData(index, ref enumValue))
        {
          var param = Params.Input[index];

          if (param.Optional && param.SourceCount == 0)
          {
            value = default;
            return false;
          }

          throw new ArgumentNullException(param.Name);
        }

        if (!typeof(T).IsEnumDefined(enumValue))
        {
          var param = Params.Input[index];
          throw new InvalidEnumArgumentException(param.Name, enumValue, typeof(T));
        }

        value = (T) Enum.ToObject(typeof(T), enumValue);
      }
      else if (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(Optional<>)))
      {
        var args = new object[] { DA, index, null };

        try { return (bool) GetInputOptionalDataInfo.MakeGenericMethod(typeof(T).GetGenericArguments()[0]).Invoke(this, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
        finally { value = args[2] != null ? (T) args[2] : default; }
      }
      else
      {
        value = default;
        if (!DA.GetData(index, ref value))
        {
          var param = Params.Input[index];
          if (param.Optional && param.SourceCount == 0)
            return false;

          throw new ArgumentNullException(param.Name);
        }
      }

      return true;
    }
    protected static readonly MethodInfo GetInputDataInfo = typeof(TransactionalComponent).GetMethod("GetInputData", BindingFlags.Instance | BindingFlags.NonPublic);

    protected bool GetInputDataList<T>(IGH_DataAccess DA, int index, out IList<T> value)
    {
      var list = new List<T>();
      if (DA.GetDataList(index, list))
      {
        value = list;
        return true;
      }
      else
      {
        value = default;
        return false;
      }
    }
    protected static readonly MethodInfo GetInputDataListInfo = typeof(TransactionalComponent).GetMethod("GetInputDataList", BindingFlags.Instance | BindingFlags.NonPublic);

    protected void ThrowArgumentNullException(string paramName, string description = null) => throw new ArgumentNullException(paramName.FirstCharUpper(), description ?? string.Empty);
    protected void ThrowArgumentException(string paramName, string description = null) => throw new ArgumentException(description ?? "Invalid value.", paramName.FirstCharUpper());
    protected void ThrowIfNotValid(string paramName, Rhino.Geometry.Point3d value)
    {
      if (!value.IsValid) ThrowArgumentException(paramName);
    }
    protected void ThrowIfNotValid(string paramName, Rhino.Geometry.GeometryBase value)
    {
      if (!value.IsValidWithLog(out var log)) ThrowArgumentException(paramName, log);
    }
    #endregion

    // Step 1.
    // protected override void BeforeSolveInstance() { }

    // Step 2.
    protected virtual void OnAfterStart(DB.Document document, string strTransactionName) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    protected virtual void OnBeforeCommit(DB.Document document, string strTransactionName) { }

    // Step 5.
    //protected override void AfterSolveInstance() {}

    // Step 5.1
    #region IFailuresPreprocessor
    void AddRuntimeMessage(DB.FailureMessageAccessor error, bool? solved = null)
    {
      var level = GH_RuntimeMessageLevel.Remark;
      switch (error.GetSeverity())
      {
        case DB.FailureSeverity.Warning: level = GH_RuntimeMessageLevel.Warning; break;
        case DB.FailureSeverity.Error: level = GH_RuntimeMessageLevel.Error; break;
      }

      string solvedMark = string.Empty;
      if (error.GetSeverity() > DB.FailureSeverity.Warning)
      {
        switch (solved)
        {
          case false: solvedMark = "❌ "; break;
          case true: solvedMark = "✔ "; break;
        }
      }

      var description = error.GetDescriptionText();
      var text = string.IsNullOrEmpty(description) ?
        $"{solvedMark}{level} {{{error.GetFailureDefinitionId().Guid}}}" :
        $"{solvedMark}{description}";

      int idsCount = 0;
      foreach (var id in error.GetFailingElementIds())
        text += idsCount++ == 0 ? $" {{{id.IntegerValue}" : $", {id.IntegerValue}";
      if (idsCount > 0) text += "} ";

      AddRuntimeMessage(level, text);
    }

    // Override to add handled failures to your component (Order is important).
    protected virtual IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => null;

    DB.FailureProcessingResult FixFailures(DB.FailuresAccessor failuresAccessor, IEnumerable<DB.FailureDefinitionId> failureIds)
    {
      foreach (var failureId in failureIds)
      {
        int solvedErrors = 0;

        foreach (var error in failuresAccessor.GetFailureMessages().Where(x => x.GetFailureDefinitionId() == failureId))
        {
          if (!failuresAccessor.IsFailureResolutionPermitted(error))
            continue;

          // Don't try to fix two times same issue
          if (failuresAccessor.GetAttemptedResolutionTypes(error).Any())
            continue;

          AddRuntimeMessage(error, true);

          failuresAccessor.ResolveFailure(error);
          solvedErrors++;
        }

        if (solvedErrors > 0)
          return DB.FailureProcessingResult.ProceedWithCommit;
      }

      return DB.FailureProcessingResult.Continue;
    }

    DB.FailureProcessingResult DB.IFailuresPreprocessor.PreprocessFailures(DB.FailuresAccessor failuresAccessor)
    {
      if (!failuresAccessor.IsTransactionBeingCommitted())
        return DB.FailureProcessingResult.Continue;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.DocumentCorruption)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
      {
        // Handled failures in order
        {
          var failureDefinitionIdsToFix = FailureDefinitionIdsToFix;
          if (failureDefinitionIdsToFix != null)
          {
            var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
            if (result != DB.FailureProcessingResult.Continue)
              return result;
          }
        }

        // Unhandled failures in incomming order
        {
          var failureDefinitionIdsToFix = failuresAccessor.GetFailureMessages().GroupBy(x => x.GetFailureDefinitionId()).Select(x => x.Key);
          var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
          if (result != DB.FailureProcessingResult.Continue)
            return result;
        }
      }

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Warning)
      {
        // Unsolved failures or warnings
        foreach (var error in failuresAccessor.GetFailureMessages().OrderBy(error => error.GetSeverity()))
          AddRuntimeMessage(error, false);

        failuresAccessor.DeleteAllWarnings();
      }

      return DB.FailureProcessingResult.Continue;
    }
    #endregion

    // Step 5.2
    #region ITransactionFinalizer
    // Step 5.2.A
    public virtual void OnCommitted(DB.Document document, string strTransactionName) { }

    // Step 5.2.B
    public virtual void OnRolledBack(DB.Document document, string strTransactionName)
    {
      foreach (var param in Params.Output)
        param.Phase = GH_SolutionPhase.Failed;
    }
    #endregion

    protected void CommitTransaction(DB.Document doc, DB.Transaction transaction)
    {
      var options = transaction.GetFailureHandlingOptions();
#if !DEBUG
      options = options.SetClearAfterRollback(true);
#endif
      options = options.SetDelayedMiniWarnings(true);
      options = options.SetForcedModalHandling(true);
      options = options.SetFailuresPreprocessor(this);
      options = options.SetTransactionFinalizer(this);

      // Disable Rhino UI if any warning-error dialog popup
      {
        External.EditScope editScope = null;
        EventHandler<DialogBoxShowingEventArgs> _ = null;
        try
        {
          Revit.ApplicationUI.DialogBoxShowing += _ = (sender, args) =>
          {
            if (editScope is null)
              editScope = new External.EditScope();
          };

          if (transaction.GetStatus() == DB.TransactionStatus.Started)
          {
            OnBeforeCommit(doc, transaction.GetName());

            transaction.Commit(options);
          }
          else transaction.RollBack(options);
        }
        finally
        {
          Revit.ApplicationUI.DialogBoxShowing -= _;

          if (editScope is IDisposable disposable)
            disposable.Dispose();
        }
      }
    }
  }
}
