namespace ClassLibrary.Models.Parking;
internal class InBuildingParkingBlockModel
{
    public string ParkingIsForBuildingName { get; private set; } = "";
    public string ParkingIsInBuilding { get; set; } = "";
    public int NumberOfParkingsLong { get; private set; } = 0;
    public int NumberOfParkingsGuest { get; private set; } = 0;
    public int NumberOfParkingsShort { get; private set; } = 0;
    public int NumberOfParkingsForDisabled { get; private set; } = 0;
    public int NumberOfParkingsForDisabledExtended { get; private set; } = 0;
    public int NumberOfParkingsForElecticCars { get; private set; } = 0;
    public int NumberOfParkingsTotal { get; private set; } = 0;
    public string PlotNumber { get; private set; } = "";
    public string ErrorMessage { get; private set; } = "";
    public InBuildingParkingBlockModel(Dictionary<string, string> attributes, string[] attributeNames, List<BuildingModel> buildings)
    {
        try
        {
            ParkingIsInBuilding = attributes[attributeNames[0]];
            ParkingIsForBuildingName = attributes[attributeNames[1]];
            NumberOfParkingsLong = Convert.ToInt32(attributes[attributeNames[2]]);
            NumberOfParkingsGuest = Convert.ToInt32(attributes[attributeNames[3]]);
            NumberOfParkingsShort = Convert.ToInt32(attributes[attributeNames[4]]);
            NumberOfParkingsForDisabled = Convert.ToInt32(attributes[attributeNames[5]]);
            NumberOfParkingsForDisabledExtended = Convert.ToInt32(attributes[attributeNames[6]]);
            NumberOfParkingsForElecticCars = Convert.ToInt32(attributes[attributeNames[7]]);
            NumberOfParkingsTotal = Convert.ToInt32(attributes[attributeNames[8]]);
            PlotNumber = buildings.Where(x => x.Name == ParkingIsInBuilding).First().PlotNumber;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
        }
    }
}
