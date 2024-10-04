namespace ClassLibrary.Models.Parking;
internal class BuiltInParkingModel
{
    public double FloorArea { get; set; }
    public int TotalParkingSpaces { get; set; }
    public int TotalDisabledParkingSpaces { get; set; }
    public int TotalDisabledBigParkingSpaces { get; set; }
    public BuiltInParkingModel(Dictionary<string, string> parameters)
    {
        FloorArea = Convert.ToDouble(parameters["п1_Пл_Общая_м2"]);
        TotalParkingSpaces = Convert.ToInt32(parameters["п1_Кол_машиномест_всего_шт"]);
        TotalDisabledParkingSpaces = Convert.ToInt32(parameters["п1_Кол_машиномест_МГН_шт"]);
        TotalDisabledBigParkingSpaces = Convert.ToInt32(parameters["п1_Кол_машиномест_МГН_больших_шт"]);
    }
}
