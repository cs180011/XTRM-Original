using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace XTRMlib
{
    // Error Number = 2212
    public class XTRMConfigEvent : XTRMEvent
    {
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
            XTRMEvent thisEvent = null;
            if (lVariant == 1)
            {
                thisEvent = new XTRMEvent();
                thisEvent.Initialize(-1);
            }
            else
            {
                thisEvent = new XTRMEvent();
            }
            thisEvent.variant = lVariant;

            try
            {
                // Load the reader with the data file and ignore all white space nodes.     
                context = new XmlParserContext(null, null, null, XmlSpace.None);
                reader = new XmlTextReader(XmlFragment, XmlNodeType.Element, context);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                // Parse the file and display each of the nodes.
                bool bResult = reader.Read();
                string outerXML;
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
                            switch (elementName.ToUpper())
                            {
                                case "THENJOB": // XTRMJob
                                    outerXML = reader.ReadOuterXml();
                                    XTRMJob thisJob = (XTRMJob)XTRMJob.consumeXML(myConfig, outerXML, 0, true);
                                    thisEvent.eventJobs.Add(thisJob);
                                    bProcessed = true;
                                    break;
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = new Dictionary<string, string>();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                //   Add to the current dictionary!
                                // XConfig
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    //myDictionaryLoader = new XDictionaryLoader();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                            }
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
                                    if (elementAttributes.ContainsKey("Serial"))
                                    {
                                        // Try to instantiate the XTRMEvent Object!
                                        int myEventID = Convert.ToInt32(elementAttributes["Serial"]);
                                        thisEvent.Initialize(myEventID);
                                    }
                                    if (elementAttributes.ContainsKey("Action"))
                                    {
                                        thisEvent.eventAction = elementAttributes["Action"];
                                    }
                                    if (elementAttributes.ContainsKey("Source"))
                                    {
                                        thisEvent.eventSource = elementAttributes["Source"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    case "SERIAL": // Serial
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "SOURCE": // Source
                                        lElementType = 2;
                                        bResult = reader.Read();
                                        break;
                                    case "ACTION": // Action
                                        lElementType = 3;
                                        bResult = reader.Read();
                                        break;
                                    //case "NORMALPATH": // NormalPath
                                    //    lElementType = 4;
                                    //    bResult = reader.Read();
                                    //    break;
                                    case "SUFFIX": // Suffix
                                        lElementType = 5;
                                        bResult = reader.Read();
                                        break;
                                    case "REGEX": // Regex
                                        lElementType = 6;
                                        bResult = reader.Read();
                                        break;
                                    case "UUID": // UUID
                                        lElementType = 7;
                                        bResult = reader.Read();
                                        break;
                                    case "NORMALPATH": // Parm1
                                    case "NORMALNAME":
                                    case "PRISMFULLNAME":
                                    case "P4FULLNAME":
                                    case "PARM1":
                                        lElementType = 8;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMPATH":
                                    case "P4VERSION":  // P4Version
                                    case "PARM2": // Parm2
                                        lElementType = 9;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMNAME":
                                    case "P4CHANGELIST":
                                    case "PARM3": // Parm3
                                        lElementType = 10;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMVERSION":
                                    case "P4TYPE":
                                    case "PARM4": // Parm4
                                        lElementType = 11;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMBRANCH":
                                    case "P4SIZE":
                                    case "PARM5": // Parm5
                                        lElementType = 12;
                                        bResult = reader.Read();
                                        break;
                                    case "P4CLIENT":
                                    case "PRISMLOCALNAME":
                                    case "PARM6": // Parm6
                                        lElementType = 13;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMTIME":
                                    case "P4TIME":
                                    case "PARM7": // Parm7
                                        lElementType = 14;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM8": // Parm8
                                        lElementType = 15;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM9": // Parm9
                                        lElementType = 16;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM10": // Parm10
                                        lElementType = 17;
                                        bResult = reader.Read();
                                        break;
                                    case "USER": // User
                                        lElementType = 18;
                                        bResult = reader.Read();
                                        break;
                                    case "PIN": // PIN
                                        lElementType = 19;
                                        bResult = reader.Read();
                                        break;
                                    case "TAG": // Tag
                                        lElementType = 20;
                                        bResult = reader.Read();
                                        break;
                                    case "DATESTAMP": // Date
                                        lElementType = 21;
                                        bResult = reader.Read();
                                        break;
                                    case "EVENTDATE": // Date (new format that imports directly).
                                        lElementType = 22;
                                        bResult = reader.Read();
                                        break;
                                    case "STATUS": // Date (new format that imports directly).
                                        lElementType = 23;
                                        bResult = reader.Read();
                                        break;
                                    case "PATH": // Date (new format that imports directly).
                                        lElementType = 24;
                                        bResult = reader.Read();
                                        break;
                                    case "DESC": // Date (new format that imports directly).
                                        lElementType = 25;
                                        bResult = reader.Read();
                                        break;
                                    //case "THENJOB": // XTRMJob
                                    //    outerXML = reader.ReadOuterXml();
                                    //    XTRMJob thisJob = (XTRMJob)XTRMJob.consumeXML(myConfig, outerXML, 0, true);
                                    //    thisEvent.eventJobs.Add(thisJob);
                                    //    break;
                                    // Reset Dictionary!
                                    // XConfig
                                    //case "BASECONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    myDictionaryLoader = new XDictionaryLoader();
                                    //    myConfig = new Dictionary<string, string>();
                                    //    myDictionaryLoader.Augment(myConfig, outerXML);
                                    //    break;
                                    //   Add to the current dictionary!
                                    // XConfig
                                    //case "WITHCONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    //myDictionaryLoader = new XDictionaryLoader();
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
                                case 1:     // Serial
                                    thisEvent.eventSerial = Convert.ToInt16(reader.Value);
                                    break;
                                case 2:     // Source
                                    thisEvent.eventSource = reader.Value;
                                    break;
                                case 3:     // Action
                                    thisEvent.eventAction = reader.Value;
                                    break;
                                case 4:     // NormalPath
                                    thisEvent.normalPath.Add(reader.Value);
                                    break;
                                case 5:     // Suffix
                                    thisEvent.suffix.Add(reader.Value);
                                    break;
                                case 6:     // Regex
                                    thisEvent.regex.Add(reader.Value);
                                    break;
                                case 7:     // eventUUID
                                    thisEvent.eventUUID = reader.Value;
                                    break;
                                case 8:     // eventParm1
                                    thisEvent.eventParm1 = reader.Value;
                                    thisEvent.normalPath.Add(reader.Value);
                                    break;
                                case 9:     // eventParm2
                                    thisEvent.eventParm2 = reader.Value;
                                    break;
                                case 10:     // eventParm3
                                    thisEvent.eventParm3 = reader.Value;
                                    break;
                                case 11:     // eventParm4
                                    thisEvent.eventParm4 = reader.Value;
                                    break;
                                case 12:     // eventParm5
                                    thisEvent.eventParm5 = reader.Value;
                                    break;
                                case 13:     // eventParm6
                                    thisEvent.eventParm6 = reader.Value;
                                    break;
                                case 14:     // eventParm7
                                    thisEvent.eventParm7 = reader.Value;
                                    break;
                                case 15:     // eventParm8
                                    thisEvent.eventParm8 = reader.Value;
                                    break;
                                case 16:     // eventParm9
                                    thisEvent.eventParm9 = reader.Value;
                                    break;
                                case 17:     // eventParm10
                                    thisEvent.eventParm10 = reader.Value;
                                    break;
                                case 18:     // eventUser
                                    thisEvent.eventUser = reader.Value;
                                    break;
                                case 19:     // eventPIN
                                    thisEvent.eventPIN = reader.Value;
                                    break;
                                case 20:     // eventTag
                                    thisEvent.eventTag = reader.Value;
                                    break;
                                case 21:     // eventDate
                                    string inputDate = reader.Value;
                                    int year, month, day, hour, min, sec;
                                    DateTime dateStamp;
                                    // If contains "/", then use method 2!
                                    if (inputDate.Contains("/"))
                                    {
                                        //year = Convert.ToInt16(inputDate.Substring(6, 4));
                                        //month = Convert.ToInt16(inputDate.Substring(0, 2));
                                        //day = Convert.ToInt16(inputDate.Substring(3, 2));
                                        //hour = Convert.ToInt16(inputDate.Substring(11, 2));
                                        //min = Convert.ToInt16(inputDate.Substring(14, 2));
                                        //sec = Convert.ToInt16(inputDate.Substring(17, 2));
                                        dateStamp = DateTime.Parse(inputDate, System.Globalization.CultureInfo.InvariantCulture);

                                    }
                                    else
                                    {
                                        year = Convert.ToInt16(inputDate.Substring(0, 4));
                                        month = Convert.ToInt16(inputDate.Substring(4, 2));
                                        day = Convert.ToInt16(inputDate.Substring(6, 2));
                                        hour = Convert.ToInt16(inputDate.Substring(9, 2));
                                        min = Convert.ToInt16(inputDate.Substring(12, 2));
                                        sec = Convert.ToInt16(inputDate.Substring(15, 2));
                                        dateStamp = new DateTime(year, month, day, hour, min, sec);
                                    }
                                    //inputDate = "6/7/2012 06:00:00";
                                    //thisEvent.eventDate = dateStamp.ToString();
                                    //thisEvent.eventDate = inputDate;
                                    break;
                                case 22:     // eventDate (mm/dd/yyyy hh24:mi:ss)
                                    thisEvent.eventDate = reader.Value;
                                    break;
                                case 23:     // Status
                                    //thisEvent. = reader.Value;
                                    break;
                                case 24:     // Path
                                    //thisEvent = reader.Value;
                                    break;
                                case 25:     // Description
                                    //thisEvent.eventDate = reader.Value;
                                    break;
                                case 26:     // eventHost
                                    thisEvent.eventHost = reader.Value;
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
            thisEvent.config = myConfig;
            return thisEvent;
        }

    }
    public class XTRMFileEvent : XTRMEvent
    {
    }
    public class XTRMCommandEvent : XTRMEvent
    {
    }
    public class XTRMPrismEvent : XTRMEvent
    {
    }
    public class XTRMP4VEvent : XTRMEvent
    {
    }
    public class XTRMEvent : XTRMObject
    {
        //
        // Variants
        //      0 Regular Event
        //      1 IfEvent
        //      2 EventFilter
        //////////////////////////////////////////////////////
        // XLATOR CONFIG SUPPORT
        // Can contain a list of jobs
        public int variant = 0;
        public Dictionary<String, String> config;
        public List<XTRMJob> eventJobs;
        public string eventPath;
        public List<string> normalPath = new List<string>();
        public List<string> suffix = new List<string>();
        public List<string> regex = new List<string>();
        public XEventMetaData meta = new XEventMetaData();
        //////////////////////////////////////////////////////

        SqlDataAdapter myAdapter;
        SqlCommandBuilder myCommandBuilder;
        DataTable myEvents = new DataTable();
        DataRow[] theseEvents = null;

        public string banner = "";
        //XLogger XLog;
        //string className = "XTRMEvent";
        //int pState = -1;

        // Accessible Members.
        //public int variant { get; set; }
        public int eventSerial { get; set; }
        public string eventSource { get; set; }
        public string eventAction { get; set; }
        public string eventDate { get; set; }
        public int eventState { get; set; }
        public string eventUser { get; set; }
        public string eventPIN { get; set; }
        public string eventParm1 { get; set; }
        public string eventParm2 { get; set; }
        public string eventParm3 { get; set; }
        public string eventParm4 { get; set; }
        public string eventParm5 { get; set; }
        public string eventParm6 { get; set; }
        public string eventParm7 { get; set; }
        public string eventParm8 { get; set; }
        public string eventParm9 { get; set; }
        public string eventParm10 { get; set; }
        public string eventUUID { get; set; }
        public string eventProcessed { get; set; }
        public string eventTag { get; set; }
        public string eventHost { get; set; }
        public int eventChain { get; set; }

        // Constructor
        // 
        public XTRMEvent() 
        {
            // Default, no initialization.
            Clear();
            //pState = 0;
            eventJobs = new List<XTRMJob>();
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
        //public override bool Clear()
        //{
        //    myEvents.Clear();
        //}
        public override bool Clear()
        {
            eventPath = "";
            eventSerial = -1;
            eventSource = null;
            eventAction = null;
            eventDate = DateTime.Now.ToString();
            eventState = -1;
            eventUser = null;
            eventPIN = null;
            eventParm1 = null;
            eventParm2 = null;
            eventParm3 = null;
            eventParm4 = null;
            eventParm5 = null;
            eventParm6 = null;
            eventParm7 = null;
            eventParm8 = null;
            eventParm9 = null;
            eventParm10 = null;
            eventUUID = Guid.NewGuid().ToString();
            eventProcessed = DateTime.Now.ToString();
            normalPath = new List<string>();
            eventChain = -1;
            base.Clear();
            //changeUser = Environment.UserName;
            //changeDate = DateTime.Now.ToString();
            //changeTag = "";
            //changeState = 0;

            return true;
        }
        public override int Initialize(int lSerial)
        {
            int rc = 0;
            Clear();
            eventSerial = lSerial;
            if (pState >= 0)    // ONLY if we are using XDB!
            {
                // Check the Connection.
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
                            XLogger(2200, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2201, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            SqlDataAdapter myFakeAdapter;
                            // Try to get the Serial (if >= 0).
                            String strTemp = "select * from Events where Event_Serial={0} ";
                            String strSelectCommand = String.Format(strTemp, (int)lSerial);

                            myEvents.Clear();

                            myFakeAdapter = new SqlDataAdapter();
                            myCommandBuilder = new SqlCommandBuilder(myFakeAdapter);
                            myCommandBuilder.QuotePrefix = "[";
                            myCommandBuilder.QuoteSuffix = "]";
                            myFakeAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            myFakeAdapter.TableMappings.Add("Table", myEvents.TableName);
                            pState = myFakeAdapter.Fill(myEvents);


                            myAdapter = new SqlDataAdapter();
                            myAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            myAdapter.TableMappings.Add("Table", myEvents.TableName);
                            pState = myAdapter.Fill(myEvents);

                            // add handlers
                            myAdapter.RowUpdating += new SqlRowUpdatingEventHandler(OnRowUpdating);
                            myAdapter.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

                            myAdapter.InsertCommand = myCommandBuilder.GetInsertCommand().Clone();
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.Both;
                            myAdapter.UpdateCommand = myCommandBuilder.GetUpdateCommand().Clone();
                            myAdapter.DeleteCommand = myCommandBuilder.GetDeleteCommand().Clone();

                            //myAdapter.InsertCommand.CommandText = String.Concat(myAdapter.InsertCommand.CommandText,
                            //        "; SELECT MyTableID=SCOPE_IDENTITY()");
                            string insertSuffix = "; SELECT Event_Serial=SCOPE_IDENTITY()";
                            myAdapter.InsertCommand.CommandText += insertSuffix;
                            myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                            //SqlParameter[] aParams = new SqlParameter[myCommandBuilder.GetInsertCommand().Parameters.Count];
                            //myCommandBuilder.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //myCommandBuilder.GetInsertCommand().Parameters.Clear();

                            SqlParameter identParam = new SqlParameter("@id", SqlDbType.BigInt, 0, "Event_Serial");
                            identParam.Direction = ParameterDirection.Output;

                            //for (int i = 0; i < aParams.Length; i++)
                            //{
                            //    myAdapter.InsertCommand.Parameters.Add(aParams[i]);
                            //} 


                            myAdapter.InsertCommand.Parameters.Add(identParam);
                            string test = myAdapter.InsertCommand.Parameters["@id"].ToString();
                            //SqlCommandBuilder bldr = new SqlCommandBuilder(myAdapter);
                            //SqlCommand cmdInsert = new SqlCommand(bldr.GetInsertCommand().CommandText, XConnection);
                            //cmdInsert.CommandText += ";Select SCOPE_IDENTITY() as id";

                            //SqlParameter[] aParams = new SqlParameter[bldr.GetInsertCommand().Parameters.Count];
                            //bldr.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //bldr.GetInsertCommand().Parameters.Clear();

                            //for (int i = 0; i < aParams.Length; i++)
                            //{
                            //    cmdInsert.Parameters.Add(aParams[i]);
                            //}

                            //myAdapter.InsertCommand = cmdInsert;
                            //pState = myAdapter.Fill(myEvents);
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                        }
                        catch (SqlException ex)
                        {
                            rc = ex.ErrorCode;
                            XLogger(2202, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2203, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                        }
                    }
                    else
                    {
                        rc = -1;
                        pState = -1;
                    }
                }
                else
                {
                    rc = -1;
                    pState = -1;
                }
                try
                {
                    theseEvents = myEvents.Select();
                    if (theseEvents.Length > 0)
                    {
                        // Clear Member Values!
                        Clear();
                        eventSerial = (int)theseEvents[0]["Event_Serial"];
                        rc = eventSerial;
                        eventSource = (string)theseEvents[0]["Event_Source"];
                        eventAction = (string)theseEvents[0]["Event_Action"];
                        eventDate = theseEvents[0]["Event_Date"].ToString();
                        eventState = (int)theseEvents[0]["Event_State"];
                        eventUser = (string)theseEvents[0]["Event_User"];
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_PIN"]))
                        {
                            eventPIN = (string)theseEvents[0]["Event_PIN"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm1"]))
                        {
                            eventParm1 = (string)theseEvents[0]["Event_Parm1"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm2"]))
                        {
                            eventParm2 = (string)theseEvents[0]["Event_Parm2"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm3"]))
                        {
                            eventParm3 = (string)theseEvents[0]["Event_Parm3"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm4"]))
                        {
                            eventParm4 = (string)theseEvents[0]["Event_Parm4"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm5"]))
                        {
                            eventParm5 = (string)theseEvents[0]["Event_Parm5"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm6"]))
                        {
                            eventParm6 = (string)theseEvents[0]["Event_Parm6"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm7"]))
                        {
                            eventParm7 = (string)theseEvents[0]["Event_Parm7"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm8"]))
                        {
                            eventParm8 = (string)theseEvents[0]["Event_Parm8"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm9"]))
                        {
                            eventParm9 = (string)theseEvents[0]["Event_Parm9"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm10"]))
                        {
                            eventParm10 = (string)theseEvents[0]["Event_Parm10"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Chain"]))
                        {
                            eventChain = (int)theseEvents[0]["Event_Chain"];
                        }

                        changeUser = (string)theseEvents[0]["Change_User"];
                        changeDate = (string)theseEvents[0]["Change_Date"].ToString();
                        if (!DBNull.Value.Equals(theseEvents[0]["Change_Tag"]))
                        {
                            changeTag = (string)theseEvents[0]["Change_Tag"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Change_State"]))
                        {
                            changeState = (int)theseEvents[0]["Change_State"];
                        }
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2204, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2205, rc, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                }
            }
            return rc;
        }
        public int Initialize(string UUID)
        {
            int rc = 0;
            Clear();
            eventSerial = -1;
            if (pState >= 0)    // ONLY if we are using XDB!
            {
                // Check the Connection.
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
                            XLogger(2200, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2201, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            SqlDataAdapter myFakeAdapter;
                            // Try to get the Serial (if >= 0).
                            String strTemp = "select * from Events where Event_UUID='{0}' ";
                            String strSelectCommand = String.Format(strTemp, UUID);

                            myEvents.Clear();

                            myFakeAdapter = new SqlDataAdapter();
                            myCommandBuilder = new SqlCommandBuilder(myFakeAdapter);
                            myCommandBuilder.QuotePrefix = "[";
                            myCommandBuilder.QuoteSuffix = "]";
                            myFakeAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            myFakeAdapter.TableMappings.Add("Table", myEvents.TableName);
                            pState = myFakeAdapter.Fill(myEvents);


                            myAdapter = new SqlDataAdapter();
                            myAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            myAdapter.TableMappings.Add("Table", myEvents.TableName);
                            pState = myAdapter.Fill(myEvents);

                            // add handlers
                            myAdapter.RowUpdating += new SqlRowUpdatingEventHandler(OnRowUpdating);
                            myAdapter.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

                            myAdapter.InsertCommand = myCommandBuilder.GetInsertCommand().Clone();
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.Both;
                            myAdapter.UpdateCommand = myCommandBuilder.GetUpdateCommand().Clone();
                            myAdapter.DeleteCommand = myCommandBuilder.GetDeleteCommand().Clone();

                            //myAdapter.InsertCommand.CommandText = String.Concat(myAdapter.InsertCommand.CommandText,
                            //        "; SELECT MyTableID=SCOPE_IDENTITY()");
                            string insertSuffix = "; SELECT Event_Serial=SCOPE_IDENTITY()";
                            myAdapter.InsertCommand.CommandText += insertSuffix;
                            myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                            //SqlParameter[] aParams = new SqlParameter[myCommandBuilder.GetInsertCommand().Parameters.Count];
                            //myCommandBuilder.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //myCommandBuilder.GetInsertCommand().Parameters.Clear();

                            SqlParameter identParam = new SqlParameter("@id", SqlDbType.BigInt, 0, "Event_Serial");
                            identParam.Direction = ParameterDirection.Output;

                            //for (int i = 0; i < aParams.Length; i++)
                            //{
                            //    myAdapter.InsertCommand.Parameters.Add(aParams[i]);
                            //} 


                            myAdapter.InsertCommand.Parameters.Add(identParam);
                            string test = myAdapter.InsertCommand.Parameters["@id"].ToString();
                            //SqlCommandBuilder bldr = new SqlCommandBuilder(myAdapter);
                            //SqlCommand cmdInsert = new SqlCommand(bldr.GetInsertCommand().CommandText, XConnection);
                            //cmdInsert.CommandText += ";Select SCOPE_IDENTITY() as id";

                            //SqlParameter[] aParams = new SqlParameter[bldr.GetInsertCommand().Parameters.Count];
                            //bldr.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //bldr.GetInsertCommand().Parameters.Clear();

                            //for (int i = 0; i < aParams.Length; i++)
                            //{
                            //    cmdInsert.Parameters.Add(aParams[i]);
                            //}

                            //myAdapter.InsertCommand = cmdInsert;
                            //pState = myAdapter.Fill(myEvents);
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                        }
                        catch (SqlException ex)
                        {
                            rc = ex.ErrorCode;
                            XLogger(2202, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2211, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                        }
                    }
                    else
                    {
                        rc = -1;
                        pState = -1;
                    }
                }
                else
                {
                    rc = -1;
                    pState = -1;
                }
                try
                {
                    theseEvents = myEvents.Select();
                    if (theseEvents.Length > 0)
                    {
                        // Clear Member Values!
                        Clear();
                        eventSerial = (int)theseEvents[0]["Event_Serial"];
                        rc = eventSerial;
                        eventSource = (string)theseEvents[0]["Event_Source"];
                        eventAction = (string)theseEvents[0]["Event_Action"];
                        eventDate = theseEvents[0]["Event_Date"].ToString();
                        eventState = (int)theseEvents[0]["Event_State"];
                        eventUser = (string)theseEvents[0]["Event_User"];
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_PIN"]))
                        {
                            eventPIN = (string)theseEvents[0]["Event_PIN"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm1"]))
                        {
                            eventParm1 = (string)theseEvents[0]["Event_Parm1"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm2"]))
                        {
                            eventParm2 = (string)theseEvents[0]["Event_Parm2"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm3"]))
                        {
                            eventParm3 = (string)theseEvents[0]["Event_Parm3"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm4"]))
                        {
                            eventParm4 = (string)theseEvents[0]["Event_Parm4"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm5"]))
                        {
                            eventParm5 = (string)theseEvents[0]["Event_Parm5"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm6"]))
                        {
                            eventParm6 = (string)theseEvents[0]["Event_Parm6"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm7"]))
                        {
                            eventParm7 = (string)theseEvents[0]["Event_Parm7"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm8"]))
                        {
                            eventParm8 = (string)theseEvents[0]["Event_Parm8"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm9"]))
                        {
                            eventParm9 = (string)theseEvents[0]["Event_Parm9"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Parm10"]))
                        {
                            eventParm10 = (string)theseEvents[0]["Event_Parm10"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_UUID"]))
                        {
                            eventUUID = (string)theseEvents[0]["Event_UUID"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Event_Chain"]))
                        {
                            eventChain = (int)theseEvents[0]["Event_Chain"];
                        }

                        changeUser = (string)theseEvents[0]["Change_User"];
                        changeDate = (string)theseEvents[0]["Change_Date"].ToString();
                        if (!DBNull.Value.Equals(theseEvents[0]["Change_Tag"]))
                        {
                            changeTag = (string)theseEvents[0]["Change_Tag"];
                        }
                        if (!DBNull.Value.Equals(theseEvents[0]["Change_State"]))
                        {
                            changeState = (int)theseEvents[0]["Change_State"];
                        }
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2204, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2205, rc, string.Format("UUID={0}; Message={1}", UUID, ex.Message));
                }
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
                try
                {
                    // Add to persistence.
                    DataRow newEvent = myEvents.NewRow();

                    // Assign
                    newEvent["Event_Serial"] = eventSerial;
                    newEvent["Event_Source"] = eventSource;
                    newEvent["Event_Action"] = eventAction;
                    newEvent["Event_Date"] = eventDate;
                    newEvent["Event_State"] = eventState;
                    newEvent["Event_User"] = eventUser;
                    newEvent["Event_PIN"] = eventPIN;
                    newEvent["Event_Parm1"] = eventParm1;
                    newEvent["Event_Parm2"] = eventParm2;
                    newEvent["Event_Parm3"] = eventParm3;
                    newEvent["Event_Parm4"] = eventParm4;
                    newEvent["Event_Parm5"] = eventParm5;
                    newEvent["Event_Parm6"] = eventParm6;
                    newEvent["Event_Parm7"] = eventParm7;
                    newEvent["Event_Parm8"] = eventParm8;
                    newEvent["Event_Parm9"] = eventParm9;
                    newEvent["Event_Parm10"] = eventParm10;
                    newEvent["Event_UUID"] = eventUUID;
                    newEvent["Event_Processed"] = eventProcessed;
                    if (eventChain != -1)
                    {
                        newEvent["Event_Chain"] = eventChain;
                    }
                    newEvent["Change_User"] = Environment.UserName;
                    newEvent["Change_Date"] = DateTime.Now;
                    newEvent["Change_Tag"] = eventTag;
                    newEvent["Event_Host"] = eventHost;
                    newEvent["Change_State"] = 0;

                    myEvents.Rows.Add(newEvent);

                    int urows = myAdapter.Update(myEvents);
                    // Accept (commit).
                    myEvents.AcceptChanges();
                    string test = myAdapter.InsertCommand.Parameters["@p1"].ToString();
                    string test2 = myAdapter.InsertCommand.Parameters["@id"].ToString();
                    theseEvents = myEvents.Select();
                    if (theseEvents.Length > 0)
                    {
                        // 
                        int testResult = (int)myEvents.Rows[0]["Event_Serial"];
                        eventSerial = (int)theseEvents[0]["Event_Serial"];
                        rc = eventSerial;
                        pState = 1; // XDB Object!
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2206, rc, string.Format("Serial={0}; Message={1}", eventSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2207, rc, string.Format("Serial={0}; Message={1}", eventSerial, ex.Message));
                }
                finally
                {
                }
            }
            else if (pState > 0)
            {
                // Update persistence.
                try
                {
                    theseEvents[0]["Event_Serial"] = eventSerial;
                    theseEvents[0]["Event_Source"] = eventSource;
                    theseEvents[0]["Event_Action"] = eventAction;
                    theseEvents[0]["Event_Date"] = eventDate;
                    theseEvents[0]["Event_State"] = eventState;
                    theseEvents[0]["Event_User"] = eventUser;
                    theseEvents[0]["Event_PIN"] = eventPIN;
                    //theseEvents[0]["Event_UUID"] = eventUUID;
                    theseEvents[0]["Event_Processed"] = eventProcessed;
                    theseEvents[0]["Event_Parm1"] = eventParm1;
                    theseEvents[0]["Event_Parm2"] = eventParm2;
                    theseEvents[0]["Event_Parm3"] = eventParm3;
                    theseEvents[0]["Event_Parm4"] = eventParm4;
                    theseEvents[0]["Event_Parm5"] = eventParm5;
                    theseEvents[0]["Event_Parm6"] = eventParm6;
                    theseEvents[0]["Event_Parm7"] = eventParm7;
                    theseEvents[0]["Event_Parm8"] = eventParm8;
                    theseEvents[0]["Event_Parm9"] = eventParm9;
                    theseEvents[0]["Event_Parm10"] = eventParm10;
                    if (eventChain != -1)
                    {
                        theseEvents[0]["Event_Chain"] = eventChain;
                    }
                    theseEvents[0]["Change_User"] = Environment.UserName;
                    theseEvents[0]["Change_Date"] = DateTime.Now;
                    theseEvents[0]["Change_Tag"] = changeTag;
                    theseEvents[0]["Change_State"] = changeState;

                    int urows = myAdapter.Update(myEvents);
                    // Accept (commit).
                    myEvents.AcceptChanges();
                    rc = eventSerial;
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2208, rc, string.Format("Serial={0}; Message={1}", eventSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2209, rc, string.Format("Serial={0}; Message={1}", eventSerial, ex.Message));
                }
                finally
                {
                }
            }
            else
            {
                // Local Object (no persistence).
            }

            return rc;
        }
        public int renderXML(string XMLOutFile, bool bDeep = false)
        {
            // Render XML to represent the object.
            // if bDeep is false, then ONLY do this object.
            // if bDeep is true, then also do recursive objects.
            return 0;
        }
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
            XTRMEvent thisEvent = null;
            if (lVariant == 1)
            {
                thisEvent = new XTRMEvent();
                thisEvent.Initialize(-1);
            }
            else
            {
                thisEvent = new XTRMEvent();
            }
            thisEvent.variant = lVariant;

            try
            {
                // Load the reader with the data file and ignore all white space nodes.     
                context = new XmlParserContext(null, null, null, XmlSpace.None);
                reader = new XmlTextReader(XmlFragment, XmlNodeType.Element, context);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                // Parse the file and display each of the nodes.
                bool bResult = reader.Read();
                string outerXML;
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
                            switch (elementName.ToUpper())
                            {
                                case "THENJOB": // XTRMJob
                                    outerXML = reader.ReadOuterXml();
                                    XTRMJob thisJob = (XTRMJob)XTRMJob.consumeXML(myConfig, outerXML, 0, true);
                                    thisEvent.eventJobs.Add(thisJob);
                                    bProcessed = true;
                                    break;
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = new Dictionary<string, string>();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                                //   Add to the current dictionary!
                                // XConfig
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    //myDictionaryLoader = new XDictionaryLoader();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    bProcessed = true;
                                    break;
                            }
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
                                    if (elementAttributes.ContainsKey("Serial"))
                                    {
                                        // Try to instantiate the XTRMEvent Object!
                                        int myEventID = Convert.ToInt32(elementAttributes["Serial"]);
                                        thisEvent.Initialize(myEventID);
                                    }
                                    if (elementAttributes.ContainsKey("Action"))
                                    {
                                        thisEvent.eventAction = elementAttributes["Action"];
                                    }
                                    if (elementAttributes.ContainsKey("Source"))
                                    {
                                        thisEvent.eventSource = elementAttributes["Source"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    case "SERIAL": // Serial
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "SOURCE": // Source
                                        lElementType = 2;
                                        bResult = reader.Read();
                                        break;
                                    case "ACTION": // Action
                                        lElementType = 3;
                                        bResult = reader.Read();
                                        break;
                                    //case "NORMALPATH": // NormalPath
                                    //    lElementType = 4;
                                    //    bResult = reader.Read();
                                    //    break;
                                    case "SUFFIX": // Suffix
                                        lElementType = 5;
                                        bResult = reader.Read();
                                        break;
                                    case "REGEX": // Regex
                                        lElementType = 6;
                                        bResult = reader.Read();
                                        break;
                                    case "UUID": // UUID
                                        lElementType = 7;
                                        bResult = reader.Read();
                                        break;
                                    case "NORMALPATH": // Parm1
                                    case "NORMALNAME":
                                    case "PRISMFULLNAME":
                                    case "P4FULLNAME":
                                    case "PARM1":
                                        lElementType = 8;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMPATH":
                                    case "P4VERSION":  // P4Version
                                    case "PARM2": // Parm2
                                        lElementType = 9;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMNAME":
                                    case "P4CHANGELIST":
                                    case "PARM3": // Parm3
                                        lElementType = 10;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMVERSION":
                                    case "P4TYPE":
                                    case "PARM4": // Parm4
                                        lElementType = 11;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMBRANCH":
                                    case "P4SIZE":
                                    case "PARM5": // Parm5
                                        lElementType = 12;
                                        bResult = reader.Read();
                                        break;
                                    case "P4CLIENT":
                                    case "PRISMLOCALNAME":
                                    case "PARM6": // Parm6
                                        lElementType = 13;
                                        bResult = reader.Read();
                                        break;
                                    case "PRISMTIME":
                                    case "P4TIME":
                                    case "PARM7": // Parm7
                                        lElementType = 14;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM8": // Parm8
                                        lElementType = 15;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM9": // Parm9
                                        lElementType = 16;
                                        bResult = reader.Read();
                                        break;
                                    case "PARM10": // Parm10
                                        lElementType = 17;
                                        bResult = reader.Read();
                                        break;
                                    case "USER": // User
                                        lElementType = 18;
                                        bResult = reader.Read();
                                        break;
                                    case "PIN": // PIN
                                        lElementType = 19;
                                        bResult = reader.Read();
                                        break;
                                    case "TAG": // Tag
                                        lElementType = 20;
                                        bResult = reader.Read();
                                        break;
                                    case "DATESTAMP": // Date
                                        lElementType = 21;
                                        bResult = reader.Read();
                                        break;
                                    case "EVENTDATE": // Date (new format that imports directly).
                                        lElementType = 22;
                                        bResult = reader.Read();
                                        break;
                                    case "STATUS": // Date (new format that imports directly).
                                        lElementType = 23;
                                        bResult = reader.Read();
                                        break;
                                    case "PATH": // Date (new format that imports directly).
                                        lElementType = 24;
                                        bResult = reader.Read();
                                        break;
                                    case "DESC": // Date (new format that imports directly).
                                        lElementType = 25;
                                        bResult = reader.Read();
                                        break;
                                    //case "THENJOB": // XTRMJob
                                    //    outerXML = reader.ReadOuterXml();
                                    //    XTRMJob thisJob = (XTRMJob)XTRMJob.consumeXML(myConfig, outerXML, 0, true);
                                    //    thisEvent.eventJobs.Add(thisJob);
                                    //    break;
                                    // Reset Dictionary!
                                    // XConfig
                                    //case "BASECONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    myDictionaryLoader = new XDictionaryLoader();
                                    //    myConfig = new Dictionary<string, string>();
                                    //    myDictionaryLoader.Augment(myConfig, outerXML);
                                    //    break;
                                    //   Add to the current dictionary!
                                    // XConfig
                                    //case "WITHCONFIG":
                                    //    outerXML = reader.ReadOuterXml();
                                    //    //myDictionaryLoader = new XDictionaryLoader();
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
                                case 1:     // Serial
                                    thisEvent.eventSerial = Convert.ToInt16(reader.Value);
                                    break;
                                case 2:     // Source
                                    thisEvent.eventSource = reader.Value;
                                    break;
                                case 3:     // Action
                                    thisEvent.eventAction = reader.Value;
                                    break;
                                case 4:     // NormalPath
                                    thisEvent.normalPath.Add(reader.Value);
                                    break;
                                case 5:     // Suffix
                                    thisEvent.suffix.Add(reader.Value);
                                    break;
                                case 6:     // Regex
                                    thisEvent.regex.Add(reader.Value);
                                    break;
                                case 7:     // eventUUID
                                    thisEvent.eventUUID = reader.Value;
                                    break;
                                case 8:     // eventParm1
                                    thisEvent.eventParm1 = reader.Value;
                                    thisEvent.normalPath.Add(reader.Value);
                                    break;
                                case 9:     // eventParm2
                                    thisEvent.eventParm2 = reader.Value;
                                    break;
                                case 10:     // eventParm3
                                    thisEvent.eventParm3 = reader.Value;
                                    break;
                                case 11:     // eventParm4
                                    thisEvent.eventParm4 = reader.Value;
                                    break;
                                case 12:     // eventParm5
                                    thisEvent.eventParm5 = reader.Value;
                                    break;
                                case 13:     // eventParm6
                                    thisEvent.eventParm6 = reader.Value;
                                    break;
                                case 14:     // eventParm7
                                    thisEvent.eventParm7 = reader.Value;
                                    break;
                                case 15:     // eventParm8
                                    thisEvent.eventParm8 = reader.Value;
                                    break;
                                case 16:     // eventParm9
                                    thisEvent.eventParm9 = reader.Value;
                                    break;
                                case 17:     // eventParm10
                                    thisEvent.eventParm10 = reader.Value;
                                    break;
                                case 18:     // eventUser
                                    thisEvent.eventUser = reader.Value;
                                    break;
                                case 19:     // eventPIN
                                    thisEvent.eventPIN = reader.Value;
                                    break;
                                case 20:     // eventTag
                                    thisEvent.eventTag = reader.Value;
                                    break;
                                case 21:     // eventDate
                                    string inputDate = reader.Value;
                                    int year, month, day, hour, min, sec;
                                    DateTime dateStamp;
                                    // If contains "/", then use method 2!
                                    if (inputDate.Contains("/"))
                                    {
                                        //year = Convert.ToInt16(inputDate.Substring(6, 4));
                                        //month = Convert.ToInt16(inputDate.Substring(0, 2));
                                        //day = Convert.ToInt16(inputDate.Substring(3, 2));
                                        //hour = Convert.ToInt16(inputDate.Substring(11, 2));
                                        //min = Convert.ToInt16(inputDate.Substring(14, 2));
                                        //sec = Convert.ToInt16(inputDate.Substring(17, 2));
                                        dateStamp = DateTime.Parse(inputDate, System.Globalization.CultureInfo.InvariantCulture);

                                    }
                                    else
                                    {
                                        year = Convert.ToInt16(inputDate.Substring(0, 4));
                                        month = Convert.ToInt16(inputDate.Substring(4, 2));
                                        day = Convert.ToInt16(inputDate.Substring(6, 2));
                                        hour = Convert.ToInt16(inputDate.Substring(9, 2));
                                        min = Convert.ToInt16(inputDate.Substring(12, 2));
                                        sec = Convert.ToInt16(inputDate.Substring(15, 2));
                                        dateStamp = new DateTime(year, month, day, hour, min, sec);
                                    }
                                    //inputDate = "6/7/2012 06:00:00";
                                    //thisEvent.eventDate = dateStamp.ToString();
                                    //thisEvent.eventDate = inputDate;
                                    break;
                                case 22:     // eventDate (mm/dd/yyyy hh24:mi:ss)
                                    thisEvent.eventDate = reader.Value;
                                    break;
                                case 23:     // Status
                                    //thisEvent. = reader.Value;
                                    break;
                                case 24:     // Path
                                    //thisEvent = reader.Value;
                                    break;
                                case 25:     // Description
                                    //thisEvent.eventDate = reader.Value;
                                    break;
                                case 26:     // eventHost
                                    thisEvent.eventHost = reader.Value;
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
            thisEvent.config = myConfig;
            return thisEvent;
        }
    }
}
