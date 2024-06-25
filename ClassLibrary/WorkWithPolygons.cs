using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using ClassLibrary.Models;

namespace ClassLibrary;
internal class WorkWithPolygons
{
    public double tolerance = 0.01;
    public MPolygon CreateMPolygonFromPolylines(List<Polyline> polylines)
    {
        var mPolygon = new MPolygon();
        for (var i = 0; i < polylines.Count; i++)
        {
            if (polylines[i].Elevation != 0)
                polylines[i].Elevation = 0;
        }
        ObjectIdCollection polylineIds = new(polylines.Select(x => x.ObjectId).ToArray());
        mPolygon.CreateLoopsFromBoundaries(polylineIds, true, Tolerance.Global.EqualPoint);
        return mPolygon;
    }
    public (List<string>?, string) GetPlotNumbersFromBlocks(List<BlockReference> blocks, List<PlotBorderModel> plots)
    {
        List<string> result = [];
        foreach (var block in blocks)
        {
            List<PointContainment> results = [];
            for (int i = 0; i < plots.Count; i++)
            {
                results.Add(GetPointContainment(plots[i].Region, block.Position));
            }
            var numberOfHits = results.Where(x => x == PointContainment.Inside).Count();
            if (numberOfHits > 1)
            {
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.SetImpliedSelection(new ObjectId[1] { block.ObjectId });
                ed.SelectImplied();
                return (null, "Блок находится в границах более одного участка");
            }
            else if (numberOfHits == 0)
            {
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.SetImpliedSelection(new ObjectId[1] { block.ObjectId });
                ed.SelectImplied();
                return (null, "Блок находится вне участков");
            }
            else
                result.Add(plots[results.IndexOf(PointContainment.Inside)].PlotNumber);
        }
        return (result, "Ok");
    }
    private PointContainment GetPointContainment(MPolygon mPolygon, Point3d point)
    {
        if (mPolygon.NumMPolygonLoops > 1)
        {
            for (var i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                if (mPolygon.IsPointOnLoopBoundary(point, i, tolerance))
                {
                    return PointContainment.OnBoundary;
                }
            }
        }
        else
        {
            if (mPolygon.IsPointOnLoopBoundary(point, 0, tolerance))
            {
                return PointContainment.OnBoundary;
            }
        }
        var inside = PointContainment.Outside;
        if (mPolygon.IsPointInsideMPolygon(point, Tolerance.Global.EqualPoint).Count > 0)
        {
            if (mPolygon.NumMPolygonLoops <= 1)
                inside = PointContainment.Inside;
            else
            {
                int inslooop = 0;
                for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
                {
                    using (MPolygon mp = new MPolygon())
                    {
                        mp.AppendMPolygonLoop(mPolygon.GetMPolygonLoopAt(i), false, tolerance);
                        if (mp.IsPointInsideMPolygon(point, tolerance).Count > 0) inslooop++;
                    }
                }
                if (inslooop % 2 > 0)
                    inside = PointContainment.Inside;
            }
        }
        return inside;

    }
}
