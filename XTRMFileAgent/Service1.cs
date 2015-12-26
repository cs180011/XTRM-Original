using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Configuration.Install;
using XTRMlib;

namespace XTRMFileAgentService
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer aTimer;
        int beats = 0;
        //bool bProcessing = false;
        XTRMFileAgentCore myFileAgentCore;
        long serviceLife = 0;
        long memorySize = 0;
        int agentHoldTime = 10;
        string xBotID = "UNDEFINED";
        int pendingStop = 0;
        Dictionary<int, TimingData> myMetrics = new Dictionary<int, TimingData>();
        public Service1()
        {
            //this.ServiceName = "XFileAgent Service";
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("XFileAgent"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "XFileAgent", "Application");
            }
            eventLog1.Source = "XFileAgent";
            eventLog1.Log = "Application";

            try
            {
                //myFileAgentCore = new XFileAgentCore(eventLog1);
                //myFileAgentCore.Initialize();
                // DO NOT CALL Run() from service!
                //myFileAgentCore.Run();
            }
            catch (Exception ex)
            {
                string tempstr = string.Format("{0} Exception in XFileAgent() Constructor; Message={1}", AssemblyInfo.buildEnv, ex.Message);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Constructor Exception", AssemblyInfo.buildEnv), tempstr);
                pendingStop = 1;
            }
        }

        protected override void OnStart(string[] args)
        {
            myMetrics.Add(0, new TimingData("Dispatcher"));
            myMetrics.Add(1, new TimingData("Initiator"));
            myMetrics.Add(99, new TimingData("OVERALL"));

            // Create a timer with an initial 10 second interval.
            aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            // Set the Interval Initially to 10 seconds (10000 milliseconds).
            aTimer.Interval = 10000;
            aTimer.Enabled = true;

            try
            {
                myFileAgentCore = new XTRMFileAgentCore(eventLog1);
                xBotID = XTRMObject.getXTRMID();
                Process execProcess = Process.GetCurrentProcess();
                int rc = myFileAgentCore.Initialize();
                if (rc >= 0)
                {
                    string tempstr = string.Format("Starting...{0} Version={1} Built={2}.", AssemblyInfo.buildEnv, AssemblyInfo.buildVersion, AssemblyInfo.buildTime);
                    myFileAgentCore.XLogger(0, tempstr, 9901);
                    eventLog1.WriteEntry(tempstr);
                    heartBeat();
                    Alert(string.Format("{0} Starting", AssemblyInfo.buildEnv), tempstr);
                }
                else
                {
                    string tempstr = string.Format("Stopping...{0} Initialization Failed; code={1}.", AssemblyInfo.buildEnv, rc);
                    myFileAgentCore.XLogger(0, tempstr, 9902);
                    eventLog1.WriteEntry(tempstr);
                    Alert(string.Format("{0} Stopping - Init Failed", AssemblyInfo.buildEnv), tempstr);
                    ExitCode = -99;
                    Stop();
                }
            }
            catch (Exception ex)
            {
                string tempstr = string.Format("{0} Exception in Initialize(); Message={1}", AssemblyInfo.buildEnv, ex.Message);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Init Exception", AssemblyInfo.buildEnv), tempstr);
                ExitCode = -98;
                Stop();
            }
            finally
            {
                if (xBotID.Equals(""))
                {
                    // Cannot Start.
                    string tempstr = string.Format("{0} XBotID Not Set; service terminating.", AssemblyInfo.buildEnv);
                    eventLog1.WriteEntry(tempstr);
                    Alert(string.Format("{0} Stopping - No XBotID", AssemblyInfo.buildEnv), tempstr);
                    ExitCode = -96;
                    Stop();
                }
            }
        }
        protected override void OnStop()
        {
            aTimer.Enabled = false;
            heartBeat();
            eventLog1.WriteEntry(string.Format("Stopping...{0} Version={1} Built={2}.", AssemblyInfo.buildEnv, AssemblyInfo.buildVersion, AssemblyInfo.buildTime));
            myFileAgentCore.XLogger(0, string.Format("Stopping...{0} Version={1} Built={2}.", AssemblyInfo.buildEnv, AssemblyInfo.buildVersion, AssemblyInfo.buildTime), 9903);

            myFileAgentCore.Quiesce();
        }
        protected void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Stopwatch thisTimeOuter = new Stopwatch();
            thisTimeOuter.Start();
            beats++;
            serviceLife += Convert.ToInt64(aTimer.Interval);
            int rc = checkParms();
            if (rc < 0)
            {
                // Stop the service, SCM will re-start!
                ExitCode = rc;
                Stop();
            }
            rc = myFileAgentCore.ProcessEvents();
            //if (rc > 0)
            //{
            //    eventLog1.WriteEntry(string.Format("{0} Event(s) Created.", rc));
            //}
            rc = myFileAgentCore.FlushEvents(agentHoldTime);
            if (rc > 0)
            {
                eventLog1.WriteEntry(string.Format("{0} Event(s) Created.", rc));
            }
        }
        protected int checkParms()
        {
            int rc = 0;
            Process execProcess = Process.GetCurrentProcess();
            if (XTRMObject.getDictionaryEntry("ReloadDictionary", "Y").Equals("Y"))
            {
                XTRMObject.XDictionary = XTRMObject.createDictionary();
            }
            int heartbeatFrequency = Convert.ToInt32(XTRMObject.getDictionaryEntry("HeartbeatFrequency", "60"));
            if (beats % heartbeatFrequency == 0)
            {
                rc = 1; // Time for a heart beat!
            }
            xBotID = XTRMObject.getXTRMID();
            //int interval = Convert.ToInt32(XObject.getDictionaryEntry("DefaultInterval", "60"));
            int interval = Convert.ToInt32(XTRMObject.getDictionaryEntry("FileAgentInterval", "60"));
            agentHoldTime = Convert.ToInt32(XTRMObject.getDictionaryEntry("FileAgentHoldTime", "10"));
            int restartAfterSeconds = Convert.ToInt32(XTRMObject.getDictionaryEntry("RestartAfterSeconds", "82400"));
            int restartOverMemory = Convert.ToInt32(XTRMObject.getDictionaryEntry("RestartOverMemory", "500000000"));
            memorySize = execProcess.VirtualMemorySize64;
            if (!aTimer.Interval.Equals(interval * 1000))
            {
                // Make Change, log message.
                string tempstr = string.Format("{0} Interval = {1} Seconds.", AssemblyInfo.buildEnv, interval);
                myFileAgentCore.XLogger(0, tempstr, 9907);
                eventLog1.WriteEntry(tempstr);
                aTimer.Interval = interval * 1000;
            }
            if (pendingStop == 1)
            {
                string tempstr = string.Format("{0} Pending Shutdown Due to Initialization Exception.", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myFileAgentCore.XLogger(0, tempstr, 9913);
                rc = -5;
            }
            else if (pendingStop == 2)
            {
                string tempstr = string.Format("{0} Pending Shutdown Due to Missing XBotID.", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myFileAgentCore.XLogger(0, tempstr, 9914);
                rc = -6;
            }
            else if (xBotID.Equals(""))
            {
                string tempstr = string.Format("{0} XBotID Not Set.", AssemblyInfo.buildEnv);
                myFileAgentCore.XLogger(0, tempstr, 9911);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - No Version", AssemblyInfo.buildEnv), tempstr);
                rc = -4;
            }
            else if (Convert.ToInt32(serviceLife / 1000) >= restartAfterSeconds)
            {
                // Time to re-start!
                string tempstr = string.Format("Restarting {0} Service; Uptime = {1} Seconds.", AssemblyInfo.buildEnv, Convert.ToInt32(serviceLife / 1000));
                myFileAgentCore.XLogger(0, tempstr, 9908);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Uptime", AssemblyInfo.buildEnv), tempstr);
                rc = -2;
            }
            else if (memorySize > restartOverMemory)
            {
                // Time to re-start!
                string tempstr = string.Format("Restarting {0} Service; Memory = {1} Bytes.", AssemblyInfo.buildEnv, execProcess.VirtualMemorySize64);
                myFileAgentCore.XLogger(0, tempstr, 9909);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Memory", AssemblyInfo.buildEnv), tempstr);
                rc = -3;
            }
            return rc;
        }
        protected void heartBeat()
        {
            Process execProcess = Process.GetCurrentProcess();
            string tempstr = string.Format("HEARTBEAT {0}: XBotID={1}; Up={2}sec; Int={3}; Current={4}; Peak={5}; WSS={6}; TotProc={7};", AssemblyInfo.buildEnv, xBotID, Convert.ToInt32((serviceLife + 500) / 1000), (int)(aTimer.Interval / 1000), execProcess.VirtualMemorySize64, execProcess.PeakVirtualMemorySize64, execProcess.WorkingSet64, execProcess.TotalProcessorTime);
            myFileAgentCore.XLogger(0, tempstr, 9999);
            eventLog1.WriteEntry(tempstr);
            if (XTRMObject.getDictionaryEntry("ShowTimingSummary", "Y").Equals("Y"))
            {
                // Report Timing Metrics.
                tempstr = string.Format("HEARTBEAT {0}: Timing=({1}:CT={2};TOT={3}ms;AVG={4};MIN={5};MAX={6};LAST={7})", AssemblyInfo.buildEnv, myMetrics[99].description, myMetrics[99].count, myMetrics[99].totTime, myMetrics[99].avgTime, myMetrics[99].minTime, myMetrics[99].maxTime, myMetrics[99].lastTime);
                myFileAgentCore.XLogger(0, tempstr, 9979);
                eventLog1.WriteEntry(tempstr);
            }
            if (XTRMObject.getDictionaryEntry("ShowTimingDetail", "Y").Equals("Y"))
            {
                for (int i = 0; i < 2; i++)
                {
                    tempstr = string.Format("HEARTBEAT {0}: Timing=({1}:CT={2};TOT={3}ms;AVG={4};MIN={5};MAX={6};LAST={7})", AssemblyInfo.buildEnv, myMetrics[i].description, myMetrics[i].count, myMetrics[i].totTime, myMetrics[i].avgTime, myMetrics[i].minTime, myMetrics[i].maxTime, myMetrics[i].lastTime);
                    myFileAgentCore.XLogger(0, tempstr, 9980 + i);
                    eventLog1.WriteEntry(tempstr);
                }
            }
            if (!XTRMObject.getDictionaryEntry("ProcessJobs", "Y").Equals("Y"))
            {
                tempstr = string.Format("{0} Job Processing Suspended", AssemblyInfo.buildEnv);
                myFileAgentCore.XLogger(0, tempstr, 9910);
                eventLog1.WriteEntry(tempstr);
            }
            return;
        }
        protected void Alert(string subject, string text)
        {
            // Send email to XBot Admin.
            string contact = XTRMObject.getDictionaryEntry("AdminMail", "chris.swift@jmp.com");
            XTRMObject.Notify(contact, null, subject, text);
        }
    }
    internal static class AssemblyInfo
    {
        public static readonly string buildTime;
        public static readonly string buildName;
        public static readonly string buildEnv;
        public static readonly string buildVersion;
        static AssemblyInfo()
        {
            string env = XTRMObject.getDictionaryEntry("ConfigID", "?");
            buildName = Assembly.GetExecutingAssembly().GetName().Name;
            Version vers = Assembly.GetExecutingAssembly().GetName().Version;
            buildVersion = vers.ToString();
            buildTime = new DateTime(2000, 1, 1).AddDays(vers.Build).AddSeconds(vers.MinorRevision * 2).ToString();
            buildEnv = string.Format("{0}/{1}", buildName, env);
        }
    }
    internal class TimingData
    {
        public long totTime;
        public long minTime;
        public long maxTime;
        public long avgTime;
        public long lastTime;
        public long count;
        public string description;
        public TimingData(string text)
        {
            totTime = 0;
            minTime = 99999;
            maxTime = 0;
            avgTime = -1;
            lastTime = 0;
            count = 0;
            description = text;
        }
    }
}

