using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace XTRMlib
{
    public class XTRMObject : XTRMBase
    {
        public static SqlConnection XTRMTASK = null;
        public static bool isTaskConnected { private set; get; }
        protected int pState { set; get; }

        public static int exCount_XML = 0;

        protected string changeUser { get; set; }
        protected string changeDate { get; set; }
        protected string changeTag { get; set; }
        protected int changeState { get; set; }

        public XTRMObject()
        {
            isTaskConnected = false;
            pState = -1;
            try
            {
                if (XTRMTASK != null)
                {
                    if (XConnection.State == ConnectionState.Open)
                    {
                        isTaskConnected = true;
                    }
                    else
                    {

                    }
                }
                else
                {
                    XTRMTASK = connect("XTRMTASK");
                    if (XTRMTASK != null)
                    {
                        isTaskConnected = true;
                        pState = 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                pState = -1;
                XLogger(2500, ex.ErrorCode, string.Format("Connection={0}; Message={1}", strName, ex.Message));
            }
            catch (Exception ex)
            {
                pState = -1;
                XLogger(2501, -1, string.Format("Connection={0}; Message={1}", strName, ex.Message));
            }
        }
        public void Close()
        {
            if (XConnection != null)
            {
                // Close the Connection!
                try
                {
                    if (XConnection.State == ConnectionState.Open)
                    {
                        XConnection.Close();
                    }
                }
                catch (SqlException ex)
                {
                    XConnection = null;
                    XLogger(2502, ex.ErrorCode, string.Format("Close Connection; Message={0}", ex.Message));

                }
                catch (Exception ex)
                {
                    XConnection = null;
                    XLogger(2503, -1, string.Format("Close Connection; Message={0}", ex.Message));
                }
            }
        }
        /*
        public bool Connect()
        {
            bool rc = true;
            if (pState < 0)
            {
                try
                {
                    isConnected = false;
                    pState = -1;
                    XConnection = connect();
                    if (XConnection != null)
                    {
                        isConnected = true;
                        pState = 0;
                    }
                }
                catch (SqlException ex)
                {
                    XLogger(2504, ex.ErrorCode, string.Format("Connect(); Message={0}", ex.Message));
                }
                catch (Exception ex)
                {
                    XLogger(2505, -1, string.Format("Connect(); Message={0}", ex.Message));
                }
            }
            return rc;
        }
        */
        public virtual int renderXML(bool bDeep = false)
        {
            return 0;
        }
        public virtual int Save()
        {
            return 0;
        }
        public virtual bool Clear()
        {
            changeUser = Environment.UserName;
            changeDate = DateTime.Now.ToString();
            changeTag = null;
            changeState = 0;
            return true;
        }
        public virtual int Initialize(int lSerial = -1)
        {
            int rc = 0;
            Clear();
            return rc;
        }
        public virtual int Initialize(int lSerial = -1, string sqlText = "")
        {
            int rc = 0;
            Clear();
            return rc;
        }
        protected int getScopeIdentity()
        {
            int rc = -1;

            SqlCommand command = new SqlCommand("select isNull(@@IDENTITY,-1)", XConnection);
            SqlDataReader reader = null;
            // Open the connection and execute the insert command.
            try
            {
                reader = command.ExecuteReader();
                if (reader != null)
                {
                    if (reader.FieldCount >= 0)
                    {
                        rc = reader.GetInt32(0);
                    }
                    reader.Close();
                }
            }
            catch (SqlException ex)
            {
                rc = (-1) * ex.ErrorCode;
                XLogger(2506, ex.ErrorCode, string.Format("GetScopeIdentity(); Message={0}", ex.Message));
            }
            catch (Exception ex)
            {
                rc = -1;
                XLogger(2507, -1, string.Format("GetScopeIdentity(); Message={0}", ex.Message));
            }
            return rc;
        }

        //
        /////////////////////////
        // Static Members!
        /////////////////////////
        //
        //public static Dictionary<String, String> XDictionary = createDictionary();
        public static SqlConnection MasterConnection = connect("TASKDB");
        public static SqlConnection TaskLogConnection = connect("XTRMDB");
        /*
        public static Dictionary<String, String> createDictionary()
        {
            // Populate the Dictionary!
            XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            return myDictionaryLoader.ParseXML();
        }
        public static XTRMObject consumeXML(Dictionary<String, String> existingConfig, string XmlFragment, int lVariant = 0, bool bDeep = false)
        {
            return new XTRMObject();
        }
        public static Dictionary<String, String> ProcessElementName(XmlTextReader reader)
        {
            // Need to get and return the attributes!
            Dictionary<String, String> elementAttributes = new Dictionary<String, String>();
            if (reader.HasAttributes)
            {
                reader.MoveToFirstAttribute();
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    elementAttributes.Add(reader.Name, reader.Value);
                    reader.MoveToNextAttribute();
                }
            }
            return elementAttributes;
        }
        public static string getDictionaryEntry(string Name, string defaultValue = null)
        {
            string value = null;
            try
            {
                if (XDictionary.ContainsKey(Name))
                {
                    value = XDictionary[Name];
                }
            }
            catch (Exception)
            {
                //XLogger(2508, -1, string.Format("getDictionaryEntry(); Name={0}; Message={1}", Name, ex.Message));
            }
            finally
            {
                if (value == null)
                {
                    if (defaultValue != null)
                    {
                        value = defaultValue;
                    }
                    else
                    {
                        value = "";
                    }
                }
            }

            return value;
        }
        public static string getXTRMID()
        {
            string result = getDictionaryEntry("XTRMID", "");
            if (result.Equals(""))
            {
                result = getDictionaryEntry("XTRMID", "");
            }
            return result;
        }
        public static List<string> getDictionaryEntries(string Name)
        {
            List<string> results = new List<string>();

            try
            {
                foreach (KeyValuePair<string, string> kvp in XDictionary)
                {
                    if (kvp.Key.Contains(Name))
                    {
                        results.Add(kvp.Value);
                    }
                }
            }
            catch (Exception)
            {
                //XLogger(2509, -1, string.Format("getDictionaryEntries(); Name={0}; Message={1}", Name, ex.Message));
            }
            return results;
        }
        public static string getConfig(Dictionary<string, string> thisConfig, string Name, string defaultValue = null)
        {
            string value = null;
            try
            {
                if (thisConfig.ContainsKey(Name))
                {
                    value = thisConfig[Name];
                }
            }
            catch (Exception)
            {
                //XLogger(2510, -1, string.Format("getConfig(); Name={0}; Message={1}", Name, ex.Message));
            }
            finally
            {
                if (value == null)
                {
                    if (defaultValue != null)
                    {
                        value = defaultValue;
                    }
                    else
                    {
                        value = "";
                    }
                }
            }

            return value;
        }
        public static void logMetrics()
        {
            try
            {
                //XLogger(2511, 0, string.Format("LogMetrics; ObjectSerial={0}", lXObjectSerial.ToString()));
                //XLogger(2512, 0, string.Format("LogMetrics; Count={0}", lCount.ToString()));
                //XLogger(2513, 0, string.Format("LogMetrics; HWM={0}", lHWM.ToString()));
            }
            catch (Exception)
            {
                //XLogger(2514, -1, string.Format("logMetrics(); Message={0}", ex.Message));
            }
        }
        public static bool validateConnection(SqlConnection thisConnection)
        {
            bool result = false;
            if (thisConnection != null)
            {
                if (thisConnection.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        thisConnection.Open();
                        result = true;
                    }
                    catch (SqlException)
                    {
                        //XLogger(2522, ex.ErrorCode, string.Format("XDB Open Failure; Message={0}", ex.Message));
                    }
                    catch (Exception)
                    {
                        //XLogger(2523, -1, string.Format("XDB Open Failure; Message={0}", ex.Message));
                    }
                }
                else
                {
                    result = true;
                }
            }
            else
            {
                // Need to do XDBConnect(); this can result from startup depending on the order of events!
                try
                {
                    thisConnection = connectXDB("static");
                }
                catch (Exception)
                {
                    //XLogger(2531, -1, string.Format("XDB Connection Failure; Message={0}", ex.Message));
                }
            }
            return result;
        }
        public static SqlConnection connectXDB(string strClassName = "", string connectString = "")
        {
            SqlConnection thisConnection = null;
            int rc = 0;
            try
            {
                // Try to Get From XDictionary!
                if (connectString.Length == 0)
                {
                    connectString = getDictionaryEntry("XDBConnectString");
                }
                thisConnection = new SqlConnection(connectString);
                if (thisConnection != null)
                {
                    try
                    {
                        thisConnection.Open();
                    }
                    catch (SqlException ex)
                    {
                        thisConnection = null;
                        rc = ex.ErrorCode;
                        //XLogger(2515, ex.ErrorCode, string.Format("connectXDB(); Class={0}; Connect={1}; Message={2}", strClassName, connectString, ex.Message));
                    }
                    catch (Exception)
                    {
                        thisConnection = null;
                        rc = -1;
                        //XLogger(2516, rc, string.Format("connectXDB(); Class={0}; Connect={1}; Message={2}", strClassName, connectString, ex.Message));
                    }
                }
            }
            catch (SqlException)
            {
                //XLogger(2517, ex.ErrorCode, string.Format("connectXDB(); Class={0}; Connect={1}; Message={2}", strClassName, connectString, ex.Message));
            }
            catch (Exception)
            {
                //XLogger(2518, -1, string.Format("connectXDB(); Class={0}; Connect={1}; Message={2}", strClassName, connectString, ex.Message));
            }

            return thisConnection;
        }
        public static int ValidateOutputFolder(string outFile)
        {
            int rc = 0;

            // Ensure that strFileName is writable.
            try
            {
                DirectoryInfo myDirInfo = Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            }
            catch (Exception ex)
            {
                rc = -1;
                XLogger(2519, rc, string.Format("ValidateOutputFolder(); OutFile={0}; Message={1}", outFile, ex.Message));
            }
            return rc;
        }
        public static bool IsDriveReady(string serverName)
        {
            // ***  SET YOUR TIMEOUT HERE  ***      
            int timeout = 2;    // 5 seconds  
            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();
            options.DontFragment = true;
            // Enter a valid ip address      
            string ipAddressOrHostName = serverName;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
            System.Net.NetworkInformation.PingReply reply = pingSender.Send(ipAddressOrHostName, timeout, buffer, options);
            return (reply.Status == System.Net.NetworkInformation.IPStatus.Success);
        }
        public static string FindMyDictionaryFile(string DictionaryFile = "XDictionary.xml")
        {
            string myConfigFile = "";
            bool bExists = false;
            try
            {
                // Get the base name of the dictionary file.
                string myDictionaryFileName = Path.GetFileName(DictionaryFile);
                // Get the process info, then the executable name.
                Process myProcess = Process.GetCurrentProcess();
                ProcessStartInfo myProcessInfo = myProcess.StartInfo;
                ProcessModule myMainModule = myProcess.MainModule;
                string myExecutableDir = myMainModule.FileName;
                //myExecutableDir = myProcessInfo.FileName;
                //myProcess.StartInfo;
                // Search hierarchically (top-down) for the config file name.
                string myCurrentDir = myExecutableDir;
                while (myCurrentDir != null)
                {
                    // Check for an acceptable config file.
                    bExists = File.Exists(myCurrentDir + "\\" + myDictionaryFileName);
                    if (bExists)
                    {
                        rootFolder = myCurrentDir + "\\";
                        myConfigFile = myCurrentDir + "\\" + myDictionaryFileName;
                        myCurrentDir = null;
                    }
                    else
                    {
                        bExists = File.Exists(myCurrentDir + "\\Config\\" + myDictionaryFileName);
                        if (bExists)
                        {
                            rootFolder = myCurrentDir + "\\";
                            myConfigFile = myCurrentDir + "\\Config\\" + myDictionaryFileName;
                            myCurrentDir = null;
                        }
                    }
                    if (!bExists)
                    {
                        DirectoryInfo myCurrentDirInfo = Directory.GetParent(myCurrentDir);
                        if (myCurrentDirInfo == null)
                        {
                            myCurrentDir = null;
                        }
                        else
                        {
                            myCurrentDir = myCurrentDirInfo.FullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(2520, -1, string.Format("FindMyDictionaryFile(); DictionaryFile={0}; Message={1}", DictionaryFile, ex.Message));
            }
            finally
            {
            }
            return myConfigFile;
        }
        public static bool IsDictionaryLoaded()
        {
            bool bValid = false;
            if (XTRMObject.getDictionaryEntry("LoadDisposition").Equals("1"))
            {
                bValid = true;
                Console.Write("\nConfig ID={0}.\n", getDictionaryEntry("ConfigID"));
            }
            return bValid;
        }
        public static string GetRootFolder()
        {
            //return XLib.XDB_Objects.XObject.getDictionaryEntry("RootFolder");
            return rootFolder;
        }
        public static string GetConfigID()
        {
            return XTRMObject.getDictionaryEntry("ConfigID");
        }
        public static bool isValidDirectory(string folder, bool bCreate = false)
        {
            bool bResult = true;
            try
            {
                if (!Directory.Exists(folder))
                {
                    if (bCreate)
                    {
                        Directory.CreateDirectory(folder);
                        bResult = true;
                    }
                    else
                    {
                        bResult = false;
                    }
                }
            }
            catch (Exception ex)
            {
                bResult = false;
                XLogger(2521, -1, string.Format("isValidDirectory(); Folder={0}; Message={1}", folder, ex.Message));
            }
            return bResult;
        }
        */
        public static void SetLogID(int lSerial)
        {
            lLogID = lSerial;
        }
        public static int XLogger(int entryType, int entryResult, string entryText = null, string entryID = null, string entrySource = null, int lRetention = -1)
        {
            int rc = -99;
            if (lLogID >= 0)
            {
                rc = XTRMTask.Log(lLogID, entryType, entryResult, entryText, entryID, entrySource, lRetention);
            }
            else
            {
                // Not Associated with Task.
            }
            return rc;
        }
        public static Dictionary<string, string> getJobData(int jobSerial)
        {
            int rc = 0;
            Dictionary<string, string> jobData = new Dictionary<string, string>();
            jobData.Clear();
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select key_name, key_value from workjobdata where job_serial = {0} order by entry_date asc  ";
                String strSelectCommand = String.Format(strTemp, jobSerial);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (SqlException ex)
            {
                XLogger(2526, -1, string.Format("LoadJobData::Exception Getting Job #{0} Data from WorkJobData; Message={0}", jobSerial, ex.Message));
            }
            catch (Exception ex)
            {
                XLogger(2527, -1, string.Format("LoadJobData::Exception Getting Job #{0} Data from WorkJobData; Message={0}", jobSerial, ex.Message));
            }
            try
            {
                theseRows = thisTable.Select();
                if (theseRows.Length > 0)
                {   // Update!
                    for (int i = 0; i < theseRows.Length; i++)
                    {
                        if (jobData.ContainsKey((string)theseRows[i]["Key_Name"]))
                        {
                            jobData[(string)theseRows[i]["Key_Name"]] = (string)theseRows[i]["Key_Value"];
                        }
                        else
                        {
                            jobData.Add((string)theseRows[i]["Key_Name"], (string)theseRows[i]["Key_Value"]);
                        }
                    }
                }
                thisTable.Clear();
            }
            catch (Exception ex)
            {
                XLogger(2528, -1, string.Format("LoadJobData::Exception Creating Dictionary for Job #{0} Data from WorkJobData; Message={1}", jobSerial, ex.Message));
            }
            return jobData;
        }
        public int saveJobData(int jobSerial, string keyName, string keyValue)
        {
            int rc = 0;

            // Much like a log, writes entries to the WorkJobData table for use in the same job (by other tasks).
            // Check the Connection.
            try
            {
                if (MasterConnection != null)
                {
                    if (MasterConnection.State != System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            MasterConnection.Open();
                        }
                        catch (SqlException ex)
                        {
                            rc = ex.ErrorCode;
                        }
                        catch (Exception)
                        {
                            rc = -1;
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            // Need to repeat any single quotes in the text!
                            keyName = keyName.Replace("'", "''");
                            keyValue = keyValue.Replace("'", "''");
                            string strSQL = "EXEC dbo.spJobData";
                            strSQL += String.Format(" @JobSerial={0}", jobSerial);
                            strSQL += String.Format(", @KeyName='{0}'", keyName);
                            strSQL += String.Format(", @KeyValue='{0}'", keyValue);
                            XLogger(9998, 0, String.Format("PRE-Updating Job Data = {0};", strSQL));
                            SqlCommand command = new SqlCommand(strSQL, MasterConnection);
                            rc = command.ExecuteNonQuery();
                            XLogger(9999, 0, String.Format("POST-Updating Job Data = {0}; rc={1}", strSQL, rc));
                        }
                        catch (Exception ex)
                        {
                            XLogger(2529, -1, string.Format("Exception Writing Name/Value Pair in JobData(); Message={0}", ex.Message));
                        }
                        finally
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(2530, -1, string.Format("Exception with Connection in JobData(); Message={0}", ex.Message));
            }
            finally
            {
            }
            return rc;
        }
        public static bool Notify(string strTargetID = null, string strCopyID = null, string strSubject = "Notification Test", string strBody = "Notification Text.")
        {
            bool rc = false;
            string strSourceID = "Christopher.Swift@jmp.com (XBot)";
            string strSourceName = "XBot Admin";
            string strSMTPServer = "mailhost.fyi.sas.com";
            string strDefaultRecipient = null;
            try
            {
                if (strTargetID == null)
                {
                    strTargetID = strDefaultRecipient;
                }
                //strTargetID = strTargetID.Replace(",", ", ");
                MailAddress from = new MailAddress(strSourceID, strSourceName);
                if (strTargetID != null)
                {
                    MailAddress to = new MailAddress(strTargetID);
                    MailMessage message = new MailMessage(from, to);
                    if (strCopyID != null)
                    {
                        MailAddress copy = new MailAddress(strCopyID);
                        message.CC.Add(copy);
                    }
                    message.Subject = strSubject;
                    if (strBody != null)
                    {
                        message.Body = @strBody;
                    }
                    else
                    {
                        message.Body = "No Message Body";
                    }
                    //Send the message.
                    SmtpClient client = new SmtpClient(strSMTPServer);
                    client.UseDefaultCredentials = true;
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.Send(message);
                    rc = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in Notify(): {0}", ex.ToString());
            }
            return rc;
        }
        public static bool Notify(List<string> strTargetList = null, List<string> strCopyList = null, string strSubject = "Notification Test", string strBody = "Notification Text.")
        {
            bool rc = false;
            string strSourceID = "Christopher.Swift@jmp.com (XBot)";
            string strSourceName = "XBot Admin";
            string strSMTPServer = "mailhost.fyi.sas.com";
            string strDefaultRecipient = null;
            try
            {
                if (strTargetList == null)
                {
                    strTargetList = new List<string>();
                    strTargetList.Add(strDefaultRecipient);
                }
                //strTargetID = strTargetID.Replace(",", ", ");
                MailAddress from = new MailAddress(strSourceID, strSourceName);
                //MailAddress test = new MailAddress();
                if (strTargetList != null)
                {
                    MailMessage message = new MailMessage();
                    message.From = from;
                    foreach (string thisRecipient in strTargetList)
                    {
                        MailAddress to = new MailAddress(thisRecipient);
                        message.To.Add(thisRecipient);
                    }
                    if (strCopyList != null)
                    {
                        foreach (string thisCC in strCopyList)
                        {
                            MailAddress to = new MailAddress(thisCC);
                            message.CC.Add(thisCC);
                        }
                        //MailAddress copy = new MailAddress(strCopyID);
                        //message.CC.Add(copy);
                    }
                    message.Subject = strSubject;
                    if (strBody != null)
                    {
                        message.Body = @strBody;
                    }
                    else
                    {
                        message.Body = "No Message Body";
                    }
                    message.IsBodyHtml = true; // HTML
                    //Send the message.
                    SmtpClient client = new SmtpClient(strSMTPServer);
                    client.UseDefaultCredentials = true;
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.Send(message);
                    rc = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in Notify(): {0}", ex.ToString());
            }
            return rc;
        }
        public string ResolveParm(string rawParm, XTRMEvent thisEvent, Dictionary<string, string> thisConfig, int taskSerial = 0, int jobSerial = -1, int depth = 0)
        {
            string resolvedParm = rawParm;
            string name;

            // @"\{\{(?i:(.*?))\}\}"
            // @"((?<name>.*?(?=\s*=)))(.*?)(?<value>(?<=\().+(?=\)))"
            MatchCollection matchSymbol = XTRMUtil.GetRegexMatches(rawParm, @"\{\{(?<name>(.*?))\}\}");
            if (matchSymbol != null)
            {
                if (matchSymbol.Count > 0)
                {
                    // Report on each match.
                    foreach (Match match in matchSymbol)
                    {
                        string test = match.Value;
                        GroupCollection groups = match.Groups;
                        name = groups["name"].Value;
                        // Dictionary Lookup. 
                        // Substitute into rawParm.
                        string result = "";
                        switch (name)
                        {
                            case "JobSerial":
                                result = jobSerial.ToString();
                                break;
                            case "TaskSerial":
                                result = taskSerial.ToString();
                                break;
                            case "EventSerial":
                                result = thisEvent.eventSerial.ToString();
                                break;
                            case "EventSource":
                                result = thisEvent.eventSource;
                                break;
                            case "EventAction":
                                result = thisEvent.eventAction;
                                break;
                            case "EventDate":
                                result = thisEvent.eventDate.ToString();
                                break;
                            case "EventState":
                                result = thisEvent.eventState.ToString();
                                break;
                            case "EventUser":
                                result = thisEvent.eventUser;
                                break;
                            case "EventPIN":
                                result = thisEvent.eventPIN;
                                break;
                            case "EventUUID":
                                result = thisEvent.eventUUID;
                                break;
                            case "EventProcessed":
                                result = thisEvent.eventProcessed.ToString();
                                break;
                            case "EventParm1":
                                result = thisEvent.eventParm1;
                                break;
                            case "EventParm2":
                                result = thisEvent.eventParm2;
                                break;
                            case "EventParm3":
                                result = thisEvent.eventParm3;
                                break;
                            case "EventParm4":
                                result = thisEvent.eventParm4;
                                break;
                            case "EventParm5":
                                result = thisEvent.eventParm5;
                                break;
                            case "EventParm6":
                                result = thisEvent.eventParm6;
                                break;
                            case "EventParm7":
                                result = thisEvent.eventParm7;
                                break;
                            case "EventParm8":
                                result = thisEvent.eventParm8;
                                break;
                            case "EventParm9":
                                result = thisEvent.eventParm9;
                                break;
                            case "EventParm10":
                                result = thisEvent.eventParm10;
                                break;
                            case "EventTag":
                                result = thisEvent.eventTag;
                                break;
                            default:
                                //result = thisEvent.config.getDictionaryEntry(name);
                                //result = getConfig(thisEvent.config, name);
                                result = getConfig(thisConfig, name);
                                // If empty, then use name as the value.
                                break;
                        }
                        resolvedParm = resolvedParm.Replace(match.Value, result);
                    }
                }
            }
            if (!resolvedParm.Equals(rawParm))
            {
                if (depth < 10)
                {
                    // THIS IS A RECURSIVE CALL!
                    resolvedParm = ResolveParm(resolvedParm, thisEvent, thisConfig, taskSerial, jobSerial, depth++);
                }
            }
            return resolvedParm;
        }
        public string ResolveText(string strText, Dictionary<string, string> thisConfig, int depth = 0)
        {
            string resolvedText = strText;
            string name;

            // @"\{\{(?i:(.*?))\}\}"
            // @"((?<name>.*?(?=\s*=)))(.*?)(?<value>(?<=\().+(?=\)))"
            MatchCollection matchSymbol = XTRMUtil.GetRegexMatches(strText, @"\{\{(?<name>(.*?))\}\}");
            if (matchSymbol != null)
            {
                if (matchSymbol.Count > 0)
                {
                    // Report on each match.
                    foreach (Match match in matchSymbol)
                    {
                        string test = match.Value;
                        GroupCollection groups = match.Groups;
                        name = groups["name"].Value;
                        // Dictionary Lookup. 
                        // Substitute into rawParm.
                        string result = "";
                        switch (name)
                        {
                            default:
                                //result = thisEvent.config.getDictionaryEntry(name);
                                //result = getConfig(thisEvent.config, name);
                                result = getConfig(XDictionary, name);
                                if (result.Equals(""))
                                {
                                    result = getConfig(XDictionary, name.ToUpper());
                                }
                                // If empty, then use name as the value.
                                break;
                        }
                        resolvedText = resolvedText.Replace(match.Value, result);
                    }
                }
            }
            if (!resolvedText.Equals(strText))
            {
                if (depth < 10)
                {
                    // THIS IS A RECURSIVE CALL!
                    resolvedText = ResolveText(resolvedText, thisConfig, depth++);
                }
            }
            return resolvedText;
        }
        public int CreateJob(XTRMJob thisJob, XTRMEvent thisEvent, Dictionary<string, string> dynConfig = null)
        {
            int rc = 0;
            int jobSerial = -1;
            int taskSerial = -1;
            XTRMJob newJob = new XTRMJob();
            XTRMTask newTask = new XTRMTask();
            newJob.Initialize(-1);
            newJob.jobType = thisJob.jobType;
            newJob.jobEvent = thisEvent.eventSerial;
            newJob.jobSequence = 0;
            newJob.jobLimit = -1;
            newJob.jobDisplay = "TEST";
            //newJob.jobName = thisJob.jobName + " - " + thisEvent.eventParm1;
            newJob.jobName = ResolveParm(thisJob.jobName, thisEvent, thisJob.config, taskSerial) + " - " + thisEvent.eventParm1;
            newJob.jobStatus = -99;
            newJob.jobResult = 0;
            jobSerial = newJob.Save();
            if (jobSerial >= 0)
            {
                XLogger(1128, 0, string.Format("Created Job Serial={0}; Name={1}", jobSerial, newJob.jobName));

                // Write job config to WorkJobData!
                try
                {
                    foreach (KeyValuePair<string, string> kvp in thisJob.config)
                    {
                        saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                    }
                    if (dynConfig != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in dynConfig)
                        {
                            saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    XLogger(1157, -1, string.Format("Saving Job Data; Message={0}", ex.Message));
                }

                int position = 0;
                foreach (XTRMTask thisTask in thisJob.tasks)
                {
                    rc = newTask.Initialize(-1);
                    newTask.jobSerial = jobSerial;
                    newTask.taskSerial = -1;
                    newTask.taskSequence = position++;
                    //newTask.taskName = thisTask.taskName;
                    newTask.taskName = ResolveParm(thisTask.taskName, thisEvent, thisTask.config, taskSerial);
                    newTask.taskPath = ResolveParm(thisTask.taskPath, thisEvent, thisTask.config, taskSerial);
                    //newTask.taskExecutable = thisTask.taskExecutable;
                    newTask.taskExecutable = ResolveParm(thisTask.taskExecutable, thisEvent, thisTask.config, taskSerial);
                    newTask.taskPID = -1;
                    newTask.taskStatus = 0;
                    newTask.taskResult = 0;
                    newTask.taskEvent = thisEvent.eventSerial;
                    //newTask.taskStart = DateTime.Now;
                    //newTask.taskStop = DateTime.Now;
                    taskSerial = newTask.Save();
                    if (taskSerial >= 0)
                    {
                        // Must do Parameters!
                        // Need to resolve {{x}} parms!
                        if (thisTask.parms != null)
                        {
                            newTask.parms.Add(newTask.taskName);
                            foreach (string thisParm in thisTask.parms)
                            {
                                //string resolvedParm = ResolveParm(thisParm, thisEvent, thisTask.config);
                                newTask.parms.Add(ResolveParm(thisParm, thisEvent, thisTask.config, taskSerial, jobSerial));
                            }
                        }
                        //newTask.taskStart = DateTime.Now;
                        //newTask.taskStop = DateTime.Now;
                        taskSerial = newTask.Save();
                        if (taskSerial >= 0)
                        {
                            XLogger(1125, 0, string.Format("Created Task Serial={0}; Name={1}; Path={2}", taskSerial, newTask.taskName, newTask.taskPath));
                        }
                        else
                        {
                            XLogger(1126, -1, string.Format("Unable To Create Task; rc={0}", taskSerial));
                        }
                    }
                    else
                    {
                        XLogger(1155, -1, string.Format("Unable To Create Task; rc={0}", taskSerial));
                    }
                }
                newJob.jobStatus = 0;   // Activate Job!
                jobSerial = newJob.Save();
                if (jobSerial < 0)
                {
                    XLogger(1156, -1, string.Format("Unable To Activate (save) Job; rc={0}", jobSerial));
                }
            }
            else
            {
                XLogger(1127, -1, string.Format("Unable To Create Job; rc={0}", jobSerial));
            }
            return rc;
        }
    }
}
