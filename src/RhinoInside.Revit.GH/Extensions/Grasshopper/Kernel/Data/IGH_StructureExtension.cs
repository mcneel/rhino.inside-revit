using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel.Data
{
  static class IGH_StructureExtension
  {
    public static GH_Structure<T> DuplicateAs<T>(this IGH_Structure structure, bool shallowCopy)
      where T : IGH_Goo
    {
      // GH_Structure<T> constructor is a bit faster if shallowCopy is true because
      // it doesn't need to cast on each item.
      if (structure is GH_Structure<T> structureT)
        return new GH_Structure<T>(structureT, shallowCopy);

      var result = new GH_Structure<T>();

      for (int p = 0; p < structure.PathCount; ++p)
      {
        var path = structure.get_Path(p);
        var srcBranch = structure.get_Branch(path);

        var destBranch = result.EnsurePath(path);
        destBranch.Capacity = srcBranch.Count;

        var data = srcBranch.As<T>();
        if (!shallowCopy)
          data = data.Select(x => x?.Duplicate() is T t ? t : default);

        destBranch.AddRange(data);
      }

      return result;
    }
  }
}
