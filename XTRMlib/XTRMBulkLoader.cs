using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace XTRMlib
{
    public class XTRMBulkLoader : XTRMObject
    {
        EventLog myLog;
        // myConfigs are the list of XLator Config Files (by name) registered in the (active) dictionary!
        List<string> myConfigs = new List<string>();
        public XTRMBulkLoader()
        {
            myLog = null;
        }
        public XTRMBulkLoader(EventLog thisLog)
        {
            myLog = thisLog;
        }
        public int Initialize()
        {
            int rc = -99;
            try
            {
                SetLogID(1);
                myConfigs = XTRMObject.getDictionaryEntries("XLatorConfigFile");
                rc = myConfigs.Count;
            }
            catch (Exception ex)
            {
                XLogger(1175, -1, string.Format("Exception in Initialize(); message={0}", ex.Message));
                rc = -1;
            }
            return rc;
        }
        public int Run(int pass = 0, bool logBeat = false)
        {
            int rc = 0;
            return rc;
        }
        public int XLogger(int result, string logtext, int ID = 9800)
        {
            return XTRMObject.XLogger(ID, result, logtext);
        }
    }
}
