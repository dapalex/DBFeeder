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
        internal List<Dictionary<string, string>> RetrieveInfo(List<Container> containers, Target target)
        {
            List<Dictionary<string, string>> entities = new List<Dictionary<string, string>>();

            try
            {
                foreach (Container targetContainer in containers) //Loop through Navigation classification
                {
                    Dictionary<string, string> tEntityData = new Dictionary<string, string>();



                    //TEMP - no element to recognize - take the classification as it is (expected 1)
                    if (target.IsSingleStandingTarget())
                    {
                        foreach (HtmlNode node in targetContainer.nodes)
                        {
                            string classification = target.recon.classification[0].name;
                            string currValue = target.GetTargetValue(node);
                            tEntityData.Add(classification, currValue);

                            UpdateGroupingEntityData(tEntityData, targetContainer.recons, target.HCValues);
                            entities.Add(tEntityData);
                            tEntityData = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        targetContainer.nodes.ForEach(targetNode => {
                            try
                            {
                                if (target.value == HtmlAttr.Table) //Table managed entirely here - previous logic ignored
                                                                    //Generate Datatable from HTML table
                                {
                                    entities.AddRange(ExtractDataTable(targetNode, target, targetContainer.recons));
                                    return;
                                }

                                string currValue = target.GetTargetValue(targetNode);

                                string classification = target.GetTargetClassification(targetNode);

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
                            catch (Exception e)
                            {
                                Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", targetNode.InnerText);
                                if (_logger != null) _logger.LogError(e, string.Format("Error for {0}", targetNode.InnerHtml));
                            }
                        }
                        );

                        if (tEntityData.Count > 0)
                        {
                            UpdateGroupingEntityData(tEntityData, targetContainer.recons, target.HCValues);
                            entities.Add(tEntityData);
                        }
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

        private List<Dictionary<string, string>> ExtractDataTable(HtmlNode targetNode, Target target, Dictionary<string, string> reconKeys)
        {
            List<Dictionary<string, string>> entities = new List<Dictionary<string, string>>();
            //Generate Datatable from HTML table
            DataTable dt = targetNode.ParseTable();

            //Table is null or too small -> not enough columns to fill the entity
            if (dt == null || dt.Columns.Count < 4) return null;

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

                    UpdateGroupingEntityData(rowEntityData, reconKeys, target.HCValues);
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

        private void UpdateGroupingEntityData(Dictionary<string, string> tEntityData, Dictionary<string, string> itemRecons, NameValue[] hcValues)
        {
            try
            {
                foreach (KeyValuePair<string, string> globalED in _diver.globalEntityData)
                    tEntityData.Add(globalED.Key, globalED.Value);

                foreach (var itemRecon in itemRecons)
                    tEntityData.Add(itemRecon.Key, itemRecon.Value);

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
