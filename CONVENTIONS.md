# Development Conventions

- Do not create CRUD components for data types that are already handled by Rhino and Grasshopper ecosystem e.g. Do not create a component that extracts wall endpoints. Extract the base curve of the wall as a Rhino curve, and then use existing Grasshopper components to grab the curve end points
- [Use Microsoft C# Coding Convention](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- **Node Naming**
    - Use `(Construct)` and `(Deconstruct)` for API related data types that do not need factory methods to be created
    - Use `Create` to create and `Analyze` to decompose elements that live in Revit document
- Units must match between Rhino and Revit in GH analysis. No unit conversion should be necessary on Grasshopper definition by user
- Components and Parameters must be available on all Revit versions. For incompatible versions show an error or warning notifying user that the component is incompatible, outputs might be invalid, or the component fails to do the work it is supposed to. Be graceful when failing.
- When new API is implemented in Revit, upgrade the existing Grasshopper components only if it does not break the component. Create new ones if they do. If whole functionality
- Keep component names simple