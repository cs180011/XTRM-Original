using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.IO;
using System.Diagnostics;


namespace XTRMlib
{
    public class XTRMBase : Object
    {
        protected SqlConnection XConnection = null;     // sample5
        protected static SqlConnection XTRMAPP = null;
        protected static int lLogID = 0;  // Default
        //public string className;
        protected static string rootFolder = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\XLator\\";
        public XTRMBase()
        {
            XTRMAPP = connect("XTRMAPP");
        }
        //
        /////////////////////////
        // Static Members!
        /////////////////////////
        //
        public static Dictionary<String, String> XDictionary = createDictionary();
        //public static SqlConnection MasterConnection = connect("XDBConnectString");
        //public static SqlConnection TaskLogConnection = connect("XDBConnectString");
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
        public static bool validateConnection(SqlConnection thisConnection, string strDBName = "TASKDB")
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
                    thisConnection = connect("static");
                }
                catch (Exception)
                {
                    //XLogger(2531, -1, string.Format("XDB Connection Failure; Message={0}", ex.Message));
                }
            }
            return result;
        }
        public static SqlConnection connect(string strDBName = "", string connectString = "")
        {
            SqlConnection thisConnection = null;
            int rc = 0;
            try
            {
                // Try to Get From XDictionary!
                if (connectString.Length == 0)
                {
                    connectString = getDictionaryEntry(strDBName);
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
                //XLogger(2519, rc, string.Format("ValidateOutputFolder(); OutFile={0}; Message={1}", outFile, ex.Message));
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
                //XLogger(2520, -1, string.Format("FindMyDictionaryFile(); DictionaryFile={0}; Message={1}", DictionaryFile, ex.Message));
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
                //XLogger(2521, -1, string.Format("isValidDirectory(); Folder={0}; Message={1}", folder, ex.Message));
            }
            return bResult;
        }
    }
}
