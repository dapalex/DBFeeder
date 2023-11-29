using Common.Properties;
using Common.Serializer;
using HtmlAgilityPack;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;

namespace Common
{
    public static class HAPHtmlNodeExtensions
    {
        //
        // Summary:
        //     Get descendant node with matching id
        //
        // Parameters:
        //   name:
        public static void FindNodeById(this HtmlNode node, string id, ref HtmlNode outNode)  //NOT thread-safe! -> outNode
        {
            if (outNode != null)
                return;

            foreach (HtmlNode item in node.ChildNodes)
            {
                if (item.Id != null && item.Id.Equals(id))
                {
                    outNode = item;
                    return;
                }
                else
                {
                    if (item.ChildNodes.Count > 0)
                        item.FindNodeById(id, ref outNode);
                }
            }


        }

        // Summary:
        //     Get descendant node with matching id
        // Parameters:
        //   name:
        public static HtmlNode FindNodeById(this HtmlNode node, string id)  //thread-safe
        {
            foreach (HtmlNode item in node.ChildNodes)
            {
                if (item.Id != null && item.Id.Equals(id))
                    return item;
                else
                {
                    HtmlNode outNode = null;

                    if (item.ChildNodes.Count > 0)
                        outNode = item.FindNodeById(id);

                    if (outNode != null)
                        return outNode;
                }
            }

            return null;
        }

        public static void SplitPage(this HtmlNode node, string separateStart, string separateEnd = null) //thread-safe
        {
            node.InnerHtml = node.InnerHtml.Remove(0, node.InnerHtml.IndexOf(separateStart));
            if (separateEnd != null)
                node.InnerHtml = node.InnerHtml.Remove(0, node.InnerHtml.IndexOf(separateEnd));
        }

        public static List<HtmlNode> GetMatchingChildrenByTag(this HtmlNode htmlNode, string tag)  //thread-safe
        {
            List<HtmlNode> retChildren = new List<HtmlNode>();

            foreach (var childNode in htmlNode.ChildNodes)
            {
                if (childNode.Name == tag)
                    retChildren.Add(childNode);
            }

            return retChildren;
        }

        public static HtmlNode GetMatchingChild(this HtmlNode htmlNode, string tag, string keyProperty = null, RegexString valueProperty = null)  //thread-safe
        {
            foreach (var childNode in htmlNode.ChildNodes)
            {
                if (childNode.Name == tag)
                    if (!string.IsNullOrWhiteSpace(keyProperty))
                    {
                        foreach (var attr in childNode.Attributes)
                            if (attr.Name == keyProperty &&
                                (!string.IsNullOrEmpty(valueProperty) ? valueProperty.Equals(attr.Value) : true))
                                return childNode;
                    }
                    else
                        return childNode;
            }

            return null;
        }

        public static HtmlNode GetMatchingDescendant(this HtmlNode htmlNode, string tag, string keyProperty = null, RegexString valueProperty = null)  //thread-safe
        {
            foreach (var childNode in htmlNode.Descendants(tag))
            {
                if (childNode.Name == tag)
                    if (!string.IsNullOrWhiteSpace(keyProperty))
                    {
                        foreach (var attr in childNode.Attributes)
                            if (attr.Name == keyProperty &&
                                (!string.IsNullOrEmpty(valueProperty) ? valueProperty.Equals(attr.Value) : true))
                                return childNode;
                    }
                    else
                        return childNode;
            }

            return null;
        }

        public static List<HtmlNode> GetMatchingDescendants(this HtmlNode htmlNode, string tag, string keyProperty = null, RegexString valueProperty = null)  //thread-safe
        {
            List<HtmlNode> retList = new List<HtmlNode>();

            foreach (var childNode in htmlNode.Descendants(tag))
            {
                if (childNode.Name == tag)
                    if (!string.IsNullOrWhiteSpace(keyProperty))
                    {
                        foreach (var attr in childNode.Attributes)
                            if (attr.Name == keyProperty &&
                                (!string.IsNullOrEmpty(valueProperty) ? valueProperty.Equals(attr.Value) : true))
                                retList.Add(childNode);
                    }
                    else
                        retList.Add(childNode);
            }

            return retList;
        }

