using Common;
using Common.AMQP;
using Common.Serializer;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Runtime.CompilerServices;
using System.Web;

namespace ScraperService
{
    internal class Scraper : IDisposable
    {
        Diver _diver = null;
        private readonly string ClassificationMISC = "MISC";
        ILogger _logger;

        public Scraper(ILogger logger)
        {
            _diver = new Diver();
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pageContent">HTML page source</param>
        /// <param name="extract">Model containing instructions for scraping</param>
        /// <returns>List of entity contents as dictionaries [field name, field value]</returns>
        internal List<Dictionary<string, string>> ScrapPage(string pageContent, Extraction extract)
        {
            try
            {
                HtmlNode? dummyContainer;
                var content = _diver.NavigateHtmlPage(pageContent, extract, out dummyContainer);
                if (content.Count > 0)
                    return RetrieveInfo(content, extract.target);
            }
            catch (Exception ex)
            {
                Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", pageContent);
                if (_logger != null) _logger.LogError(ex, ex.Message);
                return null;
            }

            return null;
        }

        /// <summary>
        ///Case 1 ==>> containers: 2  -- SIBLING
        ///                 |-------> container 1 = classification
        ///                 |-------> container 2 = target value
        ///Case 2 ==>> containers: 1 -- CHILD
        ///                 |-------> container 1 = target value
        ///                                |------->  container 2 = classification
        ///Case 3 ==>> containers: 1 -- PARENT
        ///                 |-------> container 1 = classification 
        ///                                |------->  container 2 = target value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="containers">Containers containing target information</param>
        /// <param name="target">Target object containing instructions to retrieve information</param>
        /// <returns>List of entity contents as dictionaries [field name, field value]</returns>
        internal List<Dictionary<string, string>> RetrieveInfo(Dictionary<string, List<HtmlNode>> containers, Target target)
        {
            List<Dictionary<string, string>> entities = new List<Dictionary<string, string>>();

            try
            {
                foreach (KeyValuePair<string, List<HtmlNode>> item in containers) //Loop through Navigation classification
                {

                    if (_logger != null) _logger.LogDebug(item.Key);

                    Dictionary<string, string> tEntityData = new Dictionary<string, string>();

                    item.Value.ForEach(targetContainer => //Loop through targets
                    {
                        try
                        {
                            string currValue = String.Empty;

                            HtmlNode targetNode = targetContainer;
                            if (target.tag != null)
                                targetNode = targetContainer.GetMatchingChild(target.tag.Value.Stringify(),
                                                                              target.keyProperty?.Stringify(),
                                                                              target.valueProperty);

                            switch (target.value)
                            {
                                case HtmlAttr.InnerText:
                                    currValue = targetNode.InnerText;
                                    break;
                                case HtmlAttr.Href:
                                    currValue = targetNode.Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Href.Stringify()).Value;
                                    break;
                                case HtmlAttr._SharpText:
                                    currValue = targetNode.ChildNodes.First(cn => cn.Name == target.value.Stringify()).InnerText;
                                    break;
                                case HtmlAttr.Table: //Table managed entirely here - previous logic ignored
                                                     //Generate Datatable from HTML table
                                    entities.AddRange(ExtractDataTable(targetNode, target, item.Key));

                                    return;
                                default:
                                    currValue = targetNode.InnerText;
                                    break;
                            }

                            if (target.recon != null)
                            {
                                HtmlNode reconNode = null;

                                switch (target.reconRelation)
                                {
                                    case Relation.SIBLING:
                                        reconNode = targetContainer.GetMatchingChild(target.recon.tag.Stringify(),
                                                                                     target.recon.keyProperty?.Stringify(),
                                                                                     target.recon.valueProperty);
                                        break;
                                    case Relation.CHILD:
                                        reconNode = targetNode.GetMatchingChild(target.recon.tag.Stringify(),
                                                                                target.recon.keyProperty?.Stringify(),
                                                                                target.recon.valueProperty);
                                        break;
                                }
                                string classification = null;

                                try
                                {
                                    if (reconNode != null)
                                        switch (target.recon.reconValue)
                                        {
                                            case HtmlAttr.InnerText:

                                                classification = target.recon.classification.FirstOrDefault(clsf => !string.IsNullOrWhiteSpace(clsf.GetClassificationValue(reconNode.InnerText)))?.name;
                                                break;
                                            default:
                                                if (_logger != null) _logger.LogWarning("Recon not found for target {0}", target.value);
                                                break;
                                        }
                                }
                                catch (Exception ex)
                                {
                                    if (_logger != null) _logger.LogWarning("Classification not recognized");
                                }

                                if (classification == null)
                                {
                                    classification = ClassificationMISC;

                                    if (tEntityData.ContainsKey(ClassificationMISC))
                                        tEntityData[ClassificationMISC] = string.Join(tEntityData[ClassificationMISC], classification);
                                    else
                                        tEntityData.Add(ClassificationMISC, currValue);
                                }
                                else
                                {
                                    //Clean eventual duplicated data in page
                                    if (tEntityData.ContainsKey(classification) && tEntityData[classification] == currValue)
                                        return;

                                    tEntityData.Add(classification, currValue);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", targetContainer.InnerText);
                            if (_logger != null) _logger.LogError(e, string.Format("Error for {0}", targetContainer.InnerHtml));
                        }
                    });

                    if (tEntityData.Count > 0)
                    {
                        UpdateGroupingEntityData(tEntityData, item.Key, target.HCValues);

                        entities.Add(tEntityData);
                    }
                }
            }
            catch (Exception e)
            {
                Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", containers.ToString());
                if (_logger != null) _logger.LogError(e, "Error during execution of RetrieveInfo!");
            }
            return entities;
        }

        private List<Dictionary<string, string>> ExtractDataTable(HtmlNode targetNode, Target target, string itemKey)
        {
            List<Dictionary<string, string>> entities = new List<Dictionary<string, string>>();
            //Generate Datatable from HTML table
            DataTable dt = targetNode.ParseTable();

            //Table is null -> not enough columns to fill the entity
            if (dt == null) return null;

            foreach (DataRow row in dt.Rows) //1 message for each row
            {
                try
                {
                    AMQPEntityMessage rowMessage = new AMQPEntityMessage();
                    Dictionary<string, string> rowEntityData = new Dictionary<string, string>();

                    foreach (Classification classificationKvp in target.recon.classification)
                    {
                        DataColumn[] columnsArray = new DataColumn[dt.Columns.Count];
                        row.Table.Columns.CopyTo(columnsArray, 0);
                        List<DataColumn> columnsList = columnsArray.ToList();

                        int classificationIndex = columnsList.FindIndex(c => classificationKvp.GetClassificationValue(c.ColumnName) != null);

                        if (classificationIndex < 0)
                        {
                            Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", string.Join(" - ", Array.ConvertAll<object, string>(row.ItemArray, o => o.ToString())));
                            string msg = string.Format("Classification {0} not found for {1}", classificationKvp.name, classificationKvp.findBy);
                            if (_logger != null) _logger.LogWarning(msg);
                        }
                        else
                            rowEntityData.Add(classificationKvp.name, (string)row[classificationIndex]);
                    }

                    UpdateGroupingEntityData(rowEntityData, itemKey, target.HCValues);
                    entities.Add(rowEntityData);
                }
                catch (Exception e)
                {
                    Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", string.Join(" - ", row.ItemArray));
                    if (_logger != null) _logger.LogError(e, string.Format("Error retrieving {0} information from html row {1}", target.name, string.Join(" - ", row.ItemArray)));
                }
            }

            if (_logger != null) _logger.LogDebug("{0} entities extracted", entities.Count);
            return entities;
        }

        private void UpdateGroupingEntityData(Dictionary<string, string> tEntityData, string itemKey, NameValue[] hcValues)
        {
            try
            {
                foreach (KeyValuePair<string, string> globalED in _diver.globalEntityData)
                    tEntityData.Add(globalED.Key, globalED.Value);
                foreach (Tuple<string, string> exclusiveED in _diver.exclusiveEntityData)
                    if (exclusiveED.Item2 == itemKey)
                        tEntityData.Add(exclusiveED.Item1, exclusiveED.Item2);
                if (hcValues != null)
                    foreach (NameValue hcValue in hcValues)
                        tEntityData.Add(hcValue.name, hcValue.value);


                if (_logger != null) _logger.LogDebug("Sending entity...");
                foreach (var kvp in tEntityData)
                    if (_logger != null) _logger.LogDebug(kvp.Key + ": " + kvp.Value);
            }
            catch (Exception e)
            {
                if (_logger != null) _logger.LogError(e, "Issue during add of grouping data: {0}", e.Message);
                throw e;
            }
        }

        public void Dispose()
        {
            _diver.Dispose();
            _diver = null;
        }
    }
}
