using System.Windows;

using ClassLibrary.Models;
using ClassLibrary.Models.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassLibrary.GUI;
public partial class ParkingWindowModel : ObservableObject
{
    public string[] CityNames { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateParkingTableClickCommand))]
    public int _selectedCityId;
    [ObservableProperty]
    public int _selectedXRef;
    private ParkingSettings _settings;
    private List<CityModel> _cities;
    private ParkingWindow _window;
    public ParkingWindowModel(ParkingWindow window, string city)
    {
        _settings = SettingsStorage.ReadParkingSettingsFromXML();
        _cities = SettingsStorage.ReadCitySettingsFromXML();
        CityNames = _cities.Select(x => x.Name).ToArray();
        SelectedCityId = Array.IndexOf(CityNames, city);
        _window = window;
    }

    [RelayCommand(CanExecute = nameof(CanCreateParkingTableClick))]
    public void CreateParkingTableClick()
    {
        _window.Hide();

        try
        {
            var pc = new ParkingCalculations(_settings, _cities[SelectedCityId]);
            var result = pc.CreateParkingTable();
            if (result != "Ok")
            {
                MessageBox.Show("Произошла ошибка " + result, "Сообщение", System.Windows.MessageBoxButton.OK);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("Произошла ошибка " + e.Message + e.StackTrace, "Сообщение", System.Windows.MessageBoxButton.OK);
        }
        SettingsStorage.SaveDataToDWG("Город", CityNames[SelectedCityId]);
        _window.Show();
    }
    private bool CanCreateParkingTableClick()
    {
        if (SelectedCityId >= 0)
            return true;
        return false;
    }
    [RelayCommand]
    public void RecolorParkingBlocksClick()
    {
        _window.Hide();
        var wwpb = new WorkWithParkingBlocks();
        var result = wwpb.RecolorParkingBlocksInCurrentFile(_settings);
        if (result != "Ok")
        {
            MessageBox.Show("Произошла ошибка " + result, "Сообщение", System.Windows.MessageBoxButton.OK);
        }
        _window.Show();
    }
    [RelayCommand]
    public void RecolorAllParkingBlocksClick()
    {
        _window.Hide();
        var wwpb = new WorkWithParkingBlocks();
        var result = wwpb.RecolorAllParkingBlocksIncludingXRefs(_settings);
        if (result != "Ok")
        {
            MessageBox.Show("Произошла ошибка " + result, "Сообщение", System.Windows.MessageBoxButton.OK);
        }
        _window.Show();
    }
}
