using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace XTRMlib
{
    // Error Number = 2321
    // Added Job Host 20150605
    public class XTRMJob : XTRMObject
    {
        //////////////////////////////////////////////////////
        // XLATOR CONFIG SUPPORT
        // Can contain a list of tasks
        public Dictionary<String, String> config;
        public List<XTRMTask> tasks = new List<XTRMTask>();
        public string containingWorkflow = "";
        public string containingComponent = "";
        public string containingElement = "";
        public string containingToolkit = "";
        //jobTasks. 
        
        //////////////////////////////////////////////////////
        SqlDataAdapter myAdapter;
        SqlCommandBuilder myCommandBuilder;
        DataTable myJobs = new DataTable();
        DataRow[] theseJobs = null;

        // Accessible Members.
        public int jobSerial { get; set; }
        public string jobHost { get; set; }
        public string jobName { get; set; }
        public int jobType { get; set; }
        public string jobStart { get; set; }
        public string jobStop { get; set; }
        public int jobSequence { get; set; }
        public int jobStatus { get; set; }
        public int jobResult { get; set; }
        public int jobPriority { get; set; }
        public int jobLimit { get; set; }
        public string jobDisplay { get; set; }
        public int jobRetention { get; set; }
        public int jobEvent { get; set; }

        public XTRMJob()
        {
            // Default, no initialization.
            Clear();
            className = "XTRMJob";
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
            containingElement = "";
            containingComponent = "";
            containingWorkflow = "";
            containingToolkit = "";
            jobSerial = -1;
            jobHost = "";
            jobName = "";
            jobType = -1;
            jobStart = null;
            jobStop = null;
            jobSequence = -1;
            jobStatus = -99;
            jobResult = 0;
            jobPriority = -1;
            jobLimit = -1;
            jobDisplay = "";
            jobRetention = -1;
            jobEvent = -1;
            tasks.Clear();
            base.Clear();
            return true;
        }
        public override int Initialize(int lSerial = -1)
        {
            int rc = 0;
            Clear();
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
                            XLogger(2300, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = ex.ErrorCode;
                        }
                        catch (Exception ex)
                        {
                            XLogger(2301, -1, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = -1;
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            myAdapter = new SqlDataAdapter();
                            // Try to get the Serial (if >= 0).

                            string strTemp = "select * ";
                            strTemp += " from WorkJobs a";
                            strTemp += " where a.Job_Serial = {0}";
                            String strSelectCommand = String.Format(strTemp, lSerial);

                            myJobs.Clear();

                            myCommandBuilder = new SqlCommandBuilder(myAdapter);
                            myAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            pState = myAdapter.Fill(myJobs);

                            // add handlers
                            myAdapter.RowUpdating += new SqlRowUpdatingEventHandler(OnRowUpdating);
                            myAdapter.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

                            myAdapter.InsertCommand = myCommandBuilder.GetInsertCommand().Clone();
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.Both;
                            myAdapter.UpdateCommand = myCommandBuilder.GetUpdateCommand().Clone();
                            myAdapter.DeleteCommand = myCommandBuilder.GetDeleteCommand().Clone();

                            //myAdapter.InsertCommand.CommandText = String.Concat(myAdapter.InsertCommand.CommandText,
                            //        "; SELECT MyTableID=SCOPE_IDENTITY()");
                            string insertSuffix = "; SELECT Job_Serial=SCOPE_IDENTITY()";
                            myAdapter.InsertCommand.CommandText += insertSuffix;
                            myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                            //SqlParameter[] aParams = new SqlParameter[myCommandBuilder.GetInsertCommand().Parameters.Count];
                            //myCommandBuilder.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //myCommandBuilder.GetInsertCommand().Parameters.Clear();

                            SqlParameter identParam = new SqlParameter("@id", SqlDbType.BigInt, 0, "Job_Serial");
                            identParam.Direction = ParameterDirection.Output;

                            myAdapter.InsertCommand.Parameters.Add(identParam);
                            string test = myAdapter.InsertCommand.Parameters["@id"].ToString();

                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                        }
                        catch (SqlException ex)
                        {
                            XLogger(2302, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = ex.ErrorCode;
                        }
                        catch (Exception ex)
                        {
                            XLogger(2303, -1, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = -1;
                        }
                    }
                    else
                    {
                        rc = -1;
                        pState = -1;
                        XLogger(2304, 0, string.Format("Connection Unable to be Opened"));
                    }
                }
                else
                {
                    rc = -1;
                    pState = -1;
                    XLogger(2305, 0, string.Format("No Connection Available"));
                }
                try
                {
                    theseJobs = myJobs.Select();
                    if (theseJobs.Length > 0)
                    {
                        // Clear Member Values!
                        Clear();
                        jobSerial = (int)theseJobs[0]["Job_Serial"];
                        rc = jobSerial;
                        jobHost = (string)theseJobs[0]["Job_Host"];
                        jobName = (string)theseJobs[0]["Job_Name"];
                        jobType = (int)theseJobs[0]["Job_Type"];
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Start"]))
                        {
                            jobStart = (string)theseJobs[0]["Job_Start"].ToString();
                        }
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Stop"]))
                        {
                            jobStop = (string)theseJobs[0]["Job_Stop"].ToString();
                        }
                        jobSequence = (int)theseJobs[0]["Job_Sequence"];
                        jobStatus = (int)theseJobs[0]["Job_Status"];
                        jobResult = (int)theseJobs[0]["Job_Result"];
                        jobPriority = (int)theseJobs[0]["Job_Priority"];
                        jobLimit = (int)theseJobs[0]["Job_Limit"];
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Display"]))
                        {
                            jobDisplay = (string)theseJobs[0]["Job_Display"];
                        }
                        jobRetention = (int)theseJobs[0]["Job_Retention"];
                        jobEvent = (int)theseJobs[0]["Job_Event"];

                        changeUser = (string)theseJobs[0]["Change_User"];
                        changeDate = (string)theseJobs[0]["Change_Date"].ToString();
                        // NEED TO ADD THESE!
                        //if (!DBNull.Value.Equals(theseComponents[0]["Change_Tag"]))
                        //{
                        //    changeTag = (string)theseComponents[0]["Change_Tag"];
                        //}
                        //if (!DBNull.Value.Equals(theseComponents[0]["Change_State"]))
                        //{
                        //    changeState = (int)theseComponents[0]["Change_State"];
                        //}
                        XTRMTask thisTask = new XTRMTask(false);
                        //int taskRC = thisTask.Initialize(jobSerial, "select * from WorkTasks a where a.Job_Serial = {0} and Task_Status >= 1 order by Task_Status desc");
                        int taskRC = thisTask.Initialize(jobSerial, "select * from WorkTasks a where a.Job_Serial = {0} and Task_Status >= 1 order by Task_Result asc");
                        if (taskRC > 0)
                        {
                            tasks.Add(thisTask);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2306, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2307, -1, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                }
            }
            return rc;
        }
        public int Initialize(string jobName)
        {
            int rc = 0;
            Clear();
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
                            XLogger(2308, ex.ErrorCode, string.Format("JobName={0}; Message={1}", jobName, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2309, -1, string.Format("JobName={0}; Message={1}", jobName, ex.Message));
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            myAdapter = new SqlDataAdapter();
                            // Try to get the Serial (if >= 0).

                            string strTemp = "select * ";
                            strTemp += " from WorkJobs a";
                            strTemp += " where a.Job_Name = '{0}'";
                            String strSelectCommand = String.Format(strTemp, jobName);

                            myJobs.Clear();

                            myCommandBuilder = new SqlCommandBuilder(myAdapter);
                            myAdapter.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            pState = myAdapter.Fill(myJobs);

                            // add handlers
                            myAdapter.RowUpdating += new SqlRowUpdatingEventHandler(OnRowUpdating);
                            myAdapter.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

                            myAdapter.InsertCommand = myCommandBuilder.GetInsertCommand().Clone();
                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.Both;
                            myAdapter.UpdateCommand = myCommandBuilder.GetUpdateCommand().Clone();
                            myAdapter.DeleteCommand = myCommandBuilder.GetDeleteCommand().Clone();

                            //myAdapter.InsertCommand.CommandText = String.Concat(myAdapter.InsertCommand.CommandText,
                            //        "; SELECT MyTableID=SCOPE_IDENTITY()");
                            string insertSuffix = "; SELECT Job_Serial=SCOPE_IDENTITY()";
                            myAdapter.InsertCommand.CommandText += insertSuffix;
                            myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                            //SqlParameter[] aParams = new SqlParameter[myCommandBuilder.GetInsertCommand().Parameters.Count];
                            //myCommandBuilder.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //myCommandBuilder.GetInsertCommand().Parameters.Clear();

                            SqlParameter identParam = new SqlParameter("@id", SqlDbType.BigInt, 0, "Job_Serial");
                            identParam.Direction = ParameterDirection.Output;

                            myAdapter.InsertCommand.Parameters.Add(identParam);
                            string test = myAdapter.InsertCommand.Parameters["@id"].ToString();

                            myAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                        }
                        catch (SqlException ex)
                        {
                            rc = ex.ErrorCode;
                            XLogger(2310, ex.ErrorCode, string.Format("Serial={0}; Message={1}", jobName, ex.Message));
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                            XLogger(2311, -1, string.Format("Serial={0}; Message={1}", jobName, ex.Message));
                        }
                    }
                    else
                    {
                        rc = -1;
                        pState = -1;
                        XLogger(2312, 0, string.Format("Connection Unable to be Opened"));
                    }
                }
                else
                {
                    rc = -1;
                    pState = -1;
                    XLogger(2313, 0, string.Format("No Connection Available"));
                }
                try
                {
                    theseJobs = myJobs.Select();
                    if (theseJobs.Length > 0)
                    {
                        // Clear Member Values!
                        Clear();
                        jobSerial = (int)theseJobs[0]["Job_Serial"];
                        rc = jobSerial;
                        jobHost = (string)theseJobs[0]["Job_Host"];
                        jobName = (string)theseJobs[0]["Job_Name"];
                        jobType = (int)theseJobs[0]["Job_Type"];
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Start"]))
                        {
                            jobStart = (string)theseJobs[0]["Job_Start"];
                        }
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Stop"]))
                        {
                            jobStop = (string)theseJobs[0]["Job_Stop"];
                        }
                        jobSequence = (int)theseJobs[0]["Job_Sequence"];
                        jobStatus = (int)theseJobs[0]["Job_Status"];
                        jobResult = (int)theseJobs[0]["Job_Result"];
                        jobPriority = (int)theseJobs[0]["Job_Priority"];
                        jobLimit = (int)theseJobs[0]["Job_Limit"];
                        if (!DBNull.Value.Equals(theseJobs[0]["Job_Display"]))
                        {
                            jobDisplay = (string)theseJobs[0]["Job_Display"];
                        }
                        jobRetention = (int)theseJobs[0]["Job_Retention"];
                        jobEvent = (int)theseJobs[0]["Job_Event"];

                        changeUser = (string)theseJobs[0]["Change_User"];
                        changeDate = (string)theseJobs[0]["Change_Date"].ToString();
                        // NEED TO ADD THESE!
                        //if (!DBNull.Value.Equals(theseComponents[0]["Change_Tag"]))
                        //{
                        //    changeTag = (string)theseComponents[0]["Change_Tag"];
                        //}
                        //if (!DBNull.Value.Equals(theseComponents[0]["Change_State"]))
                        //{
                        //    changeState = (int)theseComponents[0]["Change_State"];
                        //}
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2314, ex.ErrorCode, string.Format("JobName={0}; Message={1}", jobName, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2315, rc, string.Format("JobName={0}; Message={1}", jobName, ex.Message));
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
                    DataRow newJob = myJobs.NewRow();

                    // Assign
                    newJob["Job_Serial"] = jobSerial;
                    newJob["Job_Host"] = jobHost;
                    newJob["Job_Name"] = jobName;
                    newJob["Job_Type"] = jobType;
                    //newJob["Job_Start"] = jobStart;
                    //newJob["Job_Stop"] = jobStop;
                    newJob["Job_Sequence"] = jobSequence;
                    newJob["Job_Status"] = jobStatus;
                    newJob["Job_Result"] = jobResult;
                    newJob["Job_Priority"] = jobPriority;
                    newJob["Job_Limit"] = jobLimit;
                    newJob["Job_Display"] = jobDisplay;
                    newJob["Job_Retention"] = jobRetention;
                    newJob["Job_Event"] = jobEvent;
                    newJob["Change_User"] = Environment.UserName;
                    newJob["Change_Date"] = DateTime.Now;
                    newJob["Change_Tag"] = "";
                    newJob["Change_State"] = 0;

                    myJobs.Rows.Add(newJob);

                    int urows = myAdapter.Update(myJobs);
                    // Accept (commit).
                    myJobs.AcceptChanges();
                    //string test = myAdapter.InsertCommand.Parameters["@p1"].ToString();
                    //string test2 = myAdapter.InsertCommand.Parameters["@id"].ToString();
                    theseJobs = myJobs.Select();
                    if (theseJobs.Length > 0)
                    {
                        // 
                        int testResult = (int)myJobs.Rows[0]["Job_Serial"];
                        jobSerial = (int)theseJobs[0]["Job_Serial"];
                        rc = jobSerial;
                        pState = 1; // XDB Object!
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2316, ex.ErrorCode, string.Format("JobSerial={0}; Message={1}", jobSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2317, rc, string.Format("JobSerial={0}; Message={1}", jobSerial, ex.Message));
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
                    theseJobs[0]["Job_Serial"] = jobSerial;
                    theseJobs[0]["Job_Host"] = jobHost;
                    theseJobs[0]["Job_Name"] = jobName;
                    theseJobs[0]["Job_Type"] = jobType;
                    if (jobStart != null)
                    {
                        theseJobs[0]["Job_Start"] = jobStart;
                    }
                    if (jobStop != null)
                    {
                        theseJobs[0]["Job_Stop"] = jobStop;
                    }
                    theseJobs[0]["Job_Sequence"] = jobSequence;
                    theseJobs[0]["Job_Status"] = jobStatus;
                    theseJobs[0]["Job_Result"] = jobResult;
                    theseJobs[0]["Job_Priority"] = jobPriority;
                    theseJobs[0]["Job_Limit"] = jobLimit;
                    theseJobs[0]["Job_Display"] = jobDisplay;
                    theseJobs[0]["Job_Retention"] = jobRetention;
                    theseJobs[0]["Job_Event"] = jobEvent;
                    theseJobs[0]["Change_User"] = Environment.UserName;
                    theseJobs[0]["Change_Date"] = DateTime.Now;
                    theseJobs[0]["Change_Tag"] = "";
                    theseJobs[0]["Change_State"] = 0;

                    int urows = myAdapter.Update(myJobs);
                    // Accept (commit).
                    myJobs.AcceptChanges();
                    rc = jobSerial;
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2318, ex.ErrorCode, string.Format("JobSerial={0}; Message={1}", jobSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2319, rc, string.Format("JobSerial={0}; Message={1}", jobSerial, ex.Message));
                }
                finally
                {
                }
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
            XTRMJob thisJob = new XTRMJob();

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
                                case "XTRMTask": // XTRMTask
                                    outerXML = reader.ReadOuterXml();
                                    XTRMTask thisTask = (XTRMTask)XTRMTask.consumeXML(myConfig, outerXML, 1, true);
                                    thisJob.tasks.Add(thisTask);
                                    bProcessed = true;
                                    break;
                            }
                            // May wish to get all the  attributes here for new elements!
                            if (!bProcessed)
                            {
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
                                        thisJob.jobName = elementAttributes["Name"];
                                    }
                                    if (elementAttributes.ContainsKey("Host"))
                                    {
                                        thisJob.jobHost = elementAttributes["Host"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                switch (elementName.ToUpper())
                                {
                                    //case "XTRMTask": // XTRMTask
                                    //    outerXML = reader.ReadOuterXml();
                                    //    XTRMTask thisTask = (XTRMTask)XTRMTask.consumeXML(myConfig, outerXML, 1, true);
                                    //    thisJob.tasks.Add(thisTask);
                                    //    break;
                                    // Reset Dictionary!
                                    // XConfig
                                    case "BASECONFIG":
                                        outerXML = reader.ReadOuterXml();
                                        myDictionaryLoader = new XDictionaryLoader();
                                        myConfig = new Dictionary<string, string>();
                                        myDictionaryLoader.Augment(myConfig, outerXML);
                                        break;
                                    //   Add to the current dictionary!
                                    // XConfig
                                    case "WITHCONFIG":
                                        outerXML = reader.ReadOuterXml();
                                        //myDictionaryLoader = new XDictionaryLoader();
                                        myDictionaryLoader.Augment(myConfig, outerXML);
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
                                //case 1:     // PARMS
                                    //thisTask.parms.Add(reader.Value);
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
                XLogger(2320, -1, string.Format("XML={0}; Message={1}", XmlFragment, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            thisJob.config = myConfig;
            return thisJob;
        }
    }
}
