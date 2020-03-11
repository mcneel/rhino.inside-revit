using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Exceptions;
using RhinoInside.Revit.GH;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class ReconstructElementComponent :
    TransactionComponent,
    Bake.IGH_ElementIdBakeAwareObject
  {
    protected IGH_Goo[] PreviousStructure;
    System.Collections.IEnumerator PreviousStructureEnumerator;

    protected ReconstructElementComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override sealed void RegisterInputParams(GH_InputParamManager manager)
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      RegisterInputParams(manager, ReconstructInfo);
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
    protected override sealed void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Revit.ActiveDBDocument is DB.Document Document)
        Iterate(DA, Document, (DB.Document doc, ref DB.Element current) => TrySolveInstance(DA, doc, ref current));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no active Revit document");
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
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
          DA.AbortComponentSolution();
        }
        finally
        {
          if (previous is object && !ReferenceEquals(previous, element) && previous.IsValidObject)
            previous.Document.Delete(previous.Id);

          if (element?.IsValidObject == true)
            element.Pinned = true;
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
      Params?.Output.OfType<Parameters.IGH_ElementIdParam>().
      Where
      (
        x =>
        x.VolatileData.AllData(true).
        OfType<Types.IGH_ElementId>().
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
            if (newStructure[g] is Types.IGH_ElementId id)
            {
              if
              (
                id.Document.Equals(options.Document) &&
                id.Document.GetElement(id.Id) is DB.Element element
              )
              {
                element.Pinned = false;
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
