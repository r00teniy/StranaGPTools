using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using ClassLibrary.Models;

namespace ClassLibrary;
public class DataImportFromAutocad(Transaction? transaction)
{
    private readonly Database db = Application.DocumentManager.MdiActiveDocument.Database;

    internal List<T> GetAllElementsOfTypeInDrawing<T>(string xrefName = "", bool everywhere = false) where T : Entity
    {
        List<T> output = [];
        List<XrefGraphNode> xrefList = [];
        List<BlockTableRecord> btrList = [];
        BlockTable bT = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        if (everywhere)
        {
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                xrefList.Add(XrGraph.GetXrefNode(i));
            }
            btrList.Add((BlockTableRecord)transaction.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForRead));
        }
        else if (xrefName != "")
        {
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 0; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                if (XrNode.Name == xrefName)
                {
                    xrefList.Add(XrNode);
                    break;
                }
            }
        }
        if (xrefList.Count == 0)
        {
            BlockTableRecord bTr = (BlockTableRecord)transaction.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId item in bTr)
            {
                if (item.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(T))))
                {
                    output.Add((T)transaction.GetObject(item, OpenMode.ForRead));
                }
            }
        }
        else
        {
            foreach (var xref in xrefList)
            {
                btrList.Add((BlockTableRecord)transaction.GetObject(xref.BlockTableRecordId, OpenMode.ForRead));
            }
            foreach (var btr in btrList)
            {
                foreach (ObjectId item in btr)
                {
                    if (item.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(T))))
                    {
                        output.Add((T)transaction.GetObject(item, OpenMode.ForRead));
                    }
                }
            }
        }
        return output;
    }
    internal List<T> GetAllElementsOfTypeOnLayer<T>(string layer, bool exactLayerName = true, string xrefName = "", bool everywhere = false) where T : Entity
    {
        List<T> output = [];
        List<XrefGraphNode> xrefList = [];
        List<BlockTableRecord> btrList = [];
        BlockTable bT = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        if (everywhere)
        {
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                xrefList.Add(XrGraph.GetXrefNode(i));
            }
            btrList.Add((BlockTableRecord)transaction.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForRead));
        }
        else if (xrefName != "")
        {
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 0; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                if (XrNode.Name == xrefName)
                {
                    xrefList.Add(XrNode);
                    break;
                }
            }
        }
        else
        {
            btrList.Add((BlockTableRecord)transaction.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForRead));
        }

        foreach (var xref in xrefList)
        {
            btrList.Add((BlockTableRecord)transaction.GetObject(xref.BlockTableRecordId, OpenMode.ForRead));
        }
        foreach (var btr in btrList)
        {
            foreach (ObjectId item in btr)
            {
                if (item.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(T))))
                {
                    T entity = (T)transaction.GetObject(item, OpenMode.ForRead);
                    string layerToCHeck = xrefName != "" ? xrefName + "|" + layer : layer;
                    if (exactLayerName)
                    {
                        if (entity.Layer == layerToCHeck)
                        {
                            output.Add(entity);
                        }
                    }
                    else
                    {
                        if (entity.Layer.Contains(layer) && (xrefName == null || entity.Layer.Contains(xrefName + "|")))
                        {
                            output.Add(entity);
                        }
                    }
                }
            }
        }
        return output;
    }
    internal List<T> GetAllElementsOfTypeOnLayerInDatabase<T>(string layer, Transaction tr, Database database, bool exactLayerName = true) where T : Entity
    {
        List<T> output = [];
        List<XrefGraphNode> xrefList = [];
        List<BlockTableRecord> btrList = [];
        BlockTable bT = (BlockTable)tr.GetObject(database.BlockTableId, OpenMode.ForRead);
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        foreach (ObjectId item in btr)
        {
            if (item.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(T))))
            {
                T entity = (T)tr.GetObject(item, OpenMode.ForRead);
                if (exactLayerName)
                {
                    if (entity.Layer == layer)
                    {
                        output.Add(entity);
                    }
                }
                else
                {
                    if (entity.Layer.Contains(layer))
                    {
                        output.Add(entity);
                    }
                }
            }
        }
        return output;
    }
    internal List<string> GetAllLayerNamesContainingStringFromFile(string textLayersContains, string filePath, bool startsWith = false)
    {
        List<string> output = [];
        using (Database openDb = new(false, true))
        {
            openDb.ReadDwgFile(filePath, System.IO.FileShare.ReadWrite, true, "");
            using Transaction tr = openDb.TransactionManager.StartTransaction();
            LayerTable lt = (LayerTable)tr.GetObject(openDb.LayerTableId, OpenMode.ForRead);
            LayerTableRecord layer;
            foreach (ObjectId item in lt)
            {
                layer = (LayerTableRecord)tr.GetObject(item, OpenMode.ForWrite);
                if (startsWith)
                {
                    if (layer.Name.StartsWith(textLayersContains))
                    {
                        output.Add(layer.Name);
                    }
                }
                else
                {
                    if (layer.Name.Contains(textLayersContains))
                    {
                        output.Add(layer.Name);
                    }
                }
            }
            tr.Commit();
        }
        return output;
    }
    internal List<string> GetAllLayersNamesContainingString(string textLayersContains, bool startsWith = false, string xrefName = "")
    {
        List<string> output = [];
        LayerTable lt = (LayerTable)transaction.GetObject(db.LayerTableId, OpenMode.ForRead);
        foreach (ObjectId item in lt)
        {
            LayerTableRecord layer = (LayerTableRecord)transaction.GetObject(item, OpenMode.ForRead);
            if (xrefName == "")
            {
                if (startsWith)
                {
                    if (layer.Name.StartsWith(textLayersContains))
                    {
                        output.Add(layer.Name);
                    }
                }
                else
                {
                    if (layer.Name.Contains(textLayersContains))
                    {
                        output.Add(layer.Name);
                    }
                }
            }
            else
            {
                if (startsWith)
                {
                    if (layer.Name.StartsWith(xrefName + '|' + textLayersContains))
                    {
                        output.Add(layer.Name.Replace(xrefName + '|', ""));
                    }
                }
                else
                {
                    if (layer.Name.Contains(xrefName + '|') && layer.Name.Contains(textLayersContains))
                    {
                        output.Add(layer.Name.Replace(xrefName + '|', ""));
                    }
                }
            }
        }
        return output;
    }
    public (string, BlockReference?) GetSingularBlockReferenceByBlockName(string blockName)
    {
        BlockTable bT = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        var blockId = bT[blockName];
        var bbtr = transaction.GetObject(blockId, OpenMode.ForRead, false, true) as BlockTableRecord;
        var anonBlOkckRefCollection = bbtr.GetAnonymousBlockIds();

        ObjectIdCollection bRefCollection = new();

        foreach (ObjectId anonymousBtrId in anonBlOkckRefCollection)
        {
            BlockTableRecord anonymousBtr = (BlockTableRecord)transaction.GetObject(anonymousBtrId, OpenMode.ForRead);
            ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

            foreach (ObjectId id in blockRefIds)
            {
                bRefCollection.Add(id);
            }
        }

        if (bRefCollection.Count > 1)
        {
            return ($"В файле больше одного блока {blockName}", null);
        }
        if (bRefCollection.Count == 0)
        {
            return ($"В файле нет блока {blockName} или его настройка не выполнена", null);
        }

        var blockRef = (BlockReference)transaction.GetObject(bRefCollection[0], OpenMode.ForRead);

        return ("Ok", blockRef);
    }
    internal List<string> GetXRefList()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        List<string> output = new();
        XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
        for (int i = 1; i < XrGraph.NumNodes; i++)
        {
            output.Add(XrGraph.GetXrefNode(i).Name);
        }
        return output;
    }
    internal List<string> GetAllBlockNamesFromFileThatStartWith(string filePath, string blockStartsWith = "")
    {
        List<string> output = [];

        using (Database openDb = new(false, true))
        {
            openDb.ReadDwgFile(filePath, FileShare.ReadWrite, true, "");
            using Transaction tr = openDb.TransactionManager.StartTransaction();
            BlockTable bt = (BlockTable)tr.GetObject(openDb.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId objId in bt)
            {
                BlockTableRecord btr = (BlockTableRecord)objId.GetObject(OpenMode.ForRead);

                if (btr.IsLayout || btr.Name.StartsWith("*") || (blockStartsWith != "" && !btr.Name.StartsWith(blockStartsWith)))
                {
                    continue;
                }
                output.Add(btr.Name);
            }
            tr.Commit();
        }
        return output;
    }
    internal (string, HatchStyleModel, LayerModel) GetSelectedHatchStyle(string filePath, string hatchLayer)
    {
        HatchStyleModel hatchStyleModel = new();
        LayerModel layerModel = new();
        using (Database openDb = new(false, true))
        {
            openDb.ReadDwgFile(filePath, FileShare.ReadWrite, true, "");
            using Transaction ftr = openDb.TransactionManager.StartTransaction();
            BlockTable fbt = (BlockTable)ftr.GetObject(openDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord fbtr = (BlockTableRecord)ftr.GetObject(fbt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            LayerTable lt = (LayerTable)ftr.GetObject(openDb.LayerTableId, OpenMode.ForRead);
            if (lt.Has(hatchLayer))
            {
                bool foundHatch = false;
                foreach (ObjectId item in fbtr)
                {
                    Entity entity = (Entity)ftr.GetObject(item, OpenMode.ForRead);
                    if (entity is Hatch hatch && entity.Layer == hatchLayer)
                    {
                        hatchStyleModel.PatternScale = hatch.PatternScale;
                        hatchStyleModel.PatternName = hatch.PatternName;
                        hatchStyleModel.PatternAngle = hatch.PatternAngle;
                        hatchStyleModel.BackgroundColor = hatch.BackgroundColor;
                        hatchStyleModel.PatternColor = hatch.Color;
                        hatchStyleModel.Transparency = hatch.Transparency;
                        hatchStyleModel.LayerName = hatchLayer;
                        foundHatch = true;
                        break;
                    }
                }
                LayerTableRecord layer;
                foreach (var lay in lt)
                {
                    layer = (LayerTableRecord)ftr.GetObject(lay, OpenMode.ForRead);
                    if (layer.Name == hatchLayer)
                    {
                        layerModel.LayerName = layer.Name;
                        layerModel.LayerLineWeight = layer.LineWeight;
                        layerModel.LayerColor = layer.Color;
                        layerModel.IsLayerPlottable = layer.IsPlottable;
                        layerModel.LayerTransparency = layer.Transparency;
                        break;
                    }
                }
                if (!foundHatch)
                {
                    return ($"Произошла ошибка, на слое {hatchLayer} нет штриховки", hatchStyleModel, layerModel);
                }
            }
            else
            {
                return ($"Произошла ошибка, в файле нет слоя {hatchLayer}", hatchStyleModel, layerModel);
            }
        }
        return ("Ok", hatchStyleModel, layerModel);
    }
    internal string RedifineExistingBlocksFromAnotherFile(string filePath, string[] blocksNames, string[] attributesTOkeep)
    {
        int errors = 0;
        BlockTable bt = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        BlockTableRecord btr = (BlockTableRecord)transaction.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        //Getting list of blockreferences to reset
        ObjectIdCollection[] blockReferencesToReset = new ObjectIdCollection[blocksNames.Length];
        foreach (ObjectId objId in bt)
        {
            BlockTableRecord blockBtr = (BlockTableRecord)objId.GetObject(OpenMode.ForRead);
            for (int i = 0; i < blocksNames[i].Length; i++)
            {
                if (blockBtr.Name == blocksNames[i])
                {
                    ObjectIdCollection anonBlOkckRefCollection = blockBtr.GetAnonymousBlockIds();
                    foreach (ObjectId anonymousBtrId in anonBlOkckRefCollection)
                    {
                        BlockTableRecord anonymousBtr = (BlockTableRecord)transaction.GetObject(anonymousBtrId, OpenMode.ForRead);
                        ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

                        foreach (ObjectId id in blockRefIds)
                        {
                            blockReferencesToReset[i].Add(id);
                        }
                    }
                }
            }
        }
        //Getting blocks from file and replacing them in current file
        using (Database openDb = new(false, true))
        {
            ObjectIdCollection blocks = [];
            try
            {
                openDb.ReadDwgFile(filePath, FileShare.ReadWrite, true, "");
            }
            catch (System.Exception e)
            {
                return "Ошибка при попытке открытия файла с блоками, возможно файл перемещен или нет доступа" + e.Message;
            }
            using (Transaction ftr = openDb.TransactionManager.StartTransaction())
            {
                BlockTable fbt = (BlockTable)ftr.GetObject(openDb.BlockTableId, OpenMode.ForRead);
                for (int i = 0; i < blocksNames.Length; i++)
                {
                    if (fbt.Has(blocksNames[i]))
                    {
                        blocks.Add(fbt[blocksNames[i]]);
                    }
                    else
                    {
                        errors++;
                    }
                }
                ftr.Commit();
            }
            IdMapping iMap = [];
            db.WblockCloneObjects(blocks, db.BlockTableId, iMap, DuplicateRecordCloning.Replace, false);
        }
        //Resetting blocks in drawing while keeping selectedattributes
        var workWithBlocks = new WorkWithBlocks(transaction);
        try
        {
            for (int i = 0; i < blockReferencesToReset.Length; i++)
            {
                foreach (ObjectId brefId in blockReferencesToReset[i])
                {
                    BlockReference bRef = (BlockReference)transaction.GetObject(brefId, OpenMode.ForWrite, false, true);
                    var parameters = workWithBlocks.GetAllAttributesFromBlockReferences([bRef])[0];
                    bRef.ResetBlock();
                    string[] attributes = [];
                    for (var j = 0; j < attributesTOkeep.Length; j++)
                    {
                        attributes[j] = parameters[attributesTOkeep[j]];
                    }
                    workWithBlocks.SetBlockAttributes(bRef, attributesTOkeep, attributes);
                }
            }
        }
        catch (System.Exception e)
        {
            return $"Произошла ошибка {e.Message}";
        }
        if (errors != 0)
        {
            return $"В файле не найдено {errors} блоков для переопределения";
        }
        else
        {
            return "Ok";
        }
    }
    internal T? GetObjectOfTypeTByObjectId<T>(ObjectId objectId, Transaction? tr = null) where T : Entity
    {
        if (tr != null)
            return tr.GetObject(objectId, OpenMode.ForRead) as T;
        return transaction!.GetObject(objectId, OpenMode.ForRead) as T;
    }



}
