using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

using ClassLibrary.Models;
using ClassLibrary.Models.Settings;

namespace ClassLibrary;
public static class SettingsStorage
{
    public static ParkingSettings ReadParkingSettingsFromXML()
    {
        XmlSerializer serializer = new(typeof(ParkingSettings));
        ParkingSettings settings;
        using (XmlReader reader = XmlReader.Create(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\ParkingConfig.xml"))
        {
            settings = (ParkingSettings)serializer.Deserialize(reader);
        }
        return settings;
    }
    public static List<CityModel> ReadCitySettingsFromXML()
    {
        XmlSerializer serializer = new(typeof(List<CityModel>));
        List<CityModel> settings;
        using (XmlReader reader = XmlReader.Create(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Cities.xml"))
        {
            settings = (List<CityModel>)serializer.Deserialize(reader);
        }
        return settings;
    }
}
