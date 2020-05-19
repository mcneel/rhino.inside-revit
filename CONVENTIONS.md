# Development Conventions

- Use [Microsoft C# Coding Convention](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions).

## User Interface
It's a goal to achieve a one to one correspondance betwen what the user can read in Revit UI and in Rhino.Inside Revit UI.

- **Try to adhere as close as possible to Revit terminology.**
- Try to classify information using same grouping strategies Revit user is already familiar. *For Example use Disciplines, Categories, Families, etc…*

### Grasshopper:

- Do not create CRUD components for data types that are already handled by Rhino and Grasshopper ecosystem e.g. Do not create a component that extracts wall endpoints. Extract the base curve of the wall as a Rhino curve, and then use existing Grasshopper components to grab the curve end points.
- Implement type conversions when the conversion is not ambiguous. *For Example 'Wall' to 'Curve', 'Level' to 'Plane', 'Grid' to 'Curve'.*
- **Grasshopper operates in Rhino model units**. No unit conversion should be necessary on Grasshopper definition by the user, so all necesary conversions should be done in the component. This means all components should always output values, like length or geometry values, in Rhino model units. This way all components can expect Rhino model units as input. This includes radians for angles.
- **Components should have same set of parameters in all Revit versions**, emulating a default behaviour when the feature is not available on a specific version. *For Example slanted angle on a wall in versions prior to 2021 should return 0° instead of Null*.
- Components that are wrapping features completly missing on a specific Revit version should not be available at all unless correct handling is implemented. *For Example 'Add Topography (Mesh)' component is unavailable before Revit 2020*.
  - Component Exposure should be 'Hidden'.
  - The component shoud behave as if it was 'Disabled' but showing an error message like *'This component needs Revit 2020 or newer to run'*.

#### Parameters naming:
- **Use singular form for nouns**. *For Example 'Wall' instead of 'Walls'.*
- Use `Type Name` for floating parameters. *For example 'Wall', 'Level', 'Element Type'.*
  
#### Components naming:
- **Use singular form for nouns** instead of plural form as your first choice. For example 'Deconstruct Brep' instead of 'Deconstruct Breps'.
- Use `{Action} {Noun}` as the prefered form. For example 'Divide Curve' instead of 'Curve Divide'.
  - Use Type Name as Noun when there is a clear primary one.
  - Use `Construct {Type Name}` to components that construct an object from its constituen parts.
  - Use `Deconstruct {Type Name}` to components that deconstruct an object into its constituen parts.
- Use `{Type Name} {Property Name}` when the component partially deconstruct an object like a property accessor. For example 'Curve Domain' or 'Element Geometry'.
- Use `Add {Type Name}` for components that add a new Element to a Revit model. For example 'Add Level'.
- Component Parameters
  - Use singular when the parameter access is `item`, plural when is `list` or `tree`.
  - Use the type name as the first choice, For example 'Color' in 'Add Material'.
  - **If it is reflecting a Revit element parameter use the exact same name Revit uses in English.**
