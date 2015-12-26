using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
//using System.Security.Permissions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using XTRMlib;

namespace XTRMlib
{
    public class XFileAction : FileSystemEventArgs
    {
        public DateTime actionTime = DateTime.Now;
        //public FileSystemEventArgs myEvent = null;
        public XFileAction(WatcherChangeTypes thisType, string thisDir, string thisName) : base(thisType, thisDir, thisName) { }
        //    actionTime = DateTime.Now;
        //myEvent = thisEvent;
        //}
        public XFileAction(FileSystemEventArgs thisEvent) : base(thisEvent.ChangeType, thisEvent.FullPath, thisEvent.Name) { }
        //{
        //actionTime = DateTime.Now;
        //myEvent = thisEvent;
        //}
    }
    public class XTRMFileAgentCore : XTRMObject
    {
        private static Semaphore _pool;
        static EventLog myLog;
        List<string> myConfigs = new List<string>();
        public string agentTag { get; set; }
        public string agentSource { get; set; }
        public string agentUser { get; set; }
        public string agentEventPath { get; set; }
        public List<XTRMFSEntity> entities { get; set; }
        List<FileSystemWatcher> watchList = new List<FileSystemWatcher>();
        public static List<XFileAction> XFileList = new List<XFileAction>();
        static readonly ConcurrentQueue<XFileAction> ChangesQueue = new ConcurrentQueue<XFileAction>();

        // Structures for Held Events.
        //static Dictionary<string, XEvent> eventInventory = new Dictionary<string, XEvent>();
        //static List<string> eventInventory = new List<string>();
        //static Dictionary<DateTime, XEvent> eventOrder = new Dictionary<DateTime, XEvent>();
        static Dictionary<string, XTRMEvent> eventOrder = new Dictionary<string, XTRMEvent>();

