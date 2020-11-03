using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Kernel.Attributes
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class ComponentGuidAttribute : Attribute
  {
    public readonly Guid Value;
    public ComponentGuidAttribute(string value) => Value = new Guid(value);
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface| AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class NameAttribute : Attribute
  {
    public readonly string Name;
    public NameAttribute(string value) => Name = value;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class NickNameAttribute : Attribute
  {
    public readonly string NickName;
    public NickNameAttribute(string value) => NickName = value;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class DescriptionAttribute : Attribute
  {
    public readonly string Description;
    public DescriptionAttribute(string value) => Description = value;
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class CategoryAttribute : Attribute
  {
    public readonly string Category;
    public readonly string SubCategory;
    public CategoryAttribute(string category, string subCategory) { Category = category; SubCategory = subCategory; }
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class ExposureAttribute : Attribute
  {
    public readonly GH_Exposure Value;
    public ExposureAttribute(GH_Exposure value) => Value = value;
  }

  [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class DefaultValueAttribute : Attribute
  {
    public readonly object Value;
    public DefaultValueAttribute(object value) => Value = value;
  }
}
