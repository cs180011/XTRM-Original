using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XTRMlib
{
    // Error Number = 1176
    public class XTRMEventLoader : XTRMObject
    {
        EventLog myLog;
        // myConfigs are the list of XLator Config Files (by name) registered in the (active) dictionary!
        List<string> myConfigs = new List<string>();
        // We load each XLator Config File (once) and hold on to it for repeated use both in the loading of events (from XML) and in the evaluation of XDB events.
        List<XTRMRoot> myConfigLoaders = new List<XTRMRoot>();
        //XLator myConfigLoader = new XLator("Xlator");
        //XLator myEventLoader = new XLator("XEvent");
        List<int> activeEvents = new List<int>();
        //public Dictionary<String, String> dynConfig = new Dictionary<String, String>();
        bool bConfigLoaded = false;
        bool eventStatus = getDictionaryEntry("ProcessEvents", "Y").Equals("Y");
        bool commandStatus = getDictionaryEntry("ProcessCommands", "Y").Equals("Y");
        bool dataStatus = getDictionaryEntry("ProcessData", "Y").Equals("Y");
        int maxEventFileSize = Convert.ToInt32(getDictionaryEntry("MaxEventFileSize", "1024000"));

        public XTRMEventLoader()
        {
            myLog = null;
        }
        public XTRMEventLoader(EventLog thisLog)
        {
            myLog = thisLog;
        }
        public int Initialize()
        {
            int rc = -99;
            try
            {
                //int rc = 0;
                SetLogID(1);
                //string XLatorConfigFile = XLib.XDB_Objects.XObject.getDictionaryEntry("XLatorConfigFile");
                myConfigs = XTRMObject.getDictionaryEntries("XLatorConfigFile");
                //myConfigLoader.ParseConfig(XLatorConfigFile, 2, true);
                rc = myConfigs.Count;
            }
            catch (Exception ex)
            {
                XLogger(1175, -1, string.Format("Exception in Initialize(); message={0}", ex.Message));
                rc = -1;
            }
            return rc;
        }
        /*
        public int startIteration()
        {
            int rc = 0;
            int interval = 60;
            try
            {
                XObject.XDictionary = createDictionary();
                myConfigs = XLib.XDB_Objects.XObject.getDictionaryEntries("XLatorConfigFile");
                interval = Convert.ToInt32(getConfig(XObject.XDictionary, "MonitorInterval", "60"));
            }
            catch (Exception ex)
            {
                interval = 60;
            }
            return interval;
        }
         * */
        // Make Full Pass Through Processing.
        public int Run(int pass = 0, bool logBeat = false)
        {
            int rc = 0;

            // Determine what we are currently processing!
            eventStatus = getDictionaryEntry("ProcessEvents", "Y").Equals("Y");
            commandStatus = getDictionaryEntry("ProcessCommands", "Y").Equals("Y");
            dataStatus = getDictionaryEntry("ProcessData", "Y").Equals("Y");
            maxEventFileSize = Convert.ToInt32(getDictionaryEntry("MaxEventFileSize", "1024000"));

            //Process execProcess = Process.GetCurrentProcess();

            /*
            if (logBeat)
            {
                XLogger(1131, 0, string.Format("Heartbeat: Current={0}; Peak={1}; WSS={2}; TotProc={3};", execProcess.VirtualMemorySize64, execProcess.PeakVirtualMemorySize64, execProcess.WorkingSet64, execProcess.TotalProcessorTime));
            }

            // Check memory.
            // Check registry.
            if (execProcess.VirtualMemorySize64 > 500000000)
            {
                XLogger(1137, 0, string.Format("Forcing Garbage Collection: Current={0}; Peak={1}; WSS={2}; TotProc={3};", execProcess.VirtualMemorySize64, execProcess.PeakVirtualMemorySize64, execProcess.WorkingSet64, execProcess.TotalProcessorTime));
                GC.Collect();
                XLogger(1138, 0, string.Format("After Garbage Collection: Current={0}; Peak={1}; WSS={2}; TotProc={3};", execProcess.VirtualMemorySize64, execProcess.PeakVirtualMemorySize64, execProcess.WorkingSet64, execProcess.TotalProcessorTime));
                if (execProcess.VirtualMemorySize64 > 300000000)
                {
                    XLogger(1139, 0, string.Format("Re-Instantiating: Current={0}; Peak={1}; WSS={2}; TotProc={3};", execProcess.VirtualMemorySize64, execProcess.PeakVirtualMemorySize64, execProcess.WorkingSet64, execProcess.TotalProcessorTime));

                    return -1;
                }
            }
             * */

            // Validate Connection.
            if (validateConnection(MasterConnection))
            {
                exCount_XML = 0;
                // Populate activeEvents from XDB.
                switch (pass)
                {
                    // Pass 0:  Parse the XLator Configs.
                    case 0:
                        if (eventStatus)
                        {
                            if ((!bConfigLoaded) | (XTRMObject.getDictionaryEntry("ReloadConfig", "Y").Equals("Y")))
                            {
                                // Get the XLator Event Folder (from the dictionary).
                                //string strPendingEventFolder = XLib.XDB_Objects.XObject.getDictionaryEntry("XLatorPendingEventFolder");
                                // May be a list of config files!
                                //string strPendingEventFolder = myConfigLoader.configFile;
                                myConfigLoaders.Clear();
                                foreach (string thisConfig in myConfigs)
                                {
                                    XTRMRoot thisLoader = new XTRMRoot(ResolveText(thisConfig, XDictionary));
                                    thisLoader.ParseConfig(2, true);
                                    myConfigLoaders.Add(thisLoader);
                                }
                                bConfigLoaded = true;
                            }
                            // Commit.
                        }
                        break;
                    // Pass 1:  Process any pending events referenced by any of the loaders. 
                    case 1:
                        if (eventStatus)
                        {
                            foreach (XTRMRoot thisLoader in myConfigLoaders)
                            {
                                //myConfigLoader.ParseConfig(thisConfig, 2, true);
                                foreach (string strPendingEventFolder in thisLoader.eventFolders)
                                {
                                    rc = ProcessPendingEvents(strPendingEventFolder);
                                    if (rc < 0)
                                    {
                                        XLogger(1100, -1, string.Format("Run::ProcessPendingEvents() Failure; Pass={0}; rc={1}; folder={2}", pass, rc, strPendingEventFolder));
                                    }
                                }
                                //myConfigLoaders.Add(myConfigLoader);
                            }
                            if (exCount_XML > 0)
                            {
                                //Console.Write("XML Parser Exceptions\n");
                                XLogger(1101, exCount_XML, string.Format("Run::XML Parser xceptions > 0; Pass={0}; rc={1}", pass, rc));
                            }
                        }
                        break;
                }
            }
            else
            {
                myLog.WriteEntry("Master XDB Connection Failure");
            }
            return rc;
        }
        // Overall Flow:
        //
        //  Run through eack XLator config file (loading events).
        //  Query XDB for events whose state is 0 (not processed).
        //  For each XLator config, run each event against the corresponding XLator config rules generating jobs as indicated (use event-tagging on the job).
        //  When generating a job update the Event_Processed time stamp.
        //  After processing all events against each relevant XLator config, then mark event state as 1 (processed).
        //
        // Process Pending Events in XLator/Events Folder.
        public int ProcessPendingEvents(string strPendingEventFolder)
        {
            int rc = 0;
            bool bError = false;
            XTRMEvent eventTester = new XTRMEvent();
            // For Each File in the folder, need to consume into XEvent(s).
            //Directory.GetFiles(strPendingEventFolder);
            try
            {
                //Directory.GetFiles(strPendingEventFolder, "*.dll").Select(fn => new FileInfo(fn)).
                //string[] fileEntries = Directory.GetFiles(strPendingEventFolder).Select(fn => new FileInfo(fn)).OrderBy(f => f.CreationTime).ToList(); 
                string[] fileEntries = Directory.GetFiles(strPendingEventFolder);
                foreach (string fileName in fileEntries)
                {
                    bError = false;
                    try
                    {
                        // Only process file if it is less than 100KB.
                        FileInfo myInfo = new FileInfo(fileName);
                        if (myInfo.Length <= maxEventFileSize)
                        {
                            //ParseConfig(fileName);
                            XTRMRoot myEventLoader = new XTRMRoot(fileName);
                            //myEventLoader.Clear();
                            myEventLoader.ParseConfig(2, true);
                            // This will result in 0 or more XEvents to process.
                            foreach (XTRMEvent thisEvent in myEventLoader.events)
                            {
                                try
                                {
                                    // Check to see if the UUID already exists!
                                    if (eventTester.Initialize(thisEvent.eventUUID) <= 0)
                                    {
                                        // Persist the XEvent to XDB.
                                        rc = thisEvent.Save();
                                        if (rc < 0)
                                        {
                                            bError = true;
                                            XLogger(1105, -1, string.Format("ProcessPendingEvents::XEvent.Save() Failure; Folder={0}; UUID={1}; rc={2}", strPendingEventFolder, thisEvent.eventUUID, rc));
                                        }
                                    }
                                    else
                                    {
                                        XLogger(1128, -1, string.Format("Duplicate Event Skipped; Folder={0}; UUID={1}; File={2}; rc={3}", strPendingEventFolder, thisEvent.eventUUID, thisEvent.eventParm1, rc));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    XLogger(1106, rc, string.Format("Caught Event Loader Exception; Folder={0}; UUID={1}; Message={2}", strPendingEventFolder, thisEvent.eventUUID, ex.Message));
                                }
                            }
                        }
                        else
                        {
                            XLogger(1129, -1, string.Format("Event File Too Large; Folder={0}; File={1}; rc={2}", strPendingEventFolder, fileName, rc));
                        }
                    }
                    catch (Exception ex)
                    {
                        XLogger(1130, -1, string.Format("ParseConfig Exception; Folder={0}; File={1}; rc={2}", strPendingEventFolder, fileName, rc));
                    }
                    // Now Move the File Containing the Event(s) to the OK Folder.
                    try
                    {
                        FileInfo myFileInfo = new FileInfo(fileName);
                        string shortName = myFileInfo.Name;
                        //File.Move();
                        DirectoryInfo myParentDirInfo = Directory.GetParent(strPendingEventFolder);
                        string myParentDir = myParentDirInfo.FullName;
                        // REWORK the following moves.

                        if (bError)
                        {
                            //File.Move();
                            bool bExists = Directory.Exists(myParentDir + "\\Discard\\");
                            if (!bExists)
                            {
                                Directory.CreateDirectory(myParentDir + "\\Discard\\");
                            }
                            File.Move(strPendingEventFolder + "\\" + shortName, myParentDir + "\\Discard\\" + shortName);
                        }
                        else
                        {
                            //File.Move();
                            bool bExists = Directory.Exists(myParentDir + "\\OK\\");
                            if (!bExists)
                            {
                                Directory.CreateDirectory(myParentDir + "\\OK\\");
                            }
                            File.Move(strPendingEventFolder + "\\" + shortName, myParentDir + "\\OK\\" + shortName);
                        }
                        // Whether can move or not, must delete!
                        File.Delete(strPendingEventFolder + "\\" + shortName);
                    }
                    catch (Exception ex)
                    {
                        XLogger(1107, -1, string.Format("ProcessPendingEvents::Move Files Exception; Folder={0}; Message={1}", strPendingEventFolder, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(1108, -1, string.Format("ProcessPendingEvents::Get Files Exception; Folder={0}; Message={1}", strPendingEventFolder, ex.Message));
            }
            return rc;
        }
        private int LoadActiveEvents()
        {
            int rc = 0;
            activeEvents.Clear();
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select * from Events where Event_State = -1 and Event_Date < GETDATE() ";
                String strSelectCommand = String.Format(strTemp);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (Exception ex)
            {
                XLogger(1122, -1, string.Format("LoadActiveEvents::Exception Getting Events (state = -1) from Events; Message={0}", ex.Message));
            }
            theseRows = thisTable.Select();
            if (theseRows.Length > 0)
            {   // Update!
                for (int i = 0; i < theseRows.Length; i++)
                {
                    activeEvents.Add((int)theseRows[i]["Event_Serial"]);
                }
            }
            thisTable.Clear();
            return rc;
        }
        public int XLogger(int result, string logtext, int ID = 9800)
        {
            return XTRMObject.XLogger(ID, result, logtext);
        }
    }
}

