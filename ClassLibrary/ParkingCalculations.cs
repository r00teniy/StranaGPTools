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
    private List<string> PlotNumbers { get; set; } = [];

    private string GetAllPlots()
    {
        try
        {
            var plotPolylines = _dataImport.GetAllElementsOfTypeOnLayer<Polyline>(_settings.PlotsBorderLayer, false, "", true);
            if (plotPolylines.Count == 0)
            {
                return "No plot borders found";
            }
            var plotBorders = plotPolylines.GroupBy(x => x.Layer);
            foreach (var plotBorder in plotBorders)
            {
                List<Polyline> pLines = [.. plotBorder];
                var region = _workWithPolygons.CreateMPolygonFromPolylines(pLines);
                Plots.Add(new PlotBorderModel(_settings.PlotsBorderLayer, pLines, region));
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return "Ok";
    }
    private string GetAllStages()
    {
        try
        {
            var polylines = _dataImport.GetAllElementsOfTypeOnLayer<Polyline>(_settings.StageBorderLayer, false, "", true);
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
                if (br.AttributeCollection.Count == 4)
                {
                    ObjectId oi = br.AttributeCollection[1];
                    var attRef = _dataImport.GetObjectOfTypeTByObjectId<AttributeReference>(oi);
                    ObjectId oi2 = br.AttributeCollection[0];
                    var attRef2 = _dataImport.GetObjectOfTypeTByObjectId<AttributeReference>(oi2);
                    if (attRef != null && attRef.Tag == "Этап" && attRef2 != null && attRef2.Tag == "КОЛ-ВО")
                    {
                        brList.Add(br);
                        attList.Add([attRef.TextString, attRef2.TextString]);
                    }
                }
            }
            var dynBlockPropValues = _workWithBlocks.GetAllParametersFromBlockReferences(brList);
            (var plotNumbers, var result) = _workWithPolygons.GetPlotNumbersFromBlocks(brList, Plots);
            if (plotNumbers == null || result == null || result != "Ok")
            {
                return result ?? "Error with plot numbers";
            }
            for (int i = 0; i < dynBlockPropValues.Count; i++)
            {
                ParkingBlocks.Add(new ParkingBlockModel(dynBlockPropValues[i], _settings.ParkingBlockPararmArray, _settings.PakingTypeNamesInBlocks, attList[i], plotNumbers[i]));
            }
            if (ParkingBlocks.Count == 0)
            {
                return "No parking blocks Found";
            }
            return "Ok";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
    private string GetAllBuildings()
    {

        var allblocks = _dataImport.GetAllElementsOfTypeOnLayer<BlockReference>(_settings.BuildingBlocksLayer, true);
        var blocks = allblocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).StartsWith("КГП"));
        var buildingBlocks = blocks.Where(x => !(x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).Contains("Встрой") && (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) != "КГП_Места_в_паркинге").ToList();
        var additionalBlocks = blocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name).Contains("Встрой") && (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) != "КГП_Места_в_паркинге").ToList();
        var parkingBlocks = blocks.Where(x => (x.IsDynamicBlock ? ((BlockTableRecord)x.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
    x.Name) == "КГП_Места_в_паркинге").ToList();
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
        return "Ok";
    }
    private string GetBuildings(List<BlockReference> blocks)
    {
        var (plotNumbers, result) = _workWithPolygons.GetPlotNumbersFromBlocks(blocks, Plots);
        if (result == null || result != "Ok")
        {
            return result ?? "Error with finding plot numbers of buildings";
        }
        var attrBuildings = _workWithBlocks.GetAllParametersFromBlockReferences(blocks);
        for (var i = 0; i < blocks.Count; i++)
        {
            var name = blocks[i].IsDynamicBlock ? ((BlockTableRecord)blocks[i].DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name : blocks[i].Name;
            try
            {
                BuildingModel model = new(attrBuildings[i], _settings.BuildingAttributeNames, blocks[i].Position, BuildingType.Residential, plotNumbers[i], name);
                Buildings.Add(model);
            }
            catch (Exception)
            {
                return $"Проблема при считывании параметров дома {name}, проверьте данные";
            }
        }
        Regex pattern = new(@"\d+");
        Buildings = Buildings.OrderBy(x => pattern.Match(x.Name).Value).ToList();
        return "Ok";
    }
    private string GetBuiltIns(List<BlockReference> blocks)
    {
        var attrBuildings = _workWithBlocks.GetAllParametersFromBlockReferences(blocks);
        for (var i = 0; i < blocks.Count; i++)
        {
            if (!attrBuildings[i].ContainsKey("п0_Поз"))
                return "В одном из блоков встроя нет параметра п0_Поз";

            string name = attrBuildings[i]["п0_Поз"];
            BuildingModel model = Buildings.Where(x => x.Name == name).First();

            if (model == null)
                return $"Ошибка, не найден дом {name}, на который ссылается встрой";

            var blockName = blocks[i].IsDynamicBlock ? ((BlockTableRecord)blocks[i].DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name : blocks[i].Name;

            model.BuiltInParameters.Add(new BuiltInParameters(blockName, attrBuildings[i]));
        }
        return "Ok";
    }
    private string GetAllInBuildingParkingBlocks(List<BlockReference> blocks)
    {
        var attrBuildings = _workWithBlocks.GetAllParametersFromBlockReferences(blocks);
        (List<string>? plotnumbers, string result) = _workWithPolygons.GetPlotNumbersFromBlocks(blocks, Plots);
        if (result == null || plotnumbers == null || result != "Ok")
        {
            return result ?? "Error while assigning plots to blocks";
        }
        for (var i = 0; i < attrBuildings.Count; i++)
        {
            InBuildingParkingBlocks.Add(new InBuildingParkingBlockModel(attrBuildings[i], _settings.InBuildingParkingAttributes, plotnumbers[i]));
        }
        return "Ok";
    }
    private string CalculateExistingParkingByBuilding()
    {
        try
        {
            foreach (var item in Buildings)
            {
                item.FillInProvidedParking(ParkingBlocks.Where(x => x.ParkingIsForBuildingName == item.Name).ToList(), InBuildingParkingBlocks.Where(x => x.ParkingIsForBuildingName == item.Name).ToList());
            }
        }
        catch (Exception e)
        {
            return e.Message;
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
        List<string[]> output = [];
        // Lines for each plot
        try
        {
            output = CalculateExistingParkingDataForPlots();
            (var lines, var result) = CalculateExistingParkingDataTotals();
            if (result != "Ok")
            {
                return (null, result);
            }
            output.AddRange(lines);

            return (output, "Ok");
        }
        catch (Exception e)
        {
            return (null, e.Message);
        }
    }
    private (List<string[]>?, string) CalculateExistingParkingDataTotals()
    {
        //Totals
        List<string[]> output = [];
        string[] totalLine = new string[BuildingNames.Count * 6 + 9];
        //Checking if there are parkings outside
        var parkingOutside = Buildings.Where(x => x.ParkingProvidedOutsidePlot.TotalParking != 0).Count() > 0;
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
        foreach (var name in BuildingNames)
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
        totalLine[totalCounter] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalResidentialLongParking).ToString();
        totalLine[totalCounter + 1] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalResidentialShortParking).ToString();
        totalLine[totalCounter + 2] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalCommercialShortParking).ToString();
        totalLine[totalCounter + 3] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalDisabledParking).ToString();
        totalLine[totalCounter + 4] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalDisabledBigParking).ToString();
        totalLine[totalCounter + 5] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalForElectricCarsParking).ToString();
        totalLine[totalCounter + 6] = Buildings.Sum(x => x.ParkingProvidedOnPlot.TotalParking).ToString();
        output.Add(totalLine);
        if (!parkingOutside)
        {
            return (output, "Ok");
        }
        totalLine = new string[BuildingNames.Count * 6 + 9];
        totalLine[1] = "за участком ГПЗУ";
        totalCounter = 2;
        foreach (var name in BuildingNames)
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
        totalLine[totalCounter] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalResidentialLongParking).ToString();
        totalLine[totalCounter + 1] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalResidentialShortParking).ToString();
        totalLine[totalCounter + 2] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalCommercialShortParking).ToString();
        totalLine[totalCounter + 3] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalDisabledParking).ToString();
        totalLine[totalCounter + 4] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalDisabledBigParking).ToString();
        totalLine[totalCounter + 5] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalForElectricCarsParking).ToString();
        totalLine[totalCounter + 6] = Buildings.Sum(x => x.ParkingProvidedOutsidePlot.TotalParking).ToString();
        output.Add(totalLine);
        totalLine = new string[BuildingNames.Count * 6 + 9];
        totalLine[1] = "Итого";
        var numberOflines = output.Count;
        for (int i = 2; i < BuildingNames.Count * 6 + 9; i++)
        {
            totalLine[i] = (Convert.ToInt32(output[numberOflines - 1][i]) + Convert.ToInt32(output[numberOflines - 2][i])).ToString();
        }
        output.Add(totalLine);
        return (output, "Ok");
    }
    private List<string[]> CalculateExistingParkingDataForPlots()
    {
        //Numbers on plots
        List<string[]> output = [];
        foreach (var plot in PlotNumbers)
        {
            string[] line = new string[BuildingNames.Count * 6 + 9];
            var blocksOnPlot = ParkingBlocks.Where(x => x.PlotNumber == plot);

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
            foreach (var name in BuildingNames)
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
                string[] secondLine = new string[BuildingNames.Count * 6 + 9];
                secondLine[0] = "";
                secondLine[1] = "Паркинг";
                int newCounter = 2;
                foreach (var name in BuildingNames)
                {
                    var blocksForBuilding = inBuildingParking.Where(x => x.ParkingIsForBuildingName == name);
                    secondLine[newCounter] = blocksForBuilding.Select(x => x.NumberOfParkingsLong).Sum(x => x).ToString();
                    secondLine[newCounter + 1] = blocksForBuilding.Select(x => x.NumberOfParkingsGuest).Sum(x => x).ToString();
                    secondLine[newCounter + 2] = blocksForBuilding.Select(x => x.NumberOfParkingsShort).Sum(x => x).ToString();
                    secondLine[newCounter + 3] = blocksForBuilding.Select(x => x.NumberOfParkingsForDisabled).Sum(x => x).ToString();
                    secondLine[newCounter + 4] = blocksForBuilding.Select(x => x.NumberOfParkingsForDisabledExtended).Sum(x => x).ToString();
                    secondLine[newCounter + 5] = blocksForBuilding.Select(x => x.NumberOfParkingsForElecticCars).Sum(x => x).ToString();
                    newCounter += 6;
                }
                secondLine[newCounter] = inBuildingParking.Select(x => x.NumberOfParkingsLong).Sum(x => x).ToString();
                secondLine[newCounter + 1] = inBuildingParking.Select(x => x.NumberOfParkingsGuest).Sum(x => x).ToString();
                secondLine[newCounter + 2] = inBuildingParking.Select(x => x.NumberOfParkingsShort).Sum(x => x).ToString();
                secondLine[newCounter + 3] = inBuildingParking.Select(x => x.NumberOfParkingsForDisabled).Sum(x => x).ToString();
                secondLine[newCounter + 4] = inBuildingParking.Select(x => x.NumberOfParkingsForDisabledExtended).Sum(x => x).ToString();
                secondLine[newCounter + 5] = inBuildingParking.Select(x => x.NumberOfParkingsForElecticCars).Sum(x => x).ToString();
                secondLine[newCounter + 6] = inBuildingParking.Select(x => x.NumberOfParkingsTotal).Sum(x => x).ToString();
                output.Add(secondLine);
            }
        }

        return output;
    }
    private (string[]?, string) CalculateRequiredParkingDataForTable()
    {
        string[] line = new string[BuildingNames.Count * 6 + 9];
        try
        {
            foreach (var building in Buildings)
            {
                building.ParkingReqirements.CalculateParkingReqirementsForBuilding(building, _settings, _city);
            }
        }
        catch (Exception e)
        {
            return (null, e.Message);
        }
        //Filling data for the table
        line[0] = "Требуется";
        line[1] = "";
        int counter = 2;
        foreach (var name in BuildingNames)
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
            var buildingsFromBuildingModels = Buildings.Select(x => x.Name).Distinct();
            var buildingsFromParkingModels = ParkingBlocks.Select(x => x.ParkingIsForBuildingName).Distinct();
            var bulidings = buildingsFromBuildingModels.Concat(buildingsFromParkingModels);

            BuildingNames.AddRange(bulidings.Distinct().OrderBy(x => x).ToList());
            PlotNumbers.AddRange(Plots.Select(x => x.PlotNumber).Distinct().OrderBy(x => x).ToList());
        }
        catch (Exception e)
        {
            return e.Message;
        }
        if (PlotNumbers.Count == 0)
        {
            return "No plots found";
        }
        if (BuildingNames.Count == 0)
        {
            return "No buildings found";
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

                TableStyleModel style = CreateTableStyleForParkingTable(linesForTable);

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

    private TableStyleModel CreateTableStyleForParkingTable(List<string[]>? linesForTable)
    {
        TableStyleModel style = new();
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
        for (var i = 0; i < BuildingNames.Count; i++)
        {
            firstLine[2 + i * 6] = BuildingNames[i];
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
        for (var i = 0; i < BuildingNames.Count + 1; i++)
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
        for (var i = 0; i < BuildingNames.Count + 1; i++)
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
                    if (linesForTable[i + 2][0] == null || linesForTable[i + 2][0] == "")
                        numberOfRowsToMerge = 2;
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
        foreach (var building in Buildings)
        {
            Regex pattern = new(@"\d+");
            int buildingNumber = Convert.ToInt32(pattern.Match(building.Name).Value);
            visuals.Add(new VisualStyleModel()
            {
                FirstCollumn = currentCollumn,
                FirstRow = 0,
                SecondCollumn = currentCollumn + 5,
                SecondRow = lastRow,
                BackgrounColor = _settings.ParkingTableColors[buildingNumber - 1]
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
