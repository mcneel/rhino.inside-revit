using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel.Types
{
  public abstract class GH_Enumerate : GH_Integer
  {
    protected GH_Enumerate() { }
    protected GH_Enumerate(int value) : base(value) { }
    public abstract Type UnderlyingEnumType { get; }
    public override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    static Dictionary<Type, Tuple<Type, Type>> LookForEnums(Assembly assembly)
    {
      var result = new Dictionary<Type, Tuple<Type, Type>>();

      foreach (var type in assembly.ExportedTypes.Where(x => x.IsSubclassOf(typeof(GH_Enumerate))))
      {
        bool typeFound = false;
        var gooType = type;
        while (gooType != typeof(GH_Enumerate))
        {
          if (gooType.IsConstructedGenericType && gooType.GetGenericTypeDefinition() == typeof(GH_Enum<>))
          {
            var valueType = gooType.GetGenericArguments()[0];
            foreach (var param in assembly.ExportedTypes.Where(x => x.GetInterfaces().Contains(typeof(IGH_Param))))
            {
              if (!param.IsClass)
                continue;

              var paramType = param;
              while (paramType != typeof(GH_ActiveObject))
              {
                if (paramType.IsConstructedGenericType && paramType.GetGenericTypeDefinition() == typeof(Parameters.Param_Enum<>))
                {
                  if (paramType.GetGenericArguments()[0] == type)
                  {
                    result.Add(valueType, Tuple.Create(param.GetType(), type));
                    typeFound = true;
                    break;
                  }
                }

                paramType = paramType.BaseType;
              }

              if (typeFound)
                break;
            }

            if (!typeFound)
            {
              result.Add(valueType, Tuple.Create(typeof(Parameters.Param_Enum<>).MakeGenericType(type), type));
              typeFound = true;
            }
          }

          if (typeFound)
            break;

          gooType = gooType.BaseType;
        }
      }

      // Register all the ParamsTypes as params in Grasshopper
      foreach(var entry in result)
      {
        var proxy = Activator.CreateInstance(entry.Value.Item1) as IGH_ObjectProxy;
        if(!Grasshopper.Instances.ComponentServer.IsObjectCached(proxy.Guid))
          Grasshopper.Instances.ComponentServer.AddProxy(proxy);
      }
      
      return result;
    }

    static readonly Dictionary<Type, Tuple<Type, Type>> EnumTypes = LookForEnums(Assembly.GetCallingAssembly());
    public static bool TryGetParamTypes(Type type, out Tuple<Type, Type> paramTypes) =>
      EnumTypes.TryGetValue(type, out paramTypes);

    public virtual Array GetEnumValues() => Enum.GetValues(UnderlyingEnumType);
  }
}
