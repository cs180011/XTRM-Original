using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace XTRMlib
{
    static public class XTRMUtil
    {
        //static String odbcConnectionString = XTRMObject.getDictionaryEntry("ODBCConnectString");
        static public SqlConnection connectXDB(string connectString = "")
        {
            SqlConnection thisConnection = null;
            if (connectString.Equals(""))
            {
                connectString = XTRMObject.getDictionaryEntry("TaskConnectString");
            }
            try
            {
                thisConnection = new SqlConnection(connectString);
            }
            catch (SqlException) { }
            catch (Exception) { }

            return thisConnection;
        }
        static public int GetWordCount(String strText, String strTerm = null)
        {
            //Convert the string into an array of words
            string[] source = strText.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var matchQuery = from word in source
                             select word;

            // Create and execute the query. It executes immediately 
            // because a singleton value is produced.
            // Use ToLowerInvariant to match "data" and "Data" 
            if (strTerm != null)
            {
                matchQuery = from word in source
                             where word.ToLowerInvariant() == strTerm.ToLowerInvariant()
                             select word;
            }

            // Count the matches.
            return matchQuery.Count();
        }
        static public MatchCollection GetRegexMatches(string input, string pattern)
        {
            MatchCollection matches = null;
            try
            {
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

                matches = rgx.Matches(input);
            }
            catch (Exception)
            {
            }
            return matches;
        }
        static public string NormalizeXMLText(string strIN)
        {
            string strXMLout = strIN;
            strXMLout = Regex.Replace(strXMLout, @"&amp;", "&");
            strXMLout = Regex.Replace(strXMLout, @"&amp;", "&");
            strXMLout = Regex.Replace(strXMLout, @"&gt;", ">");
            strXMLout = Regex.Replace(strXMLout, @"&lt;", "<");
            strXMLout = Regex.Replace(strXMLout, @"&quot;", "\"");
            strXMLout = Regex.Replace(strXMLout, @"&apos;", "'");
            return strXMLout;
        }
    }
}
