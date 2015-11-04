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
using System.Xml.Linq;

namespace XTRMlib
{
    // Error Number = 2212
    public class XTRMFSEntity : XTRMObject
    {
        //
        // Variants
        //      0 Regular Event
        //      1 IfEvent
        //      2 EventFilter
        //////////////////////////////////////////////////////
        // XLATOR CONFIG SUPPORT
        // Can contain a list of jobs
        int variant = 0;
        //public Dictionary<String, String> config;
        //public List<XJob> eventJobs;
        //public List<string> normalPath = new List<string>();
        //List<string> suffix = new List<string>();
        //List<string> regex = new List<string>();
        //////////////////////////////////////////////////////

        DataTable myEvents = new DataTable();
        DataRow[] theseEvents = null;

        public string banner = "";

        // Accessible Members.
        public string entityPath { get; set; }
        public string entityPattern { get; set; }
        public int entityRecurse { get; set; }
        public int entityBufsize { get; set; }
        public int entityHoldTime { get; set; }
        public string entityTag { get; set; }
        public string entitySource { get; set; }
        public string entityUser { get; set; }
        public string entityEventPath { get; set; }

        // Constructor
        // 
        public XTRMFSEntity()
        {
            // Default, no initialization.
            Clear();
            //pState = 0;
            className = "XTRMFSEntity";
        }
        // handler for RowUpdating event
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
            entityPath = "";
            entityPattern = "";
            entityRecurse = 0;
            entityBufsize = 32768;
            entityTag = "";
            entitySource = "";
            entityUser = "";
            entityEventPath = "";
            base.Clear();
            return true;
        }
        public override int Initialize(int lSerial)
        {
            int rc = 0;
            Clear();
            return rc;
        }
        public override int Save()
        {
            int rc = 0;
            return rc;
        }
        public override int renderXML(bool bDeep = false)
        {
            // Render XML to represent the object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            return 0;
        }
        public static XTRMObject consumeXML(string XmlFragment, int lVariant = 0, bool bDeep = false)
        {
            //XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
            //myDictionaryLoader.Augment();
            // 
            // Consume XML to create the XComponent object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            XmlTextReader reader = null;
            XmlParserContext context = null;
            XTRMFSEntity thisEntity = null;
            if (lVariant == 1)
            {
                thisEntity = new XTRMFSEntity();
                thisEntity.Initialize(-1);
            }
            else
            {
                thisEntity = new XTRMFSEntity();
            }
            thisEntity.variant = lVariant;

            try
            {
                // Load the reader with the data file and ignore all white space nodes.     
                context = new XmlParserContext(null, null, null, XmlSpace.None);
                reader = new XmlTextReader(XmlFragment, XmlNodeType.Element, context);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                // Parse the file and display each of the nodes.
                bool bResult = reader.Read();
                int lElementType = 0;
                XDictionaryLoader myDictionaryLoader = new XDictionaryLoader();
                Dictionary<String, String> elementAttributes;
                while (bResult)
                {
                    bool bProcessed = false;
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            string elementName = reader.Name;
                            if (!bProcessed)
                            {
                                // May wish to get all the attributes here for new elements!
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
                                    // Check to see if ID is supplied!
                                    //if (elementAttributes["ID"] != null)
                                    if (elementAttributes.ContainsKey("Tag"))
                                    {
                                        thisEntity.entityTag = elementAttributes["Tag"];
                                    }
                                    if (elementAttributes.ContainsKey("Source"))
                                    {
                                        thisEntity.entitySource = elementAttributes["Source"];
                                    }
                                    if (elementAttributes.ContainsKey("User"))
                                    {
                                        thisEntity.entityUser = elementAttributes["User"];
                                    }
                                    if (elementAttributes.ContainsKey("EventPath"))
                                    {
                                        thisEntity.entityEventPath = elementAttributes["EventPath"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    case "PATH": // Path
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "PATTERN": // Pattern
                                        lElementType = 2;
                                        bResult = reader.Read();
                                        break;
                                    case "RECURSE": // Recurse
                                        lElementType = 3;
                                        bResult = reader.Read();
                                        break;
                                    case "BUFSIZE": // Bufsize
                                        lElementType = 4;
                                        bResult = reader.Read();
                                        break;
                                    case "HOLDTIME": // HoldTime
                                        lElementType = 5;
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
                                case 1:     // Path
                                    thisEntity.entityPath = reader.Value;
                                    break;
                                case 2:     // Pattern
                                    thisEntity.entityPattern = reader.Value;
                                    break;
                                case 3:     // Recurse
                                    thisEntity.entityRecurse = Convert.ToInt16(reader.Value);
                                    break;
                                case 4:     // Bufsize
                                    thisEntity.entityBufsize = Convert.ToInt16(reader.Value);
                                    break;
                                case 5:     // HoldTime
                                    thisEntity.entityHoldTime = Convert.ToInt16(reader.Value);
                                    break;
                                default:
                                    break;
                            }
                            lElementType = 0;
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
                XLogger(2210, -1, string.Format("XML={0}; Message={1}", XmlFragment, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return thisEntity;
        }
    }
}

