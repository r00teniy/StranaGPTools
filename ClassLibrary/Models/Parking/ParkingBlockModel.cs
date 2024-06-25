namespace ClassLibrary.Models.Parking;
internal class ParkingBlockModel
{
    public int NumberOfParkings { get; private set; } = 0;
    public string ParkingIsForBuildingName { get; private set; } = "";
    public ParkingType Type { get; private set; } = ParkingType.Long;
    public string Size { get; private set; } = "2.5х5.3";
    public bool IsForDisabled { get; private set; } = false;
    public bool IsForDisabledExtended { get; private set; } = false;
    public bool IsForElectricCars { get; private set; } = false;
    public string IsOnRoofOfBuilding { get; set; } = "";
    public string PlotNumber { get; private set; } = "";
    public string ErrorMessage { get; private set; } = "";
    public ParkingBlockModel(Dictionary<string, string> attributeData, string[] attributeNames, string[] pakingTypeNamesInBlocks, string[] attrib, string plotNumber)
    {
        try
        {
            NumberOfParkings = Convert.ToInt32(attrib[1]);
        }
        catch
        {
            ErrorMessage = $"В количестве парковок недопустимые символы ({attrib[1]}).";
        }
        ParkingIsForBuildingName = attrib[0];
        string type = attributeData[attributeNames[1]];
        if (type == pakingTypeNamesInBlocks[0])
        {
            Type = ParkingType.Long;
        }
        else if (type == pakingTypeNamesInBlocks[1])
        {
            Type = ParkingType.Short;
        }
        else if (type == pakingTypeNamesInBlocks[2])
        {
            Type = ParkingType.Guest;
        }
        else
        {
            ErrorMessage = $"Тип парковки {attributeData[attributeNames[2]]} не соответствует заложенным типам.";
        }
        Size = attributeData[attributeNames[0]];
        IsForDisabled = attributeData[attributeNames[2]] == "1";
        IsForDisabledExtended = attributeData[attributeNames[3]] == "1";
        PlotNumber = plotNumber;
        IsOnRoofOfBuilding = attributeData[attributeNames[5]];
        if (attributeData.ContainsKey(attributeNames[4]))
        {
            IsForElectricCars = attributeData[attributeNames[4]] == "1";
        }
    }
}
