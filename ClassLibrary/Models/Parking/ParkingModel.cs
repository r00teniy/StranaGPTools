using System.Data;
using System.Text.RegularExpressions;

using ClassLibrary.Models.Settings;

namespace ClassLibrary.Models.Parking;
internal class ParkingModel
{
    public string Name { get; set; } = "";
    public int TotalResidentialLongParking { get; set; }
    public int TotalCommercialShortParking { get; set; }
    public int TotalResidentialShortParking { get; set; }
    public int TotalParking { get { return TotalCommercialShortParking + TotalResidentialLongParking + TotalResidentialShortParking; } }
    public int TotalDisabledParking { get { return CommercialShortDisabled + ResidentialShortDisabled + ResidentialLongDisabled; } }
    public int TotalForElectricCarsParking { get { return ResidentialLongForElectricCars + CommercialShortForElectricCars + ResidentialShortForElectricCars; } }
    public int TotalDisabledBigParking { get { return CommercialShortDisabledBig + ResidentialShortDisabledBig + ResidentialLongDisabledBig; } }
    public int CommercialShortDisabled { get; set; }
    public int CommercialShortDisabledBig { get; set; }
    public int ResidentialShortDisabled { get; set; }
    public int ResidentialShortDisabledBig { get; set; }
    public int ResidentialLongDisabled { get; set; }
    public int ResidentialLongDisabledBig { get; set; }
    public int ResidentialLongForElectricCars { get; set; }
    public int ResidentialShortForElectricCars { get; set; }
    public int CommercialShortForElectricCars { get; set; }
    public ParkingModel(List<ParkingBlockModel> list, List<InBuildingParkingBlockModel> inBuildingList, string name, string plotNumber, bool InsidePlot)
    {
        Name = name;
        List<ParkingBlockModel> selectedList = [];
        List<InBuildingParkingBlockModel> selectedInBuilding = [];
        if (InsidePlot)
        {
            selectedList = list.Where(x => x.PlotNumber == plotNumber).ToList();
            selectedInBuilding = inBuildingList.Where(x => x.PlotNumber == plotNumber).ToList();
        }
        else
        {
            selectedList = list.Where(x => x.PlotNumber != plotNumber).ToList();
            selectedInBuilding = inBuildingList.Where(x => x.PlotNumber != plotNumber).ToList();
        }
        foreach (var item in selectedList)
        {
            switch (item.Type)
            {
                case ParkingType.Long:
                    TotalResidentialLongParking += item.NumberOfParkings;
                    if (item.IsForDisabled)
                    {
                        ResidentialLongDisabled += item.NumberOfParkings;
                    }
                    if (item.IsForDisabledExtended)
                    {
                        ResidentialLongDisabledBig += item.NumberOfParkings;
                    }
                    if (item.IsForElectricCars)
                    {
                        ResidentialLongForElectricCars += item.NumberOfParkings;
                    }
                    break;
                case ParkingType.Guest:
                    TotalResidentialShortParking += item.NumberOfParkings;
                    if (item.IsForDisabled)
                    {
                        ResidentialShortDisabled += item.NumberOfParkings;
                    }
                    if (item.IsForDisabledExtended)
                    {
                        ResidentialShortDisabledBig += item.NumberOfParkings;
                    }
                    if (item.IsForElectricCars)
                    {
                        ResidentialShortForElectricCars += item.NumberOfParkings;
                    }
                    break;
                case ParkingType.Short:
                    TotalCommercialShortParking += item.NumberOfParkings;
                    if (item.IsForDisabled)
                    {
                        CommercialShortDisabled += item.NumberOfParkings;
                    }
                    if (item.IsForDisabledExtended)
                    {
                        CommercialShortDisabledBig += item.NumberOfParkings;
                    }
                    if (item.IsForElectricCars)
                    {
                        CommercialShortForElectricCars += item.NumberOfParkings;
                    }
                    break;
                default:
                    break;
            }
        }
        foreach (var item in selectedInBuilding)
        {
            TotalResidentialLongParking += item.NumberOfParkingsLong;
            TotalResidentialShortParking += item.NumberOfParkingsGuest;
            TotalCommercialShortParking += item.NumberOfParkingsShort;
            ResidentialLongDisabledBig += item.NumberOfParkingsForDisabledExtended;
            ResidentialLongDisabled += item.NumberOfParkingsForDisabled;
            CommercialShortForElectricCars += item.NumberOfParkingsForElecticCars;
        }
    }
    public ParkingModel()
    {

    }
    public string CalculateParkingReqirementsForBuilding(BuildingModel building, ParkingSettings settings, CityModel city)
    {
        if (building.ParkingProvidedOnPlot != null)
        {
            building.Parameters.Add("Постоянных_на_участке_шт", building.ParkingProvidedOnPlot!.TotalResidentialLongParking.ToString());
            building.Parameters.Add("Гостевых_на_участке_шт", building.ParkingProvidedOnPlot!.TotalResidentialShortParking.ToString());
            building.Parameters.Add("Временных_на_участке_шт", building.ParkingProvidedOnPlot!.TotalCommercialShortParking.ToString());
            building.Parameters.Add("Всего_на_участке_шт", building.ParkingProvidedOnPlot!.TotalParking.ToString());
        }
        if (building.ParkingProvidedOutsidePlot != null)
        {
            building.Parameters.Add("Постоянных_за_участком_шт", building.ParkingProvidedOutsidePlot!.TotalResidentialLongParking.ToString());
            building.Parameters.Add("Гостевых_за_участком_шт", building.ParkingProvidedOutsidePlot!.TotalResidentialShortParking.ToString());
            building.Parameters.Add("Временных_за_участком_шт", building.ParkingProvidedOutsidePlot!.TotalCommercialShortParking.ToString());
            building.Parameters.Add("Всего_за_участком_шт", building.ParkingProvidedOutsidePlot!.TotalParking.ToString());
        }
        //MainBuilding
        if (building.BuildingBlockName == settings.BuildingBlockNames[0])
        {
            //Calculating additional paramaters
            building.Parameters.Add("Пл_Встроя_м2", building.BuiltInParameters.Sum(x => Convert.ToDouble(x.Parameters["п1_Пл_общая_м2"])).ToString());
            //Calculating parking
            var longFormula = ReplaceDataInFormula(city.LongResidentialParkingFormula, settings.TextToReplace, building.Parameters);
            var result = CalculateFormula(longFormula);
            if (result != null)
            {
                TotalResidentialLongParking = (Int32)Math.Ceiling((decimal)result);
                building.Parameters.Add("Постоянных_мм_шт", TotalResidentialLongParking.ToString());
            }
            var shortFormula = ReplaceDataInFormula(city.ShortResidentialParkingFormula, settings.TextToReplace, building.Parameters);
            result = CalculateFormula(shortFormula);
            if (result != null)
            {
                TotalResidentialShortParking = (Int32)Math.Ceiling((decimal)result);
                building.Parameters.Add("Гостевых_мм_шт", TotalResidentialLongParking.ToString());
            }
        }
        else if (building.BuildingBlockName != settings.BuildingBlockNames[1])
        {
            var id = Array.IndexOf(settings.BuildingBlockNames, building.BuildingBlockName);
            var formula = ReplaceDataInFormula(city.NonResidentialParkingFormulas[id - 2], settings.TextToReplace, building.Parameters);
            var result = CalculateFormula(formula);
            if (result != null)
            {
                TotalCommercialShortParking = (Int32)Math.Ceiling((decimal)result);
            }
        }


        //BuiltIns
        foreach (var item in building.BuiltInParameters)
        {
            var index = Array.IndexOf(settings.BuiltInBlockNames, item.BlockName);
            var formulaWithData = ReplaceDataInFormula(city.BuiltInParkingFormulas[index], settings.TextToReplace, item.Parameters);
            var result = CalculateFormula(formulaWithData);

            if (result != null)
            {
                TotalCommercialShortParking += Convert.ToInt32(Math.Ceiling((decimal)result));
            }
        }
        building.Parameters.Add("Временных_мм_шт", TotalCommercialShortParking.ToString());
        //Disabled
        var disabledFormula = ReplaceDataInFormula(city.DisabledResidentialShortFormula, settings.TextToReplace, building.Parameters);
        var disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            ResidentialShortDisabled = (Int32)Math.Ceiling((decimal)disabledResult);
        }

