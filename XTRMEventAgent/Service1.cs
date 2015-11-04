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
using System.IO;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Data.Common;
using System.Reflection;
using XTRMlib;

namespace XTRMEventLoaderService
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer aTimer;
        static bool bQuiesce = false;
        static bool bProcessing = false;
        XTRMEventLoader myMonitorCore;
        long serviceLife = 0;
        long memorySize = 0;
        string xBotID = "UNDEFINED";
        int pendingStop = 0;
        // Timing Metrics
        int beats = 0;
        //long totTime = 0;
        //long minTime = 0;
        //long maxTime = 0;
        //TimingData thisTiming = new TimingData();
        Dictionary<int, TimingData> myMetrics = new Dictionary<int, TimingData>();
        public Service1()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("XTRMMonitor"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "XMonitor", "Application");
            }
            eventLog1.Source = "XMonitor";
            eventLog1.Log = "Application";

            try
            {
                myMonitorCore = new XTRMEventLoader(eventLog1);
            }
            catch (Exception ex)
            {
                string tempstr = string.Format("{0} Exception in XMonitorCore() Constructor; Message={1}", AssemblyInfo.buildEnv, ex.Message);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Constructor Exception", AssemblyInfo.buildEnv), tempstr);
                pendingStop = 1;
            }
        }
        protected override void OnStart(string[] args)
        {
            myMetrics.Add(0, new TimingData("Parse XLator Configs"));
            myMetrics.Add(1, new TimingData("Process Pending Events"));
            myMetrics.Add(2, new TimingData("Process Active Events"));
            myMetrics.Add(3, new TimingData("Process Testbot Data"));
            myMetrics.Add(4, new TimingData("Process Scripting Data"));
            myMetrics.Add(5, new TimingData("Process Stringbot References"));
            myMetrics.Add(6, new TimingData("Process Element String Data"));
            myMetrics.Add(7, new TimingData("Process Toolkits Data"));
            myMetrics.Add(8, new TimingData("Process Defects Data"));
            myMetrics.Add(9, new TimingData("Process Translations Data"));
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
                xBotID = XTRMObject.getXTRMID();
                Process execProcess = Process.GetCurrentProcess();
                int rc = myMonitorCore.Initialize();
                if (rc >= 0)
                {
                    string tempstr = string.Format("Starting...{0} Version={1} Built={2}.", AssemblyInfo.buildEnv, AssemblyInfo.buildVersion, AssemblyInfo.buildTime);
                    myMonitorCore.XLogger(0, tempstr, 9801);
                    eventLog1.WriteEntry(tempstr);
                    heartBeat();
                    Alert(string.Format("{0} Starting", AssemblyInfo.buildEnv), tempstr);
                }
                else
                {
                    string tempstr = string.Format("Stopping...{0} Initialization Failed; code={1}.", AssemblyInfo.buildEnv, rc);
                    myMonitorCore.XLogger(0, tempstr, 9802);
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
            myMonitorCore.XLogger(0, string.Format("Stopping...{0} Version={1} Built={2}.", AssemblyInfo.buildEnv, AssemblyInfo.buildVersion, AssemblyInfo.buildTime), 9803);
            bQuiesce = true;    // Need to Quiesce.
            int count = 0;
            while (bProcessing & (count < 20))
            {
                count++;
                string tempstr = string.Format("{0} Waiting for Processing to Terminate...", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myMonitorCore.XLogger(0, tempstr, 9804);
                Thread.Sleep(1000);
            }
            if (bProcessing)
            {
                string tempstr = string.Format("{0} Processing Failed to Terminate Normally...", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myMonitorCore.XLogger(0, tempstr, 9805);
                Alert(string.Format("{0} Stopping - Anomaly", AssemblyInfo.buildEnv), tempstr);
            }
        }
        protected void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Stopwatch thisTimeOuter = new Stopwatch();
            thisTimeOuter.Start();
            bool logBeat = false;
            beats++;
            //serviceLife += Convert.ToInt32((aTimer.Interval / 1000));
            serviceLife += Convert.ToInt64(aTimer.Interval);
            int rc = checkParms();
            if (rc < 0)
            {
                // Stop the service, SCM will re-start!
                ExitCode = rc;
                Stop();
            }
            else if (rc == 1)
            {
                logBeat = true;
            }
            aTimer.Enabled = false;

            try
            {
                if (!bQuiesce)
                {
                    if (!bProcessing)
                    {
                        bProcessing = true;

                        if (logBeat)
                        {
                            heartBeat();
                        }
                        for (int i = 0; i < 10; i++)
                        {
                            Stopwatch thisTimeInner = new Stopwatch();
                            thisTimeInner.Start();
                            // Run the Pass.
                            if (myMonitorCore.Run(i, false) < 0)
                            {
                                break;
                            }
                            thisTimeInner.Stop();
                            long elapsed = thisTimeInner.ElapsedMilliseconds;
                            if (elapsed > myMetrics[i].maxTime)
                            {
                                myMetrics[i].maxTime = elapsed;
                            }
                            if (elapsed < myMetrics[i].minTime)
                            {
                                myMetrics[i].minTime = elapsed;
                            }
                            myMetrics[i].totTime += elapsed;
                            myMetrics[i].count++;
                            myMetrics[i].avgTime = myMetrics[i].totTime / myMetrics[i].count;
                            myMetrics[i].lastTime = elapsed;

                            logBeat = false;
                        }
                        bProcessing = false;
                    }
                }
            }
            catch (Exception ex)
            {
                string tempstr = String.Format("{0} Caught Exception ({1})", AssemblyInfo.buildEnv, ex.Message);
                myMonitorCore.XLogger(0, tempstr, 9806);
                Alert(string.Format("{0} Stopping - Exception", AssemblyInfo.buildEnv), tempstr);
                eventLog1.WriteEntry(tempstr);
                ExitCode = rc;
                Stop();
            }
            finally
            {
                bProcessing = false;
                aTimer.Enabled = true;
                thisTimeOuter.Stop();
                long elapsed = thisTimeOuter.ElapsedMilliseconds;
                if (elapsed > myMetrics[99].maxTime)
                {
                    myMetrics[99].maxTime = elapsed;
                }
                if (elapsed < myMetrics[99].minTime)
                {
                    myMetrics[99].minTime = elapsed;
                }
                myMetrics[99].totTime += elapsed;
                myMetrics[99].count++;
                myMetrics[99].avgTime = myMetrics[99].totTime / myMetrics[99].count;
                myMetrics[99].lastTime = elapsed;
                serviceLife += elapsed;
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
            int interval = Convert.ToInt32(XTRMObject.getDictionaryEntry("MonitorInterval", "60"));
            int restartAfterSeconds = Convert.ToInt32(XTRMObject.getDictionaryEntry("RestartAfterSeconds", "82400"));
            int restartOverMemory = Convert.ToInt32(XTRMObject.getDictionaryEntry("RestartOverMemory", "500000000"));
            memorySize = execProcess.VirtualMemorySize64;
            if (!aTimer.Interval.Equals(interval * 1000))
            {
                // Make Change, log message.
                string tempstr = string.Format("{0} Interval = {1} Seconds.", AssemblyInfo.buildEnv, interval);
                myMonitorCore.XLogger(0, tempstr, 9807);
                eventLog1.WriteEntry(tempstr);
                aTimer.Interval = interval * 1000;
            }
            if (pendingStop == 1)
            {
                string tempstr = string.Format("{0} Pending Shutdown Due to Initialization Exception.", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myMonitorCore.XLogger(0, tempstr, 9813);
                rc = -5;
            }
            else if (pendingStop == 2)
            {
                string tempstr = string.Format("{0} Pending Shutdown Due to Missing XBotID.", AssemblyInfo.buildEnv);
                eventLog1.WriteEntry(tempstr);
                myMonitorCore.XLogger(0, tempstr, 9814);
                rc = -6;
            }
            else if (xBotID.Equals(""))
            {
                string tempstr = string.Format("{0} XBotID Not Set.", AssemblyInfo.buildEnv);
                myMonitorCore.XLogger(0, tempstr, 9812);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - No XBotID", AssemblyInfo.buildEnv), tempstr);
                rc = -4;
            }
            else if (Convert.ToInt32(serviceLife / 1000) >= restartAfterSeconds)
            {
                // Time to re-start!
                string tempstr = string.Format("Restarting {0} Service; Uptime = {1} Seconds.", AssemblyInfo.buildEnv, Convert.ToInt32(serviceLife / 1000));
                myMonitorCore.XLogger(0, tempstr, 9808);
                eventLog1.WriteEntry(tempstr);
                Alert(string.Format("{0} Stopping - Uptime", AssemblyInfo.buildEnv), tempstr);
                rc = -2;
            }
            else if (memorySize > restartOverMemory)
            {
                // Time to re-start!
                string tempstr = string.Format("Restarting {0} Service; Memory = {1} Bytes.", AssemblyInfo.buildEnv, execProcess.VirtualMemorySize64);
                myMonitorCore.XLogger(0, tempstr, 9809);
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
            myMonitorCore.XLogger(0, tempstr, 9899);
            eventLog1.WriteEntry(tempstr);
            if (XTRMObject.getDictionaryEntry("ShowTimingSummary", "Y").Equals("Y"))
            {
                // Report Timing Metrics.
                tempstr = string.Format("HEARTBEAT {0}: Timing=({1}:CT={2};TOT={3}ms;AVG={4};MIN={5};MAX={6};LAST={7})", AssemblyInfo.buildEnv, myMetrics[99].description, myMetrics[99].count, myMetrics[99].totTime, myMetrics[99].avgTime, myMetrics[99].minTime, myMetrics[99].maxTime, myMetrics[99].lastTime);
                myMonitorCore.XLogger(0, tempstr, 9879);
                eventLog1.WriteEntry(tempstr);
            }
            if (XTRMObject.getDictionaryEntry("ShowTimingDetail", "Y").Equals("Y"))
            {
                for (int i = 0; i < 10; i++)
                {
                    tempstr = string.Format("HEARTBEAT {0}: Timing=({1}:CT={2};TOT={3}ms;AVG={4};MIN={5};MAX={6};LAST={7})", AssemblyInfo.buildEnv, myMetrics[i].description, myMetrics[i].count, myMetrics[i].totTime, myMetrics[i].avgTime, myMetrics[i].minTime, myMetrics[i].maxTime, myMetrics[i].lastTime);
                    myMonitorCore.XLogger(0, tempstr, 9880 + i);
                    eventLog1.WriteEntry(tempstr);
                }
            }
            if (!XTRMObject.getDictionaryEntry("ProcessEvents", "Y").Equals("Y"))
            {
                tempstr = string.Format("{0} Event Processing Suspended", AssemblyInfo.buildEnv);
                myMonitorCore.XLogger(0, tempstr, 9810);
                eventLog1.WriteEntry(tempstr);
            }
            if (!XTRMObject.getDictionaryEntry("ProcessData", "Y").Equals("Y"))
            {
                tempstr = string.Format("{0} Data Processing Suspended", AssemblyInfo.buildEnv);
                myMonitorCore.XLogger(0, tempstr, 9811);
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
