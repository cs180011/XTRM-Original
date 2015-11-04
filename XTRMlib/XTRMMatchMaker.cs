using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XTRMlib
{
    class XTRMMatchMaker : XTRMObject
    {
        // myConfigs are the list of XTRMRoot Config Files (by name) registered in the (active) dictionary!
        List<string> myConfigs = new List<string>();
        // We load each XTRMRoot Config File (once) and hold on to it for repeated use both in the loading of events (from XML) and in the evaluation of XDB events.
        List<XTRMRoot> myConfigLoaders = new List<XTRMRoot>();
        public Dictionary<String, String> dynConfig = new Dictionary<String, String>();
        // Process Active Events in XDB.
        // Check for Completed Jobs.
        public int ProcessActiveEvent(XTRMRoot thisLoader, XTRMEvent thisEvent)
        {
            List<XTRMJob> theseJobs = new List<XTRMJob>();
            int rc = FindXMatch(thisLoader, thisEvent, out theseJobs);
            switch (rc)
            {
                case -1:
                    // Failed Event Filter.
                    // Update event status (to Initial State).
                    break;
                case 0:
                    // No Jobs Identified.
                    // Update event status.
                    break;
                default:
                    if (rc > 0)
                    {
                        // One or more Jobs Identified.
                        // Create Job(s); XExecutive will process asynchronously.
                        // Update event status.
                    }
                    else
                    {
                        // Other negative values are ignored (at present)!
                    }
                    break;
            }
            //rc = theseJobs.Count;
            if (theseJobs.Count > 0)
            {
                foreach (XTRMJob thisJob in theseJobs)
                {
                    rc = CreateJob(thisJob, thisEvent, dynConfig);
                }
            }
            return theseJobs.Count;
        }
        public int Traverse(string candidatePath, string filePattern, string WorkspacePrefix, string DepotPrefix)
        {
            int rc = -1;

            try
            {
                XLogger(51002, 0, string.Format("Execute XTroll.exe"));

                myConfigs = XTRMObject.getDictionaryEntries("XTRMRootConfigFile");
                myConfigLoaders.Clear();
                foreach (string thisConfig in myConfigs)
                {
                    XTRMRoot thisLoader = new XTRMRoot(ResolveText(thisConfig, XDictionary));
                    thisLoader.ParseConfig(2, true);
                    myConfigLoaders.Add(thisLoader);
                }
                var fileEntries = Directory.EnumerateFiles(candidatePath, filePattern, SearchOption.AllDirectories);
                foreach (string sourceFile in fileEntries)
                {
                    string thisFile = sourceFile.Replace(WorkspacePrefix, DepotPrefix);
                    thisFile = thisFile.Replace(@"\", @"/");
                    //if (stopFlag.Equals(1))
                    //{
                    //    throw new Exception("EggTimer Expired!");
                    //}
                    // Try to Validate the Component and Element.
                    // If Element is valid, then skip (only looking for new elements).
                    // Fabricate Event Object for Element.
                    // Instantiate and Call Monitor to Identify Jobs.
                    //XMonitorCore myMonitor = new XMonitorCore();
                    XTRMMatchMaker myEventChecker = new XTRMMatchMaker();
                    // XTRMEvent Retrieved Successfully!
                    XTRMEvent thisEvent = new XTRMEvent();
                    thisEvent.Initialize(-1);
                    //  <XTRMEvent ID="626">
                    //		<Source>P4V</Source>
                    //		<Action>edit</Action>
                    //		<DateStamp>20120103 19:23:03</DateStamp>
                    //		<User>shlori</User>
                    //		<PIN>A342158B864EE75025C6F08F42C9544A</PIN>
                    //		<Status>0</Status>
                    //		<Path>//depot/main/product/doc/Portable/English/*</Path>
                    //		<Desc>Fixed page/line breaks.</Desc>
                    //		<!-- Parm1 = {Depot File} -->
                    //		<Parm1>//depot/main/product/doc/Portable/English/Basic Analysis and Graphing.pdf</Parm1>
                    //		<!-- Parm2 = {Depot Version} -->
                    //		<Parm2>56</Parm2>
                    //		<!-- Parm3 = {Change List} -->
                    //		<Parm3>83234</Parm3>
                    //		<!-- Parm4 = {File Type} -->
                    //		<Parm4>binary+lm</Parm4>
                    //		<!-- Parm5 = {File Size} -->
                    //		<Parm5>4216714</Parm5>
                    //		<!-- Parm6 = {Client} -->
                    //		<Parm6>shlori-Win</Parm6>
                    //		<!-- Parm7 = {SCM Time} -->
                    //		<Parm7>1325636582</Parm7>
                    //	</XTRMEvent>
                    thisEvent.eventAction = "edit";
                    thisEvent.eventDate = DateTime.Now.ToString();
                    thisEvent.eventSource = "P4V";
                    thisEvent.eventUser = "XTroller";
                    thisEvent.eventState = -1;
                    thisEvent.eventParm1 = thisFile;
                    int eventState = 0;
                    bool eventFlag = false;
                    foreach (XTRMRoot thisLoader in myConfigLoaders)
                    {
                        //rc = myMonitor.EvaluateEvent(thisLoader, thisEvent);
                        List<XTRMJob> theseJobs = new List<XTRMJob>();
                        rc = myEventChecker.FindXMatch(thisLoader, thisEvent, out theseJobs);
                        if (rc > 0)
                        {
                            if (theseJobs.Count > 0)
                            {
                                foreach (XTRMJob thisJob in theseJobs)
                                {
                                    Dictionary<string, string> jobData = new Dictionary<string, string>();
                                    try
                                    {
                                        foreach (KeyValuePair<string, string> kvp in thisJob.config)
                                        {
                                            //saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                                            jobData[kvp.Key.ToString().ToUpper()] = kvp.Value;
                                        }
                                        foreach (KeyValuePair<string, string> kvp in myEventChecker.dynConfig)
                                        {
                                            //saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                                            jobData[kvp.Key.ToString().ToUpper()] = kvp.Value;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        XLogger(1157, -1, string.Format("Saving Job Data; Message={0}", ex.Message));
                                    }
                                    try
                                    {
                                        if (validateInventory())
                                        {
                                            eventFlag = true;
                                        }
                                    }
                                    catch(Exception)
                                    {
                                        eventFlag = false;
                                    }
                                }
                            }
                        }
                        if (rc >= 0)
                        {
                            eventState += rc;
                        }
                        else
                        {
                            XLogger(1103, -1, string.Format("Execute::XTRMEvent.EvaluateEvent() Failure; UUID={1}; rc={2}", thisEvent.eventUUID, rc));
                        }
                    }
                    // If no jobs, then skip.
                    if (eventFlag)
                    {
                        // Creating the event will result in the Monitor processing against the production Config Loaders.
                        rc = thisEvent.Save();
                        if (rc < 0)
                        {
                            // Error!
                        }
                    }
                    // Otherwise, try to Validate the Component and Element Again.
                    // If Element is valid, then skip (only looking for new elements).
                    // Call Monitor to Create Jobs.
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                XLogger(51003, -1, string.Format("XTroll Exception in Execute(); Message={0}.", ex.Message));
            }
            return rc;
        }

        private bool validateInventory()
        {
            throw new NotImplementedException();
        }

        public int FindXMatch(XTRMRoot thisLoader, XTRMEvent thisEvent, out List<XTRMJob> myJobs)
        {
            // rc == -1     EventFilter Not Satisfied.
            // rc >= 0      Number of matching jobs.
            int rc = 0;
            bool bResult = true;
            myJobs = new List<XTRMJob>();
            dynConfig.Clear();

            // If there is a match, it will be (a) job(s) from one of (XWorkflow, XToolkit, XComponent, XElement).
            // All matches will be returned in a list of jobs to run.
            // List<XTRMJob> is Returned.

            // Check to see if the Event Filter(s) <is/are all> satisfied; if not, set return code to -1 and exit.
            foreach (XTRMEvent thisEventFilter in thisLoader.eventFilters)
            {
                if (CheckEvent(thisEvent, thisEventFilter) == false)
                {
                    return -1;
                }
            }

            // Traverse XTRMRoot structures for XWorkflow, XToolkit, XComponent, XElement looking for matches.
            foreach (XTRMWorkFlow candidate in thisLoader.workflows)
            {
                try
                {
                    foreach (XTRMEvent thisFilter in candidate.workflowEvents)
                    {
                        try
                        {
                            // If we match the filter, then create the job(s).
                            // Check the active event against the event filter.
                            if (bResult = CheckEvent(thisEvent, thisFilter))
                            {
                                // Add to List of Job(s).
                                foreach (XTRMJob thisJob in thisFilter.eventJobs)
                                {
                                    thisJob.containingWorkflow = candidate.name;
                                    myJobs.Add(thisJob);
                                    rc++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            XLogger(1118, -1, string.Format("FindXMatch::Exception(workflow); Serial={0}; Expression={1}; Message={2}", thisEvent.eventSerial, thisFilter.eventTag, ex.Message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    XLogger(1119, -1, string.Format("FindXMatch::Exception(candidate workflow); Serial={0}; Message={1}", thisEvent.eventSerial, ex.Message));
                }
            }
            return rc;
        }
        // Check EventTag (now does)
        // Check EventHost should feed into JobHost (new)
        private bool CheckEvent(XTRMEvent thisEvent, XTRMEvent thisFilter)
        {
            bool bResult = true;
            //dynConfig.Clear();
            // For test, return 1.
            // thisEvent is the real incoming event
            // thisFilter is for comparison

            try
            {
                // User
                if (bResult = CheckEventAttribute(thisEvent.eventUser, thisFilter.eventUser))
                {
                    bResult = CheckEventAttribute(thisEvent.eventTag, thisFilter.eventTag);
                }
                // Source/Action Dependent Checks
                // EventSource
                if (bResult)
                {
                    thisEvent.banner = thisEvent.eventParm1;
                    // Source
                    if (bResult = CheckEventAttribute(thisEvent.eventSource, thisFilter.eventSource))
                    {
                        // Action
                        if (bResult = CheckEventAttribute(thisEvent.eventAction, thisFilter.eventAction))
                        {
                            // Parm1
                            if (bResult = CheckNormalPath(thisEvent.eventParm1, thisFilter.normalPath))
                            {
                                // Parm2
                                if (bResult = CheckEventAttribute(thisEvent.eventParm2, thisFilter.eventParm2))
                                {
                                    // Parm3
                                    if (bResult = CheckEventAttribute(thisEvent.eventParm3, thisFilter.eventParm3))
                                    {
                                        // Parm4
                                        if (bResult = CheckEventAttribute(thisEvent.eventParm4, thisFilter.eventParm4))
                                        {
                                            // Parm5
                                            if (bResult = CheckEventAttribute(thisEvent.eventParm5, thisFilter.eventParm5))
                                            {
                                                // Parm6
                                                if (bResult = CheckEventAttribute(thisEvent.eventParm6, thisFilter.eventParm6))
                                                {
                                                    // Parm7
                                                    if (bResult = CheckEventAttribute(thisEvent.eventParm7, thisFilter.eventParm7))
                                                    {
                                                        // Parm8
                                                        if (bResult = CheckEventAttribute(thisEvent.eventParm8, thisFilter.eventParm8))
                                                        {
                                                            // Parm9
                                                            if (bResult = CheckEventAttribute(thisEvent.eventParm9, thisFilter.eventParm9))
                                                            {
                                                                // Parm10
                                                                bResult = CheckEventAttribute(thisEvent.eventParm10, thisFilter.eventParm10);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bResult = false;
                XLogger(1120, -1, string.Format("CheckEventAttribute::Exception Checking Event; Value={0}; Expression={1}; Message={2}", thisEvent.eventSerial, thisFilter.eventTag, ex.Message));
            }
            finally
            {
            }
            return bResult;
        }
        private bool CheckNormalPath(string eventAttr, List<string> filterAttrList)
        {
            bool bResult = true;
            foreach (string filterAttr in filterAttrList)
            {
                bResult = CheckEventAttribute(eventAttr, filterAttr);
                if (bResult)
                {
                    return bResult;
                }
            }
            return bResult;
        }
        private bool CheckEventAttribute(string eventAttr, string filterAttr)
        {
            bool bResult = true;
            try
            {
                // Parm1
                if (filterAttr != null)
                {
                    if (eventAttr != null)
                    {
                        // this could be Equality, Containment, Regex (or otherwise).
                        if (!eventAttr.Equals(filterAttr))
                        {
                            if (!eventAttr.Contains(filterAttr))
                            {
                                MatchCollection matchSymbol = null;
                                Regex rgx = null;
                                try
                                {
                                    rgx = new Regex(filterAttr, RegexOptions.IgnoreCase);

                                    matchSymbol = rgx.Matches(eventAttr);
                                }
                                catch (Exception ex)
                                {
                                    XLogger(1159, -1, string.Format("CheckEventAttribute::Regex Processing Exception; Value={0}; Expression={1}; Message={2}", eventAttr, filterAttr, ex.Message));
                                }
                                if (matchSymbol != null)
                                {
                                    if (matchSymbol.Count > 0)
                                    {
                                        try
                                        {
                                            // Report on each match.
                                            foreach (Match match in matchSymbol)
                                            {
                                                string test = match.Value;

                                                GroupCollection groups = match.Groups;
                                                foreach (string groupName in rgx.GetGroupNames())
                                                {
                                                    if (groups[groupName].Success)
                                                    {
                                                        dynConfig[groupName] = groups[groupName].Value;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            XLogger(1158, -1, string.Format("CheckEventAttribute::Regex Match Handling Exception; Value={0}; Expression={1}; Message={2}", eventAttr, filterAttr, ex.Message));
                                        }
                                    }
                                    else if (matchSymbol.Count == 0)
                                    {
                                        bResult = false;
                                    }
                                }
                                else
                                {
                                    bResult = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        bResult = false;
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(1121, -1, string.Format("CheckEventAttribute::Exception Checking Event Attribute; Value={0}; Expression={1}; Message={2}", eventAttr, filterAttr, ex.Message));
            }
            finally
            {
            }
            return bResult;
        }
        public int CheckEvents(string strPendingEventFolder)
        {
            int rc = 0;
            int maxEventFileSize = 100000;
            string[] fileEntries = { strPendingEventFolder };
            XTRMEvent eventTester = new XTRMEvent();
            List<string> myConfigs = new List<string>();
            // We load each XTRMRoot Config File (once) and hold on to it for repeated use both in the loading of events (from XML) and in the evaluation of XDB events.
            List<XTRMRoot> myConfigLoaders = new List<XTRMRoot>();
            XmlWriter myCheckedEvent;
            // For Each File in the folder, need to consume into XTRMEvent(s).
            try
            {
                if (File.Exists(strPendingEventFolder))
                {
                    //fileEntries = strPendingEventFolder;
                }
                else if (Directory.Exists(strPendingEventFolder))
                {
                    fileEntries = Directory.GetFiles(strPendingEventFolder);
                }
                else
                {
                    //Error
                    rc = -1;
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                myConfigs = XTRMObject.getDictionaryEntries("XTRMRootConfigFile");
                myConfigLoaders.Clear();
                foreach (string thisConfig in myConfigs)
                {
                    XTRMRoot thisLoader = new XTRMRoot(ResolveText(thisConfig, XDictionary));
                    thisLoader.ParseConfig(2, true);
                    myConfigLoaders.Add(thisLoader);
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                foreach (string fileName in fileEntries)
                {
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
                            // Going to write the same file with comments.
                            // OK, proceed!
                            XmlWriterSettings settings = new XmlWriterSettings();
                            settings.OmitXmlDeclaration = false;
                            settings.ConformanceLevel = ConformanceLevel.Document;
                            settings.CloseOutput = true;
                            settings.Indent = true;
                            settings.Encoding = Encoding.Unicode;
                            myCheckedEvent = XmlWriter.Create(fileName, settings);
                            myCheckedEvent.WriteStartDocument();
                            myCheckedEvent.WriteStartElement("XTRMRoot");
                            // This will result in 0 or more XTRMEvents to process.
                            foreach (XTRMEvent thisEvent in myEventLoader.events)
                            {
                                try
                                {
                                    myCheckedEvent.WriteStartElement("XTRMEvent");
                                    myCheckedEvent.WriteAttributeString("ID", thisEvent.eventSerial.ToString());
                                    myCheckedEvent.WriteWhitespace("\n");

                                    // Check the Event!
                                    foreach (XTRMRoot thisLoader in myConfigLoaders)
                                    {
                                        List<XTRMJob> theseJobs = new List<XTRMJob>();
                                        rc = FindXMatch(thisLoader, thisEvent, out theseJobs);
                                        if (rc > 0)
                                        {
                                            if (theseJobs.Count > 0)
                                            {
                                                foreach (XTRMJob thisJob in theseJobs)
                                                {
                                                    myCheckedEvent.WriteComment(string.Format("CONFIG = {0} ------ FOLLOWING JOB MATCHED ------", thisLoader.name));
                                                    myCheckedEvent.WriteWhitespace("\n");
                                                    if (thisLoader.configFile.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("FILE = {0}", thisLoader.configFile));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    if (thisLoader.includeFile.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("INCLUDE = {0}", thisLoader.includeFile));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    // Display the Job Information and other info (job data) depending upon detail requested.
                                                    // Add to XML Output as Comments.
                                                    if (thisJob.containingComponent.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("XComponent = {0}", thisJob.containingComponent));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    if (thisJob.containingElement.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("XElement = {0}", thisJob.containingElement));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    if (thisJob.containingToolkit.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("XToolkit = {0}", thisJob.containingToolkit));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    if (thisJob.containingWorkflow.Length > 0)
                                                    {
                                                        myCheckedEvent.WriteComment(string.Format("XWorkflow = {0}", thisJob.containingWorkflow));
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    string strComment = "Job Name :\t";
                                                    strComment += thisJob.jobName;
                                                    myCheckedEvent.WriteComment(strComment);
                                                    myCheckedEvent.WriteWhitespace("\n");
                                                    foreach (XTRMTask thisTask in thisJob.tasks)
                                                    {
                                                        strComment = string.Format("\tTask Name : \t {0}", thisTask.taskName);
                                                        myCheckedEvent.WriteComment(strComment);
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                        strComment = string.Format("\tTask Exec : \t {0}", thisTask.taskExecutable);
                                                        myCheckedEvent.WriteComment(strComment);
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                        int count = 1;
                                                        foreach (string thisParm in thisTask.parms)
                                                        {
                                                            strComment = string.Format("\t\t<Parm{0}>\t = \t {1} ", count++, thisParm);
                                                            myCheckedEvent.WriteComment(strComment);
                                                            myCheckedEvent.WriteWhitespace("\n");
                                                        }
                                                    }

                                                    Dictionary<string, string> jobData = new Dictionary<string, string>();
                                                    try
                                                    {
                                                        myCheckedEvent.WriteComment("STATIC JOB DATA ------");
                                                        myCheckedEvent.WriteWhitespace("\n");

                                                        foreach (KeyValuePair<string, string> kvp in thisJob.config)
                                                        {
                                                            //saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                                                            jobData[kvp.Key.ToString().ToUpper()] = kvp.Value;
                                                            strComment = string.Format("\t\t<{0}>\t=\t{1} ", kvp.Key.ToString().ToUpper(), kvp.Value);
                                                            myCheckedEvent.WriteComment(strComment);
                                                            myCheckedEvent.WriteWhitespace("\n");
                                                        }

                                                        myCheckedEvent.WriteComment("DYNAMIC JOB DATA ------");
                                                        myCheckedEvent.WriteWhitespace("\n");

                                                        foreach (KeyValuePair<string, string> kvp in dynConfig)
                                                        {
                                                            //saveJobData(jobSerial, kvp.Key.ToString().ToUpper(), kvp.Value);
                                                            jobData[kvp.Key.ToString().ToUpper()] = kvp.Value;
                                                            strComment = string.Format("\t\t<{0}>\t=\t{1} ", kvp.Key.ToString().ToUpper(), kvp.Value);
                                                            myCheckedEvent.WriteComment(strComment);
                                                            myCheckedEvent.WriteWhitespace("\n");
                                                        }

                                                        myCheckedEvent.WriteComment("------------------------------------------------------------------");
                                                        myCheckedEvent.WriteWhitespace("\n");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        XLogger(1157, -1, string.Format("Saving Job Data; Message={0}", ex.Message));
                                                    }
                                                }
                                                myCheckedEvent.WriteComment("*** END MATCHES ------");
                                                myCheckedEvent.WriteWhitespace("\n");
                                            }
                                            else
                                            {
                                                myCheckedEvent.WriteComment("NO JOBS MATCHED ------");
                                                myCheckedEvent.WriteWhitespace("\n");
                                            }
                                        }
                                        else
                                        {
                                            myCheckedEvent.WriteComment("NO MATCHES ------");
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    myCheckedEvent.WriteStartElement("Source");
                                    myCheckedEvent.WriteString(thisEvent.eventSource);
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    myCheckedEvent.WriteStartElement("Action");
                                    myCheckedEvent.WriteString(thisEvent.eventAction);
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    myCheckedEvent.WriteStartElement("DateStamp");
                                    myCheckedEvent.WriteString(thisEvent.eventDate);
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    myCheckedEvent.WriteStartElement("User");
                                    myCheckedEvent.WriteString(thisEvent.eventUser);
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    myCheckedEvent.WriteStartElement("PIN");
                                    myCheckedEvent.WriteString(thisEvent.eventUUID);
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    myCheckedEvent.WriteStartElement("Status");
                                    myCheckedEvent.WriteString("0");
                                    myCheckedEvent.WriteEndElement();
                                    myCheckedEvent.WriteWhitespace("\n");
                                    if (thisEvent.eventParm1 != null)
                                    {
                                        if (thisEvent.eventParm1.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm1");
                                            myCheckedEvent.WriteString(thisEvent.eventParm1);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm2 != null)
                                    {
                                        if (thisEvent.eventParm2.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm2");
                                            myCheckedEvent.WriteString(thisEvent.eventParm2);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm3 != null)
                                    {
                                        if (thisEvent.eventParm3.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm3");
                                            myCheckedEvent.WriteString(thisEvent.eventParm3);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm4 != null)
                                    {
                                        if (thisEvent.eventParm4.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm4");
                                            myCheckedEvent.WriteString(thisEvent.eventParm4);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm5 != null)
                                    {
                                        if (thisEvent.eventParm5.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm5");
                                            myCheckedEvent.WriteString(thisEvent.eventParm5);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm6 != null)
                                    {
                                        if (thisEvent.eventParm6.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm6");
                                            myCheckedEvent.WriteString(thisEvent.eventParm6);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm7 != null)
                                    {
                                        if (thisEvent.eventParm7.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm7");
                                            myCheckedEvent.WriteString(thisEvent.eventParm7);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm8 != null)
                                    {
                                        if (thisEvent.eventParm8.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm8");
                                            myCheckedEvent.WriteString(thisEvent.eventParm8);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm9 != null)
                                    {
                                        if (thisEvent.eventParm9.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm9");
                                            myCheckedEvent.WriteString(thisEvent.eventParm9);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    if (thisEvent.eventParm10 != null)
                                    {
                                        if (thisEvent.eventParm10.Length > 0)
                                        {
                                            myCheckedEvent.WriteStartElement("Parm10");
                                            myCheckedEvent.WriteString(thisEvent.eventParm10);
                                            myCheckedEvent.WriteEndElement();
                                            myCheckedEvent.WriteWhitespace("\n");
                                        }
                                    }
                                    myCheckedEvent.WriteEndElement();   // XTRMEvent
                                }
                                catch (Exception ex)
                                {
                                    XLogger(1106, rc, string.Format("Caught Event Loader Exception; Folder={0}; UUID={1}; Message={2}", strPendingEventFolder, thisEvent.eventUUID, ex.Message));
                                }
                            }
                            myCheckedEvent.WriteEndElement();   // XTRMRoot
                            myCheckedEvent.Flush();
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
                }
            }
            catch (Exception ex)
            {
                XLogger(1108, -1, string.Format("ProcessPendingEvents::Get Files Exception; Folder={0}; Message={1}", strPendingEventFolder, ex.Message));
            }
            return rc;
        }
    }
}
