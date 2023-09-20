using Common.Properties;
using HtmlAgilityPack;

namespace Common.Serializer
{
    public class AbstractModel
    {
        public Extraction extraction { get; set; }
    }

    public class Extraction
    {
        public string name { get; set; }
        public string urlBase { get; set; }
        public string urlSuffix { get; set; }
        public List<string> directUrls { get; set; }
        public string separatorId { get; set; }
        public Navigation navigation { get; set; }
        public Target target { get; set; }
        public List<Next> next { get; set; }
    }

    public class Next: Target
    {
        public Navigation navigation { get; set; }
        public int level { get; set; }
    }

    public class HtmlElementBase : HtmlElementProperty
    {
        /// <summary>
        /// Html Tag definition
        /// </summary>
        public virtual HtmlAttr tag { get; set; }
    }

    public class HtmlElementProperty
    {
        /// <summary>
        /// Html Attribute name definition
        /// </summary>
        public HtmlAttr? keyProperty { get; set; }

        /// <summary>
        /// Html Attribute value, Can be a regular expression, must be parsed first
        /// </summary>
        public RegexString valueProperty { get; set; }
    }

    public class Target : HtmlElementProperty
    {
        /// <summary>
        /// Tag containing the target
        /// </summary>
        public HtmlAttr? tag { get; set; }
        public Relation reconRelation { get; set; }
        /// <summary>
        /// Conditions to recognize the target, recon is a child node or sibling
        /// </summary>
        public Recon recon { get; set; }
        /// <summary>
        /// Identifies where to get the target
        /// Can be a regular expression, must be parsed first
        /// </summary>
        public HtmlAttr value { get; set; }
        public Regexing regex { get; set; }
        public NameValue[] HCValues { get; set; }
        public string classType { get; set; }
    }

    public class Recon : HtmlElementBase
    {
        public HtmlAttr? reconValue { get; set; }
        public Regexing regex { get; set; }
        public Classification[] classification { get; set; }

        public string GetReconValue(HtmlNode node)
        {
            string retValue = String.Empty;

            switch (reconValue)
            {
                case HtmlAttr.InnerText:
                    retValue = node.InnerText;
                    break;
                case HtmlAttr.Title:
                    retValue = node.Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Title.Stringify()).Value;
                    break;
            }

            return retValue;
        }

    }

    public class NameValue
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Classification : NameValue
    {
        public RegexString findBy { get; set; }
        public bool isExclusive { get; set; }

        public string GetClassificationValue(string input)
        {
            if (!string.IsNullOrWhiteSpace(findBy) && !findBy.Equals(input))
                return null;

            return value ?? input;
        }
    }

    public class Navigation : HtmlElementBase
    {
        /// <summary>
        /// Conditions to recognize the target, recon is a child node or sibling
        /// </summary>
        public Recon recon { get; set; }
        public Navigation nav { get; set; }
        public Relation reconRelation { get; set; }
    }

}


