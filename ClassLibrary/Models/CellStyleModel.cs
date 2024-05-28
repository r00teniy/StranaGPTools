using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class CellStyleModel
{
    public int Row { get; set; } = 0;
    public int Collumn { get; set; } = 0;
    public CellAlignment Alignment { get; set; } = CellAlignment.MiddleCenter;
    public double RotationAngle { get; set; }
}
