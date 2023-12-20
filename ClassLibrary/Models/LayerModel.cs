using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class LayerModel
{
    public string LayerName { get; set; } = "";
    public Color LayerColor { get; set; } = Color.FromColorIndex(ColorMethod.ByAci, 0);
    public bool IsLayerPlottable { get; set; } = true;
    public LineWeight LayerLineWeight { get; set; } = LineWeight.ByLineWeightDefault;
    public Transparency LayerTransparency { get; set; } = new Transparency((byte)0);
}
