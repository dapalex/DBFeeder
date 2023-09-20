using Common;
using Common.Serializer;
using HtmlAgilityPack;

namespace CrawlerService
{
    internal class Crawler : IDisposable
    {
        private Extraction _extraction;
        private Diver _diver;
        private ILogger _logger;

        public Crawler(Extraction extract, ILogger logger)
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

            foreach (var nodeList in content.Values)
                nodes.AddRange(nodeList);

            return CrawlUrls(nodes, _extraction.target, _extraction.urlBase);
        }

        internal List<string> CrawlUrls(List<HtmlNode> containers, Target target, string baseUrl)
        {
            List<string> urls = new List<string>();

            containers.ForEach(container =>
            {
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

        internal string FindNext(List<Next> next, ref HtmlNode container, List<string> fetched, string baseUrl)
        {
            if (next == null)
            {
                _logger.LogWarning("No next pages");
                return null;
            }

            var nexts = next.OrderBy(n => n.level);
            List<string> nextRetUrlSuffixes = new List<string>();

            foreach (var nextObj in nexts)
            {
                _logger.LogDebug("Finding next page using level {0}", nextObj.level);
                HtmlNode? nextContainer = container;

                //Traverse html page using navigation if any
                if (nextObj.navigation != null)
                {
                    _logger.LogDebug("Traversing container using navigation");
                    nextContainer = _diver.TraversePage(nextContainer, nextObj.navigation).FirstOrDefault();
                }
                if (nextContainer == null)
                {
                    _logger.LogWarning("Container not found during page traversal for next");
                    continue;
                }

                if (nextObj.recon != null)
                {
                    _logger.LogDebug("Finding html node using recon");
                    nextContainer = nextContainer.GetMatchingChild(nextObj.recon.tag.Stringify(),
                                                                   nextObj.recon.keyProperty?.Stringify(),
                                                                   nextObj.recon.valueProperty);
                }

                _logger.LogDebug("Extracting urls from node container {0}", nextContainer.Name);
                var detailContainers = nextContainer.ChildNodes.Where(n => n.Name == nextObj.tag.Value.Stringify());
                HtmlAttribute? currentAttr = null;

                foreach (HtmlNode node in detailContainers)
                {
                    currentAttr = node.Attributes.FirstOrDefault(attr => attr.Name == nextObj.value.Stringify() &&
                            !fetched.Contains(Utils.GetWellformedUrlString(baseUrl, attr.Value)));

                    if (currentAttr != null)
                    {
                        _logger.LogDebug("Next page found");
                        break;
                    }
                }
                if (currentAttr != null)
                    nextRetUrlSuffixes.Add(currentAttr.Value);
            }

            nextRetUrlSuffixes.Sort();
            if (nextRetUrlSuffixes.Count > 0)
                return Utils.GetWellformedUrlString(baseUrl, nextRetUrlSuffixes.First());

            _logger.LogWarning("No next pages");
            return null;
        }

        public void Dispose()
        {
            _extraction = null;
            _diver.Dispose();
            _diver = null;
        }
    }
}
