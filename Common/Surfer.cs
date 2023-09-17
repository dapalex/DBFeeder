using Common.Properties;
using Common.Serializer;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging.Abstractions;
using System.Xml;
using System.Xml.Linq;

namespace Common
{
    public static class Surfer
    {
        private static HttpClient httpClient;
        private static HttpClient Create()
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Resources.UserAgentHeader);
            }
            return httpClient;
        }

        public static async Task<string> FetchPage(string url, int fetchTime)
        {
            try
            {
                Thread.Sleep(fetchTime);

                httpClient = Create();

                return await httpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class Diver : IDisposable
    { 
        /// <summary>
        /// Dictionary containing entity information gathered during navigation globally valid
        /// Initialized at each navigation start
        /// </summary>
        public Dictionary<string, string> globalEntityData;
        /// <summary>
        /// Dictionary containing entity information gathered during navigation per container
        /// Initialized at each navigation start
        /// </summary>
        public List<Tuple<string, string>> exclusiveEntityData;

        public Diver()
        {
            globalEntityData = new Dictionary<string, string>();
            exclusiveEntityData = new List<Tuple<string, string>>();
        }

        public Dictionary<string, List<HtmlNode>> NavigateHtmlPage(string htmlPage, Extraction extraction, out HtmlNode container)
        {
            try
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlPage);

                container = htmlDoc.DocumentNode.SelectSingleNode(Resources.HtmlBodyXpath);

                if(!string.IsNullOrEmpty(extraction.separatorId))
                    container.SplitPage(extraction.separatorId);

                Dictionary<string, List<HtmlNode>> groupedContainers = new Dictionary<string, List<HtmlNode>>();
                Navigate(container, extraction.navigation, ref groupedContainers);

                return groupedContainers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<HtmlNode> TraversePage(HtmlNode htmlPage, Navigation navigation)
        {
            try
            {
                Dictionary<string, List<HtmlNode>> groupedContainers = new Dictionary<string, List<HtmlNode>>();
                Navigate(htmlPage, navigation, ref groupedContainers);

                List<HtmlNode> nodes = new List<HtmlNode>();
                
                foreach(var nodeList in groupedContainers.Values)
                    nodes.AddRange(nodeList);

                return nodes;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //temp
        private string reconval = string.Empty;

        protected void Navigate(HtmlNode htmlNode, Navigation navigation, ref Dictionary<string, List<HtmlNode>> groupedContainers)
        {
            try
            {
                if (navigation == null) //Recursion ended
                { 
                    UpdateGroupedContainers(htmlNode, groupedContainers);
                    return;
                }

                if (navigation.recon != null)
                    ManageNavRecon(navigation, htmlNode);

                //If current navigation contains Id property find the node
                if (navigation.keyProperty.HasValue && navigation.keyProperty == HtmlAttr.Id && !string.IsNullOrEmpty(navigation.valueProperty))
                {
                    HtmlNode outNode = null;
                    htmlNode.FindNodeById(navigation.valueProperty, ref outNode);
                    if (outNode == null) outNode = htmlNode;
                    Navigate(outNode, navigation.nav, ref groupedContainers);
                }
                else
                {
                    //Check child tags
                    List<HtmlNode> nodes = htmlNode.Descendants(navigation.tag.Stringify()).ToList();

                    IEnumerable<HtmlAttribute> attrsToCheck;
                    foreach (HtmlNode node in nodes)
                    {
                        if (navigation.keyProperty != null)
                        {
                            //Check keyProperty/attribute name
                            if ((attrsToCheck = node.Attributes.AttributesWithName(navigation.keyProperty?.Stringify())).Any())
                                //Check keyPropertyVal/attribute value if any
                                if (!string.IsNullOrWhiteSpace(navigation.valueProperty))
                                {
                                    if ((attrsToCheck = attrsToCheck.Where(attrToCheck => navigation.valueProperty.Equals(attrToCheck.Value))).Any())
                                        Navigate(node, navigation.nav, ref groupedContainers);
                                }
                                else
                                    Navigate(node, navigation.nav, ref groupedContainers);
                        }
                        else
                            Navigate(node, navigation.nav, ref groupedContainers);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ManageNavRecon(Navigation navigation, HtmlNode node)
        {
            if (navigation.reconRelation == Relation.REPEATING)
            {
                List<HtmlNode> classifiedNodes = node.GetMatchingChildrenByTag(navigation.recon.tag.Stringify());

                foreach(HtmlNode classifiedNode in classifiedNodes)
                    ExtractClassificationInfo(navigation.recon, classifiedNode);
            }
            else
            {
                HtmlNode classifiedNode = node.GetMatchingChild(navigation.recon.tag.Stringify(),
                                                                navigation.recon.keyProperty?.Stringify(),
                                                                navigation.recon.valueProperty);

                ExtractClassificationInfo(navigation.recon, classifiedNode);
            }
        }

        private void ExtractClassificationInfo(Recon recon, HtmlNode classifiedNode)
        {
            Classification currClassification = null;

            if (classifiedNode != null)
            {
                string currReconHtmlValue = string.Empty;

                switch (recon.reconValue)
                {
                    case HtmlAttr.InnerText:
                        currClassification = recon.classification.FirstOrDefault(clsf => !string.IsNullOrWhiteSpace(clsf.GetClassificationValue(classifiedNode.InnerText)));
                        currReconHtmlValue = classifiedNode.InnerText;
                        break;
                    case HtmlAttr.Class:
                        HtmlAttribute classificationAttr = classifiedNode.Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Class.Stringify());
                        currClassification = recon.classification.FirstOrDefault(clsf => !string.IsNullOrWhiteSpace(clsf.GetClassificationValue(classificationAttr.Value)));
                        currReconHtmlValue = classificationAttr.Value;
                        break;
                }

                if (currClassification != null)
                {
                    if (recon.regex != null)
                        reconval = recon.regex.ApplyRegex<string>(currClassification.GetClassificationValue(currReconHtmlValue));
                    else
                        reconval = currClassification.GetClassificationValue(currReconHtmlValue);

                    SetEntityData(currClassification, reconval);
                }
            }
        }

        private void UpdateGroupedContainers(HtmlNode htmlNode, Dictionary<string, List<HtmlNode>> groupedContainers)
        {
            if (groupedContainers.ContainsKey((string)reconval))
                groupedContainers[(string)reconval].Add(htmlNode);
            else
                groupedContainers.Add((string)reconval, new List<HtmlNode>() { htmlNode });
        }

        private void SetEntityData(Classification currClassif, string value)
        {
            if (currClassif.isExclusive)
                this.exclusiveEntityData.Add(new Tuple<string, string>(currClassif.name, value));
            else
                this.globalEntityData.Add(currClassif.name, value);
        }

        public void Dispose()
        {
            this.globalEntityData = null;
            this.exclusiveEntityData = null;
            reconval = null;
        }
    }
}
