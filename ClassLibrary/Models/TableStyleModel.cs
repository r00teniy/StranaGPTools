namespace ClassLibrary.Models;
internal class TableStyleModel
{
    public string Layer { get; set; } = "0";
    public string Title { get; set; } = "";
    public string StyleName { get; set; } = "";
    public string TitleStyleName { get; set; } = "";
    public int HeaderRowsCount { get; set; } = 1;
    public List<string[]> HeaderCollumnNames { get; set; } = [];
    public TableRangeModel[] HeaderMergeRanges { get; set; } = [];
    public int[] HeaderRowsHeight { get; set; } = [];
    public string HeaderStyleName { get; set; } = "";
    public string DataStyleName { get; set; } = "";
    public int DataRowsHeight { get; set; } = 8;
    public TableRangeModel[] DataMergeRanges { get; set; } = [];
    public int[] CollumnWidths { get; set; } = [];
    public int[] BlockCollumnsInData { get; set; } = [];
    public double[] BlocksScale { get; set; } = [];
}
