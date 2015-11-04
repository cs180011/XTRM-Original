using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace XTRMlib
{
    class XTRMWorkFlow : XTRMObject
    {
        //////////////////////////////////////////////////////
        // XLATOR CONFIG SUPPORT
        // Can contain a list of jobs
        Dictionary<String, String> config;
        public List<XTRMEvent> workflowEvents = new List<XTRMEvent>();
        //////////////////////////////////////////////////////

        DataTable myComponents = new DataTable();
        //DataRow[] theseComponents = null;

        // Accessible Members.
        public int componentSerial { get; set; }
        public string name { get; set; }

        public XTRMWorkFlow()
        {
            // Default, no initialization.
            Clear();
            pState = 0;
            name = "";
        }
        private void OnRowUpdating(object sender, SqlRowUpdatingEventArgs e)
        {
            //PrintEventArgs(e);
            return;
        }

        // handler for RowUpdated event
        private void OnRowUpdated(object sender, SqlRowUpdatedEventArgs e)
        {
            //PrintEventArgs(e);
            return;
        }
        public override bool Clear()
        {
            base.Clear();
            return true;
        }
        public override int Initialize(int lSerial = -1)
        {
            int rc = 0;
            Clear();
            if (pState >= 0)    // ONLY if we are using XDB!
            {
            }
            return rc;
        }
        public int Initialize(string project, string track, string component)
        {
            int rc = 0;
            //name = "";
            Clear();
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
        //public Dictionary<String, String> createDictionary()
        //{
        // Populate the Dictionary!
        //    XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
        //    return myDictionaryLoader.ParseXML();
        //}
        /*
        public new static XObject consumeXML(Dictionary<String, String> myConfig, string XmlElement, bool bDeep = false)
        {
            //XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            //myDictionaryLoader.Augment();
            // 
            // Consume XML to create the XComponent object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            return new XComponent();
        }
         * */
        public new static XTRMObject consumeXML(Dictionary<String, String> existingConfig, string XmlFragment, int lVariant = 0, bool bDeep = false)
        {
            //XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            //myDictionaryLoader.Augment();
            // 
            // Consume XML to create the XComponent object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            XmlTextReader reader = null;
            XmlParserContext context = null;
            Dictionary<String, String> myConfig = new Dictionary<string, string>(existingConfig);
            XTRMWorkFlow thisWorkflow = new XTRMWorkFlow();
            int lElementType = 0;
            XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            Dictionary<String, String> elementAttributes;
            try
            {
                // Load the reader with the data file and ignore all white space nodes.     
                context = new XmlParserContext(null, null, null, XmlSpace.None);
                reader = new XmlTextReader(XmlFragment, XmlNodeType.Element, context);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                // Parse the file and display each of the nodes.
                bool bResult = reader.Read();
                string outerXML;
                //XDictionaryLoader myDictionaryLoader;
                while (bResult)
                {
                    bool bProcessed = false;
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            string elementName = reader.Name;
                            switch (elementName.ToUpper())
                            {
                                case "IFEVENT": // XEvent
                                    outerXML = reader.ReadOuterXml();
                                    XTRMEvent thisEvent = (XTRMEvent)XTRMEvent.consumeXML(myConfig, outerXML, 1, true);
                                    thisWorkflow.workflowEvents.Add(thisEvent);
                                    bProcessed = true;
                                    break;
                                // Reset Dictionary!
                                // XConfig
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    //myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = new Dictionary<string, string>();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                //   Add to the current dictionary!
                                // XConfig
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
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
                                    if (elementAttributes.ContainsKey("Name"))
                                    {
                                        // Try to instantiate the XEvent Object!
                                        thisWorkflow.name = elementAttributes["Name"];
                                    }
                                    if (elementAttributes.ContainsKey("ID"))
                                    {
                                        // Try to instantiate the XEvent Object!
                                        thisWorkflow.name = elementAttributes["ID"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    //case "IFEVENT": // XEvent
                                    //    outerXML = reader.ReadOuterXml();
                                    //    XEvent thisEvent = (XEvent)XEvent.consumeXML(myConfig, outerXML, 1, true);
                                    //    thisWorkflow.workflowEvents.Add(thisEvent);
                                    //    break;
                                    // Reset Dictionary!
                                    // XConfig
                                    //case "BASECONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    //myDictionaryLoader = new XDictionaryLoader();
                                    //    myConfig = new Dictionary<string, string>();
                                    //    myDictionaryLoader.Augment(myConfig, outerXML);
                                    //    break;
                                    //   Add to the current dictionary!
                                    // XConfig
                                    //case "WITHCONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    myDictionaryLoader = new XDictionaryLoader();
                                    //    myDictionaryLoader.Augment(myConfig, outerXML);
                                    //    break;
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
                                //case 1:     // PARMS
                                //    thisTask.parms.Add(reader.Value);
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
                XLogger(3100, -1, string.Format("XML={0}; Message={1}", XmlFragment, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            thisWorkflow.config = myConfig;
            return thisWorkflow;
        }
    }
}