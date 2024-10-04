using System.Text.RegularExpressions;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ClassLibrary.Models;
using ClassLibrary.Models.Parking;
using ClassLibrary.Models.Settings;

namespace ClassLibrary;
public class ParkingCalculations
{
    private DataImportFromAutocad? _dataImport;
    private DataExportToAutocad? _dataExport;
    private WorkWithBlocks? _workWithBlocks;
    private UserInput _userInput;
    private readonly ParkingSettings _settings;
    private readonly CityModel _city;
    private readonly WorkWithPolygons _workWithPolygons;

    public ParkingCalculations(ParkingSettings settings, CityModel city)
    {
        _settings = settings;
        _city = city;
        _workWithPolygons = new();
        _userInput = new UserInput();
    }

    private List<PlotBorderModel> Plots { get; set; } = [];
    private List<ZoneBorderModel> Stages { get; set; } = [];
    private List<BuildingModel> Buildings { get; set; } = [];
    private List<InBuildingParkingBlockModel> InBuildingParkingBlocks { get; set; } = [];
    private List<ParkingBlockModel> ParkingBlocks { get; set; } = [];
    private List<string> BuildingNames { get; set; } = [];
    private List<string> BuildingNamesForTable { get; set; } = [];
    private List<string> PlotNumbers { get; set; } = [];

