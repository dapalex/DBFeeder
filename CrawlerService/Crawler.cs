using Common;
using Common.Serializer;
using HtmlAgilityPack;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Linq;


namespace CrawlerService
{
    internal class Crawler : IDisposable
    {
        private Extraction _extraction;
        private Diver _diver;
        private ILogger _logger;

        public Crawler(Extraction extract, ILogger logger = null) 
        { 
            this._extraction = extract;
            this._logger = logger;
            _diver = new Diver();
        }

        internal HtmlNode NavigateHtmlPage(string htmlPage, Extraction _extraction)
        {
            HtmlNode outContainer = null;
            _diver.NavigateHtmlPage(htmlPage, _extraction, out outContainer);
            return outContainer;
        }

        internal List<string> ExtractUrls(string htmlPage) 
        {
            List<HtmlNode> nodes = new List<HtmlNode>();
            HtmlNode outContainer = null;

            var content = _diver.NavigateHtmlPage(htmlPage, _extraction, out outContainer);

            foreach (IEnumerable<HtmlNode> nodeList in content.Select(gc => gc.nodes))
                nodes.AddRange(nodeList);

            return CrawlUrls(nodes, _extraction.target, _extraction.urlBase);
        }

        internal List<string> CrawlUrls(List<HtmlNode> containers, Target target, string baseUrl)
        {
            List<string> urls = new List<string>();
            
            containers.ForEach(container =>
            {
                if(target.recon != null)
                {
                    if (!target.recon.CheckRecon(container)) return;
                }
                var detailContainers = container.ChildNodes.Where(n => n.Name == target.tag.Value.Stringify());

                foreach (HtmlNode node in detailContainers)
                {
                    HtmlAttribute currentAttr = null;

                    if ((currentAttr = node.Attributes.FirstOrDefault(attr => attr.Name == target.value.Stringify())) != null)
                    {
                        string parsedValue = target.regex != null ? target.regex.ApplyRegex<string>(currentAttr.Value) : currentAttr.Value;
                        urls.Add(Utils.GetWellformedUrlString(baseUrl, parsedValue));
                    }
                }
            });

            return urls;
        }

        internal string FindNext(string currentUrl, List<Next> next, ref HtmlNode container, CrawlProgress crawlProgress, string baseUrl)
        {
            if (next == null)
            {
                if(_logger != null) _logger.LogWarning("No next pages");
                return null;
            }

            var nexts = next.OrderBy(n => n.level);
            List<string> nextRetUrlSuffixes = new List<string>();

            foreach (var nextObj in nexts)
            {
                if(_logger != null) _logger.LogDebug("Finding next page using level {1}", nextObj.name, nextObj.level);
                HtmlNode? nextContainer = container;

                //Traverse html page using navigation if any
                if (nextObj.navigation != null)
                {
                    if(_logger != null) _logger.LogDebug("Traversing container using navigation");
                    nextContainer = _diver.TraversePage(nextContainer, nextObj.navigation).FirstOrDefault();
                }
                if (nextContainer == null)
                {
                    if(_logger != null) _logger.LogWarning("Container not found during page traversal for next");
                    continue;
                }

                if (nextObj.recon != null)
                {
                    if(_logger != null) _logger.LogDebug("Finding html node using recon");
                    nextContainer = nextContainer.GetMatchingChild(nextObj.recon.tag?.Stringify(),
                                                                   nextObj.recon.keyProperty?.Stringify(),
                                                                   nextObj.recon.valueProperty);
                }

                if (nextContainer == null)
                {
                    if(_logger != null) _logger.LogWarning("Recon not found for next page");
                    continue;
                }

                if (_logger != null) _logger.LogDebug("Extracting urls from node container {0}", nextContainer.Name);
                //var detailContainers = nextContainer.ChildNodes.Where(n => n.Name == nextObj.tag.Value.Stringify());
                var detailContainers = nextContainer.GetMatchingChildrenByTag(nextObj.tag.Value.Stringify());
                HtmlAttribute? currentAttr = null;

                foreach (HtmlNode node in detailContainers)
                {
                    currentAttr = node.Attributes.FirstOrDefault(attr => attr.Name == nextObj.value.Stringify() &&
                            !crawlProgress.crawled.Contains(Utils.GetWellformedUrlString(baseUrl, attr.Value)));

                    if (currentAttr != null)
                    {
                        nextRetUrlSuffixes.Add(currentAttr.Value);
                        if(_logger != null) _logger.LogDebug("Next page found");
                        break;
                    }
                }
            }
            crawlProgress.checkedUrls.Add(currentUrl);

            nextRetUrlSuffixes.Sort();
            if (nextRetUrlSuffixes.Count > 0)
                return Utils.GetWellformedUrlString(baseUrl, nextRetUrlSuffixes.First());

            if (_logger != null) _logger.LogWarning("No next page to crawl, getting last fetched...");

            return crawlProgress.crawled.Where(crw => !crawlProgress.checkedUrls.Contains(crw)).Last();
        }

        public void Dispose()
        {
            _extraction = null;
            _diver.Dispose();
            _diver = null;
        }
    }
}
