using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Data.Common;
using System.Xml;

namespace XTRMlib
{
    class XTRMRoot : XTRMObject
    {
        public List<XTRMEvent> eventFilters { get; set; }
        public List<XTRMEvent> events { get; set; }
        public List<string> eventFolders { get; set; }
        public List<XTRMWorkFlow> workflows { get; set; }
        public string name = "";
        public string includeFile = "";
        DataTable myEvents = new DataTable();
        DataRow[] theseEvents = null;
        public string configFile { get; set; }

        public XTRMRoot(string filename, bool bConnect = false) 
            : base(false)   // XLator does not map to XDB!
        {
            eventFilters = new List<XTRMEvent>();
            events = new List<XTRMEvent>();
            eventFolders = new List<string>();
            workflows = new List<XTRMWorkFlow>();
            pState = 0;
            className = "XTRM";
            configFile = filename;
            name = "";
        }
        public override bool Clear()
        {
            eventFilters.Clear();
            events.Clear();
            eventFolders.Clear();
            workflows.Clear();
            base.Clear();
            return true;
        }
        public override int Initialize(int lSerial)
        {
            int rc = 0;
            Clear();
            if (pState >= 0)    // ONLY if we are using XDB!
            {
            }
            return rc;
        }
        public override int Save()
        {
            int rc = 0;
            // Try/Catch!
            // Check the pState!
            if (pState == 0)
            {
            }
            else if (pState > 0)
            {
                // Update persistence.
            }
            else
            {
            }
            return rc;
        }
        public override int renderXML(bool bDeep = false)
        {
            //
            // Render XML to represent the object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            return 0;
        }
        public int ParseConfig(int lVariant = 0, bool bDeep = false)
        {
            // 
            // Consume XML to create the XLator object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            XmlTextReader reader = null;

            int rc = -1;
            string connectString = XTRMObject.getDictionaryEntry("TaskConnectString");
            Dictionary<String, String> myConfig = new Dictionary<string, string>(XTRMObject.XDictionary);
            string outerXML;
            int lElementType = 0;
            XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            Dictionary<String, String> elementAttributes;
            events.Clear();
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
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                case "EVENTFILTER":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMEvent thisFilter = (XTRMEvent)XTRMEvent.consumeXML(myConfig, outerXML, 2, true);
                                    eventFilters.Add(thisFilter);
                                    bProcessed = true;
                                    break;
                                case "EVENT":
                                case "XTRMEVENT":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMEvent thisEvent = (XTRMEvent)XTRMEvent.consumeXML(myConfig, outerXML, 1, true);
                                    events.Add(thisEvent);
                                    bProcessed = true;
                                    break;
                                //   Add to the current dictionary!
                                case "WORKFLOW":
                                case "XTRMWORKFLOW":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMWorkFlow thisFlow = (XTRMWorkFlow)XTRMWorkFlow.consumeXML(myConfig, outerXML, 1, true);
                                    workflows.Add(thisFlow);
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
                                        elementAttributes.Add(reader.Name, reader.Value);
                                        reader.MoveToNextAttribute();
                                    }
                                    if (elementAttributes.ContainsKey("Name"))
                                    {
                                        // Try to instantiate the XEvent Object!
                                        name = elementAttributes["Name"];
                                    }
                                    if (elementAttributes.ContainsKey("File"))
                                    {
                                        // Try to instantiate the XEvent Object!
                                        includeFile = elementAttributes["File"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                switch (elementName.ToUpper())
                                {
                                    case "INCLUDE":
                                        if (includeFile != null)
                                        {
                                            IncludeConfig(ResolveText(includeFile, XDictionary), myConfig, lVariant, bDeep);
                                        }
                                        else
                                        {
                                            // Include File Ineffective.
                                            XLogger(2401, -1, string.Format("XLator::parseConfig(); ConfigFile={0}; Invalid Include.", configFile));
                                        }
                                        bResult = reader.Read();
                                        break;
                                    case "EVENTFOLDER":
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "XTRM":
                                    case "XTRMROOT":
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
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Comment:
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.EndElement:
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Text:
                            switch (lElementType)
                            {
                                case 1:     // EventFolder
                                    eventFolders.Add(ResolveText(reader.Value, XDictionary));
                                    break;
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
                XLogger(2400, -1, string.Format("XTRMRoot::parseConfig(); ConfigFile={0}; Message={1}", configFile, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return rc;
        }
        public int IncludeConfig(string inFile, Dictionary<String, String> myConfig, int lVariant = 0, bool bDeep = false)
        {
            // 
            // Consume XML to create the XLator object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            int rc = -1;
            XmlTextReader reader = null;
            string connectString = XTRMObject.getDictionaryEntry("TaskConnectString");
            string outerXML;
            int lElementType = 0;
            XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            Dictionary<String, String> elementAttributes;
            try
            {
                // Load the reader with the data file and ignore all white space nodes.         
                reader = new XmlTextReader(inFile);
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
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                case "EVENTFILTER":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMEvent thisFilter = (XTRMEvent)XTRMEvent.consumeXML(myConfig, outerXML, 2, true);
                                    eventFilters.Add(thisFilter);
                                    bProcessed = true;
                                    break;
                                case "XTRMEVENT":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMEvent thisEvent = (XTRMEvent)XTRMEvent.consumeXML(myConfig, outerXML, 1, true);
                                    events.Add(thisEvent);
                                    bProcessed = true;
                                    break;
                                case "XTRMWORKFLOW":
                                    outerXML = reader.ReadOuterXml();
                                    XTRMWorkFlow thisFlow = (XTRMWorkFlow)XTRMWorkFlow.consumeXML(myConfig, outerXML, 1, true);
                                    workflows.Add(thisFlow);
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
                                        elementAttributes.Add(reader.Name, reader.Value);
                                        reader.MoveToNextAttribute();
                                    }
                                    if (elementAttributes.ContainsKey("Name"))
                                    {
                                        // Try to instantiate the XEvent Object!
                                        name = elementAttributes["Name"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                //switch (reader.Name)
                                {
                                    case "EVENTFOLDER":
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "XTRM":
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
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Comment:
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.EndElement:
                            bResult = reader.Read();
                            break;
                        case XmlNodeType.Text:
                            switch (lElementType)
                            {
                                case 1:     // EventFolder
                                    eventFolders.Add(ResolveText(reader.Value, XDictionary));
                                    break;
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
                XLogger(2402, -1, string.Format("XLator::IncludeConfig(); ConfigFile={0}; Message={1}", configFile, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }


            return rc;
        }
    }
}
