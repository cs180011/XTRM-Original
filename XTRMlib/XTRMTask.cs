using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Timers;

namespace XTRMlib
{
    // Error Number = 2632
    public class XTRMTask : XTRMObject
    {
        private System.Timers.Timer aTimer;
        //////////////////////////////////////////////////////
        // XLATOR CONFIG SUPPORT
        // Can contain a list of tasks
        public Dictionary<String, String> config;
        public List<string> parms = new List<string>();
        //public List<XParm> parms = new List<XParm>();
        //////////////////////////////////////////////////////
        //SqlDataAdapter changeLog;
        //SqlCommandBuilder myLogBuilder;
        SqlDataAdapter taskInfo;
        SqlCommandBuilder myCommandBuilder;
        DataTable thisTable = new DataTable();
        DataRow[] theseRows = null;
        //string className = "XTRMTask";
        // Get the current process.
        Process currentProcess;
        Process taskProcess;
        public XTRMEvent origEvent = new XTRMEvent();
        public Dictionary<string, string> jobData = null;

        // Accessible Members.
        public String[] taskParms = new String[11];
        //protected int runID { get; set; }
        public int jobSerial { get; set; }
        public int taskSerial { get; set; }
        public int taskSequence { get; set; }
        public string taskName { get; set; }
        public string taskExecutable { get; set; }
        public string taskPath { get; set; }
        //public int taskType { get; set; }
        public int taskPID { get; set; }
        public int taskStatus { get; set; }
        public int taskResult { get; set; }
        public string taskStart { get; set; }
        public string taskStop { get; set; }
        public int taskCondition { get; set; }
        public int taskPriority { get; set; }
        public int taskLimit { get; set; }
        public string taskDisplay { get; set; }
        public int taskRunID { get; set; }
        public int taskRetention { get; set; }
        public int taskEvent { get; set; }
        public bool taskOriginator = false;
        //public int taskParm0 { get; set; }
        //public int taskParm1 { get; set; }
        //public int taskParm2 { get; set; }
        //public int taskParm3 { get; set; }
        //public int taskParm4 { get; set; }
        //public int taskParm5 { get; set; }
        //public int taskParm6 { get; set; }
        //public int taskParm7 { get; set; }
        //public int taskParm8 { get; set; }
        //public int taskParm9 { get; set; }
        //public int taskParm10 { get; set; }

        int taskType = 0;   // 0=Local; 1=XDB;
        //int taskSerial = -1;
        bool bInit = false;
        string command = Environment.CommandLine;

