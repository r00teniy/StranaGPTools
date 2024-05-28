using System.Data;
using System.Text.RegularExpressions;

using ClassLibrary.Models.Settings;

namespace ClassLibrary.Models.Parking;
internal class ParkingReqirementsModel
{
    public int TotalResidentialLongParking { get; set; }
    public int TotalCommercialShortParking { get; set; }
    public int TotalResidentialShortParking { get; set; }
    public ParkingReqirementsModel()
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
        return "Ok";
    }
    private string ReplaceDataInFormula(string formula, string[] dataToReplace, Dictionary<string, string> dataToReplaceWith)
    {
        var output = formula;
        for (var i = 0; i < dataToReplace.Length; i++)
        {
            if (dataToReplaceWith.ContainsKey(dataToReplace[i]))
            {
                output.Replace(dataToReplace[i], dataToReplaceWith[dataToReplace[i]]);
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
