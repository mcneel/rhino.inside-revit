using System;
using Grasshopper.Kernel;

namespace Grasshopper.Kernel.Attributes
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class ExposureAttribute : Attribute
  {
    public readonly GH_Exposure Exposure;
    public ExposureAttribute(GH_Exposure value) => Exposure = value;
  }
}