        /// <summary>
        /// Transforms a simple table (single header) in DataTable
        /// </summary>
        /// <param name="htmlNode"></param>
        public static DataTable ParseTable(this HtmlNode htmlNode)  //thread-safe
        {
            try
            {
                DataTable dt = new DataTable();
                List<HtmlNode> headers;
                List<HtmlNode> rows;
                HtmlNode headerTable = htmlNode.GetMatchingChild(HtmlAttr.THead.Stringify());

                if (headerTable != null)
                {
                    headers = headerTable.GetMatchingChildrenByTag(HtmlAttr.Tr.Stringify());
                    HtmlNode bodyTable = htmlNode.GetMatchingChild(HtmlAttr.TBody.Stringify());
                    rows = bodyTable.GetMatchingChildrenByTag(HtmlAttr.Tr.Stringify());
                }
                else
                {
                    headerTable = htmlNode.GetMatchingChild(HtmlAttr.TBody.Stringify());

                    //Vertical header cells are considered as data cells
                    headers = headerTable.GetMatchingChildrenByTag(HtmlAttr.Tr.Stringify())
                                        .Where(child => child.ChildNodes.All(cn => cn.Name == HtmlAttr.Th.Stringify() || cn.Name == HtmlAttr._SharpText.Stringify())).ToList();

                    //rows = headerTable.ChildNodes.TakeLast(headerTable.ChildNodes.Count - headers.Count).ToList();

                    rows = headerTable.GetMatchingChildrenByTag(HtmlAttr.Tr.Stringify())
                                        .Where(child => child.ChildNodes.Any(cn => cn.Name == HtmlAttr.Td.Stringify())).ToList();
                }

                DataTableUtil dtu = new DataTableUtil();
                dtu.UpdateDataTable(dt, headers, TableSection.HEADER);
                dtu.UpdateDataTable(dt, rows, TableSection.BODY);

                return dt;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal class DataTableUtil
        {
            internal void UpdateDataTable(DataTable dt, List<HtmlNode> rows, TableSection tableSection)  
            {
                List<DataColumn> columns = new List<DataColumn>();
                ///key: column position
                ///value: number of spans
                Dictionary<int, int> rowsSpanned = new Dictionary<int, int>();

                for (int i = 0; i < rows.Count; i++) //Cycle through source rows
                {
                    try
                    {
                        List<HtmlNode> inputColumns = null;
                        switch (tableSection)
                        {
                            case TableSection.HEADER:
                                inputColumns = rows[i].ChildNodes.Where(cn => cn.Name == HtmlAttr.Th.Stringify()).ToList();
                                break;
                            case TableSection.BODY:
                                inputColumns = rows[i].ChildNodes.Where(cn => cn.Name == HtmlAttr.Th.Stringify() || cn.Name == HtmlAttr.Td.Stringify()).ToList();
                                break;
                        }

                        int spannedColumns = 0;
                        int headerColumnIndex = 0;
                        int currentColumn = 0;

                        DataRow dr = null;

                        if (tableSection == TableSection.BODY)
                            dr = dt.NewRow();

                        for (int j = 0; j < inputColumns.Count; j++) //Cycle through source columns
                        {
                            try
                            {
                                int currColspan = 1;
                                int currRowspan = 1;

                                //----COLSPAN MANAGEMENT----
                                HtmlAttribute? colspanAttr = inputColumns[j].Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Colspan.Stringify());
                                if (colspanAttr != null) currColspan = int.Parse(colspanAttr.Value);

                                //----ROWSPAN MANAGEMENT----
                                HtmlAttribute? rowspanAttr = inputColumns[j].Attributes.FirstOrDefault(attr => attr.Name == HtmlAttr.Rowspan.Stringify());
                                if (rowspanAttr != null) currRowspan = int.Parse(rowspanAttr.Value);

                                //----COLSPAN MANAGEMENT----
                                for (int k = 0; k < currColspan; k++) //Cycle through column spans
                                {
                                    try
                                    {
                                        currentColumn = spannedColumns + headerColumnIndex + k;

                                        //----ROWSPAN MANAGEMENT----
                                        while (rowsSpanned.ContainsKey(currentColumn))
                                        {
                                            if (rowsSpanned[currentColumn] > 1)
                                                rowsSpanned[currentColumn]--;
                                            else
                                                rowsSpanned.Remove(currentColumn);

                                            switch (tableSection)
                                            {
                                                case TableSection.BODY:
                                                    dr[currentColumn] = dt.Rows[i - 1][currentColumn];
                                                    break;
                                            }

                                            spannedColumns++;
                                            currentColumn++;
                                        }
                                        if (currRowspan > 1)
                                            rowsSpanned.Add(currentColumn, currRowspan - 1);

                                        switch (tableSection)
                                        {
                                            case TableSection.HEADER:
                                                string headerText = inputColumns[j].InnerText;
                                                if (tableSection == TableSection.HEADER && currColspan > 1 && currRowspan == rows.Count)
                                                {   //got a situation here
                                                    int piece = inputColumns[j].InnerText.Length / currColspan;
                                                    int start = piece * k;
                                                    headerText = inputColumns[j].InnerText.Substring(start, piece);
                                                }

                                                if (columns.Count < currentColumn + 1)
                                                    columns.Add(new DataColumn());

                                                if(!Utils.ContainsText(columns[currentColumn].ColumnName, headerText)) //Avoid rewriting footer headers
                                                    columns[currentColumn].ColumnName += SetHeaderName(headerText);
                                                break;
                                            case TableSection.BODY:
                                                dr[currentColumn] = SetCellValue(inputColumns[j].InnerText);
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        //Exception during cell parse
                                        throw e;
                                    }
                                }
                                headerColumnIndex += currColspan;

                            }
                            catch (Exception e)
                            {
                                //Exception during column parse
                                throw e;
                            }
                        }

                        switch (tableSection)
                        {
                            case TableSection.BODY:
                                //THE REMAINING VALUES HERE HAVE TO BE TAKEN FROM PREVIOUS ROW
                                while (currentColumn < dt.Columns.Count - 1)
                                {
                                    currentColumn++;
                                    if (rowsSpanned.ContainsKey(currentColumn))
                                    {
                                        if (rowsSpanned[currentColumn] > 1)
                                            rowsSpanned[currentColumn]--;
                                        else
                                            rowsSpanned.Remove(currentColumn);
                                        dr[currentColumn] = dt.Rows[i - 1][currentColumn];
                                    }
                                    else
                                        dr[currentColumn] = string.Empty;
                                }

                                dt.Rows.Add(dr);
                                break;
                        }

                    }
                    catch (Exception e)
                    {
                        //Exception during column parse
                        throw e;
                    }
                }
                //VALUES CLEANING
                switch (tableSection)
                {
                    case TableSection.HEADER:
                        columns.ForEach(col =>
                        {
                            if (col.ColumnName.EndsWith(Resources.THeaderSeparator))
                                col.ColumnName = col.ColumnName.Substring(0, col.ColumnName.Length - 1);
                        });

                        dt.Columns.AddRange(columns.ToArray());
                        break;
                }
            }
        }

        private static string SetHeaderName(string innerText) //thread-safe
        {
            return string.Format("{0}{1}", SetCellValue(HttpUtility.HtmlDecode(innerText)), Resources.THeaderSeparator);
        }

        private static string SetCellValue(string innerText) //thread-safe
        {
            string retString = Regex.Replace(HttpUtility.HtmlDecode(innerText).Trim().Replace("\r", String.Empty).Replace("\n", string.Empty), "&#[0-9]+;[a-z0-9]?&#[0-9]+;", "");
            retString = Regex.Replace(retString, "&#[0-9]+;", string.Empty);
            return retString;
        }
    }
}
