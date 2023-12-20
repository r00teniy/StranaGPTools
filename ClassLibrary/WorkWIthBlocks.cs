using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary;
internal class WorkWithBlocks(Transaction transaction)
{
    internal List<Dictionary<string, string>> GetAllAttributesFromBlockReferences(List<BlockReference> blockReferenceList)
    {
        var output = new List<Dictionary<string, string>>();
        for (var i = 0; i < blockReferenceList.Count; i++)
        {
            output.Add([]);
            foreach (ObjectId id in blockReferenceList[i].AttributeCollection)
            {
                // open the attribute reference
                var attRef = (AttributeReference)transaction.GetObject(id, OpenMode.ForRead);
                //Adding it to dictionary
                if (attRef != null)
                {
                    output[i].Add(attRef.Tag, attRef.TextString);
                }
            }
        }
        return output;
    }
    internal void SetBlockAttributes(BlockReference block, string[] attrNames, string[] attrValues)
    {
        foreach (ObjectId id in block.AttributeCollection)
        {
            // open the attribute reference
            var attRef = (AttributeReference)transaction.GetObject(id, OpenMode.ForRead);
            //Checking fot tag & setting value
            for (int i = 0; i < attrNames.Length; i++)
            {
                if (attRef.Tag == attrNames[i])
                {
                    attRef.UpgradeOpen();
                    attRef.TextString = attrValues[i];
                }
            }
        }
    }
}
