namespace ClassLibrary.Models.Settings;
[Serializable]
public class ParkingSettings
{
    public string StageBorderLayer { get; set; } = "";
    public string PlotsBorderLayer { get; set; } = "";
    public string BuildingBlocksLayer { get; set; } = "";
    public string ParkTableStyleName { get; set; } = "";
    //Parameters_of_dyn_blocks
    public string[] ParkingBlockPararmArray { get; set; } = [];
    //Arrays with data for table
    public short[] ParkingTableColors { get; set; } = [];
    public string[] PakingTypeNamesInBlocks { get; set; } = [];
    public string[] BuildingAttributeNames { get; set; } = [];
    public string[] BuildingBlockNames { get; set; } = [];
    public string[] ParkAttributeNames { get; set; } = [];
    public string[] InBuildingParkingAttributes { get; set; } = [];
    public string[] BuiltInBlockNames { get; set; } = [];
    public string[] TextToReplace { get; set; } = [];
    public string ParkingBlocksLayer { get; set; } = "";
    public string BuiltInParkingBlockName { get; set; } = "";
}
