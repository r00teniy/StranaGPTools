using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace ClassLibrary;
internal class UserInput
{
    private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    internal ObjectId? GetObjectIdOfEntity<T>(string type) where T : Entity
    {
        var options = new PromptEntityOptions($"\nВыберите {type}: ");
        options.SetRejectMessage($"\nВы выбрали не {type}");
        options.AddAllowedClass(typeof(T), true);
        var result = ed.GetEntity(options);
        if (result.Status == PromptStatus.OK)
        {
            return result.ObjectId;
        }
        return null;
    }
    internal Point3d? GetInsertionPoint()
    {
        PromptPointOptions pPtOpts = new("\nВыберете точку положения таблицы: ");
        var result = ed.GetPoint(pPtOpts);
        if (result.Status == PromptStatus.OK)
        {
            return result.Value;
        }
        return null;
    }
    internal (Point3d, double) GetInsertionPointAndRotation()
    {
        PromptPointOptions pPtOpts = new("\nВыберете точку для вставки: ");
        var pt = ed.GetPoint(pPtOpts).Value;
        var ucsMat = ed.CurrentUserCoordinateSystem;
        var ucs = ucsMat.CoordinateSystem3d;
        var zdir = ucsMat.CoordinateSystem3d.Zaxis;
        var ocsMat = MakeOcs(zdir);
        var ptCorrect = pt.TransformBy(ucsMat.PreMultiplyBy(ocsMat));
        var ocsXdir = ocsMat.CoordinateSystem3d.Xaxis;
        double rot = ocsXdir.GetAngleTo(ucs.Xaxis, zdir);
        return (ptCorrect, rot);
    }
    private Matrix3d MakeOcs(Vector3d zdir)
    {
        double d = 1.0 / 64.0;
        zdir = zdir.GetNormal();
        var xdir = Math.Abs(zdir.X) < d && Math.Abs(zdir.Y) < d ?
            Vector3d.YAxis.CrossProduct(zdir).GetNormal() :
            Vector3d.ZAxis.CrossProduct(zdir).GetNormal();
        var ydir = zdir.CrossProduct(xdir).GetNormal();
        return new Matrix3d([xdir.X, xdir.Y, xdir.Z, 0.0, ydir.X, ydir.Y, ydir.Z, 0.0, zdir.X, zdir.Y, zdir.Z, 0.0, 0.0, 0.0, 0.0, 1.0]);
    }
}
