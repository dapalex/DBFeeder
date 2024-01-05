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
        public virtual HtmlAttr? tag { get; set; }


        public bool IsEqual(HtmlNode node)
        {
            return node.Name == tag?.Stringify() && base.HasEqual(node.Attributes);

        }
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

        public bool HasEqual(HtmlAttributeCollection attributes)
        {
            if (keyProperty == null) return true;

            foreach (HtmlAttribute attribute in attributes)
                if (keyProperty.Value.Stringify() == attribute.Name)
                    if (valueProperty == null || valueProperty.Equals(attribute.Value))
                        return true;

            return false;
        }
    }

    public class Navigation : HtmlElementBase
    {
        /// <summary>
        /// Conditions to recognize the target, recon is a child node or sibling
        /// </summary>
        public List<Recon> recon { get; set; }
        public Navigation nav { get; set; }
    }

    public class Target : HtmlElementProperty
    {
        public HtmlAttr? tag { get; set; }
        /// <summary>
        /// Tag containing the target
        /// </summary>
        //public HtmlAttr? tag { get; set; }
        public string name { get; set; }

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

        public bool IsSingleStandingTarget()
        { 
            return recon != null && recon.tag == null && recon.classification != null && recon.classification.Length == 1; 
        }

        public string GetTargetValue(HtmlNode node)
        {
            HtmlNode targetValueNode;

            if (tag != null)
                targetValueNode = node.GetMatchingChild(tag.Value.Stringify(),
                                                        keyProperty?.Stringify(),
                                                        valueProperty);
            else
                targetValueNode = node;

            switch (value)
            {
                case HtmlAttr.InnerText:
                    return targetValueNode.InnerText;
                    break;
                case HtmlAttr.Href:
                    return targetValueNode.Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Href.Stringify()).Value;
                    break;
                case HtmlAttr._SharpText:
                    return targetValueNode.ChildNodes.First(cn => cn.Name == value.Stringify()).InnerText;
                    break;
                default:
                    return targetValueNode.InnerText;
                    break;
            }

            return null;
        }

        public string GetTargetClassification(HtmlNode targetNode)
        {
            HtmlNode reconNode = null;

            string classification = null;

            switch (reconRelation) //USELESS
            {
                case Relation.SIBLING:
                    reconNode = targetNode.GetMatchingChild(recon.tag?.Stringify(),
                                                            recon.keyProperty?.Stringify(),
                                                            recon.valueProperty);
                    break;
                case Relation.CHILD:
                    reconNode = targetNode.GetMatchingChild(recon.tag?.Stringify(),
                                                            recon.keyProperty?.Stringify(),
                                                            recon.valueProperty);
                    break;
            }

            try
            {
                if (reconNode != null)
                    switch (recon.reconValue)
                    {
                        case HtmlAttr.InnerText:
                            return recon.classification.FirstOrDefault(clsf => !string.IsNullOrWhiteSpace(clsf.GetClassificationValue(reconNode.InnerText)))?.name;
                            break;
                        default:
                            throw new Exception(string.Format("Recon not found for target {0}", value));
                            break;
                    }
            }
            catch (Exception ex)
            {
                throw new Exception("Classification not recognized", ex);
            }

            return null;
        }
    }

    public class Recon : HtmlElementBase
    {
        public Relation reconRelation { get; set; }
        public HtmlAttr? reconValue { get; set; }
        public Regexing regex { get; set; }
        public Classification[] classification { get; set; }

        public bool CheckRecon(HtmlNode node)
        {
            return node.GetMatchingChild(tag?.Stringify(), keyProperty?.Stringify(), valueProperty) != null;
        }

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

}