    private string GetAllPlots()
    {
        try
        {
            var plotPolylines = _dataImport!.GetAllElementsOfTypeOnLayer<Polyline>(_settings.PlotsBorderLayer, false, "", true);
            if (plotPolylines.Count == 0)
            {
                return "Не найдены границы участков";
            }
            var plotBorders = plotPolylines.GroupBy(x => x.Layer);
            foreach (var plotBorder in plotBorders)
            {
                List<Polyline> pLines = [.. plotBorder];
                MPolygon region;
                try
                {
                    region = _workWithPolygons.CreateMPolygonFromPolylines(pLines);
                }
                catch (Exception e)
                {
                    return $"Пробема с границами участка, необходимо проверить их на замкнутость и самопересечение " + e.Message;
                }
                Plots.Add(new PlotBorderModel(_settings.PlotsBorderLayer, pLines, region));
            }
        }
        catch (Exception e)
        {
            return "Ошибка при считывании участков " + e.Message;
        }
        return "Ok";
    }
    private string GetAllStages()
    {
        try
        {
            var polylines = _dataImport!.GetAllElementsOfTypeOnLayer<Polyline>(_settings.StageBorderLayer, false, "", true);
            if (polylines.Count == 0)
            {
                return "No stage borders found";
            }
            var borders = polylines.GroupBy(x => x.Layer);
            foreach (var border in borders)
            {
                List<Polyline> pLines = [.. border];
                var region = _workWithPolygons.CreateMPolygonFromPolylines(pLines);
                Stages.Add(new ZoneBorderModel(_settings.StageBorderLayer, pLines, region));
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return "Ok";
    }
    private string GetAllPakingBlocks()
    {
        try
        {
            var blocks = _dataImport!.GetAllElementsOfTypeInDrawing<BlockReference>("", true);
            List<BlockReference> brList = [];
            List<string[]> attList = [];
            foreach (var br in blocks)
            {
                if (br.AttributeCollection.Count == 3)
                {
                    try
                    {
                        ObjectId oi = br.AttributeCollection[1];
                        var attRef = _dataImport.GetObjectOfTypeTByObjectId<AttributeReference>(oi);
                        ObjectId oi2 = br.AttributeCollection[0];
                        var attRef2 = _dataImport.GetObjectOfTypeTByObjectId<AttributeReference>(oi2);
                        if (attRef != null && attRef.Tag == _settings.ParkingBlockAttributeNames[0] && attRef2 != null && attRef2.Tag == _settings.ParkingBlockAttributeNames[1])
                        {
                            brList.Add(br);
                            attList.Add([attRef.TextString, attRef2.TextString]);
                        }
                    }
                    catch (Exception e)
                    {
                        return $"Проблема при считывании аттрибутов блока парковки с именем {br.Name} " + e.Message;
                    }
                }
            }
            var dynBlockPropValues = _workWithBlocks!.GetAllParametersFromBlockReferences(brList);
            (var plotNumbers, var result) = _workWithPolygons.GetPlotNumbersFromBlocks(brList, Plots);
            if (plotNumbers == null || result == null || result != "Ok")
            {
                return result ?? "Error with plot numbers";
            }
            var errors = 0;
            for (int i = 0; i < dynBlockPropValues.Count; i++)
            {
                try
                {
                    ParkingBlocks.Add(new ParkingBlockModel(dynBlockPropValues[i], _settings.ParkingBlockPararmArray, _settings.PakingTypeNamesInBlocks, attList[i], plotNumbers[i]));
                }
                catch (Exception)
                {
                    errors++;
                }
            }
            if (errors > 0)
            {
                return $"У {errors} блоков парковок нехватает требуемых параметров";
            }
            if (ParkingBlocks.Count == 0)
            {
                return "Не найдены блоки парковок";
            }
            return "Ok";
        }
        catch (Exception e)
        {
            return "Ошибка при обработке блоков существующей парковки" + e.Message;
        }
    }
    private string GetAllBuildings()
    {
        var allblocks = _dataImport!.GetAllElementsOfTypeOnLayer<BlockReference>(_settings.BuildingBlocksLayer, true);
        var blocks = allblocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).StartsWith(_settings.KGPBlocksPrefix));
        var buildingBlocks = blocks.Where(x => !(x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).Contains(_settings.BuiltInBlocksContain) && (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) != _settings.InBuildingParkingBlockName).ToList();
        var additionalBlocks = blocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).Contains(_settings.BuiltInBlocksContain) && (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) != _settings.InBuildingParkingBlockName).ToList();
        var parkingBlocks = blocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) == _settings.InBuildingParkingBlockName).ToList();
        var result = GetBuildings(buildingBlocks);
        if (result != "Ok")
        {
            return result;
        }
        result = GetBuiltIns(additionalBlocks);
        if (result != "Ok")
        {
            return result;
        }
        result = GetAllInBuildingParkingBlocks(parkingBlocks);
        if (result != "Ok")
        {
            return result;
        }
        result = GetAdditionalDataFromDrawing(_city.Name);
        if (result != "Ok")
        {
            return result;
        }
        return "Ok";
    }
    private string GetBuildings(List<BlockReference> blocks)
    {
        var (plotNumbers, result) = _workWithPolygons.GetPlotNumbersFromBlocks(blocks, Plots);
        if (result == null || result != "Ok")
        {
            return result ?? "Неопознанная ошибка при определении номеров участка";
        }
        var attrBuildings = _workWithBlocks!.GetAllParametersFromBlockReferences(blocks);
        for (var i = 0; i < blocks.Count; i++)
        {
            var name = blocks[i].IsDynamicBlock ? ((BlockTableRecord)blocks[i].DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name : blocks[i].Name;
            var id = Array.IndexOf(_settings.BuildingBlockNames, name);
            if (id == -1)
            {
                return $"Не распознан блок с именем {name}, возможно он лишний";
            }
            BuildingType type = id == 0 ? BuildingType.Residential : id == 1 ? BuildingType.Parking : BuildingType.Other;
            try
            {
                BuildingModel model = new(attrBuildings[i], _settings.BuildingAttributeNames, blocks[i].Position, type, plotNumbers![i], name);
                Buildings.Add(model);
            }
            catch (Exception)
            {
                return $"Проблема при считывании параметров дома {name}, проверьте данные";
            }
        }
        var positions = Buildings.Select(x => x.Name);
        foreach (var item in positions)
        {
            if (positions.Where(x => x == item).Count() > 1)
                return $"Найдено больше одного здания с позицией {item}, необходимо оставить только одно";
        }

        Regex pattern = new(@"\d+");
        Buildings = Buildings.OrderBy(x => pattern.Match(x.Name).Value).ToList();
        return "Ok";
    }
    private string GetBuiltIns(List<BlockReference> blocks)
    {
        var attrBuildings = _workWithBlocks!.GetAllParametersFromBlockReferences(blocks);
        for (var i = 0; i < blocks.Count; i++)
        {
            if (!attrBuildings[i].ContainsKey(_settings.KGPBlockPositionParameterName))
                return "В одном из блоков встроя нет параметра п0_Поз";
            var blockName = blocks[i].IsDynamicBlock ? ((BlockTableRecord)blocks[i].DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name : blocks[i].Name;
            BuildingModel model;
            string name = attrBuildings[i][_settings.KGPBlockPositionParameterName];
            try
            {
                model = Buildings.Where(x => x.Name == name).First();
            }
            catch (Exception)
            {
                return $"Не найдено здания для встроя {name}";
            }
            if (model == null)
                return $"Ошибка, не найден дом {name}, на который ссылается встрой";

            if (blockName != _settings.BuiltInParkingBlockName)
            {
                model.BuiltInParameters.Add(new BuiltInParameters(blockName, attrBuildings[i]));
            }
            else
            {
                model.BuiltInParking = new BuiltInParkingModel(attrBuildings[i]);
            }
        }
        return "Ok";
    }
    private string GetAllInBuildingParkingBlocks(List<BlockReference> blocks)
    {
        try
        {
            var attrBuildings = _workWithBlocks!.GetAllParametersFromBlockReferences(blocks);
            /*Using plots of building itself, so we don't care about plots of blocks
            (List<string>? plotnumbers, string result) = _workWithPolygons.GetPlotNumbersFromBlocks(blocks, Plots);
            if (result == null || plotnumbers == null || result != "Ok")
            {
                return result ?? "Error while assigning plots to blocks";
            }*/
            for (var i = 0; i < attrBuildings.Count; i++)
            {
                InBuildingParkingBlocks.Add(new InBuildingParkingBlockModel(attrBuildings[i], _settings.InBuildingParkingAttributes, Buildings));
            }
            return "Ok";
        }
        catch (Exception e)
        {

            return e.Message;
        }
    }
    private string CalculateExistingParkingByBuilding()
    {
        foreach (var item in Buildings)
        {
            try
            {
                item.FillInProvidedParking(ParkingBlocks.Where(x => x.ParkingIsForBuildingName == item.Name).ToList(), InBuildingParkingBlocks.Where(x => x.ParkingIsForBuildingName == item.Name).ToList());
            }
            catch (Exception e)
            {
                return $"Проблема при подсчете существующихпарковок для здания {item.Name} " + e.Message;
            }
        }

        return "Ok";
    }
    private (List<string[]>?, string) CalculateExistingParkingDataForTable()
    {
        if (ParkingBlocks.Count == 0)
        {
            return (null, "There are no parking blocks in drawing");
        }
        if (Buildings.Count == 0)
        {
            return (null, "There are no buildings blocks in drawing");
        }

        // Lines for each plot
        try
        {
            (var output, string result) = CalculateExistingParkingDataForPlots();
            if (result != "Ok" || output == null)
            {
                return (null, result == "Ok" ? "Неизвестная ошибка при подсчёте существующих парковок по участкам" : result);
            }
            (var lines, result) = CalculateExistingParkingDataTotals();
            if (result != "Ok")
            {
                return (null, result);
            }
            output.AddRange(lines);

            return (output, "Ok");
        }
        catch (Exception e)
        {
            return (null, "Проблема при создании строк существующих парковок" + e.Message);
        }
    }
    private (List<string[]>?, string) CalculateExistingParkingDataTotals()
    {
        //Totals
        List<string[]> output = [];
        string[] totalLine = new string[BuildingNamesForTable.Count * 6 + 9];
        //Checking if there are parkings outside
        var parkingOutside = Buildings.Where(x => x.ParkingProvidedOutsidePlot!.TotalParking != 0).Count() > 0;
        if (parkingOutside)
        {
            totalLine[0] = "Размещено";
            totalLine[1] = "на участке ГПЗУ";
        }
        else
        {
            totalLine[0] = "Размещено";
            totalLine[1] = "";
        }

        int totalCounter = 2;
        foreach (var name in BuildingNamesForTable)
        {
            var building = Buildings.Where(x => x.Name == name).First();
            var onPlotParking = building.ParkingProvidedOnPlot;
            if (onPlotParking != null)
            {
                totalLine[totalCounter] = onPlotParking.TotalResidentialLongParking.ToString();
                totalLine[totalCounter + 1] = onPlotParking.TotalResidentialShortParking.ToString();
                totalLine[totalCounter + 2] = onPlotParking.TotalCommercialShortParking.ToString();
                totalLine[totalCounter + 3] = onPlotParking.TotalDisabledParking.ToString();
                totalLine[totalCounter + 4] = onPlotParking.TotalDisabledBigParking.ToString();
                totalLine[totalCounter + 5] = onPlotParking.TotalForElectricCarsParking.ToString();
                totalCounter += 6;
            }
            else
            {
                return (null, $"For building {name} there was no on plot parking created");
            }
        }
        totalLine[totalCounter] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalResidentialLongParking).ToString();
        totalLine[totalCounter + 1] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalResidentialShortParking).ToString();
        totalLine[totalCounter + 2] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalCommercialShortParking).ToString();
        totalLine[totalCounter + 3] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalDisabledParking).ToString();
        totalLine[totalCounter + 4] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalDisabledBigParking).ToString();
        totalLine[totalCounter + 5] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalForElectricCarsParking).ToString();
        totalLine[totalCounter + 6] = Buildings.Sum(x => x.ParkingProvidedOnPlot!.TotalParking).ToString();
        output.Add(totalLine);
        if (!parkingOutside)
        {
            return (output, "Ok");
        }
        totalLine = new string[BuildingNamesForTable.Count * 6 + 9];
        totalLine[1] = "за участком ГПЗУ";
        totalCounter = 2;
        foreach (var name in BuildingNamesForTable)
        {
            var building = Buildings.Where(x => x.Name == name).First();
            var outsidePlotParking = building.ParkingProvidedOutsidePlot;
            if (outsidePlotParking != null)
            {
                totalLine[totalCounter] = outsidePlotParking.TotalResidentialLongParking.ToString();
                totalLine[totalCounter + 1] = outsidePlotParking.TotalResidentialShortParking.ToString();
                totalLine[totalCounter + 2] = outsidePlotParking.TotalCommercialShortParking.ToString();
                totalLine[totalCounter + 3] = outsidePlotParking.TotalDisabledParking.ToString();
                totalLine[totalCounter + 4] = outsidePlotParking.TotalDisabledBigParking.ToString();
                totalLine[totalCounter + 5] = outsidePlotParking.TotalForElectricCarsParking.ToString();
                totalCounter += 6;
            }
            else
            {
                return (null, $"For building {name} there was no outside plot parking created");
            }
        }
        totalLine[totalCounter] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalResidentialLongParking).ToString();
        totalLine[totalCounter + 1] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalResidentialShortParking).ToString();
        totalLine[totalCounter + 2] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalCommercialShortParking).ToString();
        totalLine[totalCounter + 3] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalDisabledParking).ToString();
        totalLine[totalCounter + 4] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalDisabledBigParking).ToString();
        totalLine[totalCounter + 5] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalForElectricCarsParking).ToString();
        totalLine[totalCounter + 6] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot!.TotalParking).ToString();
        output.Add(totalLine);
        totalLine = new string[BuildingNamesForTable.Count * 6 + 9];
        totalLine[1] = "Итого";
        var numberOflines = output.Count;
        for (int i = 2; i < BuildingNamesForTable.Count * 6 + 9; i++)
        {
            totalLine[i] = (Convert.ToInt32(output[numberOflines - 1][i]) + Convert.ToInt32(output[numberOflines - 2][i])).ToString();
        }
        output.Add(totalLine);
        return (output, "Ok");
    }
    private (List<string[]>?, string) CalculateExistingParkingDataForPlots()
    {
        //Numbers on plots
        List<string[]> output = [];
        foreach (var plot in PlotNumbers)
        {
            string[] line = new string[BuildingNamesForTable.Count * 6 + 9];
            var buildingsOnCurrentPlot = Buildings.Where(x => x.PlotNumber == plot);
            BuildingModel? buildingOnCurrentPlot;
            if (buildingsOnCurrentPlot.Count() == 1)
            {
                buildingOnCurrentPlot = buildingsOnCurrentPlot.First();
            }
            else
            {
                List<BuildingModel> parkingList = [];
                foreach (var item in buildingsOnCurrentPlot)
                {
                    if (item.BuildingType == BuildingType.Parking)
                    {
                        parkingList.Add(item);
                    }
                    else
                    {
                        if (item.BuiltInParking != null)
                        {
                            parkingList.Add(item);
                        }
                    }
                }
                if (parkingList.Count == 1)
                {
                    buildingOnCurrentPlot = parkingList[0];
                }
                else
                {
                    buildingOnCurrentPlot = null;
                }
            }

            var blocksOnPlot = ParkingBlocks.Where(x => x.PlotNumber == plot && x.IsOnRoofOfBuilding == _settings.NotOnBuildingRoofText);

            var inBuildingParking = InBuildingParkingBlocks.Where(x => x.PlotNumber == plot);
            line[0] = plot;
            if (inBuildingParking.Count() > 0)
            {
                line[1] = "Открытые парковки";
            }
            else
            {
                line[1] = "";
            }
            int counter = 2;
            foreach (var name in BuildingNamesForTable)
            {
                var blocksForBuilding = blocksOnPlot.Where(x => x.ParkingIsForBuildingName == name);
                line[counter] = blocksForBuilding.Where(x => x.Type == ParkingType.Long).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                line[counter + 1] = blocksForBuilding.Where(x => x.Type == ParkingType.Guest).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                line[counter + 2] = blocksForBuilding.Where(x => x.Type == ParkingType.Short).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                line[counter + 3] = blocksForBuilding.Where(x => x.IsForDisabled == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                line[counter + 4] = blocksForBuilding.Where(x => x.IsForDisabledExtended == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                line[counter + 5] = blocksForBuilding.Where(x => x.IsForElectricCars == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                counter += 6;
            }
            line[counter] = blocksOnPlot.Where(x => x.Type == ParkingType.Long).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 1] = blocksOnPlot.Where(x => x.Type == ParkingType.Guest).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 2] = blocksOnPlot.Where(x => x.Type == ParkingType.Short).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 3] = blocksOnPlot.Where(x => x.IsForDisabled == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 4] = blocksOnPlot.Where(x => x.IsForDisabledExtended == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 5] = blocksOnPlot.Where(x => x.IsForElectricCars == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            line[counter + 6] = blocksOnPlot.Select(x => x.NumberOfParkings).Sum(x => x).ToString();
            output.Add(line);

            if (inBuildingParking.Count() > 0)
            {
                var parkingBuildings = inBuildingParking.Select(x => x.ParkingIsInBuilding).Distinct();
                foreach (var item in parkingBuildings)
                {
                    var building = Buildings.Where(x => x.Name == item).First();

                    //Check for missing building
                    if (building == null)
                    {
                        return (null, $"Не найден паркинг {item}, которой прописан в блоке парковок");
                    }
                    string[] secondLine = new string[BuildingNamesForTable.Count * 6 + 9];
                    secondLine[0] = "";
                    if (building.BuildingType == BuildingType.Parking)
                    {
                        secondLine[1] = "Паркинг " + building.Name + $"({building.Parameters["п1_Кол_машиномест_всего_шт"]} м/мест)";
                    }
                    else
                    {
                        if (building.BuiltInParking == null)
                        {
                            return (null, $"У здания {item} не найден встроенный паркинг, хотя это здание прописано в блоке парковок и не является паркингом");
                        }
                        else
                        {
                            secondLine[1] = "Паркинг " + building.Name + $"({building.BuiltInParking.TotalParkingSpaces} м/мест)";
                        }
                    }
                    int newCounter = 2;

                    foreach (var name in BuildingNamesForTable)
                    {
                        var blocksForBuilding = inBuildingParking.Where(x => x.ParkingIsForBuildingName == name && x.ParkingIsInBuilding == item);
                        secondLine[newCounter] = blocksForBuilding.Select(x => x.NumberOfParkingsLong).Sum(x => x).ToString();
                        secondLine[newCounter + 1] = blocksForBuilding.Select(x => x.NumberOfParkingsGuest).Sum(x => x).ToString();
                        secondLine[newCounter + 2] = blocksForBuilding.Select(x => x.NumberOfParkingsShort).Sum(x => x).ToString();
                        secondLine[newCounter + 3] = blocksForBuilding.Select(x => x.NumberOfParkingsForDisabled).Sum(x => x).ToString();
                        secondLine[newCounter + 4] = blocksForBuilding.Select(x => x.NumberOfParkingsForDisabledExtended).Sum(x => x).ToString();
                        secondLine[newCounter + 5] = blocksForBuilding.Select(x => x.NumberOfParkingsForElecticCars).Sum(x => x).ToString();
                        newCounter += 6;
                    }
                    var totalInBuildingParking = inBuildingParking.Where(x => x.ParkingIsInBuilding == item);
                    secondLine[newCounter] = totalInBuildingParking.Select(x => x.NumberOfParkingsLong).Sum(x => x).ToString();
                    secondLine[newCounter + 1] = totalInBuildingParking.Select(x => x.NumberOfParkingsGuest).Sum(x => x).ToString();
                    secondLine[newCounter + 2] = totalInBuildingParking.Select(x => x.NumberOfParkingsShort).Sum(x => x).ToString();
                    secondLine[newCounter + 3] = totalInBuildingParking.Select(x => x.NumberOfParkingsForDisabled).Sum(x => x).ToString();
                    secondLine[newCounter + 4] = totalInBuildingParking.Select(x => x.NumberOfParkingsForDisabledExtended).Sum(x => x).ToString();
                    secondLine[newCounter + 5] = totalInBuildingParking.Select(x => x.NumberOfParkingsForElecticCars).Sum(x => x).ToString();
                    secondLine[newCounter + 6] = totalInBuildingParking.Select(x => x.NumberOfParkingsTotal).Sum(x => x).ToString();
                    output.Add(secondLine);
                }
            }
            var blocksOnRoof = ParkingBlocks.Where(x => x.PlotNumber == plot && x.IsOnRoofOfBuilding != _settings.NotOnBuildingRoofText);
            if (blocksOnRoof.Count() > 0)
            {
                var buildingNames = blocksOnRoof.Select(x => x.IsOnRoofOfBuilding).Distinct();
                //Create lines for them
                foreach (var item in buildingNames)
                {
                    var blocksOnRoofOfBuilding = blocksOnRoof.Where(x => x.IsOnRoofOfBuilding == item);
                    var building = Buildings.Where(x => x.Name == item).First();

                    //Check for missing building
                    if (building == null)
                    {
                        return (null, $"Не найдено здание {item}, кровля которого указана в блоке парковок");
                    }
                    string[] newLine = new string[BuildingNamesForTable.Count * 6 + 9];
                    newLine[0] = "";
                    newLine[1] = building.BuildingType == BuildingType.Parking ? "На кровле паркинга " + building.Name : "На кровле здания " + building.Name;
                    counter = 2;
                    foreach (var name in BuildingNamesForTable)
                    {
                        var blocksForBuilding = blocksOnRoofOfBuilding.Where(x => x.ParkingIsForBuildingName == name);
                        newLine[counter] = blocksForBuilding.Where(x => x.Type == ParkingType.Long).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        newLine[counter + 1] = blocksForBuilding.Where(x => x.Type == ParkingType.Guest).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        newLine[counter + 2] = blocksForBuilding.Where(x => x.Type == ParkingType.Short).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        newLine[counter + 3] = blocksForBuilding.Where(x => x.IsForDisabled == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        newLine[counter + 4] = blocksForBuilding.Where(x => x.IsForDisabledExtended == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        newLine[counter + 5] = blocksForBuilding.Where(x => x.IsForElectricCars == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                        counter += 6;
                    }
                    newLine[counter] = blocksOnRoofOfBuilding.Where(x => x.Type == ParkingType.Long).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 1] = blocksOnRoofOfBuilding.Where(x => x.Type == ParkingType.Guest).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 2] = blocksOnRoofOfBuilding.Where(x => x.Type == ParkingType.Short).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 3] = blocksOnRoofOfBuilding.Where(x => x.IsForDisabled == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 4] = blocksOnRoofOfBuilding.Where(x => x.IsForDisabledExtended == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 5] = blocksOnRoofOfBuilding.Where(x => x.IsForElectricCars == true).Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    newLine[counter + 6] = blocksOnRoofOfBuilding.Select(x => x.NumberOfParkings).Sum(x => x).ToString();
                    output.Add(newLine);
                }
            }
        }
        return (output, "Ok");
    }
    private (string[]?, string) CalculateRequiredParkingDataForTable()
    {
        string[] line = new string[BuildingNamesForTable.Count * 6 + 9];
        try
        {
            foreach (var building in Buildings)
            {
                var result = building.ParkingReqirements.CalculateParkingReqirementsForBuilding(building, _settings, _city);
                if (result != "Ok")
                {
                    return (null, result);
                }
            }
        }
        catch (Exception e)
        {
            return (null, "Проблема при создании строки с требуемыми местами для таблицы: " + e.Message);
        }
        //Filling data for the table
        line[0] = "Требуется";
        line[1] = "";
        int counter = 2;
        foreach (var name in BuildingNamesForTable)
        {
            var building = Buildings.Where(x => x.Name == name).First();
            line[counter] = building.ParkingReqirements.TotalResidentialLongParking.ToString();
            line[counter + 1] = building.ParkingReqirements.TotalResidentialShortParking.ToString();
            line[counter + 2] = building.ParkingReqirements.TotalCommercialShortParking.ToString();
            line[counter + 3] = building.ParkingReqirements.TotalDisabledParking.ToString();
            line[counter + 4] = building.ParkingReqirements.TotalDisabledBigParking.ToString();
            line[counter + 5] = building.ParkingReqirements.TotalForElectricCarsParking.ToString();
            counter += 6;
        }
        line[counter] = Buildings.Select(x => x.ParkingReqirements.TotalResidentialLongParking).Sum().ToString();
        line[counter + 1] = Buildings.Select(x => x.ParkingReqirements.TotalResidentialShortParking).Sum().ToString();
        line[counter + 2] = Buildings.Select(x => x.ParkingReqirements.TotalCommercialShortParking).Sum().ToString();
        line[counter + 3] = Buildings.Select(x => x.ParkingReqirements.TotalDisabledParking).Sum().ToString();
        line[counter + 4] = Buildings.Select(x => x.ParkingReqirements.TotalDisabledBigParking).Sum().ToString();
        line[counter + 5] = Buildings.Select(x => x.ParkingReqirements.TotalForElectricCarsParking).Sum().ToString();
        line[counter + 6] = Buildings.Select(x => x.ParkingReqirements.TotalParking).Sum().ToString();

        return (line, "Ok");
    }
    private string FindAllBuildingNamesAndPlots()
    {
        try
        {
            var buildings = Buildings.Select(x => x.Name).Distinct();

            var buildingsFromParkingModels = ParkingBlocks.Select(x => x.ParkingIsForBuildingName).Distinct();

            string errorbuildings = "";
            foreach (var building in buildingsFromParkingModels)
            {
                if (!buildings.Contains(building))
                    errorbuildings += " " + building;
            }
            if (errorbuildings != "")
                return $"Найдены парковки для несуществующих зданий:{errorbuildings}";
            BuildingNames.AddRange(buildings.Distinct().OrderBy(x => x).ToList());
            PlotNumbers.AddRange(Plots.Select(x => x.PlotNumber).Distinct().OrderBy(x => x).ToList());
            foreach (var building in Buildings)
            {
                if (building.BuildingType == BuildingType.Parking && building.BuiltInParameters.Count == 0)
                {
                    continue;
                }
                BuildingNamesForTable.Add(building.Name);
            }
        }
        catch (Exception e)
        {
            return "Проблема при составлении списка зданий и участков: " + e.Message;
        }
        if (PlotNumbers.Count == 0)
        {
            return "Не найдены участки";
        }
        if (BuildingNames.Count == 0)
        {
            return "Не найдены здания";
        }
        return "Ok";
    }
    private string GetAdditionalDataFromDrawing(string cityName)
    {
        if (cityName.Contains("Москва"))
        {
            (var result, var blockRef) = _dataImport.GetSingularBlockReferenceByBlockName("Баллы для  МСК");
            if (result != "Ok")
            {
                return result;
            }
            (var result2, var blockRef2) = _dataImport.GetSingularBlockReferenceByBlockName("Коэфф МСК");
            if (result2 != "Ok")
            {
                return result2;
            }
            var attributes = _workWithBlocks!.GetAllAttributesFromBlockReferences([blockRef!, blockRef2!]);
            try
            {
                for (var i = 0; i < Buildings.Count; i++)
                {
                    Buildings[i].Parameters.Add("ПРОЦЕНТ", attributes[0]["ПРОЦЕНТ"]);
                    Buildings[i].Parameters.Add("КООФ_УРБАН", attributes[1]["КООФ_УРБАН"]);
                    Buildings[i].Parameters.Add("КООФ_ГПТ", attributes[1]["КООФ_ГПТ"]);
                    for (var j = 0; j < Buildings[i].BuiltInParameters.Count; j++)
                    {
                        Buildings[i].BuiltInParameters[j].Parameters.Add("КООФ_УРБАН", attributes[1]["КООФ_УРБАН"]);
                        Buildings[i].BuiltInParameters[j].Parameters.Add("КООФ_ГПТ", attributes[1]["КООФ_ГПТ"]);
                    }
                }
            }
            catch (Exception e)
            {
                return "В блоке параметров Москвы не найдены нужные параметры " + e.Message;
            }
        }
        return "Ok";
    }
    public string CreateParkingTable()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        using (DocumentLock acLckDoc = doc.LockDocument())
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                _dataExport = new(tr);
                _dataImport = new(tr);
                _workWithBlocks = new(tr);

                string result = GetAllPlots();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while getting plots";
                }
                /*result = GetAllStages();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while getting stages";
                }*/
                result = GetAllBuildings();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while getting buildings";
                }
                result = GetAllPakingBlocks();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while calculating blocks";
                }
                result = FindAllBuildingNamesAndPlots();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while finding plot numbers and building names";
                }
                result = CalculateExistingParkingByBuilding();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while sorting parking block by Buildings";
                }
                result = CheckParkingBlocksForErrors();
                if (result == null || result != "Ok")
                {
                    return result ?? "Unkonwn error";
                }
                (var linesForTable, result) = CalculateExistingParkingDataForTable();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while calculating existing parking data";
                }
                (var requiredline, result) = CalculateRequiredParkingDataForTable();
                if (result == null || result != "Ok")
                {
                    return result ?? "Errors while calculating required parking data";
                }
                linesForTable!.Add(requiredline!);
                //Calculating deficit/proficit
                var deficitLine = new string[requiredline!.Length];
                var proficitLine = new string[requiredline!.Length];
                deficitLine[0] = "Дефицит";
                proficitLine[0] = "Профицит";
                for (int i = 2; i < requiredline!.Length; i++)
                {
                    var difference = Convert.ToInt32(linesForTable[linesForTable.Count - 1][i]) - Convert.ToInt32(linesForTable[linesForTable.Count - 2][i]);
                    if (difference > 0)
                    {
                        deficitLine[i] = difference.ToString();
                        proficitLine[i] = "0";
                    }
                    else
                    {
                        deficitLine[i] = "0";
                        proficitLine[i] = (difference * -1).ToString();
                    }
                }

                if (deficitLine.Skip(2).Where(x => x != "0").Count() > 0)
                {
                    linesForTable.Add(deficitLine);
                }
                if (proficitLine.Skip(2).Where(x => x != "0").Count() > 0)
                {
                    linesForTable.Add(proficitLine);
                }
                TableStyleModel style;
                try
                {
                    style = CreateTableStyleForParkingTable(linesForTable);
                }
                catch (Exception e)
                {
                    return "Проблема при созаднии оформления таблицы" + e.Message;
                }

                var point = _userInput.GetInsertionPoint();

                if (point != null)
                {
                    _dataExport.CreateTableInDrawing((Point3d)point, linesForTable, new List<double[]>(), style);
                }
                else
                {
                    return "Проблема при получении точки вставки";
                }

                tr.Commit();
                return "Ok";
            }
        }
    }

    private string CheckParkingBlocksForErrors()
    {
        //Check buildings that have InBuildingParking blocks refer to
        var buildNames = InBuildingParkingBlocks.Select(x => x.ParkingIsForBuildingName).Distinct();
        foreach (var buildName in buildNames)
        {
            if (!BuildingNames.Contains(buildName))
            {
                return $"Не найден блок здания {buildName}, на которое ссылается блок парковок для паркинга";
            }
        }
        //Check buildings where OnBuildingParking blocks claim to be
        buildNames = InBuildingParkingBlocks.Select(x => x.ParkingIsInBuilding).Distinct();
        foreach (var buildName in buildNames)
        {
            if (!BuildingNames.Contains(buildName))
            {
                return $"Не найден блок здания {buildName}, в котором должен быть расположен блок парковок для паркинга";
            }
        }
        //Checking normal parking blocks for buildings they refer to
        buildNames = ParkingBlocks.Select(x => x.ParkingIsForBuildingName).Distinct();
        foreach (var buildName in buildNames)
        {
            if (!BuildingNames.Contains(buildName))
            {
                return $"Не найден блок здания {buildName}, на которое ссылаются обычные блоки парковок";
            }
        }
        var parkingBlocksToCheck = ParkingBlocks.Where(x => x.IsOnRoofOfBuilding != _settings.NotOnBuildingRoofText).Distinct();
        foreach (var buildName in parkingBlocksToCheck)
        {
            if (!BuildingNames.Contains(buildName.IsOnRoofOfBuilding))
            {
                return $"Не найден блок здания {buildName.IsOnRoofOfBuilding}, на крышу которого ссылаются обычные блоки парковок";
            }
            if (buildName.PlotNumber != Buildings.Where(x => x.Name == buildName.IsOnRoofOfBuilding).First().PlotNumber)
            {
                return $"Блок парковки на кровле {buildName.IsOnRoofOfBuilding} находится на другом участке, нежели само здание";
            }
        }
        //Check if inbuilding parking blocks use less that parking capacity is
        var parkingsByParkingBuilding = InBuildingParkingBlocks.GroupBy(x => x.ParkingIsInBuilding);
        foreach (var item in parkingsByParkingBuilding)
        {
            //getting capacity numbers 
            var building = Buildings.Where(x => x.Name == item.Key).First();
            var totalCapacity = building.BuildingType == BuildingType.Parking ? Convert.ToInt32(building.Parameters[_settings.InBuildingParkingAttributes[8]]) : building.BuiltInParking!.TotalParkingSpaces;
            var totalDisabled = building.BuildingType == BuildingType.Parking ? Convert.ToInt32(building.Parameters[_settings.InBuildingParkingAttributes[5]]) : building.BuiltInParking!.TotalDisabledParkingSpaces;
            var totalDisabledBig = building.BuildingType == BuildingType.Parking ? Convert.ToInt32(building.Parameters[_settings.InBuildingParkingAttributes[6]]) : building.BuiltInParking!.TotalDisabledBigParkingSpaces;
            //getting used numbers
            var usedCapacity = item.Select(x => x.NumberOfParkingsTotal).Sum();
            var usedDisabled = item.Select(x => x.NumberOfParkingsForDisabled).Sum();
            var usedDisabledBig = item.Select(x => x.NumberOfParkingsForDisabledExtended).Sum();
            if (usedCapacity > totalCapacity)
            {
                return $"В паркинге {building.Name} размещено {usedCapacity} м/мест, хотя вместимость паринга {totalCapacity} м/мест, необходимо исправить";
            }
            if (usedDisabled > totalDisabled)
            {
                return $"В паркинге {building.Name} размещено {usedDisabled} м/мест для инвалидов, хотя в паринге предусмотрено только {totalDisabled} м/мест для инвалидов, необходимо исправить";
            }
            if (usedDisabledBig > totalDisabledBig)
            {
                return $"В паркинге {building.Name} размещено {usedDisabledBig} расширенных м/мест для инвалидов, хотя в паринге предусмотрено только {totalDisabledBig} расширенных м/мест для инвалидов, необходимо исправить";
            }
        }
        return "Ok";
    }

    private TableStyleModel CreateTableStyleForParkingTable(List<string[]> linesForTable)
    {
        TableStyleModel style = new();
        style.Layer = "71_Парк._Ведомости";
        style.StyleName = "ГП Таблица паркомест";
        style.TitleStyleName = "Название";
        style.Title = "Сводная таблица автостоянок на площадке";
        style.HeaderStyleName = "Заголовок";
        style.DataStyleName = "Данные";
        style.HeaderRowsCount = 3;
        style.HeaderRowsHeight = [8, 8, 30];
        var header = new List<TableRangeModel>();
        var cells = new List<CellStyleModel>();
        var visuals = new List<VisualStyleModel>();
        style.CollumnWidths = new int[linesForTable[0].Length];
        style.CollumnWidths[0] = 10;
        style.CollumnWidths[1] = 20;
        for (int i = 2; i < linesForTable[0].Length; i++)
        {
            style.CollumnWidths[i] = 6;
        }
        var lastRow = linesForTable.Count + 3;
        var lastCollumn = linesForTable[0].Length - 1;
        var firstLine = new string[linesForTable[0].Length];

        firstLine[0] = "Позиция";
        var range2 = new TableRangeModel()
        {
            FirstCollumn = 0,
            FirstRow = 1,
            SecondCollumn = 1,
            SecondRow = 1
        };
        header.Add(range2);
        for (var i = 0; i < BuildingNamesForTable.Count; i++)
        {
            firstLine[2 + i * 6] = BuildingNamesForTable[i];
            var range = new TableRangeModel()
            {
                FirstCollumn = 2 + i * 6,
                FirstRow = 1,
                SecondCollumn = 7 + i * 6,
                SecondRow = 1
            };

            header.Add(range);
        }
        firstLine[firstLine.Length - 7] = "По всем домам";
        range2 = new TableRangeModel()
        {
            FirstCollumn = firstLine.Length - 7,
            FirstRow = 1,
            SecondCollumn = firstLine.Length - 2,
            SecondRow = 1
        };
        header.Add(range2);
        firstLine[firstLine.Length - 1] = "Итого";
        range2 = new TableRangeModel()
        {
            FirstCollumn = firstLine.Length - 1,
            FirstRow = 1,
            SecondCollumn = firstLine.Length - 1,
            SecondRow = 3,
            RotationAngle = Math.PI / 2
        };
        header.Add(range2);
        var body = new List<TableRangeModel>();
        range2 = new TableRangeModel()
        {
            FirstCollumn = 0,
            FirstRow = linesForTable.Count + 3,
            SecondCollumn = 1,
            SecondRow = linesForTable.Count + 3
        };
        body.Add(range2);
        var secondLine = new string[linesForTable[0].Length];
        secondLine[0] = "Номер участка";
        range2 = new TableRangeModel()
        {
            FirstCollumn = 0,
            FirstRow = 2,
            SecondCollumn = 1,
            SecondRow = 3
        };
        header.Add(range2);
        for (var i = 0; i < BuildingNamesForTable.Count + 1; i++)
        {
            secondLine[2 + i * 6] = "Постоянные";
            var range = new TableRangeModel()
            {
                FirstCollumn = 2 + i * 6,
                FirstRow = 2,
                SecondCollumn = 2 + i * 6,
                SecondRow = 3,
                RotationAngle = Math.PI / 2
            };
            header.Add(range);
            secondLine[3 + i * 6] = "Гостевые";
            range = new TableRangeModel()
            {
                FirstCollumn = 3 + i * 6,
                FirstRow = 2,
                SecondCollumn = 3 + i * 6,
                SecondRow = 3,
                RotationAngle = Math.PI / 2
            };
            header.Add(range);
            secondLine[4 + i * 6] = "Временные";
            range = new TableRangeModel()
            {
                FirstCollumn = 4 + i * 6,
                FirstRow = 2,
                SecondCollumn = 4 + i * 6,
                SecondRow = 3,
                RotationAngle = Math.PI / 2
            };
            header.Add(range);
            secondLine[5 + i * 6] = "в т.ч. для";
            range = new TableRangeModel()
            {
                FirstCollumn = 5 + i * 6,
                FirstRow = 2,
                SecondCollumn = 7 + i * 6,
                SecondRow = 2
            };
            header.Add(range);
        }
        var thirdLine = new string[linesForTable[0].Length];
        for (var i = 0; i < BuildingNamesForTable.Count + 1; i++)
        {
            thirdLine[5 + i * 6] = "МГН всего";
            cells.Add(new CellStyleModel()
            {
                Collumn = 5 + i * 6,
                Row = 3,
                RotationAngle = Math.PI / 2
            });
            thirdLine[6 + i * 6] = "МГН уширенных";
            cells.Add(new CellStyleModel()
            {
                Collumn = 6 + i * 6,
                Row = 3,
                RotationAngle = Math.PI / 2
            });
            thirdLine[7 + i * 6] = "электромобилей";
            cells.Add(new CellStyleModel()
            {
                Collumn = 7 + i * 6,
                Row = 3,
                RotationAngle = Math.PI / 2
            });
        }
        //Merging required cells
        for (int i = 0; i < linesForTable.Count; i++)
        {
            if (linesForTable[i][0] != null && linesForTable[i][0] != "")
            {
                if (linesForTable[i] != null && (linesForTable[i][1] == null || linesForTable[i][1] == ""))
                {
                    body.Add(new TableRangeModel()
                    {
                        FirstCollumn = 0,
                        FirstRow = i + 4,
                        SecondCollumn = 1,
                        SecondRow = i + 4
                    }
                    );
                    visuals.Add(new()
                    {
                        FirstCollumn = 0,
                        FirstRow = i + 4,
                        SecondCollumn = lastCollumn,
                        SecondRow = i + 4,
                        SetBottomBorder = true
                    });
                }
                else if (i != linesForTable.Count - 1 && (linesForTable[i + 1][0] == null || linesForTable[i + 1][0] == ""))
                {
                    var numberOfRowsToMerge = 1;
                    var j = 0;
                    while (linesForTable[i + 2 + j][0] == null || linesForTable[i + 2 + j][0] == "")
                    {
                        numberOfRowsToMerge++;
                        j++;
                    }

                    body.Add(new TableRangeModel()
                    {
                        FirstCollumn = 0,
                        FirstRow = i + 4,
                        SecondCollumn = 0,
                        SecondRow = i + 4 + numberOfRowsToMerge,
                        RotationAngle = Math.PI / 2
                    }
                    );
                    visuals.Add(new()
                    {
                        FirstCollumn = 0,
                        FirstRow = i + 4 + numberOfRowsToMerge,
                        SecondCollumn = lastCollumn,
                        SecondRow = i + 4 + numberOfRowsToMerge,
                        SetBottomBorder = true
                    });
                }
            }
        }
        //Visual stuff

        visuals.Add(new VisualStyleModel()
        {
            FirstCollumn = 1,
            FirstRow = 0,
            SecondCollumn = 1,
            SecondRow = lastRow,
            SetRightBorder = true
        });
        visuals.Add(new VisualStyleModel()
        {
            FirstCollumn = lastCollumn - 1,
            FirstRow = 0,
            SecondCollumn = lastCollumn - 1,
            SecondRow = lastRow,
            SetRightBorder = true
        });
        var currentCollumn = 2;
        foreach (var building in BuildingNamesForTable)
        {
            Regex pattern = new(@"\d+");
            int buildingNumber = Convert.ToInt32(pattern.Match(building).Value);
            visuals.Add(new VisualStyleModel()
            {
                FirstCollumn = currentCollumn,
                FirstRow = 0,
                SecondCollumn = currentCollumn + 5,
                SecondRow = lastRow,
                BackgrounColor = _settings.ParkingTableColors[buildingNumber]
            });
            visuals.Add(new VisualStyleModel()
            {
                FirstCollumn = currentCollumn + 5,
                FirstRow = 0,
                SecondCollumn = currentCollumn + 5,
                SecondRow = lastRow,
                SetRightBorder = true
            });
            currentCollumn += 6;
        }
        visuals.Add(new VisualStyleModel()
        {
            FirstCollumn = 0,
            FirstRow = 1,
            SecondCollumn = lastCollumn,
            SecondRow = 1,
            SetBottomBorder = true
        });

        style.VisualStyleModel = visuals.ToArray();
        style.HeaderCollumnNames = [firstLine, secondLine, thirdLine];
        style.HeaderMergeRanges = header.ToArray();
        style.DataMergeRanges = body.ToArray();
        style.CellStyleModels = cells.ToArray();
        return style;
    }
}