        disabledFormula = ReplaceDataInFormula(city.DisabledCommercialShortFormula, settings.TextToReplace, building.Parameters);
        disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            CommercialShortDisabled = (Int32)Math.Ceiling((decimal)disabledResult);
        }

        disabledFormula = ReplaceDataInFormula(city.DisabledResidentialLongFormula, settings.TextToReplace, building.Parameters);
        disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            ResidentialLongDisabled = (Int32)Math.Ceiling((decimal)disabledResult);
        }
        //Disabled Big
        disabledFormula = ReplaceDataInFormula(city.DisabledBigResidentialShortFormula, settings.TextToReplace, building.Parameters);
        disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            ResidentialShortDisabledBig = (Int32)Math.Ceiling((decimal)disabledResult);
        }

        disabledFormula = ReplaceDataInFormula(city.DisabledBigCommercialShortFormula, settings.TextToReplace, building.Parameters);
        disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            CommercialShortDisabledBig = (Int32)Math.Ceiling((decimal)disabledResult);
        }

        disabledFormula = ReplaceDataInFormula(city.DisabledBigResidentialLongFormula, settings.TextToReplace, building.Parameters);
        disabledResult = CalculateFormula(disabledFormula);

        if (disabledResult != null)
        {
            ResidentialLongDisabledBig = (Int32)Math.Ceiling((decimal)disabledResult);
        }

        //Electric
        var electricFormula = ReplaceDataInFormula(city.ElecticCommercialShortFormula, settings.TextToReplace, building.Parameters);
        var electricResult = CalculateFormula(electricFormula);

        if (electricResult != null)
        {
            CommercialShortForElectricCars = (Int32)Math.Ceiling((decimal)electricResult);
        }

        electricFormula = ReplaceDataInFormula(city.ElecticResidentialShortFormula, settings.TextToReplace, building.Parameters);
        electricResult = CalculateFormula(electricFormula);

        if (electricResult != null)
        {
            ResidentialShortForElectricCars = (Int32)Math.Ceiling((decimal)electricResult);
        }

        electricFormula = ReplaceDataInFormula(city.ElecticResidentialLongFormula, settings.TextToReplace, building.Parameters);
        electricResult = CalculateFormula(electricFormula);

        if (electricResult != null)
        {
            ResidentialLongForElectricCars = (Int32)Math.Ceiling((decimal)electricResult);
        }

        return "Ok";
    }
    private string ReplaceDataInFormula(string formula, string[] dataToReplace, Dictionary<string, string> dataToReplaceWith)
    {
        var output = formula;
        for (var i = 0; i < dataToReplace.Length; i++)
        {
            if (dataToReplaceWith.ContainsKey(dataToReplace[i]))
            {
                output = output.Replace(dataToReplace[i], dataToReplaceWith[dataToReplace[i]]);
            }
        }
        return output;
    }
    private decimal? CalculateFormula(string formula)
    {
        //Check if we removed all text from formula
        Regex checkForLetters = new Regex(@"а-я", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var err = checkForLetters.Match(formula).ToString();
        if (err != "")
        {
            return null;
        }
        DataTable table = new();
        table.Columns.Add("myExpression", string.Empty.GetType(), formula);
        DataRow row = table.NewRow();
        table.Rows.Add(row);
        return decimal.Parse((string)row["myExpression"]);
    }
}
