using System.IO;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

using ClassLibrary.Models.Settings;

namespace ClassLibrary;
public class WorkWithParkingBlocks
{
    private readonly Document doc = Application.DocumentManager.MdiActiveDocument;
    private Database db = Application.DocumentManager.MdiActiveDocument.Database;
    public string RecolorParkingBlocksInCurrentFile(ParkingSettings settings)
    {
        using (DocumentLock acLckDoc = doc.LockDocument())
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var _dataImport = new DataImportFromAutocad(tr);
                var blocks = _dataImport.GetAllElementsOfTypeOnLayer<BlockReference>(settings.ParkingBlocksLayer, true);
                var result = RecolorBlocks(tr, blocks, settings, _dataImport);
                if (result != "Ok")
                {
                    return result;
                }
                tr.Commit();
            }
        }
        return "Ok";
    }
    public string RecolorAllParkingBlocksIncludingXRefs(ParkingSettings settings)
    {
        using (DocumentLock acLckDoc = doc.LockDocument())
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var _dataImport = new DataImportFromAutocad(tr);
                BlockTable bT = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                var allBlocks = _dataImport.GetAllElementsOfTypeOnLayer<BlockReference>(settings.ParkingBlocksLayer, false, "", true);
                var xRefNames = allBlocks.Select(x => x.Layer.Split('|')[0]).Distinct().ToList();
                ObjectIdCollection col = [];

                foreach (var item in bT)
                {
                    BlockTableRecord blockTableRecord = (BlockTableRecord)tr.GetObject(item, OpenMode.ForRead);
                    if (xRefNames.Contains(blockTableRecord.Name))
                    {
                        if (blockTableRecord.IsFromExternalReference)
                        {
                            var xRefDb = blockTableRecord.GetXrefDatabase(false);
                            if (xRefDb != null)
                            {
                                if (IsFileLockedOrReadOnly(new FileInfo(xRefDb.Filename)))
                                {
                                    return $"Файл {xRefDb.Filename} открыт или доступен только для чтения";
                                }
                                var name = xRefDb.ProjectName;
                                using (var xf = XrefFileLock.LockFile(xRefDb.XrefBlockId))
                                {
                                    xRefDb.RestoreOriginalXrefSymbols();
                                    using (var trans = xRefDb.TransactionManager.StartTransaction())
                                    {
                                        var blocks = _dataImport.GetAllElementsOfTypeOnLayerInDatabase<BlockReference>(settings.ParkingBlocksLayer, trans, xRefDb);

                                        var result = RecolorBlocks(trans, blocks, settings, _dataImport);
                                        if (result != "Ok")
                                        {
                                            return result;
                                        }

                                        trans.Commit();
                                    }
                                    xRefDb.RestoreForwardingXrefSymbols();
                                }
                                col.Add(item);
                            }
                        }
                    }
                }
                try
                {
                    db.ReloadXrefs(col);
                }
                catch (Exception e)
                {
                    return $"Произошла ошибка при обновлении внешних ссылок, обновите вручную ({e.Message})";
                }
                tr.Commit();
            }
        }
        return "Ok";
    }
    private bool IsFileLockedOrReadOnly(FileInfo fileinfo)
    {
        FileStream? fs = null;
        try
        {
            fs = fileinfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception ex)
        {
            if (ex is IOException || ex is UnauthorizedAccessException)
            {
                return true;
            }
        }
        finally
        {
            if (fs != null)
                fs.Close();
        }
        return false;
    }
    private string RecolorBlocks(Transaction tr, List<BlockReference> blocks, ParkingSettings settings, DataImportFromAutocad _dataImport)
    {
        if (blocks == null)
        {
            return "Ошибка при считывании блоков парковок";
        }
        try
        {
            var _workWithBlocks = new WorkWithBlocks(tr);
            var dynBlockPropValues = _workWithBlocks.GetAllParametersFromBlockReferences(blocks);
            for (var i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].AttributeCollection.Count == 4)
                {
                    ObjectId oi = blocks[i].AttributeCollection[1];
                    var attRef = _dataImport.GetObjectOfTypeTByObjectId<AttributeReference>(oi, tr);
                    if (attRef != null)
                    {
                        Regex pattern = new(@"\d+");
                        int buildingNumber = Convert.ToInt32(pattern.Match(attRef.TextString).Value);
                        blocks[i].UpgradeOpen();
                        blocks[i].Color = Color.FromColorIndex(ColorMethod.ByAci, settings.ParkingTableColors[buildingNumber - 1]);
                    }
                }
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return "Ok";
    }
}
