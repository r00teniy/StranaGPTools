using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary;
internal class WorkWithBlocks(Transaction transaction)
{
    public List<Dictionary<string, string>> GetAllAttributesFromBlockReferences(List<BlockReference> blockReferenceList)
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
    public List<Dictionary<string, string>> GetAllParametersFromBlockReferences(List<BlockReference> blockReferenceList)
    {
        List<Dictionary<string, string>> output = new List<Dictionary<string, string>>();
        foreach (var blockReference in blockReferenceList)
        {
            var dict = new Dictionary<string, string>();
            var propertyCollection = blockReference.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty property in propertyCollection)
            {
                if (!dict.ContainsKey(property.PropertyName))
                {
                    dict.Add(property.PropertyName, property.Value.ToString());
                }
            }
            output.Add(dict);
        }
        return output;
    }
    public void SetBlockAttributes(BlockReference block, string[] attrNames, string[] attrValues)
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
    public void SetBlockParameters(BlockReference block, string[] attrNames, string[] attrValues)
    {
        var propertyCollection = block.DynamicBlockReferencePropertyCollection;
        foreach (DynamicBlockReferenceProperty prop in propertyCollection)
        {
            if (attrNames.Contains(prop.PropertyName))
            {
                var parameterValue = attrValues[Array.IndexOf(attrNames, prop.PropertyName)];
                switch (prop.PropertyTypeCode)
                {
                    case 5:
                        prop.Value = parameterValue;
                        break;
                    case 2:
                        prop.Value = Convert.ToInt32(parameterValue);
                        break;
                    case 3:
                        prop.Value = Convert.ToInt16(parameterValue);
                        break;
                    case 1:
                        prop.Value = Convert.ToDouble(parameterValue.Replace(',', '.'));
                        break;
                    case 4:
                        prop.Value = Convert.ToSByte(parameterValue.Replace(',', '.'));
                        break;
                    case 13:
                        prop.Value = Convert.ToInt64(parameterValue.Replace(',', '.'));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
