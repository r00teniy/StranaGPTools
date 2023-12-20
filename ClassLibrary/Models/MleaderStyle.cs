
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class MleaderStyle
{
    public string MLeaderLayer { get; set; } = "";
    public Color MLeaderColor { get; set; } = Color.FromColorIndex(ColorMethod.ByAci, 256);
    public LineWeight MLeaderLineWeight { get; set; } = LineWeight.ByLayer;
    public string MLeaderStyle { get; set; } = "";
    public string BlockName { get; set; } = "";
    public string[] BlockAttributes { get; set; } = new string[0];

}
