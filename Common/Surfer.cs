using Common.Properties;
using Common.Serializer;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging.Abstractions;
using System.Xml;
using System.Xml.Linq;

namespace Common
{
    public class Container
    {
        public HtmlNode node;
        public Dictionary<string, string> recons;
    }


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

        public Diver()
        {
            globalEntityData = new Dictionary<string, string>();
        }

        public List<Container> NavigateHtmlPage(string htmlPage, Extraction extraction, out HtmlNode container)
        {
            try
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlPage);

                container = htmlDoc.DocumentNode.SelectSingleNode(Resources.HtmlBodyXpath);

                if (!string.IsNullOrEmpty(extraction.separatorId))
                    container.SplitPage(extraction.separatorId);

                List<Container> containers = new List<Container>();
                Navigate(container, extraction.navigation, ref containers);

                return containers;
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
                List<Container> containers = new List<Container>();
                Navigate(htmlPage, navigation, ref containers);

                List<HtmlNode> nodes = new List<HtmlNode>();

                foreach (IEnumerable<HtmlNode> nodeList in containers.Select(gc => gc.node))
                    nodes.AddRange(nodeList);

                return nodes;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal Dictionary<string, string> currentRecons = new Dictionary<string, string>();

        protected void Navigate(HtmlNode htmlNode, Navigation navigation, ref List<Container> containers)
        {
            try
            {
                if (navigation == null) //Recursion ended
                {
                    Dictionary<string, string> reconsDict = new Dictionary<string, string>(currentRecons);
                    containers.Add(new Container() { node = htmlNode, recons = reconsDict });
                    return;
                }

                //Actual node used for navigation
                HtmlNode currentNode = htmlNode.Clone();
                HtmlNode originalNode = htmlNode.Clone();
                if (navigation.recon != null) currentNode.ChildNodes.Clear();
                bool repeatingChunkExtracted = false;
                bool reconFound = false;
                string reconValue = string.Empty;

                foreach (var childNode in htmlNode.ChildNodes)
                {
                    if (navigation.recon != null)
                    {
                        foreach (Recon recon in navigation.recon)
                        {
                            switch (recon.reconRelation)
                            {
                                case Relation.REPEATING:
                                    if (recon.IsEqual(childNode))
                                    {
                                        if (currentRecons.Count > 0 && currentRecons.ContainsKey(recon.classification[0].name) && currentNode.ChildNodes.Count > 0)
                                        {
                                            repeatingChunkExtracted = true;
                                            break;
                                        }
                                        else
                                        {
                                            Utils.SwapChild(childNode, originalNode, currentNode);
                                            reconValue = ExtractClassificationInfo(recon, childNode);

                                            if (currentRecons.ContainsKey(recon.classification[0].name))
                                                currentRecons.Remove(recon.classification[0].name);

                                            currentRecons.Add(recon.classification[0].name, reconValue);
                                            reconFound = true;
                                        }
                                    }
                                    break;
                                default:
                                    HtmlNode classifiedNode = currentNode.GetMatchingChild(recon.tag.Stringify(),
                                                                                    recon.keyProperty?.Stringify(),
                                                                                    recon.valueProperty);

                                    if (classifiedNode == null)
                                        classifiedNode = currentNode.GetMatchingDescendant(recon.tag.Stringify(),
                                                                                    recon.keyProperty?.Stringify(),
                                                                                    recon.valueProperty);

                                    reconValue = ExtractClassificationInfo(recon, classifiedNode);
                                    currentRecons.Add(recon.classification[0].name, reconValue);

                                    originalNode.ChildNodes.Clear();
                                    break;
                            }
                        }

                        if (repeatingChunkExtracted)
                            break;

                        if (!reconFound)
                            Utils.RemoveSameChild(childNode, originalNode);
                        else
                            Utils.SwapChild(childNode, originalNode, currentNode);

                    }
                }
                //Check child tags
                List<HtmlNode> childNodes = currentNode.GetMatchingDescendants(navigation.tag.Stringify(), navigation.keyProperty?.Stringify(), navigation.valueProperty).ToList();

                //Delve into the children
                foreach (HtmlNode childNode in childNodes)
                    Navigate(childNode, navigation.nav, ref containers);

                //If partial delving, repeat
                if (navigation.recon != null && originalNode.ChildNodes.Count() > 0)
                {
                    Navigate(originalNode, navigation, ref containers);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string ExtractClassificationInfo(Recon recon, HtmlNode classifiedNode)
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
                        string reconValue;

                        if (recon.regex != null)
                            reconValue = recon.regex.ApplyRegex<string>(currClassification.GetClassificationValue(currReconHtmlValue));
                        else
                            reconValue = currClassification.GetClassificationValue(currReconHtmlValue);

                        return reconValue;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Dispose()
        {
            this.globalEntityData = null;
            this.currentRecons.Clear();
            this.currentRecons = null;
        }
    }
}
