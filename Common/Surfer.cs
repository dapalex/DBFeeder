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

        public static async Task<string> FetchPage(string url, int fetchTime = 0)
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
                    foreach (Recon recon in navigation.recon)
                    {
                        switch (recon.reconRelation)
                        {
                            case Relation.REPEATING:
                                //Actual node used for navigation
                                HtmlNode currentNode = htmlNode.Clone();

                                do
                                {
                                    currentNode.ChildNodes.Clear();
                                    //Check first recon found
                                    bool reconFound = false;
                                    List<HtmlNode> childrenList = htmlNode.ChildNodes.ToList();
                                    foreach (var node in childrenList)
                                        if (!reconFound)
                                        {
                                            if (recon.IsEqual(node))
                                            {
                                                Utils.SwapChild(node, htmlNode, currentNode);
                                                ExtractClassificationInfo(recon, node);
                                                reconFound = true;
                                            }
                                            else
                                                htmlNode.ChildNodes.Remove(node);
                                        }
                                        else
                                            if (recon.IsEqual(node)) break;
                                        else
                                            Utils.SwapChild(node, htmlNode, currentNode);

                                    //With list of recon will navigate n times, correct?
                                    NavigateInner(currentNode, navigation, ref groupedContainers);
                                } while (htmlNode.ChildNodes.Where(cn => recon.IsEqual(cn)).Count() > 0);
                                break;
                            default:
                                HtmlNode classifiedNode = htmlNode.GetMatchingChild(recon.tag.Stringify(),
                                                                                recon.keyProperty?.Stringify(),
                                                                                recon.valueProperty);

                                if (classifiedNode == null)
                                    classifiedNode = htmlNode.GetMatchingDescendant(recon.tag.Stringify(),
                                                                                recon.keyProperty?.Stringify(),
                                                                                recon.valueProperty);

                                ExtractClassificationInfo(recon, classifiedNode);
                                break;
                        }
                    }

                NavigateInner(htmlNode, navigation, ref groupedContainers);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected void NavigateInner(HtmlNode htmlNode, Navigation navigation, ref Dictionary<string, List<HtmlNode>> groupedContainers)
        {
            try
            {
                //If current navigation contains Id property find the node
                if (navigation.keyProperty != null && navigation.keyProperty == HtmlAttr.Id && !string.IsNullOrEmpty(navigation.valueProperty))
                {
                    HtmlNode outNode = null;
                    outNode = htmlNode.FindNodeById(navigation.valueProperty);
                    if (outNode == null) outNode = htmlNode;
                    Navigate(outNode, navigation.nav, ref groupedContainers);
                }
                else
                {
                    //Check child tags
                    List<HtmlNode> nodes = htmlNode.GetMatchingDescendants(navigation.tag.Stringify(), navigation.keyProperty?.Stringify(), navigation.valueProperty).ToList();

                    foreach (HtmlNode node in nodes)
                        Navigate(node, navigation.nav, ref groupedContainers);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ExtractClassificationInfo(Recon recon, HtmlNode classifiedNode)
        {
            try
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
            } catch(Exception e)
            {
                throw e;
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
            try
            {
                if (currClassif.isExclusive)
                {
                    if (this.exclusiveEntityData.FirstOrDefault(ed => ed.Item1 == currClassif.name && ed.Item2 == value) == null)
                        this.exclusiveEntityData.Add(new Tuple<string, string>(currClassif.name, value));
                }
                else
                    this.globalEntityData.Add(currClassif.name, value);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Dispose()
        {
            this.globalEntityData = null;
            this.exclusiveEntityData = null;
            reconval = null;
        }
    }
}
