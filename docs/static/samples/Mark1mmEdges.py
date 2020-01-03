import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino

def MarkShortEdges():
    tol = 1
    mm = Rhino.UnitSystem.Millimeters
    units = sc.doc.ModelUnitSystem
    
    tol = Rhino.RhinoMath.UnitScale(mm, units)
   
    while True:
        
        if sc.sticky.has_key('EDGELENGTH_TOL'):
            tol = sc.sticky['EDGELENGTH_TOL']
            
        go = Rhino.Input.Custom.GetObject()
        
        opTol = Rhino.Input.Custom.OptionDouble(tol)
        go.AddOptionDouble("EdgeLength", opTol)
        go.AcceptNumber(True, False)
        res = go.Get()
        
        if ( go.CommandResult() != Rhino.Commands.Result.Success ):
            return
        if res==Rhino.Input.GetResult.Object:
            go.Object(0)
            break
            
        if res==Rhino.Input.GetResult.Option:
            tol = opTol.CurrentValue
            sc.sticky['EDGELENGTH_TOL'] = tol
            continue
        if res==Rhino.Input.GetResult.Number:
            tol = go.Number()
            sc.sticky['EDGELENGTH_TOL'] = tol
            continue

    sTol = str(round(tol, 4))

    brepId = rs.GetObject( filter = 8 + 16, preselect=True)
    if brepId is None: return
    
    brep = sc.doc.Objects.Find(brepId).Geometry
    edges = brep.Edges
    
    count = 0
    for edge in edges:
        l = edge.GetLength()
        if edge.GetLength() <= tol:
            if count == 0:
                grp = rs.AddGroup()
            temp = rs.AddTextDot("!!!", edge.PointAtStart)
            rs.AddObjectsToGroup(temp, grp)
            count += 1
    if count == 1:
        msg = " edge found at or below " +sTol + " in length."
    else:
        msg = " edges found at or below " +sTol + " in length."
        
    print str(count) + msg
    
if __name__ == "__main__":
    MarkShortEdges()