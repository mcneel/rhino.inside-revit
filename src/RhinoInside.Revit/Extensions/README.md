# Extensions

This folder contains extension methods for classes not defined in this
assembly and classes that from a logical point of view belong to an
external namespace.

This code is for **internal** use only, if you are considering to expose
any class here DO move this class to a namespace under your control.

✔️ DO put extension methods in the same namespace as the extended type and
declare all _sponsor_ classes as `internal` in order to prevent conflicts
in case this assembly is referenced from another one.

❌ DO NOT mix types from this assembly in the extension methods declared
here. This folder is for adding functionality that the external assembly might
have but does not have.


#### See also
[Microsoft Extension Methods Design Gidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/extension-methods)
