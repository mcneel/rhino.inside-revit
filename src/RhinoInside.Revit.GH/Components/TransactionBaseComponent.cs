using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Exceptions;
  using Grasshopper.Kernel.Attributes;
  using Kernel.Attributes;

  [Obsolete]
  public abstract class TransactionBaseComponent :
    Component,
    DB.IFailuresPreprocessor,
    DB.ITransactionFinalizer
  {
    protected TransactionBaseComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected DB.Transaction NewTransaction(DB.Document doc) => NewTransaction(doc, Name);
    protected DB.Transaction NewTransaction(DB.Document doc, string name)
    {
      var transaction = new DB.Transaction(doc, name);

      var options = transaction.GetFailureHandlingOptions();
      options = options.SetClearAfterRollback(true);
      options = options.SetDelayedMiniWarnings(false);
      options = options.SetForcedModalHandling(true);

      options = options.SetFailuresPreprocessor(this);
      options = options.SetTransactionFinalizer(this);

      transaction.SetFailureHandlingOptions(options);

      return transaction;
    }

    protected DB.TransactionStatus CommitTransaction(DB.Document doc, DB.Transaction transaction)
    {
      // Disable Rhino UI if any warning-error dialog popup
      {
        var uiApplication = Revit.ActiveUIApplication;
        External.UI.EditScope scope = null;
        EventHandler<DialogBoxShowingEventArgs> _ = null;
        try
        {
          uiApplication.DialogBoxShowing += _ = (sender, args) =>
          {
            if (scope is null)
              scope = new External.UI.EditScope(uiApplication);
          };

          if (transaction.GetStatus() == DB.TransactionStatus.Started)
          {
            OnBeforeCommit(doc, transaction.GetName());

            return transaction.Commit();
          }
          else return transaction.RollBack();
        }
        finally
        {
          uiApplication.DialogBoxShowing -= _;

          if (scope is IDisposable disposable)
            disposable.Dispose();
        }
      }
    }

    // Setp 1.
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
      if (error.GetFailureDefinitionId() == DBX.ExternalFailures.TransactionFailures.SimulatedTransaction)
      {
        // Simulation signal is already reflected in the canvas changing the component color,
        // So it's up to the component show relevant information about what 'simulation' means.
        // As an example Purge component shows a remarks that reads like 'No elements were deleted'.
        //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, error.GetDescriptionText());

        return;
      }

      var level = GH_RuntimeMessageLevel.Remark;
      switch (error.GetSeverity())
      {
        case DB.FailureSeverity.Warning:            level = GH_RuntimeMessageLevel.Warning; break;
        case DB.FailureSeverity.Error:              level = GH_RuntimeMessageLevel.Error;   break;
        case DB.FailureSeverity.DocumentCorruption: level = GH_RuntimeMessageLevel.Error;   break;
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
      if (failuresAccessor.GetSeverity() == DB.FailureSeverity.Error)
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

      var severity = failuresAccessor.GetSeverity();
      if (severity >= DB.FailureSeverity.Warning)
      {
        // Unsolved errors or warnings
        foreach (var error in failuresAccessor.GetFailureMessages().OrderBy(error => error.GetSeverity()))
          AddRuntimeMessage(error, false);

        failuresAccessor.DeleteAllWarnings();
      }

      if (severity >= DB.FailureSeverity.Error)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      return DB.FailureProcessingResult.Continue;
    }
    #endregion

    // Step 5.2
    #region ITransactionFinalizer
    // Step 5.2.A
    public virtual void OnCommitted(DB.Document document, string strTransactionName)
    {
    }

    // Step 5.2.B
    public virtual void OnRolledBack(DB.Document document, string strTransactionName)
    {
      foreach (var param in Params.Output)
        param.Phase = GH_SolutionPhase.Failed;
    }
    #endregion

    #region Solve Optional values
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

    protected static bool SolveOptionalCategory(ref Optional<DB.Category> category, DB.Document doc, DB.BuiltInCategory builtInCategory, string paramName)
    {
      bool wasMissing = category.IsMissing;

      if (wasMissing)
      {
        if (doc.IsFamilyDocument)
          category = doc.OwnerFamily.FamilyCategory;

        if(category.IsMissing)
        {
          category = Autodesk.Revit.DB.Category.GetCategory(doc, builtInCategory) ??
          throw new ArgumentException("No suitable Category has been found.", paramName);
        }
      }

      else if (category.Value == null)
        throw new ArgumentNullException(paramName);

      return wasMissing;
    }

    protected static bool SolveOptionalType<T>(DB.Document doc, ref Optional<T> type, DB.ElementTypeGroup group, string paramName) where T : DB.ElementType
    {
      return SolveOptionalType(doc, ref type, group, (document, name) => throw new ArgumentNullException(paramName), paramName);
    }

    protected static bool SolveOptionalType<T>(DB.Document doc, ref Optional<T> type, DB.ElementTypeGroup group, Func<DB.Document, string, T> recoveryAction, string paramName) where T : DB.ElementType
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = (T) doc.GetElement(doc.GetDefaultElementTypeId(group)) ??
        throw new ArgumentException($"No suitable {group} has been found.", paramName);

      else if (type.Value == null)
        type = (T) recoveryAction.Invoke(doc, paramName);

      return wasMissing;
    }

    protected static bool SolveOptionalType(DB.Document doc, ref Optional<DB.FamilySymbol> type, DB.BuiltInCategory category, string paramName)
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

    protected static bool SolveOptionalLevel(DB.Document doc, DB.Element host, ref Optional<DB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
      {
        if (host?.Document.GetElement(host.LevelId) is DB.Level newLevel)
          level = newLevel;
      }

      else if (level.Value == null)
        throw new ArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(level));

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(DB.Document doc, double height, ref Optional<DB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
        level = doc.FindLevelByHeight(height / Revit.ModelUnits) ??
                throw new ArgumentException("No suitable level has been found.", nameof(height));

      else if (level.Value == null)
        throw new ArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(level));

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(DB.Document doc, Point3d point, ref Optional<DB.Level> level, out BoundingBox bbox)
    {
      bbox = new Rhino.Geometry.BoundingBox(point, point);
      return SolveOptionalLevel(doc, point.IsValid ? point.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, Line line, ref Optional<DB.Level> level, out BoundingBox bbox)
    {
      bbox = line.BoundingBox;
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, GeometryBase geometry, ref Optional<DB.Level> level, out BoundingBox bbox)
    {
      bbox = geometry.GetBoundingBox(true);
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, IEnumerable<GeometryBase> geometries, ref Optional<DB.Level> level, out BoundingBox bbox)
    {
      bbox = Rhino.Geometry.BoundingBox.Empty;
      foreach (var geometry in geometries)
        bbox.Union(geometry.GetBoundingBox(true));

      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static void SolveOptionalLevelsFromBase(DB.Document doc, double height, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var b = doc.FindBaseLevelByHeight(height / Revit.ModelUnits, out var t) ??
                t ?? throw new ArgumentException("No suitable base level has been found.", nameof(height));

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

    protected static void SolveOptionalLevelsFromTop(DB.Document doc, double height, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var t = doc.FindTopLevelByHeight(height / Revit.ModelUnits, out var b) ??
                b ?? throw new ArgumentException("No suitable top level has been found.", nameof(height));

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

    protected static bool SolveOptionalLevels(DB.Document doc, Rhino.Geometry.Curve curve, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      bool result = true;

      result &= SolveOptionalLevel(doc, Math.Min(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);
      result &= SolveOptionalLevel(doc, Math.Max(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);

      return result;
    }
    #endregion

    #region Geometry Conversion
    public static bool TryGetCurveAtPlane(Curve curve, Plane plane, out DB.Curve projected)
    {
      if (Curve.ProjectToPlane(curve, plane) is Curve p)
      {
        if (p.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
          projected = line.ToLine();
        else if (p.TryGetArc(plane, out var arc, Revit.VertexTolerance * Revit.ModelUnits))
          projected = arc.ToArc();
        else if (p.TryGetEllipse(plane, out var ellipse, out var interval, Revit.VertexTolerance * Revit.ModelUnits))
          projected = ellipse.ToCurve(interval);
        else
          projected = p.ToCurve();

        return true;
      }

      projected = default;
      return false;
    }
    #endregion
  }

  [Obsolete("Please use TransactionalComponent")]
  public abstract class TransactionComponent : TransactionBaseComponent
  {
    protected TransactionComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Autodesk.Revit.DB.Transacion support
    protected DB.Transaction CurrentTransaction;
    protected DB.TransactionStatus TransactionStatus => CurrentTransaction?.GetStatus() ?? DB.TransactionStatus.Uninitialized;

    protected void StartTransaction(DB.Document document)
    {
      if (document is null)
        return;

      CurrentTransaction = NewTransaction(document, Name);
      if (CurrentTransaction.Start() != DB.TransactionStatus.Started)
      {
        CurrentTransaction.Dispose();
        CurrentTransaction = null;
        throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
      }
    }

    protected DB.TransactionStatus CommitTransaction(DB.Document document)
    {
      try     { return base.CommitTransaction(document, CurrentTransaction); }
      finally { CurrentTransaction.Dispose(); CurrentTransaction = default; }
    }
    #endregion

    // Step 1.
    protected override void BeforeSolveInstance()
    {
      if (Parameters.Document.GetDataOrDefault(this, default, default, out var Document))
      {
        StartTransaction(Document);

        OnAfterStart(Document, CurrentTransaction.GetName());
      }
    }

    // Step 2.
    //protected override void OnAfterStart(Document document, string strTransactionName) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    //protected override void OnBeforeCommit(Document document, string strTransactionName) { }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      try
      {
        if (RunCount <= 0)
          return;

        if (TransactionStatus == DB.TransactionStatus.Uninitialized)
          return;

        if (Phase != GH_SolutionPhase.Failed)
        {
          if (Parameters.Document.GetDataOrDefault(this, default, default, out var Document))
            CommitTransaction(Document);
        }
      }
      finally
      {
        switch (TransactionStatus)
        {
          case DB.TransactionStatus.Uninitialized:
          case DB.TransactionStatus.Started:
          case DB.TransactionStatus.Committed:
            break;
          default:
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {TransactionStatus} and aborted.");
            break;
        }

        CurrentTransaction?.Dispose();
        CurrentTransaction = null;
      }
    }
  }

  public abstract class ReflectedComponent : TransactionComponent
  {
    protected ReflectedComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Reflection
    static readonly Dictionary<Type, (Type ParamType, Type GooType)> ParamTypes = new Dictionary<Type, (Type, Type)>()
    {
      { typeof(bool),                         (typeof(Param_Boolean),               typeof(GH_Boolean))             },
      { typeof(int),                          (typeof(Param_Integer),               typeof(GH_Integer))             },
      { typeof(double),                       (typeof(Param_Number),                typeof(GH_Number))              },
      { typeof(string),                       (typeof(Param_String),                typeof(GH_String))              },
      { typeof(Guid),                         (typeof(Param_Guid),                  typeof(GH_Guid))                },
      { typeof(DateTime),                     (typeof(Param_Time),                  typeof(GH_Time))                },

      { typeof(Transform),                    (typeof(Param_Transform),             typeof(GH_Transform))           },
      { typeof(Point3d),                      (typeof(Param_Point),                 typeof(GH_Point))               },
      { typeof(Vector3d),                     (typeof(Param_Vector),                typeof(GH_Vector))              },
      { typeof(Plane),                        (typeof(Param_Plane),                 typeof(GH_Plane))               },
      { typeof(Line),                         (typeof(Param_Line),                  typeof(GH_Line))                },
      { typeof(Arc),                          (typeof(Param_Arc),                   typeof(GH_Arc))                 },
      { typeof(Circle),                       (typeof(Param_Circle),                typeof(GH_Circle))              },
      { typeof(Curve),                        (typeof(Param_Curve),                 typeof(GH_Curve))               },
      { typeof(Surface),                      (typeof(Param_Surface),               typeof(GH_Surface))             },
      { typeof(Brep),                         (typeof(Param_Brep),                  typeof(GH_Brep))                },
//    { typeof(Extrusion),                    (typeof(Param_Extrusion),             typeof(GH_Extrusion))           },
      { typeof(Mesh),                         (typeof(Param_Mesh),                  typeof(GH_Mesh))                },
      { typeof(SubD),                         (typeof(Param_SubD),                  typeof(GH_SubD))                },

      { typeof(IGH_Goo),                      (typeof(Param_GenericObject),         typeof(IGH_Goo))                },
      { typeof(IGH_GeometricGoo),             (typeof(Param_Geometry),              typeof(IGH_GeometricGoo))       },

      { typeof(DB.Document),                  (typeof(Parameters.Document),         typeof(Types.Document))         },
      { typeof(DB.ElementFilter),             (typeof(Parameters.ElementFilter),    typeof(Types.ElementFilter))    },
      { typeof(DB.FilterRule),                (typeof(Parameters.FilterRule),       typeof(Types.FilterRule))       },
      { typeof(DB.ParameterElement),          (typeof(Parameters.ParameterKey),     typeof(Types.ParameterKey))     },

      { typeof(DB.ElementType),               (typeof(Parameters.ElementType),      typeof(Types.ElementType))      },
      { typeof(DB.Element),                   (typeof(Parameters.Element),          typeof(Types.Element))          },

      { typeof(DB.Category),                  (typeof(Parameters.Category),         typeof(Types.Category))         },
      { typeof(DB.Family),                    (typeof(Parameters.Family),           typeof(Types.Family))           },
      { typeof(DB.View),                      (typeof(Parameters.View),             typeof(Types.View))             },
      { typeof(DB.Group),                     (typeof(Parameters.Group),            typeof(Types.Group))            },

      { typeof(DB.SketchPlane),               (typeof(Parameters.SketchPlane),      typeof(Types.SketchPlane))      },
      { typeof(DB.Level),                     (typeof(Parameters.Level),            typeof(Types.Level))            },
      { typeof(DB.Grid),                      (typeof(Parameters.Grid),             typeof(Types.Grid))             },
      { typeof(DB.Material),                  (typeof(Parameters.Material),         typeof(Types.Material))         },

      { typeof(DB.HostObjAttributes),         (typeof(Parameters.HostObjectType),   typeof(Types.HostObjectType))   },
      { typeof(DB.HostObject),                (typeof(Parameters.HostObject),       typeof(Types.HostObject))       },
      { typeof(DB.Wall),                      (typeof(Parameters.Wall),             typeof(Types.Wall))             },
      { typeof(DB.Floor),                     (typeof(Parameters.Floor),            typeof(Types.Floor))            },
      { typeof(DB.Ceiling),                   (typeof(Parameters.Ceiling),          typeof(Types.Ceiling))          },
      { typeof(DB.RoofBase),                  (typeof(Parameters.Roof),             typeof(Types.Roof))             },
      { typeof(DB.CurtainSystem),             (typeof(Parameters.CurtainSystem),    typeof(Types.CurtainSystem))    },
      { typeof(DB.CurtainGridLine),           (typeof(Parameters.CurtainGridLine),  typeof(Types.CurtainGridLine))  },
      { typeof(DB.Architecture.BuildingPad),  (typeof(Parameters.BuildingPad),      typeof(Types.BuildingPad))      },

      { typeof(DB.FamilySymbol),              (typeof(Parameters.FamilySymbol),     typeof(Types.FamilySymbol))     },
      { typeof(DB.FamilyInstance),            (typeof(Parameters.FamilyInstance),   typeof(Types.FamilyInstance))   },

      { typeof(DB.SpatialElement),            (typeof(Parameters.SpatialElement),   typeof(Types.SpatialElement))   },
    };

    protected bool TryGetParamTypes(Type type, out (Type ParamType, Type GooType) paramTypes)
    {
      if (type.IsEnum)
      {
        if (Types.GH_Enum.TryGetParamTypes(type, out var enumTypes))
          paramTypes = (enumTypes.Item1, enumTypes.Item2);
        else
          paramTypes = (typeof(Param_Integer), typeof(GH_Integer));

        return true;
      }

      while (type != typeof(object))
      {
        if (ParamTypes.TryGetValue(type, out paramTypes))
          return true;

        type = type.BaseType;
      }

      paramTypes = default;
      return false;
    }

    IGH_Param CreateParam(Type argumentType)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return new Param_GenericObject();

      return (IGH_Param) Activator.CreateInstance(paramTypes.ParamType);
    }

    IGH_Goo CreateGoo(Type argumentType, object value)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return default;

      return (IGH_Goo) Activator.CreateInstance(paramTypes.GooType, value);
    }

    protected Type GetArgumentType(ParameterInfo parameter, out GH_ParamAccess access, out bool optional)
    {
      var parameterType = parameter.ParameterType.GetElementType() ?? parameter.ParameterType;

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

    protected void GetParams(MethodInfo methodInfo, out List<IGH_Param> inputs, out List<IGH_Param> outputs)
    {
      inputs = new List<IGH_Param>();
      outputs = new List<IGH_Param>();

      foreach (var parameter in methodInfo.GetParameters())
      {
        if (parameter.Position < 1)
          continue;

        if ((parameter.Position == 1) != (parameter.IsOut || parameter.ParameterType.IsByRef))
          throw new NotImplementedException();

        var argumentType = GetArgumentType(parameter, out var access, out var optional);
        var nickname = parameter.Name.First().ToString().ToUpperInvariant();
        var name = nickname + parameter.Name.Substring(1);

        if (parameter.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault() is NameAttribute nameAttribute)
          name = nameAttribute.Name;

        if (parameter.GetCustomAttributes(typeof(NickNameAttribute), false).FirstOrDefault() is NickNameAttribute nickNameAttribute)
          nickname = nickNameAttribute.NickName;

        var description = string.Empty;
        foreach (var descriptionAttribute in parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>())
          description = (description.Length > 0) ? $"{description}{Environment.NewLine}{descriptionAttribute.Description}" : descriptionAttribute.Description;

        var paramType = (parameter.GetCustomAttributes(typeof(ParamTypeAttribute), false).FirstOrDefault() as ParamTypeAttribute)?.Type;

        var param = paramType is null ? CreateParam(argumentType) : Activator.CreateInstance(paramType) as IGH_Param;
        {
          param.Name = name;
          param.NickName = nickname;
          param.Description = description;
          param.Access = access;
          param.Optional = optional;

          if(parameter.Position == 1)
            outputs.Add(param);
          else
            inputs.Add(param);
        }

        if (parameter.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() is DefaultValueAttribute defaultValueAttribute)
        {
          if (defaultValueAttribute.Value is object && param.GetType().IsGenericSubclassOf(typeof(GH_PersistentParam<>)))
          {
            dynamic persistentParam = param;
            persistentParam.SetPersistentData(defaultValueAttribute.Value);
          }
        }

        if (argumentType.IsEnum && param is Param_Integer integerParam)
        {
          foreach (var e in Enum.GetValues(argumentType))
            integerParam.AddNamedValue(Enum.GetName(argumentType, e), (int) e);
        }
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
    static readonly MethodInfo GetInputOptionalDataInfo = typeof(ReflectedComponent).GetMethod("GetInputOptionalData", BindingFlags.Instance | BindingFlags.NonPublic);

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
          throw new System.ComponentModel.InvalidEnumArgumentException(param.Name, enumValue, typeof(T));
        }

        value = (T) Enum.ToObject(typeof(T), enumValue);
      }
      else if (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(Optional<>)))
      {
        var args = new object[] { DA, index, null };

        try { return (bool) GetInputOptionalDataInfo.MakeGenericMethod(typeof(T).GetGenericArguments()[0]).Invoke(this, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
        finally { value = args[2] is object ? (T) args[2] : default; }
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
    protected static readonly MethodInfo GetInputDataInfo = typeof(ReflectedComponent).GetMethod("GetInputData", BindingFlags.Instance | BindingFlags.NonPublic);

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
    protected static readonly MethodInfo GetInputDataListInfo = typeof(ReflectedComponent).GetMethod("GetInputDataList", BindingFlags.Instance | BindingFlags.NonPublic);

    static string FirstCharUpper(string text)
    {
      if (char.IsUpper(text, 0))
        return text;

      var chars = text.ToCharArray();
      chars[0] = char.ToUpperInvariant(chars[0]);
      return new string(chars);
    }

    protected void ThrowArgumentNullException(string paramName, string description = null) => throw new ArgumentNullException(FirstCharUpper(paramName), description ?? string.Empty);

    protected void ThrowArgumentException(string paramName, string description = null)
    {
      if (description is null)
        description = "Input value is not valid.";

      description = description.TrimEnd(Environment.NewLine.ToCharArray());

      throw new ArgumentException(description, FirstCharUpper(paramName));
    }

    protected bool ThrowIfNotValid(string paramName, Point3d value)
    {
      if (!value.IsValid) ThrowArgumentException(paramName);
      return true;
    }

    protected bool ThrowIfNotValid(string paramName, GeometryBase value)
    {
      if (value is null) ThrowArgumentNullException(paramName);
      if (!value.IsValidWithLog(out var log))
      {
        AddGeometryRuntimeError(GH_RuntimeMessageLevel.Error, default, value);
        ThrowArgumentException(paramName, $"Input geometry is not valid.{Environment.NewLine}{log}");
      }

      return true;
    }
    #endregion
  }

  public abstract class ReconstructElementComponent :
    ReflectedComponent,
    Bake.IGH_ElementIdBakeAwareObject
  {
    protected IGH_Goo[] PreviousStructure;
    System.Collections.IEnumerator PreviousStructureEnumerator;

    protected ReconstructElementComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    List<IGH_Param> inputs;
    List<IGH_Param> outputs;

    protected override void PostConstructor()
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      GetParams(ReconstructInfo, out inputs, out outputs);

      foreach (var param in inputs.Concat(outputs))
      {
        if (string.IsNullOrEmpty(param.NickName)) param.NickName = param.Name;
        if (param.Description is null) param.Description = string.Empty;
        if (param.Description == string.Empty) param.Description = param.Name;
      }

      base.PostConstructor();
    }

    protected sealed override void RegisterInputParams(GH_InputParamManager manager)
    {
      foreach (var input in inputs)
      {
        if (input.Attributes is null)
          input.Attributes = new GH_LinkedParamAttributes(input, Attributes);

        manager.AddParameter(input);
      }
    }

    protected sealed override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var output in outputs)
      {
        if (output.Attributes is null)
          output.Attributes = new GH_LinkedParamAttributes(output, Attributes);

        manager.AddParameter(output);
      }
    }

    protected static void ReplaceElement<T>(ref T previous, T next, ICollection<DB.BuiltInParameter> parametersMask = null) where T : DB.Element
    {
      next.CopyParametersFrom(previous, parametersMask);
      previous = next;
    }

    // Step 2.
    protected override void OnAfterStart(DB.Document document, string strTransactionName)
    {
      PreviousStructureEnumerator = PreviousStructure?.GetEnumerator();
    }

    // Step 3.
    protected sealed override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Parameters.Document.GetDataOrDefault(this, default, default, out var Document))
        Iterate(DA, Document, (DB.Document doc, ref DB.Element current) => TrySolveInstance(DA, doc, ref current));
    }

    delegate void CommitAction(DB.Document doc, ref DB.Element element);

    void Iterate(IGH_DataAccess DA, DB.Document doc, CommitAction action)
    {
      var element = PreviousStructureEnumerator?.MoveNext() ?? false ?
                    (
                      PreviousStructureEnumerator.Current is Types.Element x && doc.Equals(x.Document) ?
                      doc.GetElement(x.Id) :
                      null
                    ) :
                    null;

      if (element?.Pinned != false)
      {
        var previous = element;

        if (element?.DesignOption?.Id is DB.ElementId elementDesignOptionId)
        {
          var activeDesignOptionId = DB.DesignOption.GetActiveDesignOptionId(element.Document);

          if (elementDesignOptionId != activeDesignOptionId)
            element = null;
        }

        try
        {
          action(doc, ref element);
        }
        catch (CancelException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          var message = e.Message.Split("\r\n".ToCharArray()).First().Replace("Application.ShortCurveTolerance", "Revit.ShortCurveTolerance");
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {message}");
          element = null;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (System.ComponentModel.WarningException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
          element = null;
        }
        catch (System.ArgumentNullException)
        {
          // Grasshopper components use to send a Null when they receive a Null without throwing any error
          element = null;
        }
        catch (System.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (System.Exception e)
        {
          throw e;
        }
        finally
        {
          if (previous?.IsValidObject == true && !previous.IsEquivalent(element))
            previous.Document.Delete(previous.Id);

          if (element?.IsValidObject == true)
          {
            try { element.Pinned = true; }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
          }
        }
      }

      DA.SetData(0, element);
    }

    void TrySolveInstance
    (
      IGH_DataAccess DA,
      DB.Document doc,
      ref DB.Element element
    )
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      var parameters = ReconstructInfo.GetParameters();

      var arguments = new object[parameters.Length];
      try
      {
        arguments[0] = doc;
        arguments[1] = element;

        var args = new object[] { DA, null, null };
        foreach (var parameter in parameters)
        {
          var paramIndex = parameter.Position - 2;

          if (paramIndex < 0)
            continue;

          args[1] = paramIndex;

          try
          {
            switch (Params.Input[paramIndex].Access)
            {
              case GH_ParamAccess.item: GetInputDataInfo.MakeGenericMethod(parameter.ParameterType).Invoke(this, args); break;
              case GH_ParamAccess.list: GetInputDataListInfo.MakeGenericMethod(parameter.ParameterType.GetGenericArguments()[0]).Invoke(this, args); break;
              default: throw new NotImplementedException();
            }
          }
          catch (TargetInvocationException e) { throw e.InnerException; }
          finally { arguments[parameter.Position] = args[2]; args[2] = null; }
        }

        ReconstructInfo.Invoke(this, arguments);
      }
      catch (TargetInvocationException e) { throw e.InnerException; }
      finally { element = (DB.Element) arguments[1]; }
    }

    // Step 4.
    protected override void OnBeforeCommit(DB.Document document, string strTransactionName)
    {
      // Remove extra unused elements
      while (PreviousStructureEnumerator?.MoveNext() ?? false)
      {
        if (PreviousStructureEnumerator.Current is Types.Element elementId && document.Equals(elementId.Document))
        {
          if (document.GetElement(elementId.Id) is DB.Element element)
          {
            try { document.Delete(element.Id); }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
          }
        }
      }
    }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      try { base.AfterSolveInstance(); }
      finally { PreviousStructureEnumerator = null; }
    }

    // Step 5.2.A
    public override void OnCommitted(DB.Document document, string strTransactionName)
    {
      // Update previous elements
      PreviousStructure = Params.Output[0].VolatileData.AllData(false).ToArray();
    }

    #region IGH_ElementIdBakeAwareObject
    bool Bake.IGH_ElementIdBakeAwareObject.CanBake(Bake.BakeOptions options) =>
      Params?.Output.OfType<Kernel.IGH_ElementIdParam>().
      Where
      (
        x =>
        x.VolatileData.AllData(true).
        OfType<Types.IGH_Element>().
        Where(goo => options.Document.Equals(goo.Document)).
        Any()
      ).
      Any() ?? false;

    bool Bake.IGH_ElementIdBakeAwareObject.Bake(Bake.BakeOptions options, out ICollection<DB.ElementId> ids)
    {
      using (var trans = new DB.Transaction(options.Document, "Bake"))
      {
        if (trans.Start() == DB.TransactionStatus.Started)
        {
          var list = new List<DB.ElementId>();
          var newStructure = (IGH_Goo[]) PreviousStructure.Clone();
          for (int g = 0; g < newStructure.Length; g++)
          {
            if (newStructure[g] is Types.IGH_Element id)
            {
              if
              (
                id.Document.Equals(options.Document) &&
                id.Document.GetElement(id.Id) is DB.Element element
              )
              {
                try { element.Pinned = false; }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

                list.Add(element.Id);
                newStructure[g] = default;
              }
            }
          }

          if (trans.Commit() == DB.TransactionStatus.Committed)
          {
            ids = list;
            PreviousStructure = newStructure;
            ExpireSolution(false);
            return true;
          }
        }
      }

      ids = default;
      return false;
    }
    #endregion
  }
}
