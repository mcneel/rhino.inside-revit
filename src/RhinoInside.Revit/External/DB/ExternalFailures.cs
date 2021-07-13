using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  /// <summary>
  /// Provides a container of all Rhino.Inside built-in FailureDefinitionId instances.
  /// </summary>
  public static class ExternalFailures
  {
    internal static void CreateFailureDefinitions()
    {
      TransactionFailures.CreateFailureDefinitions();
      ElementFailures.CreateFailureDefinitions();
    }

    /// <summary>
    /// Failures about Transactions.
    /// </summary>
    public static class TransactionFailures
    {
      /// <summary>
      /// Transaction was simulated. All changes it did are not effective.
      /// </summary>
      public static readonly FailureDefinitionId SimulatedTransaction = new FailureDefinitionId(new Guid("6E69B3C2-E7D5-48E2-A9A4-1BEF01C6283C"));

      internal static void CreateFailureDefinitions()
      {
        FailureDefinition.CreateFailureDefinition
        (
          id: SimulatedTransaction,
          severity: FailureSeverity.Error,
          messageString: "Transaction was simulated. All changes it did are not effective."
        );
      }
    }

    /// <summary>
    /// Failures about Elements.
    /// </summary>
    public static class ElementFailures
    {
      /// <summary>
      /// Are you sure you want to delete those elements?
      /// </summary>
      public static readonly FailureDefinitionId ConfirmDeleteElement = new FailureDefinitionId(new Guid("BD477F3C-8560-4A51-8BBD-870A5C97EE22"));

      /// <summary>
      /// Failed to purge element. This element is in use.
      /// </summary>
      public static readonly FailureDefinitionId FailedToPurgeElement = new FailureDefinitionId(new Guid("D2732B32-E917-4D3A-B639-A72E3A20F2E5"));

      internal static void CreateFailureDefinitions()
      {
        FailureDefinition.CreateFailureDefinition
        (
          id: ConfirmDeleteElement,
          severity: FailureSeverity.Warning,
          messageString: "Are you sure you want to delete these elements?"
        );

        FailureDefinition.CreateFailureDefinition
        (
          id: FailedToPurgeElement,
          severity: FailureSeverity.Error,
          messageString: "Failed to purge element. This element is in use."
        );
      }
    }
  }
}
