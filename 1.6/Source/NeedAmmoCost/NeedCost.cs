using RimWorld;
using System.Xml;
using Verse;

namespace NeedAmmoCost
{
    public class NeedCost
    {
        public NeedDef need;

        public float cost;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "need", xmlRoot.Name);
            if (xmlRoot.HasChildNodes)
            {
                cost = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
            }
        }
    }
}
