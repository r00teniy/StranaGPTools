using System.Collections;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

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
    internal static void SaveDataToDWG(string parameterName, string cityName)
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            using (DocumentLock acLckDoc = doc.LockDocument())
            {
                if (cityName != "")
                {
                    db.SetCustomProperty(parameterName, cityName);
                }
                tr.Commit();
            }
        }
    }
    internal static string ReadData(string parameterName)
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;

        return db.GetCustomProperty(parameterName);
    }
    public static string GetCustomProperty(this Database db, string key)
    {
        DatabaseSummaryInfoBuilder sumInfo = new(db.SummaryInfo);
        IDictionary custProps = sumInfo.CustomPropertyTable;
        return (string)custProps[key];
    }
    public static void SetCustomProperty(this Database db, string key, string value)
    {
        DatabaseSummaryInfoBuilder infoBuilder = new(db.SummaryInfo);
        IDictionary custProps = infoBuilder.CustomPropertyTable;
        if (custProps.Contains(key))
            custProps[key] = value;
        else
            custProps.Add(key, value);
        db.SummaryInfo = infoBuilder.ToDatabaseSummaryInfo();
    }
}