        public XTRMFileAgentCore(EventLog thisLog)
        {
            myLog = thisLog;
            entities = new List<XTRMFSEntity>();
        }
        public override bool Clear()
        {
            agentTag = "Monitor";
            agentSource = "NTFS";
            agentUser = "jmptool";
            agentEventPath = "";
            base.Clear();
            return true;
        }
        public int Initialize()
        {
            int rc = 0;
            _pool = new Semaphore(1, 1);
            Clear();
            SetLogID(3);
            myConfigs = XTRMObject.getDictionaryEntries("XAgentConfigFile");
            // Read file, call parseConfig, then iterate around addPath().
            foreach (string thisAgentFile in myConfigs)
            {
                // Read each file
                ParseConfig(ResolveText(thisAgentFile, XDictionary), true);
                //ParseConfig(thisAgentFile, true);
            }
            foreach (XTRMFSEntity thisEntity in entities)
            {
                // addEntity
                addEntity(thisEntity);
            }
            return rc;
        }
        protected static void Alert(string subject, string text)
        {
            // Send email to XBot Admin.
            string contact = XTRMObject.getDictionaryEntry("AdminMail", "chris.swift@jmp.com");
            XTRMObject.Notify(contact, null, subject, text);
        }
        public int Run(int pass = 0, bool logBeat = false)
        {
            int rc = 0;
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
            return rc;
        }
        public int Quiesce()
        {
            myLog.WriteEntry("Quiesce");

            // Begin watching.
            foreach (FileSystemWatcher watcher in watchList)
            {
                watcher.EnableRaisingEvents = false;
            }
            ProcessEvents();
            return FlushEvents(0);
        }
        public int ProcessEvents()
        {
            int rc = 0;
            string tempstr = "";
            try
            {
                _pool.WaitOne();
                // Process Everything in XFileList
                XFileAction thisAction;
                while (ChangesQueue.TryDequeue(out thisAction))
                //foreach (XFileAction thisAction in XFileList)
                {
                    XTRMFSEntity thisEntity = findEntity(thisAction);
                    XTRMEvent thisEvent = new XTRMEvent();
                    thisEvent.Initialize(-1);
                    //  <XEvent ID="626">
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
                    //	</XEvent>
                    thisEvent.eventAction = thisAction.ChangeType.ToString();
                    thisEvent.eventDate = DateTime.Now.ToString();
                    thisEvent.eventState = -1;
                    thisEvent.eventParm1 = thisAction.FullPath;
                    string thisEventPath = "";
                    if (thisEntity == null)
                    {
                        // Use Defaults.
                        thisEvent.eventTag = agentTag;
                        thisEvent.eventSource = agentSource;
                        thisEvent.eventUser = agentUser;
                        //thisEventPath = agentEventPath;
                        thisEvent.meta.eventPath = agentEventPath;
                        thisEvent.meta.holdTime = 0;
                    }
                    else
                    {
                        // Use this entity, then defaults.
                        if (thisEntity.entityTag.Equals(""))
                        {
                            thisEvent.eventTag = agentTag;
                        }
                        else
                        {
                            thisEvent.eventTag = thisEntity.entityTag;
                        }
                        if (thisEntity.entitySource.Equals(""))
                        {
                            thisEvent.eventSource = agentSource;
                        }
                        else
                        {
                            thisEvent.eventSource = thisEntity.entitySource;
                        }
                        if (thisEntity.entityUser.Equals(""))
                        {
                            thisEvent.eventUser = agentUser;
                        }
                        else
                        {
                            thisEvent.eventUser = thisEntity.entityUser;
                        }
                        if (thisEntity.entityTag.Equals(""))
                        {
                            thisEvent.eventTag = agentTag;
                        }
                        else
                        {
                            thisEvent.eventTag = thisEntity.entityTag;
                        }
                        if (thisEntity.entityEventPath.Equals(""))
                        {
                            //thisEventPath = agentEventPath;
                            thisEvent.meta.eventPath = agentEventPath;
                        }
                        else
                        {
                            //thisEventPath = thisEntity.entityEventPath;
                            thisEvent.meta.eventPath = thisEntity.entityEventPath;
                        }
                        if (thisEntity.entityHoldTime.Equals(""))
                        {
                            thisEvent.meta.holdTime = 0;
                        }
                        else
                        {
                            thisEvent.meta.holdTime = thisEntity.entityHoldTime;
                        }
                    }

                    //thisEvent.eventDate = DateTime.Now.ToString();
                    thisEvent.eventDate = thisAction.actionTime.ToString();

                    // Add Event to Dictionaries (by time and by path::action).
                    thisEvent.eventPath = thisEventPath;
                    string eventKey = string.Format("{0}::{1}", thisEvent.eventParm1, thisEvent.eventAction);
                    // Are we holding this event already?
                    if (eventOrder.ContainsKey(eventKey).Equals(false))
                    {
                        //eventInventory.Add(eventKey);
                        //eventOrder.Add(DateTime.Now, thisEvent);
                        eventOrder.Add(eventKey, thisEvent);
                    }
                }
                //XFileList.Clear();
            }
            catch (Exception ex)
            {
                tempstr = string.Format("FileSystemWatcher Exception : {0}", ex.Message);
                myLog.WriteEntry(tempstr);
            }
            finally
            {
                _pool.Release();
            }
            return rc;
        }
        public int FlushEvents(int timeDelay = 0)
        {
            string tempstr = "";
            int count = 0;

            try
            {
                _pool.WaitOne();

                //myLog.WriteEntry("Flush Events");

                if (timeDelay > 0)
                {
                    timeDelay *= -1;
                }
                // Write Events that are older than configured time.
                //Dictionary<DateTime, XEvent>.KeyCollection eventKeys = eventOrder.Keys;
                List<string> eventKeys = new List<string>(eventOrder.Keys);
                eventKeys.Sort();
                foreach (string key in eventKeys)
                {
                    XTRMEvent thisEvent = eventOrder[key];
                    bool bReadyFlag = false;
                    //if (timeDelay < 0)
                    //{
                    //    bReadyFlag = true;
                    //}
                    if (timeDelay.Equals(-1))
                    {
                        bReadyFlag = true;
                    }
                    else if (thisEvent.meta.holdTime > 0)
                    {
                        if (timeDelay > 0)
                        {
                            timeDelay *= -1;
                        }
                        if ((DateTime.Now.AddSeconds((-1) * thisEvent.meta.holdTime)) > thisEvent.meta.entryTime)
                        {
                            bReadyFlag = true;
                        }
                    }
                    else if ((DateTime.Now.AddSeconds(timeDelay)) > thisEvent.meta.entryTime)
                    {
                        bReadyFlag = true;
                    }
                    if (bReadyFlag)
                    {
                        // to file...
                        if (Directory.Exists(thisEvent.meta.eventPath))
                        {
                            string randomFile = Path.GetRandomFileName();
                            randomFile = Regex.Replace(randomFile, @"\..*", @".xml");
                            string outputFile = string.Format("{0}\\{1}-{2}-{3}-{4}", thisEvent.meta.eventPath, thisEvent.eventSource, thisEvent.eventTag, thisEvent.eventAction, randomFile);
                            if (thisEvent.renderXML(outputFile).Equals(0))
                            {
                                eventOrder.Remove(key);
                                //eventOrder[key] = null;
                                //string eventKey = string.Format("{0}::{1}", thisEvent.eventParm1, thisEvent.eventAction);
                                //eventInventory.Remove(eventKey);
                                count++;
                            }
                        }
                        else
                        {
                            // Error
                            tempstr = string.Format("Event Path Does Not Exist (using default): {0}", thisEvent.eventPath);
                            myLog.WriteEntry(tempstr);
                            Directory.CreateDirectory("c:\\temp\\eventdefault");
                            string randomFile = Path.GetRandomFileName();
                            randomFile = Regex.Replace(randomFile, @"\..*", @".xml");
                            string outputFile = string.Format("{0}\\{1}-{2}-{3}-{4}", "c:\\temp\\eventdefault", thisEvent.eventSource, thisEvent.eventTag, thisEvent.eventAction, randomFile);
                            if (thisEvent.renderXML(outputFile).Equals(0))
                            {
                                eventOrder.Remove(key);
                                //eventOrder[key] = null;
                                //string eventKey = string.Format("{0}::{1}", thisEvent.eventParm1, thisEvent.eventAction);
                                //eventInventory.Remove(eventKey);
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tempstr = string.Format("FileSystemWatcher Exception : {0}", ex.Message);
                myLog.WriteEntry(tempstr);
            }
            finally
            {
                _pool.Release();
            }
            return count;
        }
        public int addEntity(XTRMFSEntity newEntity)
        {
            //string[] args = System.Environment.GetCommandLineArgs();
            try
            {
                if (Directory.Exists(newEntity.entityPath))
                {
                    // Set Properties for FileSystemWatcher.
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.InternalBufferSize = newEntity.entityBufsize;
                    watcher.Path = newEntity.entityPath;
                    /* Watch for changes in LastAccess and LastWrite times, and
                       the renaming of files or directories. */
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    //watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    //watcher.NotifyFilter = NotifyFilters.LastWrite;
                    // Only watch text files.
                    watcher.Filter = newEntity.entityPattern;
                    if (newEntity.entityRecurse.Equals(1))
                    {
                        watcher.IncludeSubdirectories = true;
                    }
                    else
                    {
                        watcher.IncludeSubdirectories = false;
                    }

                    // Add event handlers.
                    watcher.Changed += new FileSystemEventHandler(OnChanged);
                    watcher.Created += new FileSystemEventHandler(OnChanged);
                    watcher.Deleted += new FileSystemEventHandler(OnChanged);
                    watcher.Renamed += new RenamedEventHandler(OnRenamed);
                    watcher.Error += OnError;

                    // Begin watching.
                    watcher.EnableRaisingEvents = true;

                    // Wait for the user to quit the program.
                    //Console.WriteLine("Press \'q\' to quit the sample.");
                    //while (Console.Read() != 'q') ;

                    watchList.Add(watcher);
                }
                else
                {
                    //Error
                    string tempstr = string.Format("Path Not Valid : {0}", newEntity.entityPath);
                    myLog.WriteEntry(tempstr);
                }
            }
            catch (Exception ex)
            {
                string tempstr = string.Format("FileSystemWatcher Exception : {0}", ex.Message);
                myLog.WriteEntry(tempstr);
            }
            return 0;
        }
        // From the event, determine the entity to enable creation of XEvent!
        private XTRMFSEntity findEntity(FileSystemEventArgs e)
        {
            XTRMFSEntity matchEntity = null;
            foreach (XTRMFSEntity thisEntity in entities)
            {
                //e.FullPath
                FileInfo myFileInfo = new FileInfo(e.FullPath);
                string shortName = myFileInfo.Name;
                string pathName = myFileInfo.DirectoryName;
                //if (thisEntity.entityPath.Equals(pathName))
                if ((Directory.Exists(pathName)) && (pathName.Contains(thisEntity.entityPath)))
                {
                    matchEntity = thisEntity;
                }
                else if (thisEntity.entityPath.Equals(pathName))
                {
                    // Check the full path using Visual Basic LikeString() Method.
                    string fullPattern = thisEntity.entityPath + "\\" + thisEntity.entityPattern;
                    if (Operators.LikeString(e.FullPath, fullPattern, CompareMethod.Text))
                    {
                        //Console.WriteLine("This matched!");
                        matchEntity = thisEntity;
                    }
                }
                else
                {
                    if (thisEntity.entityRecurse.Equals(1) && pathName.Contains(thisEntity.entityPath))
                    {
                        // Check ony the file name using Visual Basic LikeString() Method.
                        string fullPattern = thisEntity.entityPath + "\\" + thisEntity.entityPattern;
                        if (Operators.LikeString(e.FullPath, fullPattern, CompareMethod.Text))
                        {
                            //Console.WriteLine("This matched!");
                            matchEntity = thisEntity;
                        }
                    }
                }
            }
            return matchEntity;
        }
        public int addPath(string thisPath)
        {
            //string[] args = System.Environment.GetCommandLineArgs();
            if (Directory.Exists(thisPath))
            {
                // Set Properties for FileSystemWatcher.
                FileSystemWatcher watcher = new FileSystemWatcher();
                //watcher.InternalBufferSize = 4 * 4096;
                watcher.Path = thisPath;
                /* Watch for changes in LastAccess and LastWrite times, and
                   the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watcher.Error += OnError;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                //Console.WriteLine("Press \'q\' to quit the sample.");
                //while (Console.Read() != 'q') ;

                watchList.Add(watcher);
            }
            else
            {
                //Error
                string tempstr = string.Format("Path Not Valid : {0}", thisPath);
                myLog.WriteEntry(tempstr);
            }
            return 0;
        }
        public int ParseConfig(string configFile, bool bDeep = false)
        {
            // 
            // Consume XML to create the XFSEntity objects.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            XmlTextReader reader = null;

            int rc = -1;
            string connectString = XTRMObject.getDictionaryEntry("TaskConnectString");
            string outerXML;
            int lElementType = 0;
            XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            Dictionary<String, String> elementAttributes;
            entities.Clear();
            try
            {
                // Load the reader with the data file and ignore all white space nodes.         
                reader = new XmlTextReader(configFile);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                // Parse the file and display each of the nodes.
                bool bResult = reader.Read();
                while (bResult)
                {
                    bool bProcessed = false;
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            string elementName = reader.Name;
                            switch (elementName.ToUpper())
                            {
                                case "XFSENTITY":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMFSEntity thisEntity = (XTRMFSEntity)XTRMFSEntity.consumeXML(outerXML, 1, true);
                                    entities.Add(thisEntity);
                                    bProcessed = true;
                                    break;
                            }
                            if (!bProcessed)
                            {
                                // May wish to get all the  attributes here for new elements!
                                elementAttributes = new Dictionary<String, String>();
                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    for (int i = 0; i < reader.AttributeCount; i++)
                                    {
                                        //reader.GetAttribute(i);
                                        elementAttributes.Add(reader.Name, reader.Value);
                                        reader.MoveToNextAttribute();
                                    }
                                    if (elementAttributes.ContainsKey("Tag"))
                                    {
                                        agentTag = elementAttributes["Tag"];
                                    }
                                    if (elementAttributes.ContainsKey("Source"))
                                    {
                                        agentSource = elementAttributes["Source"];
                                    }
                                    if (elementAttributes.ContainsKey("User"))
                                    {
                                        agentUser = elementAttributes["User"];
                                    }
                                    if (elementAttributes.ContainsKey("EventPath"))
                                    {
                                        agentEventPath = elementAttributes["EventPath"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    case "XFILEAGENTCONFIG":
                                        // Advance into Elements!
                                        reader.MoveToContent();
                                        bResult = reader.Read();
                                        break;
                                    default:
                                        bResult = reader.Read();
                                        break;
                                }
                            }
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            //writer.WriteProcessingInstruction(reader.Name, reader.Value);
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Comment:
                            //writer.WriteComment(reader.Value);
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.EndElement:
                            //writer.WriteFullEndElement();
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Text:
                            //Console.Write(reader.Value);
                            switch (lElementType)
                            {
                                default:
                                    break;
                            }
                            bResult = reader.Read();
                            break;
                        default:
                            bResult = reader.Read();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                exCount_XML++;
                XLogger(2400, -1, string.Format("XTRMFileAgent::parseConfig(); ConfigFile={0}; Message={1}", configFile, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }


            return rc;
        }
        // Define the event handlers. 
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            string tempstr = "";
            FileInfo thisEventFile = new FileInfo(e.FullPath);
            string thisName = thisEventFile.Name;
            string thisPath = thisEventFile.DirectoryName;
            XFileAction thisAction = new XFileAction(e.ChangeType, thisPath, thisName);
            ChangesQueue.Enqueue(thisAction);
            /*try
            {
                _pool.WaitOne();
                XFileList.Add(thisAction);
            }
            catch(Exception ex)
            {
                tempstr = string.Format("FileSystemWatcher Exception : {0}", ex.Message);
                myLog.WriteEntry(tempstr);
            }
            finally
            {
                _pool.Release();
            }*/

            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            tempstr = string.Format("Counts=({0},{1}); File: {2} {3}", ChangesQueue.Count, eventOrder.Count, e.FullPath, e.ChangeType);
            myLog.WriteEntry(tempstr);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            string tempstr;
            try
            {
                // Deleted...
                FileInfo myOldFileInfo = new FileInfo(e.OldFullPath);
                string oldName = myOldFileInfo.Name;
                string oldPath = myOldFileInfo.DirectoryName;
                FileSystemEventArgs deleted = new FileSystemEventArgs(System.IO.WatcherChangeTypes.Deleted, oldPath, oldName);
                //OnChanged(source, deleted);
                XFileAction sourceAction = new XFileAction(e.ChangeType, oldPath, oldName);
                ChangesQueue.Enqueue(sourceAction);
                // Created...
                FileInfo myNewFileInfo = new FileInfo(e.FullPath);
                string newName = myNewFileInfo.Name;
                string newPath = myNewFileInfo.DirectoryName;
                FileSystemEventArgs created = new FileSystemEventArgs(System.IO.WatcherChangeTypes.Created, newPath, newName);
                XFileAction targetAction = new XFileAction(e.ChangeType, newPath, newName);
                ChangesQueue.Enqueue(targetAction);
                //OnChanged(source, created);

                // Specify what is done when a file is renamed.
                Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
                tempstr = string.Format("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
                myLog.WriteEntry(tempstr);
            }
            catch (Exception ex)
            {
                tempstr = string.Format("FileSystemWatcher Exception : {0}", ex.Message);
                myLog.WriteEntry(tempstr);
            }
        }
        private static void OnError(object source, ErrorEventArgs e)
        {
            Console.WriteLine("Error!");
            Console.WriteLine(e.GetException());
            myLog.WriteEntry(e.GetException().ToString());
            Alert("FileSystemWatcher Error", e.GetException().ToString());
        }
        public int XLogger(int result, string logtext, int ID = 9900)
        {
            return XTRMObject.XLogger(ID, result, logtext);
        }
    }
}

