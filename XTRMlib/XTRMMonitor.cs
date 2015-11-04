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
    public class XTRMMonitor : XTRMObject
    {
        EventLog myLog;
        // myConfigs are the list of XLator Config Files (by name) registered in the (active) dictionary!
        List<string> myConfigs = new List<string>();
        // We load each XLator Config File (once) and hold on to it for repeated use both in the loading of events (from XML) and in the evaluation of XDB events.
        List<XTRMRoot> myConfigLoaders = new List<XTRMRoot>();
        List<int> activeEvents = new List<int>();
        bool bConfigLoaded = false;
        bool eventStatus = getDictionaryEntry("ProcessEvents", "Y").Equals("Y");
        bool commandStatus = getDictionaryEntry("ProcessCommands", "Y").Equals("Y");
        bool dataStatus = getDictionaryEntry("ProcessData", "Y").Equals("Y");
        int maxEventFileSize = Convert.ToInt32(getDictionaryEntry("MaxEventFileSize", "1024000"));

        public XTRMMonitor()
        {
            myLog = null;
        }
        public XTRMMonitor(EventLog thisLog)
        {
            myLog = thisLog;
        }
        public int Initialize()
        {
            int rc = -99;
            try
            {
                SetLogID(1);
                myConfigs = XTRMObject.getDictionaryEntries("XLatorConfigFile");
                rc = myConfigs.Count;
            }
            catch (Exception ex)
            {
                XLogger(1175, -1, string.Format("Exception in Initialize(); message={0}", ex.Message));
                rc = -1;
            }
            return rc;
        }
        // Make Full Pass Through Processing.
        public int Run(int pass = 0, bool logBeat = false)
        {
            int rc = 0;

            // Determine what we are currently processing!
            eventStatus = getDictionaryEntry("ProcessEvents", "Y").Equals("Y");
            commandStatus = getDictionaryEntry("ProcessCommands", "Y").Equals("Y");
            dataStatus = getDictionaryEntry("ProcessData", "Y").Equals("Y");
            maxEventFileSize = Convert.ToInt32(getDictionaryEntry("MaxEventFileSize", "1024000"));

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
                    // Pass 2:  Process all active XDB events against each of the loaders.
                    case 2:
                        if (eventStatus)
                        {
                            // Loop through all XDB events
                            // Run query to get active XDB events.
                            rc = LoadActiveEvents();
                            if (rc < 0)
                            {
                                XLogger(1102, -1, string.Format("Run::LoadActiveEvents() Failure; Pass={0}; rc={1}", pass, rc));
                            }
                            XTRMEvent thisEvent = new XTRMEvent();
                            // For each event:
                            foreach (int thisEventSerial in activeEvents)
                            {
                                int eventState = 0;
                                //XEvent thisEvent = new XEvent();
                                rc = thisEvent.Initialize(thisEventSerial);
                                // For each loader:
                                if (rc >= 0)
                                {
                                    // XEvent Retrieved Successfully!
                                    foreach (XTRMRoot thisLoader in myConfigLoaders)
                                    {
                                        XTRMMatchMaker myEventChecker = new XTRMMatchMaker();
                                        rc = myEventChecker.ProcessActiveEvent(thisLoader, thisEvent);
                                        if (rc >= 0)
                                        {
                                            eventState += rc;
                                        }
                                        else
                                        {
                                            XLogger(1103, -1, string.Format("Run::XTRMEvent.ProcessActiveEvent() Failure; Pass={0}; UUID={1}; rc={2}", pass, thisEvent.eventUUID, rc));
                                        }
                                    }
                                    thisEvent.eventState = eventState;
                                    rc = thisEvent.Save();
                                    if (rc >= 0)
                                    {
                                        XLogger(1124, eventState, string.Format("Event={0}; {1}", rc, thisEvent.banner), thisEvent.eventUser);
                                    }
                                    else
                                    {
                                        XLogger(1123, -1, string.Format("Unable to Save() Active Event; rc={2}", pass, thisEvent.eventUUID, rc));
                                    }
                                }
                                else
                                {
                                    // Error!
                                    XLogger(1104, -1, string.Format("Run::XEvent.Initialize() Failure; Pass={0}; UUID={1}; rc={2}", pass, thisEvent.eventUUID, rc));
                                }
                                thisEvent.Clear();
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

