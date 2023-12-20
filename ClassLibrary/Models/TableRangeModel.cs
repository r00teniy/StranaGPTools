using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class TableRangeModel
{
    public int FirstRow { get; set; } = 0;
    public int FirstCollumn { get; set; } = 0;
    public int SecondRow { get; set; } = 0;
    public int SecondCollumn { get; set; } = 0;
    public CellAlignment Alignment { get; set; } = CellAlignment.MiddleCenter;
    public TableRangeModel()
    {

    }
}
