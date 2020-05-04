using System;

namespace RhinoInside.Revit.Convert
{
  /// <summary>
  /// The exception that is thrown when a geometry conversion error occurs.
  /// </summary>
  class ConversionException : Exceptions.CancelException
  {
    public ConversionException() { }
    public ConversionException(string message) : base(message) { }
    public ConversionException(string message, Exception inner) : base(message, inner)
    {
    }
  }
}
