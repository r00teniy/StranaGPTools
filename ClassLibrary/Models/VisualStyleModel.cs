

using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class VisualStyleModel
{
    public int FirstRow { get; set; } = 0;
    public int FirstCollumn { get; set; } = 0;
    public int SecondRow { get; set; } = 0;
    public int SecondCollumn { get; set; } = 0;
    public bool SetRightBorder { get; set; } = false;
    public bool SetBottomBorder { get; set; } = false;
    public LineWeight BorderLineWeight { get; set; } = LineWeight.LineWeight050;
    public short BackgrounColor { get; set; } = -1;
}
