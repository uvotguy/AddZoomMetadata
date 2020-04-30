using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace AddZoomMetadata
{
    public static class Utilities
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("file");

        public static void logInfo(NLog.LogLevel lvl,
                                   string msg)
        {
            logger.Log(lvl, msg);
        }

        // Kaltura timestamps are standard UNIX timestamps.
        public static DateTime unixToDotNetTime(int unixTime)
        {
            return new DateTime(1970, 1, 1).AddSeconds(unixTime).ToLocalTime();
        }

        public static int dotNetToUnixTime(DateTime dotNetTime)
        {
            DateTime unixRef = new DateTime(1970, 1, 1).ToLocalTime();
            TimeSpan ts = dotNetTime - unixRef;
            return (int)ts.TotalSeconds;
        }

        public static string formatExceptionString(Exception ex)
        {
            string stack = "";
            if (ex != null)
            {
                stack = ex.StackTrace;
            }
            string ret = "";
            while (ex != null)
            {
                ret += ex.Message + "\n";
                ex = ex.InnerException;
            }
            ret += stack;

            return ret;
        }

        public static void writeXmlFile(string dateStr, int pageNumber, int numberOfPages, List<string> lstXml)
        {
            string appName = Environment.GetCommandLineArgs()[0].Replace(".exe", "");
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filename = Path.Combine(homeDir,
                                           appName,
                                           string.Format("{0}_CustomMetadata_{1}of{2}.xml", dateStr, pageNumber, numberOfPages));
            StreamWriter sw = new StreamWriter(filename);
            sw.Write("<mrss version=\"1.0\"><channel>");
            foreach (string xml in lstXml) sw.Write(xml);
            sw.Write("</channel></mrss>");
            sw.Close();
            string msg = string.Format("\nXML file saved:  {0}", filename);
            Utilities.logInfo(LogLevel.Info, msg);
        }
    }
}
