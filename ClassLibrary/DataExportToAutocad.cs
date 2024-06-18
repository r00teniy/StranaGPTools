using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using ClassLibrary.Models;
namespace ClassLibrary;
public class DataExportToAutocad
{
    private readonly Database db = Application.DocumentManager.MdiActiveDocument.Database;
    private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    private readonly Transaction _transaction;
    private readonly BlockTable _blockTable;
    private readonly UserInput _userInput;

    public DataExportToAutocad(Transaction transaction)
    {
        _transaction = transaction;
        _blockTable = (BlockTable)_transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        _userInput = new();
    }
    internal string CreateTempLines(List<(Point3d, Point3d)> pointsList, ElementStyleModel style)
    {
        var bT = (BlockTable)_transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        BlockTableRecord btr = (BlockTableRecord)_transaction.GetObject(bT[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        try
        {
            foreach (var points in pointsList)
            {
                var (ptStart, ptEnd) = points;
                using Line acLine = new();
                acLine.Color = style.ElementColor;
                acLine.Layer = style.ElementLayer;
                acLine.LineWeight = style.ElementLineWeight;
                acLine.Transparency = style.ElementTransparency;
                acLine.StartPoint = ptStart;
                acLine.EndPoint = ptEnd;
                // Add the new object to the block table record and the transaction
                btr.AppendEntity(acLine);
                _transaction.AddNewlyCreatedDBObject(acLine, true);
            }
            return "Ok";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
    internal string CreateMleadersWithText(List<string> texts, List<Point3d> points, MleaderStyleModel style)
    {
        BlockTableRecord btr = (BlockTableRecord)_transaction.GetObject(_blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        //creating Mleaders
        LayerTable lt = (_transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)!;
        if (texts.Count != points.Count)
        {
            return "Передано разное кол-во точек и текста, выноски не будут построены";
        }
        DBDictionary mlStyles = (_transaction.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary)!;
        ObjectId mlStyleId;
        try
        {
            mlStyleId = mlStyles.GetAt(style.MLeaderStyle);
        }
        catch (Exception)
        {
            return $"В файле нет стиля выноски {style.MLeaderStyle}, необходимо скопировать его из шаблона";
        }
        if (!lt.Has(style.MLeaderLayer))
        {
            return $"В файле нет слоя {style.MLeaderLayer} для размещения выносок, его необходимо создать или скопировать из шаблона.";
        }
        try
        {
            for (int i = 0; i < texts.Count; i++)
            {
                //createing Mleader
                MLeader leader = new();
                leader.SetDatabaseDefaults();
                leader.MLeaderStyle = mlStyleId;
                leader.ContentType = ContentType.MTextContent;
                leader.Layer = style.MLeaderLayer;
                MText mText = new();
                mText.SetDatabaseDefaults();
                mText.Width = 0.675;
                mText.Height = 1.25;
                mText.TextHeight = 1.25;
                mText.SetContentsRtf(texts[i]);
                mText.Location = new Point3d(points[i].X + 2, points[i].Y + 2, points[i].Z);
                mText.Rotation = 0;
                mText.BackgroundFill = true;
                mText.BackgroundScaleFactor = 1.1;
                leader.MText = mText;
                _ = leader.AddLeaderLine(points[i]);
                btr.AppendEntity(leader);
                _transaction.AddNewlyCreatedDBObject(leader, true);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return "Ok";
    }
    internal string CreateMleaderWithBlockForGroupOfobjects(List<List<Point3d>> pointList, MleaderStyleModel style, List<string[]> data)
    {
        BlockTableRecord btr = (BlockTableRecord)_transaction.GetObject(_blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        //Getting rotation of current UCS to pass it to block
        Matrix3d UCS = ed.CurrentUserCoordinateSystem;
        CoordinateSystem3d cs = UCS.CoordinateSystem3d;
        double rotAngle = cs.Xaxis.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
        //Creating Mleaders for each group
        DBDictionary mlStyles = (_transaction.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary)!;
        ObjectId mlStyleId;
        try
        {
            mlStyleId = mlStyles.GetAt(style.MLeaderStyle);
        }
        catch (Exception)
        {
            return $"В файле нет стиля выноски {style.MLeaderStyle}, необходимо скопировать его из шаблона";
        }
        LayerTable lt = (_transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)!;
        if (!lt.Has(style.MLeaderLayer))
        {
            return $"В файле нет слоя {style.MLeaderLayer} для размещения выносок, его необходимо создать или скопировать из шаблона.";
        }
        for (var i = 0; i < pointList.Count; i++)
        {
            try
            {
                var leader = new MLeader();
                leader.SetDatabaseDefaults();
                leader.MLeaderStyle = mlStyleId;
                leader.ContentType = ContentType.BlockContent;
                leader.Layer = style.MLeaderLayer;
                leader.BlockContentId = _blockTable[style.BlockName];
                leader.BlockPosition = new Point3d(pointList[i][0].X + 5, pointList[i][0].Y + 5, 0);
                leader.BlockRotation = rotAngle;
                int idx = leader.AddLeaderLine(pointList[i][0]);
                // adding leader points for each element
                // TODO: temporary solution, need sorting algorithm for better performance.
                if (pointList[i].Count > 1)
                {
                    foreach (Point3d m in pointList[i])
                    {
                        leader.AddFirstVertex(idx, m);
                    }
                }
                //Handle Block Attributes
                BlockTableRecord blkLeader = (_transaction.GetObject(leader.BlockContentId, OpenMode.ForRead) as BlockTableRecord)!;
                //Doesn't take in consideration oLeader.BlockRotation
                Matrix3d Transfo = Matrix3d.Displacement(leader.BlockPosition.GetAsVector());
                foreach (ObjectId blkEntId in blkLeader)
                {
                    AttributeDefinition? AttributeDef = _transaction.GetObject(blkEntId, OpenMode.ForRead) as AttributeDefinition;
                    if (AttributeDef != null)
                    {
                        AttributeReference AttributeRef = new();
                        AttributeRef.SetAttributeFromBlock(AttributeDef, Transfo);
                        AttributeRef.Position = AttributeDef.Position.TransformBy(Transfo);
                        // setting attributes
                        for (var j = 0; j < style.BlockAttributes.Length; j++)
                        {
                            if (AttributeRef.Tag == style.BlockAttributes[j])
                            {
                                AttributeRef.TextString = data[i][j];
                            }
                            leader.SetBlockAttribute(blkEntId, AttributeRef);
                        }
                    }
                }
                // adding Mleader to blocktablerecord
                btr.AppendEntity(leader);
                _transaction.AddNewlyCreatedDBObject(leader, true);
            }
            catch (System.Exception e)
            {
                return "При попытке создать выноску произошла ошибка " + e.Message + " в модуле " + e.StackTrace;
            }
        }
        return "Ok";
    }
    internal void CheckIfLayerExistAndCreateIfNot(LayerModel model)
    {
        LayerTable lt = (_transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)!;
        if (!lt.Has(model.LayerName))
        {
            var newLayer = new LayerTableRecord
            {
                Name = model.LayerName,
                Color = model.LayerColor,
                LineWeight = model.LayerLineWeight,
                IsPlottable = model.IsLayerPlottable
            };

            lt.UpgradeOpen();
            lt.Add(newLayer);
            _transaction.AddNewlyCreatedDBObject(newLayer, true);
            newLayer.Transparency = model.LayerTransparency;
        }
    }
    internal void ClearTempGeometry(LayerModel model)
    {
        var btr = (_transaction.GetObject(_blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord)!;
        foreach (ObjectId objectId in btr)
        {
            var obj = (_transaction.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity)!;
            if (obj.Layer == model.LayerName) // Checking for temporary layer
            {
                obj.Erase();
            }
        }
    }
    internal string InsertBlockFromDifferentFileByName(string filePath, string blockname, LayerModel model, List<ParameterToChange>? parameters = null)
    {
        try
        {
            ObjectIdCollection block = new();
            using Database OpenDb = new Database(false, true);
            if (!_blockTable.Has(blockname))
            {
                OpenDb.ReadDwgFile(filePath, System.IO.FileShare.ReadWrite, true, "");
                using (Transaction ftr = OpenDb.TransactionManager.StartTransaction())
                {
                    var fbt = (BlockTable)ftr.GetObject(OpenDb.BlockTableId, OpenMode.ForRead);
                    if (fbt.Has(blockname))
                    {
                        block.Add(fbt[blockname]);
                    }
                    ftr.Commit();
                }
                if (block.Count != 0)
                {
                    IdMapping iMap = new();
                    db.WblockCloneObjects(block, db.BlockTableId, iMap, DuplicateRecordCloning.Ignore, false);
                }
            }
            var pt = _userInput.GetInsertionPoint();
            var blockId = _blockTable[blockname];
            CheckIfLayerExistAndCreateIfNot(model);
            if (pt != null)
            {
                using BlockReference newBlock = new((Point3d)pt, blockId);
                var btr = (BlockTableRecord)_transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                newBlock.Layer = model.LayerName;
                btr.AppendEntity(newBlock);
                _transaction.AddNewlyCreatedDBObject(newBlock, true);
                //Dealing with attributes
                BlockTableRecord blockBTR = (BlockTableRecord)blockId.GetObject(OpenMode.ForRead);
                foreach (var item in blockBTR)
                {
                    var attributeDef = _transaction.GetObject(item, OpenMode.ForRead) as AttributeDefinition;
                    if (attributeDef != null)
                    {
                        AttributeReference attributeRef = new();
                        attributeRef.SetAttributeFromBlock(attributeDef, newBlock.BlockTransform);
                        if (attributeDef.Constant)
                            continue;
                        newBlock.AttributeCollection.AppendAttribute(attributeRef);
                        _transaction.AddNewlyCreatedDBObject(attributeRef, true);
                        string newId = """"%<\_ObjId """" + newBlock.ObjectId.ToString().Substring(1, newBlock.ObjectId.ToString().Length - 2) + """">%"""";
                        var textString = attributeDef.getTextWithFieldCodes().Replace("?BlockRefId", newId);
                        var field = new Field(textString);
                        field.Evaluate();
                        var evalResult = field.EvaluationStatus;
                        if (evalResult.Status == FieldEvaluationStatus.Success)
                        {
                            attributeRef.SetField(field);
                            _transaction.AddNewlyCreatedDBObject(field, true);
                        }
                        else
                        {
                            attributeRef.TextString = textString;
                        }
                    }
                }
                if (parameters != null)
                {
                    var wwb = new WorkWithBlocks(_transaction);
                    wwb.SetBlockParameters(newBlock, parameters.Select(x => x.Name).ToArray(), parameters.Select(x => x.Value).ToArray());
                }
            }
            else
            {
                return "Не была выбрана точка вставки.";
            }
            return "Ok";
        }
        catch (System.Exception e)
        {
            return $"Произошла ошибка {e.Message}";
        }
    }
    internal string CreateHatchInDrawingFromStyles(HatchStyleModel style, LayerModel model)
    {
        ObjectIdCollection pls = new();
        var bt = (BlockTable)_transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
        var btr = (BlockTableRecord)_transaction.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        //CHecking if layer exist and creating it if not
        var pl = _userInput.GetObjectIdOfEntity<Polyline>("полилинию");
        if (pl != null)
        {
            pls.Add((ObjectId)pl);
        }
        else
        {
            return $"Вы не выбрали Полилинию";
        }
        var pLine = (_transaction.GetObject((ObjectId)pl, OpenMode.ForRead) as Polyline)!;
        if (pLine.Closed != true)
        {
            return $"Полилиния должна быть замкнутая";
        }
        CheckIfLayerExistAndCreateIfNot(model);
        var hat = new Hatch()
        {
            Layer = style.LayerName,
            Color = style.PatternColor,
            Transparency = style.Transparency
        };
        hat.PatternScale = style.PatternScale;
        hat.SetHatchPattern(HatchPatternType.PreDefined, style.PatternName);
        hat.BackgroundColor = style.BackgroundColor;
        hat.PatternAngle = style.PatternAngle;
        btr.AppendEntity(hat);
        _transaction.AddNewlyCreatedDBObject(hat, true);
        hat.Associative = true;
        hat.AppendLoop(HatchLoopTypes.External, pls);
        hat.EvaluateHatch(true);
        return "Ok";
    }
    internal string CreateTableInDrawing(Point3d pt, List<string[]> data, List<double[]> blockScale, TableStyleModel style)
    {
        var btr = (_transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord)!;
        ObjectId tbSt = new();
        DBDictionary tsd = (DBDictionary)_transaction.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);
        foreach (DBDictionaryEntry entry in tsd)
        {
            var tStyle = (TableStyle)_transaction.GetObject(entry.Value, OpenMode.ForRead);
            if (tStyle.Name == style.StyleName)
            { tbSt = entry.Value; }
        }
        if (tbSt.Handle.Value == 0)
        {
            return $"В файле отсутствует нужный стиль таблиц, необходимо работать в шаблоне или скопировать стиль Таблицы {style.StyleName} в этот файл";
        }
        var tb = new Table()
        {
            Position = pt,
            TableStyle = tbSt
        };
        try
        {
            tb.Layer = style.Layer;
        }
        catch (Exception)
        {
            return $"В файле отсутствует нужный слой для таблиц {style.Layer}, необходимо работать в шаблоне. Таблица создана на текущем слое";
        }
        tb.Rows[0].Style = style.TitleStyleName;
        tb.Cells[0, 0].TextString = style.Title;
        tb.SetRowHeight(10);
        tb.SetColumnWidth(style.CollumnWidths[0]);
        //Creating Header
        var currentRow = 1;
        for (int i = 0; i < style.HeaderRowsCount; i++)
        {
            tb.InsertRows(currentRow, style.HeaderRowsHeight[i], 1);
            tb.Rows[currentRow].Style = style.HeaderStyleName;
            currentRow++;
        }
        currentRow = 1;
        for (int j = 1; j < style.HeaderCollumnNames[0].Length; j++)
        {
            tb.InsertColumns(j, style.CollumnWidths[j], 1);
            tb.Cells[currentRow, j].Alignment = CellAlignment.MiddleCenter;

        }
        for (int i = 0; i < style.HeaderRowsCount; i++)
        {

            tb.Cells[currentRow, 0].TextString = style.HeaderCollumnNames[i][0] == null ? "" : style.HeaderCollumnNames[i][0];
            for (int j = 1; j < style.HeaderCollumnNames[i].Length; j++)
            {
                tb.Cells[currentRow, j].TextString = style.HeaderCollumnNames[i][j] == null ? "" : style.HeaderCollumnNames[i][j];
            }
            currentRow++;
        }
        MergeCellsAndSetAlignment(style.HeaderMergeRanges, tb);
        //Filling data
        for (int i = 0; i < data.Count(); i++)
        {
            tb.InsertRows(currentRow, style.DataRowsHeight, 1);
            for (int j = 0; j < data[i].Length; j++)
            {
                if (style.BlockCollumnsInData.Contains(j))
                {
                    var id = Array.IndexOf(style.BlockCollumnsInData, j);
                    tb.Cells[currentRow, j].Alignment = CellAlignment.MiddleCenter;
                    tb.Cells[currentRow, j].BlockTableRecordId = _blockTable[data[i][j]];
                    if (blockScale[id][i] != 0)
                    {
                        tb.Cells[currentRow, j].Contents[0].IsAutoScale = false;
                        tb.Cells[currentRow, j].Contents[0].Scale = blockScale[id][i];
                    }
                }
                else
                {
                    tb.Cells[currentRow, j].Alignment = CellAlignment.MiddleCenter;
                    tb.Cells[currentRow, j].TextString = data[i][j] == null || data[i][j] == "0" || data[i][j] == "" ? "-" : data[i][j];
                }
            }
            currentRow++;
        }
        MergeCellsAndSetAlignment(style.DataMergeRanges, tb);
        SetCellStyle(style.CellStyleModels, tb);
        SetVisualStyles(style.VisualStyleModel, tb);
        tb.GenerateLayout();
        btr.AppendEntity(tb);
        _transaction.AddNewlyCreatedDBObject(tb, true);
        return "Ok";
    }
    //SupportMethods
    private void SetVisualStyles(VisualStyleModel[] styles, Table tb)
    {
        foreach (var item in styles)
        {
            var range = CellRange.Create(tb, item.FirstRow, item.FirstCollumn, item.SecondRow, item.SecondCollumn);
            if (item.SetRightBorder)
            {
                range.Borders.Right.LineWeight = item.BorderLineWeight;
            }
            if (item.SetBottomBorder)
            {
                range.Borders.Bottom.LineWeight = item.BorderLineWeight;
            }
            if (item.BackgrounColor != -1)
            {
                range.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, item.BackgrounColor);
            }
        }
    }
    private void SetCellStyle(CellStyleModel[] styles, Table tb)
    {
        foreach (var style in styles)
        {
            tb.Cells[style.Row, style.Collumn].Alignment = style.Alignment;
            tb.Cells[style.Row, style.Collumn].Contents[0].Rotation = style.RotationAngle;
        }
    }
    private void MergeCellsAndSetAlignment(TableRangeModel[] ranges, Table tb)
    {
        foreach (var item in ranges)
        {
            var range = CellRange.Create(tb, item.FirstRow, item.FirstCollumn, item.SecondRow, item.SecondCollumn);
            tb.MergeCells(range);
            range.Alignment = item.Alignment;
            tb.Cells[item.FirstRow, item.FirstCollumn].Contents[0].Rotation = item.RotationAngle;
        }
    }
}
