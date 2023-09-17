using Common.Properties;
using Docker.DotNet.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using System.Data;
using Resources = Common.Properties.Resources;

namespace Common
{
    public static class Persistence
    {
        private static SqliteConnection sqlConn;

        public static bool CreateConnection()
        {
            if (sqlConn == null)
                sqlConn = new SqliteConnection(Resources.SQLiteConnectionString);
            try
            {
                if (sqlConn.State == ConnectionState.Broken || sqlConn.State == ConnectionState.Closed)
                    sqlConn.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return true;
        }

        public static object insertCrawlLock = new object();

        internal static int InsertCrawlProgress(string source, string url)
        {
            lock (insertCrawlLock)
            {
                using (var tr = sqlConn.BeginTransaction())
                {
                    using (SqliteCommand sqlite_cmd = sqlConn.CreateCommand())
                    {
                        sqlite_cmd.Transaction = tr;
                        try
                        {
                            string nowTime = string.Join(" ", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());

                            SqliteParameter sourceParam = new SqliteParameter("source", source);
                            SqliteParameter urlParam = new SqliteParameter("url", url);
                            SqliteParameter nowTimeParam = new SqliteParameter("nowTime", nowTime);
                            sqlite_cmd.Parameters.AddRange(new SqliteParameter[3] { sourceParam, urlParam, nowTimeParam });

                            sqlite_cmd.CommandText = "INSERT INTO URL_CRAWLED (source, url, crawl_date) VALUES(@source,@url,@nowTime);";

                            Console.WriteLine(string.Format("Inserting  {0}", url));
                            sqlite_cmd.ExecuteNonQuery();
                            tr.Commit();

                            return 0;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
            }

            return -1;
        }

        public static CrawlProgress ReadCrawlProgress(string source)
        {
            CrawlProgress currentCrawlProgress = new CrawlProgress(source);

            try
            {
                using (SqliteCommand sqlite_cmd = sqlConn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "SELECT source, url FROM url_crawled WHERE source = \'" + source + "\'";

                    using (SqliteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader())
                    {
                        while (sqlite_datareader.Read())
                        {
                            currentCrawlProgress.fetched.Add(sqlite_datareader.GetString(1));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ex.Source = string.Format("Error reading crawl progresses");
                throw ex;
            }

            return currentCrawlProgress;
        }
    }

    public class CrawlProgress
    {
        public string source;
        public List<string> fetched;

        public CrawlProgress(string source)
        {
            this.source = source;
            this.fetched = new List<string>();
        }

        public void UpdateProgress(string url)
        {
            //Write to DB
            Persistence.InsertCrawlProgress(source, url);
            //Add in-memory
            this.fetched.Add(url);
        }
    }
}
