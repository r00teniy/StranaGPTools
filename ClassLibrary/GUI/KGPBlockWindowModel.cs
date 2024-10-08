﻿
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using ClassLibrary.Models;
using ClassLibrary.Models.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassLibrary.GUI;
public partial class KGPBlockWindowModel : ObservableObject
{
    public string[] CityNames { get; set; } = [];
    [ObservableProperty]
    public int _selectedCityId = -1;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InsertBlockClickCommand))]
    public int _selectedBlockId;
    private string[] _allBlocks = [];
    [ObservableProperty]
    public List<string> _blocks = [];
    public string[] Types { get; set; } = ["Здания", "Встрой"];
    [ObservableProperty]
    public int _selectedTypeId;
    [ObservableProperty]
    public string? _selectedBlockName;
    private ParkingSettings _settings;
    private List<CityModel> _cities;
    private KGPBlocksWindow _window;
    [ObservableProperty]
    public List<ParameterToChange> _parameters = [];
    private FillPropertiesWindow? _fpw = null;

    public KGPBlockWindowModel(KGPBlocksWindow window, string city)
    {
        _settings = SettingsStorage.ReadParkingSettingsFromXML();
        _cities = SettingsStorage.ReadCitySettingsFromXML();
        CityNames = _cities.Select(x => x.Name).ToArray();
        _window = window;
        GetAllBlockNames();
        SelectedCityId = Array.IndexOf(CityNames, city);
    }

    private void GetAllBlockNames()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        using DocumentLock acLckDoc = doc.LockDocument();
        using Transaction tr = db.TransactionManager.StartTransaction();
        var _dataImport = new DataImportFromAutocad(tr);
        _allBlocks = _dataImport.GetAllBlockNamesFromFileThatStartWith(_settings.KPGBlocksFilePath, _settings.KGPBlocksPrefix).ToArray();
    }
    partial void OnSelectedCityIdChanged(int value)
    {
        Blocks = [];
        if (value != -1)
        {
            if (SelectedTypeId == 1)
            {
                Blocks.Add(_settings.BuiltInParkingBlockName);
                for (var i = 0; i < _cities[value].BuiltInParkingFormulas.Length; i++)
                {
                    if (_cities[value].BuiltInParkingFormulas[i] != "0")
                        Blocks.Add(_settings.BuiltInBlockNames[i]);
                }
            }
            else
            {
                Blocks.Add(_settings.BuildingBlockNames[0]);
                Blocks.Add(_settings.BuildingBlockNames[1]);
                for (var i = 0; i < _cities[value].NonResidentialParkingFormulas.Length; i++)
                {
                    if (_cities[value].NonResidentialParkingFormulas[i] != "0")
                        Blocks.Add(_settings.BuildingBlockNames[i + 2]);
                }
            }
        }
    }
    partial void OnSelectedTypeIdChanged(int value)
    {
        Blocks = [];
        if (SelectedCityId != -1)
        {
            if (value == 1)
            {
                Blocks.Add(_settings.BuiltInParkingBlockName);
                for (var i = 0; i < _cities[SelectedCityId].BuiltInParkingFormulas.Length; i++)
                {
                    if (_cities[SelectedCityId].BuiltInParkingFormulas[i] != "0")
                        Blocks.Add(_settings.BuiltInBlockNames[i]);
                }
            }
            else
            {
                Blocks.Add(_settings.BuildingBlockNames[0]);
                Blocks.Add(_settings.BuildingBlockNames[1]);
                for (var i = 0; i < _cities[SelectedCityId].NonResidentialParkingFormulas.Length; i++)
                {
                    if (_cities[SelectedCityId].NonResidentialParkingFormulas[i] != "0")
                        Blocks.Add(_settings.BuildingBlockNames[i + 2]);
                }
            }
        }
    }
    [RelayCommand(CanExecute = nameof(CanInsertBlockClick))]
    public void InsertBlockClick()
    {
        //Finding parameters you need to fill
        Parameters = [];
        Parameters.Add(new() { Name = _settings.KGPBlockPositionParameterName });
        string formula = "";
        if (SelectedTypeId == 0)
        {
            foreach (var item in _settings.MustHaveParametersForBuilding)
            {
                Parameters.Add(new() { Name = item });
            }
            if (SelectedBlockName == _settings.BuildingBlockNames[0])
            {
                foreach (var item in _settings.MustHaveParametersForResidentialBuilding)
                {
                    Parameters.Add(new() { Name = item });
                }
            }
            else if (SelectedBlockName == _settings.BuildingBlockNames[1])
            {
                foreach (var item in _settings.MustHaveParametersForParkingBuilding)
                {
                    Parameters.Add(new() { Name = item });
                }
            }
            else
            {
                formula = _cities[SelectedCityId].NonResidentialParkingFormulas[Array.IndexOf(_settings.BuildingBlockNames, SelectedBlockName) - 2];
            }
        }
        else
        {
            if (SelectedBlockName == _settings.BuiltInParkingBlockName)
            {
                foreach (var item in _settings.MustHaveParametersForParkingBuilding)
                {
                    Parameters.Add(new() { Name = item });
                }
            }
            else
            {
                formula = _cities[SelectedCityId].BuiltInParkingFormulas[Array.IndexOf(_settings.BuiltInBlockNames, SelectedBlockName)];
            }
            foreach (var item in _settings.MustHaveParametersForBuiltIns)
            {
                if (!formula.Contains(item))
                    Parameters.Add(new() { Name = item });
            }
        }
        foreach (var text in _settings.TextToReplace)
        {
            if (formula.Contains(text))
                Parameters.Add(new() { Name = text });
        }
        //popping window to fill them
        _fpw = new FillPropertiesWindow();
        _fpw.DataContext = this;
        _window.Topmost = false;
        _fpw.Show();
    }
    private bool CanInsertBlockClick()
    {
        if (SelectedCityId >= 0 && SelectedBlockId >= 0)
            return true;
        return false;
    }
    [RelayCommand]
    public void CreateBlockClick()
    {
        if (Parameters.Where(x => x.Value == "").Count() == 0)
        {
            _window.Hide();
            _fpw!.Close();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            ReplaceCommaWithDotsInParameters();
            using DocumentLock acLckDoc = doc.LockDocument();
            using Transaction tr = db.TransactionManager.StartTransaction();
            var dataExport = new DataExportToAutocad(tr);
            var result = dataExport.InsertBlockFromDifferentFileByName(_settings.KPGBlocksFilePath, SelectedBlockName!, _settings.KGPLayer, Parameters);
            if (result != "Ok")
                System.Windows.MessageBox.Show("Произошла ошибка " + result, "Сообщение", System.Windows.MessageBoxButton.OK);
            tr.Commit();
            SettingsStorage.SaveDataToDWG("Город", CityNames[SelectedCityId]);
            _window.Show();
        }
        else
        {
            System.Windows.MessageBox.Show("Необходимо заполнить все поля перед вставкой. ", "Сообщение", System.Windows.MessageBoxButton.OK);
        }

    }
    private void ReplaceCommaWithDotsInParameters()
    {
        for (int i = 0; i < Parameters.Count; i++)
        {
            Parameters[i].Value = Parameters[i].Value.Replace(',', '.');
        }
    }
}
