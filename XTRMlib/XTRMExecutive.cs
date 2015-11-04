using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace XTRMlib
{
    // Error Number = 1228
    public class XTRMExecutive : XTRMObject
    {
        EventLog myLog;
        //int jobsAllowed = 10;
        List<int> activeJobs = new List<int>();
        List<int> activeJobTasks = new List<int>();
        List<int> candidateJobs = new List<int>();
        List<XTRMTask> runningTasks = new List<XTRMTask>();
        List<XTRMJob> runningJobs = new List<XTRMJob>();
        //bool bInitialized = false;
        bool initiatorStatus = getDictionaryEntry("ProcessJobs", "Y").Equals("Y");
        int jobsAllowed = Convert.ToInt32(getDictionaryEntry("MaximumJobs", "10"));

        public XTRMExecutive(EventLog thisLog)
        {
            myLog = thisLog;
        }
        public int Initialize()
        {
            int rc = 0;
            SetLogID(2);
            // Executive ONLY executes jobs; does not care about the XLator config.
            // Build list of currently active jobs
            /*
            runningJobs = new List<XJob>();
            rc = InventoryRunningJobs(runningJobs);
            if (rc >= 0)
            {
                bInitialized = true;
            }
             * */
            rc = CheckStatus();
            //if (rc >= 0)
            //{
            //    bInitialized = true;
            //}
            return rc;
        }
        public int CheckStatus()
        {
            int rc = 0;
            runningJobs = new List<XTRMJob>();
            rc = InventoryRunningJobs(runningJobs);
            //if (rc >= 0)
            //{
            //    bInitialized = true;
            //}
            return rc;
        }
        // Make Full Pass Through Processing.
        public int Run(int pass = 0, bool logBeat = false)
        {
            int rc = 0;

            bool initiatorStatus = getDictionaryEntry("ProcessJobs", "Y").Equals("Y");

            // Validate Connection.
            if (validateConnection(MasterConnection))
            {
                switch (pass)
                {
                    // Pass 1:  Evaluate current run states.
                    //          Review Active Jobs.
                    case 1:
                        //myLog.WriteEntry("Starting Pass 0");
                        if (CheckStatus() >= 0)
                        //Initialize();
                        //if (bInitialized)
                        {
                            // Inspect each running job.
                            //XLogger(1200, 0, string.Format("Jobs Executing={0}; Allowed={1}", runningJobs.Count, jobsAllowed));
                            foreach (XTRMJob thisJob in runningJobs)
                            {
                                //rc = thisJob.Initialize(thisJob.jobSerial);
                                //if (rc >= 0)
                                //{
                                if (thisJob.jobStatus == 1)
                                {
                                    int lActiveTasks = 0;
                                    // Inspect each running task (should be one at most).
                                    foreach (XTRMTask thisTask in thisJob.tasks)
                                    {
                                        // Refresh Task.
                                        //rc = thisTask.Initialize(thisTask.taskSerial);
                                        //if (rc >= 0)
                                        //{
                                        /*
                                        if (thisTask.taskStatus > 9)
                                        {
                                            // Done.
                                            // Start next one if no other active tasks.
                                            //thisJob.tasks.Remove(thisTask);
                                        }
                                            */
                                        if (thisTask.taskStatus == 1)
                                        {
                                            lActiveTasks++;
                                            // What was this condition originally???
                                            if (false)
                                            {
                                                // If not original task object, then let's check closer.
                                                // This can happen if XExecutive gets re-initialized with running jobs.
                                                if (!thisTask.taskOriginator)
                                                {
                                                    if (thisTask.taskPID != -1)
                                                    {
                                                        // Future, Check the [time]-limit?
                                                        // Check the PID; If exists, then wait.
                                                        try
                                                        {
                                                            Process thisProcess = Process.GetProcessById(thisTask.taskPID);
                                                            if (thisProcess.HasExited)
                                                            {
                                                                thisTask.taskStatus = 95;   // Complete; disposition unknown.
                                                                rc = thisTask.Save();
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            // Mark the task as complete.
                                                            thisTask.taskStatus = 91;   // Complete; disposition unknown.
                                                            rc = thisTask.Save();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Other status values ignored (for now) - should not be pending!
                                            //XLogger(1201, 0, string.Format("Unexpected TaskStatus Value={0}", thisTask.taskStatus));
                                            // If result < 0, then mark job for termination!
                                            if (thisTask.taskResult < 0)
                                            {
                                                XLogger(1201, 0, string.Format("Job #{0} Terminated Due to Task #{1} Result={2}.", thisJob.jobSerial, thisTask.taskSerial, thisTask.taskResult));
                                                thisJob.jobStatus = 98; // Terminated.
                                                thisJob.jobStop = DateTime.Now.ToString();
                                                lActiveTasks = 1;
                                                rc = thisJob.Save();
                                                if (rc < 0)
                                                {
                                                    XLogger(1227, 0, string.Format("thisJob.Save() Failed; rc={0}", rc));
                                                }
                                                // Send email to XBot Admin.
                                                string contact = XTRMObject.getDictionaryEntry("AdminMail", "chris.swift@jmp.com");
                                                Notify(contact, null, string.Format("Job #{0}/{1} Failed {2}", thisJob.jobSerial, thisTask.taskSerial, thisJob.jobName), string.Format("Exec={0}; Path={1}; Result={2}.", thisTask.taskExecutable, thisTask.taskPath, thisTask.taskResult));
                                            }
                                        }
                                        //}
                                        //else
                                        //{
                                        // Error on Initialize() of XTask.
                                        //    XLogger(1202, 0, string.Format("XTask.Initialize() Failure; rc={0}", rc));
                                        //}
                                    }
                                    //if (thisJob.tasks.Count == 0)
                                    if (lActiveTasks == 0)
                                    {
                                        // Look for next task (within this job).
                                        List<int> pendingTaskSerials = InventoryPendingTasks(thisJob);
                                        if (pendingTaskSerials.Count <= 0)
                                        {
                                            // Job Complete
                                            thisJob.jobStatus = 99;
                                            thisJob.jobStop = DateTime.Now.ToString();
                                            rc = thisJob.Save();
                                            if (rc < 0)
                                            {
                                                XLogger(1203, 0, string.Format("thisJob.Save() Failed; rc={0}", rc));
                                            }
                                            //runningJobs.Remove(thisJob);
                                        }
                                        else
                                        {
                                            XTRMTask pendingTask = new XTRMTask();
                                            if (pendingTaskSerials[0] < 1)
                                            {
                                                // Error
                                                XLogger(1204, 0, "No Pending TaskSerials");
                                            }
                                            rc = pendingTask.Initialize(pendingTaskSerials[0]);
                                            if (rc >= 0)
                                            {
                                                //thisJob.tasks.Add(pendingTask);
                                                //runningTasks.Add(pendingTask);

                                                rc = pendingTask.Start();
                                                if (rc < 0)
                                                {
                                                    XLogger(1205, 0, string.Format("pendingTask.Start() Failed; Serial={0}; rc={1}", pendingTask.taskSerial, rc));
                                                    // Send email to XBot Admin.
                                                    string contact = XTRMObject.getDictionaryEntry("AdminMail", "chris.swift@jmp.com");
                                                    Notify(contact, null, string.Format("Job Task #{0}/{1} {2} Could Not Start", thisJob.jobSerial, pendingTask.taskSerial, pendingTask.taskName), string.Format("Exec={0}; Path={1}.", pendingTask.taskExecutable, pendingTask.taskPath));
                                                }
                                                else
                                                {
                                                    XLogger(1206, 0, string.Format("Task #{0} Started", pendingTask.taskSerial));
                                                }
                                                //thisJob.tasks.Add(pendingTask);
                                            }
                                            else
                                            {
                                                // Error
                                                XLogger(1207, 0, string.Format("XTask.Initialize() Failure for Pending Task; rc={0}", rc));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Wait for other pending task(s) - should not happen!
                                        //XLogger(1208, 0, "Waiting for Job Task(s) to Complete.");
                                    }
                                    //}
                                    //else
                                    //{
                                    // Error on Initialize() of XJob.
                                    //    XLogger(1209, 0, string.Format("XJob.Initialize() Failure; rc={0}", rc));
                                    //}
                                }
                            }
                            // Purge Jobs that have completed! jobStatus >= 9
                            bool purgeFlag = true;
                            while (purgeFlag)
                            {
                                purgeFlag = false;
                                foreach (XTRMJob thisJob in runningJobs)
                                {
                                    if (thisJob.jobStatus >= 9)
                                    {
                                        runningJobs.Remove(thisJob);
                                        purgeFlag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Error!
                            XLogger(1210, 0, "Executive Not Initialized in Pass 0");
                        }
                        break;
                    // Pass 0:  Start New Jobs. 
                    //          Review Pending Jobs.
                    //          Spawn Candidate Jobs.
                    case 0:
                        if (initiatorStatus)
                        {
                            //myLog.WriteEntry("Starting Pass 1");
                            // If we are under the MPR, then start a new job.
                            if (runningJobs.Count < jobsAllowed)
                            {
                                rc = LoadCandidateJobs();
                                if (rc >= 0)
                                {
                                    foreach (int thisJobSerial in candidateJobs)
                                    {
                                        // Mark the Job as Started.
                                        XTRMJob newJob = new XTRMJob();
                                        if (runningJobs.Count < jobsAllowed)
                                        {
                                            rc = newJob.Initialize(thisJobSerial);
                                            if (rc >= 0)
                                            {
                                                newJob.jobStatus = 1;
                                                newJob.jobStart = DateTime.Now.ToString();
                                                //newJob.jobStop = DBNull.Value.ToString();
                                                rc = newJob.Save();
                                                if (rc >= 0)
                                                {
                                                    // Job Started; first task will start in next call to case 0:
                                                    runningJobs.Add(newJob);
                                                    XLogger(1222, 0, string.Format("Job #{0} Started; Executing={1}; Allowed={2}", newJob.jobSerial, runningJobs.Count, jobsAllowed));
                                                }
                                                else
                                                {
                                                    // Error
                                                    XLogger(1211, 0, string.Format("newJob.Save() Failure; rc={0}", rc));
                                                }
                                            }
                                            else
                                            {
                                                // Initialize Error (XJob).
                                                XLogger(1212, 0, string.Format("newJob.Initialize() Failure; rc = {0}", rc));
                                            }
                                        }
                                        else
                                        {
                                            XLogger(1213, 0, "Waiting for Runnng Jobs To Complete");
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // Error
                                    XLogger(1214, 0, string.Format("Failure in LoadCandidateJobs(); rc={0}", rc));
                                }
                            }
                            else
                            {
                                //XLogger(1215, 0, "Waiting for Capacity");
                                // Wait for capacity.
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
        // XSelectRunJobs
        private int InventoryRunningJobs(List<XTRMJob> runningJobs)
        {
            int rc = 0;
            //XJob thisJob = new XJob(false);
            //XTask thisTask = new XTask(true);
            runningJobs.Clear();
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select * from WorkJobs where Job_Status = 1 ";
                String strSelectCommand = String.Format(strTemp);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (Exception ex)
            {
                XLogger(1216, -1, string.Format("InventoryRunningJobs::Exception Getting Jobs (status = 1) from WorkJobs; Message={0}", ex.Message));
                rc = -1216;
            }
            theseRows = thisTable.Select();
            if (theseRows.Length > 0)
            {   // Update!
                for (int i = 0; i < theseRows.Length; i++)
                {
                    XTRMJob thisJob = new XTRMJob();
                    int thisJobSerial = (int)theseRows[i]["Job_Serial"];
                    rc = thisJob.Initialize(thisJobSerial);
                    if (rc >= 0)
                    {
                        rc = InventoryRunningTasks(thisJob);
                        runningJobs.Add(thisJob);
                    }
                    else
                    {
                        // Error
                        XLogger(1217, 0, string.Format("InventoryRunningJobs::thisJob.Initialize() Failure; rc={0}", rc));
                        rc = -1217;
                    }
                }
            }
            thisTable.Clear();
            return rc;
        }
        // XSelectRunTasks
        private int InventoryRunningTasks(XTRMJob thisJob)
        {
            int rc = 0;
            //XTask thisTask = new XTask(false);
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select * from WorkTasks where Job_Serial = {0} and Task_Status = 1 order by Task_Serial ";
                String strSelectCommand = String.Format(strTemp, thisJob.jobSerial);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (Exception ex)
            {
                XLogger(1218, -1, string.Format("InventoryRunningTasks::Exception Getting Tasks (status = 1) from WorkTasks; Message={0}", ex.Message));
            }
            theseRows = thisTable.Select();
            if (theseRows.Length > 0)
            {   // Update!
                for (int i = 0; i < theseRows.Length; i++)
                {
                    XTRMTask thisTask = new XTRMTask();
                    int thisTaskSerial = (int)theseRows[i]["Task_Serial"];
                    rc = thisTask.Initialize(thisTaskSerial);
                    if (rc >= 0)
                    {
                        thisJob.tasks.Add(thisTask);
                    }
                    else
                    {
                        // Error
                        XLogger(1219, 0, string.Format("InventoryRunningTasks::thisTask.Initialize() Failure; rc={0}", rc));
                    }
                }
            }
            thisTable.Clear();
            return rc;
        }
        // XSelectPendTasks
        private List<int> InventoryPendingTasks(XTRMJob thisJob)
        {
            int rc = 0;
            //XTask thisTask = new XTask(true);
            List<int> pendingTasks = new List<int>();
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select * from WorkTasks where Job_Serial = {0} and Task_Status = 0 order by Task_Serial ";
                String strSelectCommand = String.Format(strTemp, thisJob.jobSerial);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (Exception ex)
            {
                XLogger(1220, -1, string.Format("InventoryPendingTasks::Exception Getting Tasks (status = 0) from WorkTasks; Message={0}", ex.Message));
            }
            theseRows = thisTable.Select();
            if (theseRows.Length > 0)
            {   // Update!
                for (int i = 0; i < theseRows.Length; i++)
                {
                    int thisTaskSerial = (int)theseRows[i]["Task_Serial"];
                    pendingTasks.Add(thisTaskSerial);
                }
            }
            thisTable.Clear();
            return pendingTasks;
        }
        // XSelectQueueJobs
        private int LoadCandidateJobs()
        {
            int rc = 0;
            SqlDataAdapter eventQuery = new SqlDataAdapter();
            DataRow[] theseRows = null;
            DataTable thisTable = new DataTable();
            thisTable.CaseSensitive = true;
            candidateJobs.Clear();
            try
            {
                SqlCommandBuilder myCommandBuilder = new SqlCommandBuilder(eventQuery);
                String strTemp = "select * from WorkJobs where Job_Status = 0 order by Job_Serial ";
                String strSelectCommand = String.Format(strTemp);
                eventQuery.SelectCommand = new SqlCommand(strSelectCommand, MasterConnection);
                rc = eventQuery.Fill(thisTable);
            }
            catch (Exception ex)
            {
                XLogger(1221, -1, string.Format("LoadCandidateJobs::Exception Getting Jobs (status = 0) from WorkJobs; Message={0}", ex.Message));
            }
            theseRows = thisTable.Select();
            if (theseRows.Length > 0)
            {   // Update!
                for (int i = 0; i < theseRows.Length; i++)
                {
                    candidateJobs.Add((int)theseRows[i]["Job_Serial"]);
                }
            }
            thisTable.Clear();
            return rc;
        }
        /*
        public bool OKtoExit()
        {
            int rc = 0;
            bool bResult = false;
            runningJobs = new List<XJob>();
            //rc = InventoryRunningJobs(runningJobs);
            //if (rc >= 0)
            //{
            if (runningJobs.Count == 0)
            {
                bResult = true;
            }
            //}
            return bResult;
        }
         * */
        public int XLogger(int result, string logtext, int ID = 9900)
        {
            return XTRMObject.XLogger(ID, result, logtext);
        }
    }
}
