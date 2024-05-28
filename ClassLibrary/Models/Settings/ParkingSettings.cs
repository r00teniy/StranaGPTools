using System.Xml.Serialization;

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
    public string[] ParkingTypes { get; set; } = [];
    public short[] ParkingTableColors { get; set; } = [];
    public string ApartmensLayer { get; set; } = "";
    public string NonresidentialLayer { get; set; } = "";
    public string SocialLayer { get; set; } = "";
    public string[] PakingTypeNamesInBlocks { get; set; } = [];
    public string[] BuildingAttributeNames { get; set; } = [];
    public string[] BuildingBlockNames { get; set; } = [];
    public string[] SeparateBuildingAttributeNames { get; set; } = [];
    public string[] ParkAttributeNames { get; set; } = [];
    public string[] InBuildingParkingAttributes { get; set; } = [];
    public string[] BuiltInBlockNames { get; set; } = [];

    [XmlArrayItem("ParameterArray")]
    public List<string[]> BuiltInParameterNames { get; set; } = [];
    public string[] TextToReplace { get; set; } = [];
}
