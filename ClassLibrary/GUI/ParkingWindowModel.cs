using System.Windows;

using ClassLibrary.Models;
using ClassLibrary.Models.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassLibrary.GUI;
public partial class ParkingWindowModel : ObservableObject
{
    public List<string> CityNames { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateParkingTableClickCommand))]
    public int _selectedCityId;
    public List<string> XRefs { get; set; }
    [ObservableProperty]
    public int _selectedXRef;
    private ParkingSettings _settings;
    private List<CityModel> _cities;
    private ParkingWindow _window;
    public ParkingWindowModel(ParkingWindow window)
    {
        _settings = SettingsStorage.ReadParkingSettingsFromXML();
        _cities = SettingsStorage.ReadCitySettingsFromXML();
        CityNames = _cities.Select(x => x.Name).ToList();
        var dataImport = new DataImportFromAutocad(null);
        XRefs = dataImport.GetXRefList();
        _window = window;
    }

    [RelayCommand(CanExecute = nameof(CanCreateParkingTableClick))]
    public void CreateParkingTableClick()
    {
        _window.Hide();

        var pc = new ParkingCalculations(_settings, _cities[SelectedCityId]);
        var result = pc.CreateParkingTable();
        if (result != "Ok")
        {
            MessageBox.Show("Произошла ошибка " + result, "Сообщение", System.Windows.MessageBoxButton.OK);
        }
        _window.Show();
    }
    private bool CanCreateParkingTableClick()
    {
        if (SelectedCityId >= 0)
            return true;
        return false;
    }
}