        // Reserved for othe things such as passing in information to XObject.
        //public XTRMTask(...)
        //{
        //}
        // Tasks ALWAYS Request an XConnection!
        public XTRMTask(string name = "")
        {
            // Get the current process.
            //Process currentProcess = Process.GetCurrentProcess();
            taskName = name;
        }
        private void connection_check(object sender, System.Data.StateChangeEventArgs e)
        {
            Console.WriteLine("  MasterConnection: OriginalState= {0} CurrentState= {1}", e.OriginalState, e.CurrentState);
            if (e.CurrentState.Equals(System.Data.ConnectionState.Closed))
            {
                //throw new XDBException(-99, "Master Connection Closed");
            }
        }
        public bool Runnable()
        {
            return Validate();
        }
        virtual public bool Validate()
        {
            return bInit;
        }
        public void Invalidate()
        {
            bInit = false;
        }
        public int Run(string [] args = null)
        {
            int rc = -99999;
            try
            {
                MasterConnection.StateChange += new System.Data.StateChangeEventHandler(connection_check);
                if (Initialize(args))
                {
                    SetLogID(taskSerial);
                    // Set the current process.
                    currentProcess = Process.GetCurrentProcess();
                    taskPID = currentProcess.Id;
                    //currentProcess.Id
                    jobData = getJobData(jobSerial);
                    // Instantiate the event object.
                    origEvent.Initialize(taskEvent);
                    if (Validate())
                    {
                        rc = Execute(args);
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(2631, 0, string.Format("Message={0}", ex.Message));
                rc = -1;
            }
            finally
            {
                MasterConnection.StateChange -= new System.Data.StateChangeEventHandler(connection_check);
            }
            return rc;
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
            parms.Clear();
            taskSerial = -1;
            taskSequence = -1;
            taskName = "";
            taskExecutable = "";
            taskPath = "";
            taskPID = -1;
            taskStatus = 0;
            taskResult = 0;
            //taskStart = DateTime.Now;
            //taskStop = DateTime.Now;
            taskStart = null;
            taskStop = null;
            taskCondition = -1;
            taskPriority = -1;
            taskLimit = -1;
            taskDisplay = "";
            taskRunID = -1;
            taskRetention = -1;
            taskEvent = -1;
            taskParms.Initialize();
            base.Clear();
            return true;
        }
        virtual public int Execute(string [] args = null) { return 0; }
        public override int Initialize(int lSerial = -1)
        {
            return Initialize(lSerial, "select * from WorkTasks a where a.Task_Serial = {0}");
        }
        public override int Initialize(int lSerial = -1, string sqlText = "select * from WorkTasks a where a.Task_Serial = {0}")
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
                            XLogger(2600, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = ex.ErrorCode;
                        }
                        catch (Exception ex)
                        {
                            XLogger(2601, 0, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = -1;
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            taskInfo = new SqlDataAdapter();
                            // Try to get the Serial (if >= 0).

                            string strTemp = "select * ";
                            strTemp += " from WorkTasks a";
                            strTemp += " where a.Task_Serial = {0}";
                            String strSelectCommand = String.Format(sqlText, lSerial);

                            thisTable.Clear();

                            myCommandBuilder = new SqlCommandBuilder(taskInfo);
                            taskInfo.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                            pState = taskInfo.Fill(thisTable);

                            // add handlers
                            taskInfo.RowUpdating += new SqlRowUpdatingEventHandler(OnRowUpdating);
                            taskInfo.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

                            taskInfo.InsertCommand = myCommandBuilder.GetInsertCommand().Clone();
                            taskInfo.InsertCommand.UpdatedRowSource = UpdateRowSource.Both;
                            taskInfo.UpdateCommand = myCommandBuilder.GetUpdateCommand().Clone();
                            taskInfo.DeleteCommand = myCommandBuilder.GetDeleteCommand().Clone();

                            //myAdapter.InsertCommand.CommandText = String.Concat(myAdapter.InsertCommand.CommandText,
                            //        "; SELECT MyTableID=SCOPE_IDENTITY()");
                            string insertSuffix = "; SELECT Task_Serial=SCOPE_IDENTITY()";
                            taskInfo.InsertCommand.CommandText += insertSuffix;
                            taskInfo.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                            //SqlParameter[] aParams = new SqlParameter[myCommandBuilder.GetInsertCommand().Parameters.Count];
                            //myCommandBuilder.GetInsertCommand().Parameters.CopyTo(aParams, 0);
                            //myCommandBuilder.GetInsertCommand().Parameters.Clear();

                            SqlParameter identParam = new SqlParameter("@id", SqlDbType.BigInt, 0, "Task_Serial");
                            identParam.Direction = ParameterDirection.Output;

                            taskInfo.InsertCommand.Parameters.Add(identParam);
                            string test = taskInfo.InsertCommand.Parameters["@id"].ToString();

                            taskInfo.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                        }
                        catch (SqlException ex)
                        {
                            XLogger(2602, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = ex.ErrorCode;
                        }
                        catch (Exception ex)
                        {
                            XLogger(2603, 0, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                            rc = -1;
                        }
                    }
                    else
                    {
                        rc = -1;
                        pState = -1;
                        XLogger(2604, 0, string.Format("Connection Unable to be Opened"));
                    }
                }
                else
                {
                    rc = -1;
                    pState = -1;
                    XLogger(2605, 0, string.Format("No Connection Available"));
                }
                try
                {
                    theseRows = thisTable.Select();
                    if (theseRows.Length > 0)
                    {
                        // Clear Member Values!
                        Clear();
                        jobSerial = (int)theseRows[0]["Job_Serial"];
                        taskSerial = (int)theseRows[0]["Task_Serial"];
                        rc = taskSerial;
                        taskSequence = (int)theseRows[0]["Task_Sequence"];
                        taskName = (string)theseRows[0]["Task_Name"];
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Executable"]))
                        {
                            taskExecutable = (string)theseRows[0]["Task_Executable"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Path"]))
                        {
                            taskPath = (string)theseRows[0]["Task_Path"];
                        }
                        taskPID = (int)theseRows[0]["Task_PID"];
                        taskStatus = (int)theseRows[0]["Task_Status"];
                        taskResult = (int)theseRows[0]["Task_Result"];
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Start_Time"]))
                        {
                            taskStart = (string)theseRows[0]["Task_Start_Time"].ToString();
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Stop_Time"]))
                        {
                            taskStop = (string)theseRows[0]["Task_Stop_Time"].ToString();
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Condition"]))
                        {
                            taskCondition = (int)theseRows[0]["Task_Condition"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Priority"]))
                        {
                            taskPriority = (int)theseRows[0]["Task_Priority"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Limit"]))
                        {
                            taskLimit = (int)theseRows[0]["Task_Limit"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Display"]))
                        {
                            taskDisplay = (string)theseRows[0]["Task_Display"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Run_ID"]))
                        {
                            taskRunID = (int)theseRows[0]["Task_Run_ID"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Event"]))
                        {
                            taskEvent = (int)theseRows[0]["Task_Event"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Retention"]))
                        {
                            taskRetention = (int)theseRows[0]["Task_Retention"];
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm0"]))
                        {
                            taskParms[0] = ((string)theseRows[0]["Task_Parm0"]);
                            parms.Add(taskParms[0]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm1"]))
                        {
                            taskParms[1] = ((string)theseRows[0]["Task_Parm1"]);
                            parms.Add(taskParms[1]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm2"]))
                        {
                            taskParms[2] = ((string)theseRows[0]["Task_Parm2"]);
                            parms.Add(taskParms[2]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm3"]))
                        {
                            taskParms[3] = ((string)theseRows[0]["Task_Parm3"]);
                            parms.Add(taskParms[3]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm4"]))
                        {
                            taskParms[4] = ((string)theseRows[0]["Task_Parm4"]);
                            parms.Add(taskParms[4]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm5"]))
                        {
                            taskParms[5] = ((string)theseRows[0]["Task_Parm5"]);
                            parms.Add(taskParms[5]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm6"]))
                        {
                            taskParms[6] = ((string)theseRows[0]["Task_Parm6"]);
                            parms.Add(taskParms[6]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm7"]))
                        {
                            taskParms[7] = ((string)theseRows[0]["Task_Parm7"]);
                            parms.Add(taskParms[7]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm8"]))
                        {
                            taskParms[8] = ((string)theseRows[0]["Task_Parm8"]);
                            parms.Add(taskParms[8]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm9"]))
                        {
                            taskParms[9] = ((string)theseRows[0]["Task_Parm9"]);
                            parms.Add(taskParms[9]);
                        }
                        if (!DBNull.Value.Equals(theseRows[0]["Task_Parm10"]))
                        {
                            taskParms[10] = ((string)theseRows[0]["Task_Parm10"]);
                            parms.Add(taskParms[10]);
                        }

                        changeUser = (string)theseRows[0]["Change_User"];
                        changeDate = (string)theseRows[0]["Change_Date"].ToString();
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
                    XLogger(2606, ex.ErrorCode, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                    rc = ex.ErrorCode;
                }
                catch (Exception ex)
                {
                    XLogger(2607, -1, string.Format("Serial={0}; Message={1}", lSerial, ex.Message));
                    rc = -1;
                }
            }
            return rc;
        }
        public bool Initialize(string[] args = null)
        {
            int rc = 0;
            bInit = false;
            // If no parameters, then initialize new task!
            taskType = -1;
            taskSerial = -1;
            if (pState >= 0)
            {
                // If >= 1 parameter, then try to initialize from XDB using first parm as task serial (parm0 must match that of existing task)!
                if (args.Length > 0)
                {
                    int lTask = -1;
                    try
                    {
                        lTask = Convert.ToInt32(args[0]);
                        rc = Initialize(lTask);
                        if (rc > 0)
                        {
                            taskSerial = (int)theseRows[0]["Task_Serial"];
                        }
                        else
                        {
                            //XLogger(2608, -1, string.Format("Initialize() Failed; Serial={0}; Message={1}", lTask));
                            XLogger(2608, 0, string.Format("XBot Command Line Invocation"));
                        }
                    }
                    catch (Exception ex)
                    {
                        XLogger(2609, -1, string.Format("Serial={0}; Message={1}", lTask, ex.Message));
                    }
                }
                // If cannot initialize, try again from the environment[XDB_TASK] (parm0 must match that of existing task).
                if (taskSerial.Equals(-1))
                {
                    string strXDB_TASK = Environment.GetEnvironmentVariable("XDB_TASK");
                    int lTask = -1;
                    try
                    {
                        lTask = Convert.ToInt32(strXDB_TASK);
                        rc = Initialize(lTask);
                        if (rc > 0)
                        {
                            taskSerial = (int)theseRows[0]["Task_Serial"];
                        }
                    }
                    catch (Exception ex)
                    {
                        XLogger(2610, -1, string.Format("Serial={0}; Message={1}", lTask, ex.Message));
                    }
                }
                //theseRows[0]["Job_Serial"];
                //theseRows[0]["Task_Serial"];
                //theseRows[0]["Task_Sequence"];
                //theseRows[0]["Task_Name"];
                //theseRows[0]["Task_PID"];
                //theseRows[0]["Task_Status"];
                //theseRows[0]["Task_Result"];
                //theseRows[0]["Task_Start_Time"];
                //theseRows[0]["Task_Stop_Time"];
                //theseRows[0]["Task_Condition"];
                //theseRows[0]["Task_Priority"];
                //theseRows[0]["Task_Limit"];
                //theseRows[0]["Task_Display"];
                //theseRows[0]["Task_Run_ID"];
                //theseRows[0]["Task_Parm0"];
                //theseRows[0]["Task_Parm1"];
                //theseRows[0]["Task_Parm2"];
                //theseRows[0]["Task_Parm3"];
                //theseRows[0]["Task_Parm4"];
                //theseRows[0]["Task_Parm5"];
                //theseRows[0]["Task_Parm6"];
                //theseRows[0]["Task_Parm7"];
                //theseRows[0]["Task_Parm8"];
                //theseRows[0]["Task_Parm9"];
                //theseRows[0]["Task_Retention"];
                //theseRows[0]["Change_User"];
                //theseRows[0]["Change_Date"];
                //theseRows[0]["Change_State"];
                //theseRows[0]["Change_Tag"];
                if (taskSerial.Equals(-1))
                {
                    // Create New Local Task!
                    DataRow newTask = thisTable.NewRow();
                    newTask["Job_Serial"] = 0;      // Local Task (Job #1 Bucket) By Definition!
                    newTask["Task_Sequence"] = -1;  // Signifies Local Task!
                    newTask["Task_Name"] = "XTask";
                    // Get the current process.
                    currentProcess = Process.GetCurrentProcess();
                    newTask["Task_PID"] = (int)currentProcess.Id;
                    newTask["Task_Status"] = 0;
                    newTask["Task_Result"] = -1;
                    newTask["Change_User"] = "XExecutive";
                    DateTime myTaskTime = DateTime.Now;
                    newTask["Change_Date"] = myTaskTime;
                    newTask["Task_Sequence"] = (int)myTaskTime.Ticks;

                    // Fill in the parameters whether from the command line.
                    taskParms = args;
                    newTask["Task_Parm0"] = "XTask";
                    int i = 0;
                    //string strIndex = "Task_Parm{0}";
                    foreach (string pX in args)
                    {
                        if (i > 0)
                        {
                            string strIndex = String.Format("Task_Parm{0}", i);
                            newTask[strIndex] = pX;
                        }
                        i++;
                    }
                    //newTask["Task_Parm1"] = "";
                    //string test = "Task_Parm1";
                    //newTask[test] = "";

                    thisTable.Rows.Add(newTask);

                    int urows = taskInfo.Update(thisTable);
                    //taskInfo.

                    //taskSerial = getScope();

                    thisTable.AcceptChanges();

                    thisTable.Clear();

                    // Error Handling!
                    // RE-Query the taskInfo!
                    String strTemp = "select * from WorkTasks ";
                    strTemp += "where Task_PID={0} ";
                    strTemp += "and Task_Sequence={1} ";
                    String strSelectCommand = String.Format(strTemp, (int)currentProcess.Id, (int)myTaskTime.Ticks);
                    taskInfo.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                    taskInfo.Fill(thisTable);
                    DataRow[] theseRows;
                    theseRows = thisTable.Select();
                    if (theseRows.Length > 0)
                    {
                        taskSerial = (int)theseRows[0]["Task_Serial"];
                        taskRunID = taskSerial;
                        theseRows[0]["Task_Sequence"] = -1;  // Signifies Local Task!
                        theseRows[0]["Task_Run_ID"] = taskSerial;
                    }

                    urows = taskInfo.Update(thisTable);
                    thisTable.AcceptChanges();
                    taskType = 0;
                    // If cannot initialize, then initialize new task using specified parameters.
                    // rc = -1 failure
                    // Get the taskSerial (which also becomes the runID)
                }
                else
                {
                    taskType = 1;
                    // rc = 0 : new task initialized.
                    //

                    // Fill in the parameters whether from XDB.
                    taskParms = args;
                    // Get the taskSerial (which also becomes the runID)
                }
            }
            else
            {
                // Local Task (No XDB).
                taskType = 1;
                taskParms = args;
            }

            bInit = true;
            return bInit;
        }
        public int getTaskType()
        {
            return taskType;
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
                    DataRow newTask = thisTable.NewRow();

                    newTask["Job_Serial"] = jobSerial;      // Local Task (Job #1 Bucket) By Definition!
                    newTask["Task_Sequence"] = -1;          // Signifies Local Task!
                    newTask["Task_Name"] = taskName;
                    newTask["Task_Executable"] = taskExecutable;
                    newTask["Task_Path"] = taskPath;
                    // Get the current process.
                    //currentProcess = Process.GetCurrentProcess();
                    newTask["Task_PID"] = taskPID;
                    newTask["Task_Status"] = 0;
                    newTask["Task_Result"] = -1;
                    newTask["Change_User"] = "XMonitor";
                    DateTime myTaskTime = DateTime.Now;
                    newTask["Change_Date"] = myTaskTime;
                    newTask["Task_Sequence"] = (int)myTaskTime.Ticks;
                    newTask["Task_Event"] = taskEvent;

                    // Fill in the parameters whether from the command line.
                    newTask["Task_Parm0"] = taskName;
                    int i = 0;
                    //string strIndex = "Task_Parm{0}";
                    foreach (string pX in parms)
                    {
                        if (i > 0)
                        {
                            string strIndex = String.Format("Task_Parm{0}", i);
                            newTask[strIndex] = pX;
                        }
                        i++;
                    }

                    newTask["Change_Tag"] = "";
                    newTask["Change_State"] = 0;

                    thisTable.Rows.Add(newTask);

                    int urows = taskInfo.Update(thisTable);
                    // Accept (commit).
                    thisTable.AcceptChanges();
                    string test = taskInfo.InsertCommand.Parameters["@p1"].ToString();
                    string test2 = taskInfo.InsertCommand.Parameters["@id"].ToString();
                    theseRows = thisTable.Select();
                    if (theseRows.Length > 0)
                    {
                        // 
                        //int testResult = (int)thisTable.Rows[0]["Task_Serial"];
                        taskSerial = (int)theseRows[0]["Task_Serial"];
                        rc = taskSerial;
                        pState = 1; // XDB Object!
                    }
                }
                catch (SqlException ex)
                {
                    XLogger(2611, ex.ErrorCode, string.Format("Serial={0}; Message={1}", taskSerial, ex.Message));
                    rc = ex.ErrorCode;
                }
                catch (Exception ex)
                {
                    XLogger(2612, -1, string.Format("Serial={0}; Message={1}", taskSerial, ex.Message));
                    rc = -1;
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
                    //theseRows[0]["Job_Serial"];
                    //theseRows[0]["Task_Serial"];
                    //theseRows[0]["Task_Sequence"];
                    //theseRows[0]["Task_Name"];
                    //theseRows[0]["Task_PID"];
                    //theseRows[0]["Task_Status"];
                    //theseRows[0]["Task_Result"];
                    //theseRows[0]["Task_Start_Time"];
                    //theseRows[0]["Task_Stop_Time"];
                    //theseRows[0]["Task_Condition"];
                    //theseRows[0]["Task_Priority"];
                    //theseRows[0]["Task_Limit"];
                    //theseRows[0]["Task_Display"];
                    //theseRows[0]["Task_Run_ID"];
                    //theseRows[0]["Task_Parm0"];
                    //theseRows[0]["Task_Parm1"];
                    //theseRows[0]["Task_Parm2"];
                    //theseRows[0]["Task_Parm3"];
                    //theseRows[0]["Task_Parm4"];
                    //theseRows[0]["Task_Parm5"];
                    //theseRows[0]["Task_Parm6"];
                    //theseRows[0]["Task_Parm7"];
                    //theseRows[0]["Task_Parm8"];
                    //theseRows[0]["Task_Parm9"];
                    //theseRows[0]["Task_Retention"];
                    //theseRows[0]["Change_User"];
                    //theseRows[0]["Change_Date"];
                    //theseRows[0]["Change_State"];
                    //theseRows[0]["Change_Tag"];
                    theseRows[0]["Job_Serial"] = jobSerial;
                    theseRows[0]["Task_Serial"] = taskSerial;
                    theseRows[0]["Task_Sequence"] = taskSequence;
                    theseRows[0]["Task_Name"] = taskName;
                    theseRows[0]["Task_Executable"] = taskExecutable;
                    theseRows[0]["Task_Path"] = taskPath;
                    theseRows[0]["Task_PID"] = taskPID;
                    theseRows[0]["Task_Status"] = taskStatus;
                    theseRows[0]["Task_Result"] = taskResult;
                    //theseRows[0]["Task_Type"] = taskType;
                    if (taskStart != null)
                    {
                        theseRows[0]["Task_Start_Time"] = taskStart;
                    }
                    if (taskStop != null)
                    {
                        theseRows[0]["Task_Stop_Time"] = taskStop;
                    }
                    theseRows[0]["Task_Condition"] = taskCondition;
                    theseRows[0]["Task_Priority"] = taskPriority;
                    theseRows[0]["Task_Limit"] = taskLimit;
                    theseRows[0]["Task_Display"] = taskDisplay;
                    theseRows[0]["Task_Run_ID"] = taskRunID;
                    theseRows[0]["Task_Event"] = taskEvent;
                    theseRows[0]["Task_Retention"] = taskRetention;
                    //theseRows[0]["Task_Parm0"] = taskParms[0];
                    //theseRows[0]["Task_Parm1"] = taskParms[1];
                    //theseRows[0]["Task_Parm2"] = taskParms[2];
                    //theseRows[0]["Task_Parm3"] = taskParms[3];
                    //theseRows[0]["Task_Parm4"] = taskParms[4];
                    //theseRows[0]["Task_Parm5"] = taskParms[5];
                    //theseRows[0]["Task_Parm6"] = taskParms[6];
                    //theseRows[0]["Task_Parm7"] = taskParms[7];
                    //theseRows[0]["Task_Parm8"] = taskParms[8];
                    //theseRows[0]["Task_Parm9"] = taskParms[9];
                    //theseRows[0]["Task_Parm10"] = taskParms[10];
                    theseRows[0]["Task_Parm0"] = taskName;
                    int i = 0;
                    //string strIndex = "Task_Parm{0}";
                    foreach (string pX in parms)
                    {
                        if (i > 0)
                        {
                            string strIndex = String.Format("Task_Parm{0}", i);
                            theseRows[0][strIndex] = pX;
                        }
                        i++;
                    }
                    theseRows[0]["Change_User"] = Environment.UserName;
                    theseRows[0]["Change_Date"] = DateTime.Now;
                    theseRows[0]["Change_Tag"] = "";
                    theseRows[0]["Change_State"] = 0;

                    int urows = taskInfo.Update(thisTable);
                    // Accept (commit).
                    thisTable.AcceptChanges();
                    //string test = taskInfo.InsertCommand.Parameters["@p1"].ToString();
                    //string test2 = taskInfo.InsertCommand.Parameters["@id"].ToString();
                    theseRows = thisTable.Select();
                    if (theseRows.Length > 0)
                    {
                        // 
                        int testResult = (int)thisTable.Rows[0]["Task_Serial"];
                        taskSerial = (int)theseRows[0]["Task_Serial"];
                        rc = taskSerial;
                        pState = 1; // XDB Object!
                    }
                }
                catch (SqlException ex)
                {
                    rc = ex.ErrorCode;
                    XLogger(2613, rc, string.Format("Serial={0}; Message={1}", taskSerial, ex.Message));
                }
                catch (Exception ex)
                {
                    rc = -1;
                    XLogger(2614, rc, string.Format("Serial={0}; Message={1}", taskSerial, ex.Message));
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
            XTRMTask thisTask = new XTRMTask();
            thisTask.taskPath = getConfig(myConfig, "TaskPath", @"C:\");

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
                                case "BASECONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    myDictionaryLoader = new XDictionaryLoader();
                                    myConfig = new Dictionary<string, string>();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    thisTask.taskPath = getConfig(myConfig, "TaskPath");
                                    bProcessed = true;
                                    break;
                                //   Add to the current dictionary!
                                // XConfig
                                case "WITHCONFIG":
                                    outerXML = reader.ReadOuterXml();
                                    //myDictionaryLoader = new XDictionaryLoader();
                                    myDictionaryLoader.Augment(myConfig, outerXML);
                                    thisTask.taskPath = getConfig(myConfig, "TaskPath");
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
                                    if (elementAttributes.ContainsKey("Path"))
                                    {
                                        thisTask.taskPath = elementAttributes["Path"];
                                    }
                                    if (elementAttributes.ContainsKey("Name"))
                                    {
                                        thisTask.taskName = elementAttributes["Name"];
                                    }
                                    if (elementAttributes.ContainsKey("Exec"))
                                    {
                                        thisTask.taskExecutable = elementAttributes["Exec"];
                                    }
                                    if (elementAttributes.ContainsKey("Execute"))
                                    {
                                        thisTask.taskExecutable = elementAttributes["Execute"];
                                    }
                                    reader.MoveToElement();
                                }
                                // Need to see if we are interested in this element!
                                lElementType = 0;
                                //string elementName = reader.Name;
                                switch (elementName.ToUpper())
                                {
                                    case "PARM":
                                    case "XPARM":
                                    case "XTRMPARM":
                                        lElementType = 1;
                                        bResult = reader.Read();
                                        break;
                                    case "EXEC":
                                    case "EXECUTABLE":
                                        lElementType = 2;
                                        bResult = reader.Read();
                                        break;
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
                                        //myDictionaryLoader = new XDictionaryLoader();
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
                                case 1:     // XParms
                                    thisTask.parms.Add(reader.Value);
                                    break;
                                case 2:     // Executable
                                    thisTask.taskExecutable = reader.Value;
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
                XLogger(2615, -1, string.Format("XML={0}; Message={1}", XmlFragment, ex.Message));
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            thisTask.config = myConfig;
            return thisTask;
        }
        public int Start()
        {
            int rc = 0;
            string formatParms = "";
            string formatFileName = "";
            string path = "";
            try
            {
                taskProcess = new Process();
                taskProcess.StartInfo.UseShellExecute = false;
                //taskProcess.StartInfo.FileName = @"c:\XImportScriptIndexes.bat";
                taskProcess.StartInfo.CreateNoWindow = true;

                // You can start any process, HelloWorld is a do-nothing example.
                // Execution Directory (from Dictionary).
                //path = string.Format("{0}", getDictionaryEntry("ExecutablePath", ""));
                string pathAndSlash = "";
                if (taskPath.Length > 0)
                {
                    pathAndSlash = taskPath + @"\";
                }
                // Now use taskPath (member of XTRMTask)!
                if (taskExecutable.Length > 0)
                {
                    //formatFileName = string.Format("{0}{1}", getDictionaryEntry("ExecutableLocation", ""), taskExecutable);
                    //formatFileName = string.Format("{0}", taskExecutable);
                    formatFileName = string.Format(@"{0}{1}", pathAndSlash, taskExecutable);
                }
                else
                {
                    //formatFileName = string.Format("{0}{1}", getDictionaryEntry("ExecutableLocation", ""), taskName);
                    //formatFileName = string.Format("{0}", taskName);
                    formatFileName = string.Format(@"{0}{1}", pathAndSlash, taskName);
                }
                taskProcess.StartInfo.FileName = formatFileName;
                // Augment Path with TaskPath!
                // May want other environment variables...
                path = taskPath;
                if (path.Length > 0)
                {
                    path += ";";
                    path += taskProcess.StartInfo.EnvironmentVariables["Path"];
                    taskProcess.StartInfo.EnvironmentVariables["Path"] = path;
                }
                // Impute (prepend) taskSerial as the first argument; followed by the remaining args in the order of their specification.
                int i = 0;
                //if (!formatFileName.Contains("XPerl"))
                //{
                //    formatParms = taskSerial.ToString();
                //}
                foreach (string pX in taskParms)
                {
                    if (i > 0)
                    {
                        if (pX != null)
                        {
                            if (pX.Length > 0)
                            {
                                formatParms += " ";
                                formatParms += pX;
                                //formatParms += "";
                            }
                        }
                    }
                    i++;
                }
                taskProcess.StartInfo.Arguments = formatParms;

                // Hook up the Elapsed event for the timer.
                aTimer = new System.Timers.Timer(10000);
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                // Set the Interval to 2 seconds (2000 milliseconds).
                aTimer.Interval = 2000;
                aTimer.Enabled = true;
                taskProcess.EnableRaisingEvents = true;
                taskProcess.Exited += new EventHandler(taskProcess_Exited);
                taskOriginator = true;
                taskStatus = 1;
                taskStart = DateTime.Now.ToString();
                //taskStop = DateTime.Now.ToString();
                rc = Save();
                if (rc >= 0)
                {
                    taskProcess.StartInfo.RedirectStandardOutput = true;
                    taskProcess.StartInfo.RedirectStandardError = true;
                    taskProcess.OutputDataReceived += new DataReceivedEventHandler(taskProcess_stdout_Handler);
                    taskProcess.ErrorDataReceived += new DataReceivedEventHandler(taskProcess_stderr_Handler);
                    if (taskProcess.Start())
                    {
                        taskPID = taskProcess.Id;
                        Save();
                        TaskLogger(2616, 0, string.Format("Task #{0} Started: {1} {2} {3}.", taskSerial, formatFileName, formatParms, path));
                        taskProcess.BeginOutputReadLine();
                        taskProcess.BeginErrorReadLine();
                    }
                    else
                    {
                        TaskLogger(2624, 0, string.Format("Task #{0} NOT Started: {1} {2} {3}.", taskSerial, formatFileName, formatParms, path));
                        rc = -99;
                    }
                }
                else
                {
                    XLogger(2617, 0, string.Format("XTRMTask.Save() Failed for Task #{1}; rc= #{0}; Task Not Started.", rc, taskSerial));
                }
            }
            catch (Exception ex)
            {
                TaskLogger(2625, 0, string.Format("Task #{0} NOT Started: {1} {2} {3}.", taskSerial, formatFileName, formatParms, path));
                taskStatus = 99;
                taskResult = -1;
                Save();
                aTimer.Close();
                taskProcess.OutputDataReceived -= taskProcess_stdout_Handler;
                taskProcess.ErrorDataReceived -= taskProcess_stderr_Handler;
                taskProcess.Close();
                XLogger(2618, -1, string.Format("Process Could Not Start; Cleaned Up; Serial={0}; Message={1}", taskSerial, ex.Message));
                rc = -99;
            }
            return rc;
        }
        protected void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // To be used (future) to stop the process when the execution limit is reached (watchdog).
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }
        public void taskProcess_stdout_Handler(object sender, DataReceivedEventArgs outLine)
        {
            try
            {
                //string output = "STDOUT:" + outLine.Data;
                TaskLogger(2622, 0, string.Format("{0}", outLine.Data));
                //TaskLogger(2622, 0, string.Format("{0}", output));
            }
            catch (Exception ex)
            {
                XLogger(2626, -1, string.Format("Cannot Get STDOUT; Serial={0}; Message={1}", taskSerial, ex.Message));
            }
            finally
            {
            }
        }
        public void taskProcess_stderr_Handler(object sender, DataReceivedEventArgs outLine)
        {
            try
            {
                //string output = "STDERR:" + outLine.Data;
                TaskLogger(2623, 0, string.Format("{0}", outLine.Data));
            }
            catch (Exception ex)
            {
                XLogger(2627, -1, string.Format("Cannot Get STDERR; Serial={0}; Message={1}", taskSerial, ex.Message));
            }
            finally
            {
            }
        }
        public void taskProcess_Exited(object sender, System.EventArgs e)
        {
            try
            {
                XLogger(2619, taskProcess.ExitCode, string.Format("End of Task #{0}", taskSerial));
                TaskLogger(2619, taskProcess.ExitCode, string.Format("End of Task #{0}", taskSerial));
                Console.WriteLine("Exit time:    {0}\r\n" +
                    "Exit code:    {1}\r\n", taskProcess.ExitTime, taskProcess.ExitCode);
                //taskProcess_ExitHouseKeeping();
                taskStop = taskProcess.ExitTime.ToString();
                taskResult = taskProcess.ExitCode;
                taskStatus = 99;
                int rc = Save();
                if (rc < 0)
                {
                    // Error
                    XLogger(2620, rc, string.Format("Task #{0} Exit Handling Failed", taskSerial));
                }
                aTimer.Close();
                taskProcess.OutputDataReceived -= taskProcess_stdout_Handler;
                taskProcess.ErrorDataReceived -= taskProcess_stderr_Handler;
                taskProcess.Close();
                return;
            }
            catch (Exception ex)
            {
                XLogger(2621, -1, string.Format("Exception During Process Exit Handling; Serial={0}; Message={1}", taskSerial, ex.Message));
            }
        }
        public void taskProcess_ExitHouseKeeping()
        {
            try
            {
                taskStop = taskProcess.ExitTime.ToString();
                if (taskProcess.HasExited)
                {
                    taskResult = taskProcess.ExitCode;
                }
                else
                {
                    taskResult = -1;
                }
                taskStatus = 99;
                int rc = Save();
                if (rc < 0)
                {
                    // Error
                    XLogger(2620, rc, string.Format("Task #{0} Exit Handling Failed", taskSerial));
                }
                aTimer.Close();
                taskProcess.OutputDataReceived -= taskProcess_stdout_Handler;
                taskProcess.ErrorDataReceived -= taskProcess_stderr_Handler;
                taskProcess.Close();
            }
            catch (Exception ex)
            {
                XLogger(2628, -1, string.Format("Exception During Process Exit HouseKeeping; Serial={0}; Message={1}", taskSerial, ex.Message));
            }
            return;
        }
        /*
        public int saveJobData(string keyName, string keyValue)
        {
            int rc = 0;

            // Much like a log, writes entries to the WorkJobData table for use in the same job (by other tasks).
            // Check the Connection.
            try
            {
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
                        }
                        catch (Exception ex)
                        {
                            rc = -1;
                        }
                    }
                    if (MasterConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            // Need to repeat any single quotes in the text!
                            keyName.Replace("'", "''");
                            keyValue.Replace("'", "''");
                            string strSQL = "EXEC dbo.spJobData";
                            strSQL += String.Format(" @JobSerial={0}", jobSerial);
                            strSQL += String.Format(", @KeyName={0}", keyName);
                            strSQL += String.Format(", @KeyValue={0}", keyValue);
                            SqlCommand command = new SqlCommand(strSQL, MasterConnection);
                            rc = command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            XLogger(2629, -1, string.Format("Exception Writing Name/Value Pair in JobData(); Task Serial={0}; Message={1}", taskSerial, ex.Message));
                        }
                        finally
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XLogger(2630, -1, string.Format("Exception with Connection in JobData(); Task Serial={0}; Message={1}", taskSerial, ex.Message));
            }
            finally
            {
            }
            return rc;
        }
         * */
        public Dictionary<string, string> getJobData()
        {
            return XTRMObject.getJobData(jobSerial);
        }
        public int TaskLogger(int entryType, int entryResult, string entryText = null, string entryID = null, string entrySource = null, int lRetention = -1)
        {
            return XTRMTask.Log(taskSerial, entryType, entryResult, entryText, entryID, entrySource, lRetention);
        }
        public static int Log(int entryTask, int entryType, int entryResult, string entryText = null, string entryID = null, string entrySource = null, int lRetention = -1)
        {
            int rc = -99;

            if (entryTask < 0)
            {
                entryTask = 0;
            }
            
            // Check the Connection.
            try
            {
                if (TaskLogConnection != null)
                {
                    if (TaskLogConnection.State != System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            TaskLogConnection.Open();
                        }
                        catch (SqlException ex)
                        {
                            rc = ex.ErrorCode;
                        }
                        catch (Exception)
                        {
                            rc = -1;
                        }
                    }
                    if (TaskLogConnection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            // Need to repeat any single quotes in the text!
                            string strSQL = "EXEC dbo.spTaskLogger";
                            strSQL += String.Format(" @Task={0}", entryTask);
                            strSQL += String.Format(", @Type={0}", entryType);
                            strSQL += String.Format(", @Result={0}", entryResult);
                            if (lRetention != -1)
                            {
                                strSQL += String.Format(", @Retention={0}", lRetention);
                            }
                            if (entryText != null)
                            {
                                entryText = entryText.Replace("'", "''");
                                strSQL += String.Format(", @Text='{0}'", entryText);
                            }
                            if (entrySource != null)
                            {
                                entrySource = entrySource.Replace("'", "''");
                                strSQL += String.Format(", @Source='{0}'", entrySource);
                            }
                            if (entryID != null)
                            {
                                entryID = entryID.Replace("'", "''");
                                strSQL += String.Format(", @UserID='{0}'", entryID);
                            }
                            SqlCommand command = new SqlCommand(strSQL, TaskLogConnection);
                            rc = command.ExecuteNonQuery();
                            //SqlCommand commit = new SqlCommand("commit", TaskLogConnection);
                            //rc = command.ExecuteNonQuery();
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
            }
            return rc;
        }
        //private string ResolveParm(string rawParm, XTRMEvent thisEvent, Dictionary<string, string> thisConfig, int taskSerial = 0, int jobSerial = -1)
        public string ResolveText(string strText, int depth = 0)
        {
            string resolvedText = strText;
            string name;

            // @"\{\{(?i:(.*?))\}\}"
            // @"((?<name>.*?(?=\s*=)))(.*?)(?<value>(?<=\().+(?=\)))"
            MatchCollection matchSymbol = XTRMUtil.GetRegexMatches(strText, @"\{\{(?<name>(.*?))\}\}");
            if (matchSymbol != null)
            {
                if (matchSymbol.Count > 0)
                {
                    // Report on each match.
                    foreach (Match match in matchSymbol)
                    {
                        string test = match.Value;
                        GroupCollection groups = match.Groups;
                        name = groups["name"].Value;
                        // Dictionary Lookup. 
                        // Substitute into rawParm.
                        string result = "";
                        switch (name)
                        {
                            case "JobSerial":
                                result = jobSerial.ToString();
                                break;
                            case "TaskSerial":
                                result = taskSerial.ToString();
                                break;
                            case "EventSerial":
                                result = origEvent.eventSerial.ToString();
                                break;
                            case "EventSource":
                                result = origEvent.eventSource;
                                break;
                            case "EventAction":
                                result = origEvent.eventAction;
                                break;
                            case "EventDate":
                                result = origEvent.eventDate.ToString();
                                break;
                            case "EventState":
                                result = origEvent.eventState.ToString();
                                break;
                            case "EventUser":
                                result = origEvent.eventUser;
                                break;
                            case "EventPIN":
                                result = origEvent.eventPIN;
                                break;
                            case "EventUUID":
                                result = origEvent.eventUUID;
                                break;
                            case "EventProcessed":
                                result = origEvent.eventProcessed.ToString();
                                break;
                            case "EventParm1":
                                result = origEvent.eventParm1;
                                break;
                            case "EventParm2":
                                result = origEvent.eventParm2;
                                break;
                            case "EventParm3":
                                result = origEvent.eventParm3;
                                break;
                            case "EventParm4":
                                result = origEvent.eventParm4;
                                break;
                            case "EventParm5":
                                result = origEvent.eventParm5;
                                break;
                            case "EventParm6":
                                result = origEvent.eventParm6;
                                break;
                            case "EventParm7":
                                result = origEvent.eventParm7;
                                break;
                            case "EventParm8":
                                result = origEvent.eventParm8;
                                break;
                            case "EventParm9":
                                result = origEvent.eventParm9;
                                break;
                            case "EventParm10":
                                result = origEvent.eventParm10;
                                break;
                            case "EventTag":
                                result = origEvent.eventTag;
                                break;
                            default:
                                result = getConfig(jobData, name.ToUpper());
                                // If empty, then use name as the value.
                                break;
                        }
                        resolvedText = resolvedText.Replace(match.Value, result);
                    }
                }
            }
            if (!resolvedText.Equals(strText))
            {
                if (depth < 10)
                {
                    // THIS IS A RECURSIVE CALL!
                    resolvedText = ResolveText(resolvedText, depth++);
                }
            }
            return resolvedText;
        }
    }
}
