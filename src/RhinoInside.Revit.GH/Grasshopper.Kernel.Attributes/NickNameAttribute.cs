using System;
using Grasshopper.Kernel;

namespace Grasshopper.Kernel.Attributes
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class NickNameAttribute : Attribute
  {
    public readonly string NickName;
    public NickNameAttribute(string value) => NickName = value;
  }
}
