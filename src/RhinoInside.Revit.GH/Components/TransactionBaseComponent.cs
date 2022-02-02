using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.Units;
  using ElementTracking;
  using Exceptions;
  using External.DB.Extensions;
  using Kernel.Attributes;

  [Obsolete]
  public abstract class TransactionBaseComponent : ElementTrackerComponent
  {
    protected TransactionBaseComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Solve Optional values
    protected static double LiteralLengthValue(double meters)
    {
      var modelUnitScale = UnitScale.GetModelScale(Rhino.RhinoDoc.ActiveDoc);
      if (modelUnitScale == UnitScale.None || modelUnitScale == UnitScale.Inches || modelUnitScale == UnitScale.Feet)
      {
        return UnitScale.Convert
        (
          Math.Round(UnitScale.Convert(meters, UnitScale.Meters, UnitScale.Internal)),
          UnitScale.Internal,
          modelUnitScale
        );
      }
      else return UnitScale.Convert(meters, UnitScale.Meters, modelUnitScale);
    }

    protected static void ChangeElementTypeId<T>(ref T element, ARDB.ElementId elementTypeId) where T : ARDB.Element
    {
      if (element is object && elementTypeId != element.GetTypeId())
      {
        var doc = element.Document;
        if (element.IsValidType(elementTypeId))
        {
          var newElmentId = element.ChangeTypeId(elementTypeId);
          if (newElmentId != ARDB.ElementId.InvalidElementId)
            element = (T) doc.GetElement(newElmentId);
        }
        else element = null;
      }
    }

    protected static void ChangeElementType<E, T>(ref E element, Optional<T> elementType) where E : ARDB.Element where T : ARDB.ElementType
    {
      if (elementType.HasValue && element is object)
      {
        if (!element.Document.Equals(elementType.Value.Document))
          throw new RuntimeArgumentException(nameof(elementType), $"{nameof(ChangeElementType)} failed to assign a type from a diferent document.");

        ChangeElementTypeId(ref element, elementType.Value.Id);
      }
    }

    protected static bool SolveOptionalCategory(ref Optional<ARDB.Category> category, ARDB.Document doc, ARDB.BuiltInCategory builtInCategory, string paramName)
    {
      bool wasMissing = category.IsMissing;

      if (wasMissing)
      {
        if (doc.IsFamilyDocument)
          category = doc.OwnerFamily.FamilyCategory;

        if(category.IsMissing)
        {
          category = ARDB.Category.GetCategory(doc, builtInCategory) ??
          throw new RuntimeArgumentException(paramName, "No suitable Category has been found.");
        }
      }

      else if (category.Value == null)
        throw new RuntimeArgumentNullException(paramName);

      return wasMissing;
    }

    protected static bool SolveOptionalType<T>(ARDB.Document doc, ref Optional<T> type, ARDB.ElementTypeGroup group, string paramName) where T : ARDB.ElementType
    {
      return SolveOptionalType(doc, ref type, group, (document, name) => throw new RuntimeArgumentNullException(paramName), paramName);
    }

    protected static bool SolveOptionalType<T>(ARDB.Document doc, ref Optional<T> type, ARDB.ElementTypeGroup group, Func<ARDB.Document, string, T> recoveryAction, string paramName) where T : ARDB.ElementType
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = (T) doc.GetElement(doc.GetDefaultElementTypeId(group)) ??
               (T) recoveryAction.Invoke(doc, paramName) ??
        throw new RuntimeArgumentException(paramName, $"No suitable {group} has been found.");

      else if (type.Value == null)
        throw new RuntimeArgumentNullException(paramName);

      else if (!type.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(type), $"{nameof(SolveOptionalType)} failed to assign a type from a diferent document.");

      return wasMissing;
    }

    protected static bool SolveOptionalType(ARDB.Document doc, ref Optional<ARDB.FamilySymbol> type, ARDB.BuiltInCategory category, string paramName)
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = doc.GetElement(doc.GetDefaultFamilyTypeId(new ARDB.ElementId(category))) as ARDB.FamilySymbol ??
               throw new RuntimeArgumentException(paramName, "No suitable type has been found.");

      else if (type.Value == null)
        throw new RuntimeArgumentNullException(paramName);

      else if (!type.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(type), $"{nameof(SolveOptionalType)} failed to assign a type from a diferent document.");

      if (!type.Value.IsActive)
        type.Value.Activate();

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, ARDB.Element host, ref Optional<ARDB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
      {
        if (host?.Document.GetElement(host.LevelId) is ARDB.Level newLevel)
          level = newLevel;
      }

      else if (level.Value == null)
        throw new RuntimeArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(level), "Failed to assign a level from a diferent document.");

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, double height, ref Optional<ARDB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
        level = doc.FindLevelByHeight(height / Revit.ModelUnits) ??
                throw new RuntimeArgumentException(nameof(height), "No suitable level has been found.");

      else if (level.Value == null)
        throw new RuntimeArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(level), "Failed to assign a level from a diferent document.");

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, Point3d point, ref Optional<ARDB.Level> level, out BoundingBox bbox)
    {
      bbox = new Rhino.Geometry.BoundingBox(point, point);
      return SolveOptionalLevel(doc, point.IsValid ? point.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, Line line, ref Optional<ARDB.Level> level, out BoundingBox bbox)
    {
      bbox = line.BoundingBox;
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, GeometryBase geometry, ref Optional<ARDB.Level> level, out BoundingBox bbox)
    {
      bbox = geometry.GetBoundingBox(true);
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(ARDB.Document doc, IEnumerable<GeometryBase> geometries, ref Optional<ARDB.Level> level, out BoundingBox bbox)
    {
      bbox = Rhino.Geometry.BoundingBox.Empty;
      foreach (var geometry in geometries)
        bbox.Union(geometry.GetBoundingBox(true));

      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static void SolveOptionalLevelsFromBase(ARDB.Document doc, double height, ref Optional<ARDB.Level> baseLevel, ref Optional<ARDB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var b = doc.FindBaseLevelByHeight(height / Revit.ModelUnits, out var t) ??
                t ?? throw new RuntimeArgumentException(nameof(height), "No suitable base level has been found.");

        if (!baseLevel.HasValue)
          baseLevel = b;

        if (!topLevel.HasValue)
          topLevel = t ?? b;
      }

      else if (baseLevel.Value == null)
        throw new RuntimeArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new RuntimeArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(baseLevel), "Failed to assign a level from a diferent document.");

      else if (!topLevel.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(topLevel), "Failed to assign a level from a diferent document.");
    }

    protected static void SolveOptionalLevelsFromTop(ARDB.Document doc, double height, ref Optional<ARDB.Level> baseLevel, ref Optional<ARDB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var t = doc.FindTopLevelByHeight(height / Revit.ModelUnits, out var b) ??
                b ?? throw new RuntimeArgumentException(nameof(height), "No suitable top level has been found.");

        if (!topLevel.HasValue)
          topLevel = t;

        if (!baseLevel.HasValue)
          baseLevel = b ?? t;
      }

      else if (baseLevel.Value == null)
        throw new RuntimeArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new RuntimeArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(baseLevel), "Failed to assign a level from a diferent document.");

      else if (!topLevel.Value.Document.Equals(doc))
        throw new RuntimeArgumentException(nameof(topLevel), "Failed to assign a level from a diferent document.");
    }

    protected static bool SolveOptionalLevels(ARDB.Document doc, Rhino.Geometry.Curve curve, ref Optional<ARDB.Level> baseLevel, ref Optional<ARDB.Level> topLevel)
    {
      bool result = true;

      result &= SolveOptionalLevel(doc, Math.Min(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);
      result &= SolveOptionalLevel(doc, Math.Max(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);

      return result;
    }
    #endregion

    #region Geometry Conversion
    public static bool TryGetCurveAtPlane(Curve curve, Plane plane, out ARDB.Curve projected)
    {
      if (Curve.ProjectToPlane(curve, plane) is Curve p)
      {
        var tol = GeometryObjectTolerance.Model;

        if (p.TryGetLine(out var line, tol.VertexTolerance))
          projected = line.ToLine();
        else if (p.TryGetArc(plane, out var arc, tol.VertexTolerance))
          projected = arc.ToArc();
        else if (p.TryGetEllipse(plane, out var ellipse, out var interval, tol.VertexTolerance))
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

  public abstract class ReflectedComponent : TransactionBaseComponent
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

      { typeof(ARDB.Document),                  (typeof(Parameters.Document),         typeof(Types.Document))         },
      { typeof(ARDB.ElementFilter),             (typeof(Parameters.ElementFilter),    typeof(Types.ElementFilter))    },
      { typeof(ARDB.FilterRule),                (typeof(Parameters.FilterRule),       typeof(Types.FilterRule))       },
      { typeof(ARDB.ParameterElement),          (typeof(Parameters.ParameterKey),     typeof(Types.ParameterKey))     },

      { typeof(ARDB.ElementType),               (typeof(Parameters.ElementType),      typeof(Types.ElementType))      },
      { typeof(ARDB.Element),                   (typeof(Parameters.Element),          typeof(Types.Element))          },

      { typeof(ARDB.Category),                  (typeof(Parameters.Category),         typeof(Types.Category))         },
      { typeof(ARDB.Family),                    (typeof(Parameters.Family),           typeof(Types.Family))           },
      { typeof(ARDB.View),                      (typeof(Parameters.View),             typeof(Types.View))             },
      { typeof(ARDB.Group),                     (typeof(Parameters.Group),            typeof(Types.Group))            },

      { typeof(ARDB.CurveElement),              (typeof(Parameters.CurveElement),     typeof(Types.CurveElement))     },
      { typeof(ARDB.SketchPlane),               (typeof(Parameters.SketchPlane),      typeof(Types.SketchPlane))      },
      { typeof(ARDB.Level),                     (typeof(Parameters.Level),            typeof(Types.Level))            },
      { typeof(ARDB.Grid),                      (typeof(Parameters.Grid),             typeof(Types.Grid))             },
      { typeof(ARDB.Material),                  (typeof(Parameters.Material),         typeof(Types.Material))         },

      { typeof(ARDB.HostObjAttributes),         (typeof(Parameters.HostObjectType),   typeof(Types.HostObjectType))   },
      { typeof(ARDB.HostObject),                (typeof(Parameters.HostObject),       typeof(Types.HostObject))       },
      { typeof(ARDB.Wall),                      (typeof(Parameters.Wall),             typeof(Types.Wall))             },
      { typeof(ARDB.Floor),                     (typeof(Parameters.Floor),            typeof(Types.Floor))            },
      { typeof(ARDB.Ceiling),                   (typeof(Parameters.Ceiling),          typeof(Types.Ceiling))          },
      { typeof(ARDB.RoofBase),                  (typeof(Parameters.Roof),             typeof(Types.Roof))             },
      { typeof(ARDB.CurtainSystem),             (typeof(Parameters.CurtainSystem),    typeof(Types.CurtainSystem))    },
      { typeof(ARDB.CurtainGridLine),           (typeof(Parameters.CurtainGridLine),  typeof(Types.CurtainGridLine))  },
      { typeof(ARDB.Architecture.BuildingPad),  (typeof(Parameters.BuildingPad),      typeof(Types.BuildingPad))      },

      { typeof(ARDB.FamilySymbol),              (typeof(Parameters.FamilySymbol),     typeof(Types.FamilySymbol))     },
      { typeof(ARDB.FamilyInstance),            (typeof(Parameters.FamilyInstance),   typeof(Types.FamilyInstance))   },

      { typeof(ARDB.SpatialElement),            (typeof(Parameters.SpatialElement),   typeof(Types.SpatialElement))   },
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

      optional = parameter.IsOptional;
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

    protected void GetParams(MethodInfo methodInfo, out List<(IGH_Param Param, ParamRelevance Relevance)> inputs, out List<(IGH_Param Param, ParamRelevance Relevance)> outputs)
    {
      inputs = new List<(IGH_Param Param, ParamRelevance Relevance)>();
      outputs = new List<(IGH_Param Param, ParamRelevance Relevance)>();

      foreach (var parameter in methodInfo.GetParameters())
      {
        // HACK: Only Tracked Element may be ByRef
        if (((parameter.Position == 1) != parameter.ParameterType.IsByRef) && !parameter.IsIn && !parameter.IsOut)
          throw new NotImplementedException();

        var argumentType = GetArgumentType(parameter, out var access, out var optional);
        var nickname = parameter.Name.First().ToString().ToUpperInvariant();
        var name = nickname + parameter.Name.Substring(1);

        // HACK: for Document parameter
        var relevance = parameter.Position == 0 ? ParamRelevance.Occasional : ParamRelevance.Binding;

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

          if (parameter.ParameterType.IsByRef)
          {
            if (!parameter.IsIn && !parameter.IsOut)
            {
              outputs.Add((param, relevance));
            }
            else
            {
              if (parameter.IsIn)
                inputs.Add((param, relevance));

              if (parameter.IsOut)
                outputs.Add((param, relevance));
            }
          }
          else
            inputs.Add((param, relevance));
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

          throw new RuntimeArgumentNullException(param.Name);
        }

        if (!typeof(T).IsEnumDefined(enumValue))
        {
          var param = Params.Input[index];
          throw new RuntimeArgumentException(param.Name);
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
        if (!DA.GetData(index, ref value) || ReferenceEquals(value, null))
        {
          var param = Params.Input[index];
          if (param.Optional && param.SourceCount == 0)
            return false;

          throw new RuntimeArgumentNullException(param.Name);
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

    protected void ThrowArgumentNullException(string paramName, string description = null) => throw new RuntimeArgumentNullException(FirstCharUpper(paramName), description ?? string.Empty);

    protected void ThrowArgumentException(string paramName, string description = null)
    {
      if (description is null)
        description = "Input value is not valid.";

      description = description.TrimEnd(Environment.NewLine.ToCharArray());

      throw new RuntimeArgumentException(FirstCharUpper(paramName), description);
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
    protected ReconstructElementComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void PostConstructor()
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      GetParams(ReconstructInfo, out var ins, out var outs);

      inputs = ins.Select
      (
        x =>
        {
          if (string.IsNullOrEmpty(x.Param.NickName)) x.Param.NickName = x.Param.Name;
          if (x.Param.Description is null) x.Param.Description = string.Empty;
          if (x.Param.Description == string.Empty) x.Param.Description = x.Param.Name;
          return new ParamDefinition(x.Param, x.Relevance);
        }
      ).ToArray();

      outputs = outs.Select
      (
        x =>
        {
          if (string.IsNullOrEmpty(x.Param.NickName)) x.Param.NickName = x.Param.Name;
          if (x.Param.Description is null) x.Param.Description = string.Empty;
          if (x.Param.Description == string.Empty) x.Param.Description = x.Param.Name;
          return new ParamDefinition(x.Param, x.Relevance);
        }
      ).ToArray();

      base.PostConstructor();
    }

    private ParamDefinition[] inputs;
    protected override ParamDefinition[] Inputs => inputs;

    private ParamDefinition[] outputs;
    protected override ParamDefinition[] Outputs => outputs;

    protected static void ReplaceElement<T>(ref T previous, T next, ICollection<ARDB.BuiltInParameter> parametersMask = null) where T : ARDB.Element
    {
      next.CopyParametersFrom(previous, parametersMask);
      previous = next;
    }

    // Step 3.
    protected sealed override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var document))
      {
        if (document.IsValid)
        {
          StartTransaction(document.Value);
          Iterate
          (
            DA,
            document.Value,
            (ARDB.Document doc, ref ARDB.Element current) => TrySolveInstance(DA, doc, ref current)
          );
        }
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter Document failed to collect data");
    }

    delegate void CommitAction(ARDB.Document doc, ref ARDB.Element element);

    void Iterate(IGH_DataAccess DA, ARDB.Document doc, CommitAction action)
    {
      // Previous Output
      var trackedParam = Params.Output[0];
      Params.ReadTrackedElement(trackedParam.Name, doc, out ARDB.Element previous);
      var element = previous; 

      if (element?.DesignOption?.Id is ARDB.ElementId elementDesignOptionId)
      {
        var activeDesignOptionId = ARDB.DesignOption.GetActiveDesignOptionId(element.Document);

        if (elementDesignOptionId != activeDesignOptionId)
          element = null;
      }

      var graphical = element is object && Types.GraphicalElement.IsValidElement(element);
      var pinned = element?.Pinned != false;

      try
      {
        if(!graphical || pinned)
          action(doc, ref element);
      }
      catch (RuntimeArgumentNullException)
      {
        // Grasshopper components use to send a Null when
        // they receive a Null without throwing any error
        element = null;
      }
      catch (RuntimeArgumentException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
        element = null;
      }
      finally
      {
        Params.WriteTrackedElement(trackedParam.Name, doc, element);
        DA.SetData(trackedParam.Name, element);

        if (Types.GraphicalElement.IsValidElement(element))
        {
          // In case element is crated on this iteratrion we pin it here by default
          if (pinned && !element.Pinned)
          {
            try { element.Pinned = true; }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
          }
        }
      }
    }

    void TrySolveInstance
    (
      IGH_DataAccess DA,
      ARDB.Document doc,
      ref ARDB.Element element
    )
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      var parameters = ReconstructInfo.GetParameters();

      var arguments = new object[parameters.Length];
      int docParamIndex = Params.IndexOfInputParam("Document");
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

          // HACK: Skip Document if present
          paramIndex += docParamIndex + 1;

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
      finally { element = (ARDB.Element) arguments[1]; }
    }

    // Step 2.1
    public override void OnStarted(ARDB.Document document)
    {
      base.OnStarted(document);
    }

    // Step 3.1
    public override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      base.OnPrepare(documents);
    }

    // Step 3.2
    public override void OnDone(ARDB.TransactionStatus status)
    {
      base.OnDone(status);
    }
    #region IGH_ElementIdBakeAwareObject

    IEnumerable<Types.IGH_GraphicalElement> GetElementsToBake(ARDB.Document document) =>
      Params.Output.Where(x => x is Kernel.IGH_ElementIdParam).
      SelectMany(x => x.VolatileData.AllData(true).OfType<Types.IGH_GraphicalElement>()).
      Where(x => x.Document.IsEquivalent(document));

    bool Bake.IGH_ElementIdBakeAwareObject.CanBake(Bake.BakeOptions options) =>
      ARDB.DirectShape.IsSupportedDocument(options.Document) &&
      GetElementsToBake(options.Document).Any();

    bool Bake.IGH_ElementIdBakeAwareObject.Bake(Bake.BakeOptions options, out ICollection<ARDB.ElementId> ids)
    {
      using (var trans = new ARDB.Transaction(options.Document, "Bake"))
      {
        if (trans.Start() == ARDB.TransactionStatus.Started)
        {
          var newIds = new List<ARDB.ElementId>();
          var graphicalElements = GetElementsToBake(options.Document);

          foreach (var graphicalElement in graphicalElements)
          {
            if (graphicalElement.Value is ARDB.Element element && element.get_BoundingBox(null) != null)
            {
              using (var geometryOptions = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
              {
                using (var geometry = element.get_Geometry(geometryOptions))
                {
                  var list = geometry.ToDirectShapeGeometry().ToList();
                  if (list.Count > 0)
                  {
                    var categoryId = ARDB.DirectShape.IsValidCategoryId(element.Category.Id, options.Document) ?
                      element.Category.Id : new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);

                    try
                    {
                      var shape = ARDB.DirectShape.CreateElement(options.Document, categoryId);
                      shape.SetShape(list);
                      newIds.Add(shape.Id);
                    }
                    catch { }
                  }
                }
              }
            }
          }

          if (trans.Commit() == ARDB.TransactionStatus.Committed)
          {
            ids = newIds;
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
