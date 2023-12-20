using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace ClassLibrary;
internal class UserInput
{
    private Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
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

}
