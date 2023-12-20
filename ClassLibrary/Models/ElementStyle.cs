using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class ElementStyle
{
    public string ElementLayer { get; set; } = "";
    public Color ElementColor { get; set; } = Color.FromColorIndex(ColorMethod.ByAci, 256);
    public LineWeight ElementLineWeight { get; set; } = LineWeight.ByLayer;
    public Transparency ElementTransparency { get; set; } = new Transparency(TransparencyMethod.ByLayer);
}
