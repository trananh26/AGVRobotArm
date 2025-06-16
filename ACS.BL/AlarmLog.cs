using ACS.DL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.BL
{
    public class AlarmLog
    {
        /// <summary>
        /// Ghi nhan lich su loi
        /// </summary>
        /// <param name="AlarmCode"></param>
        public static void LogAlarmToDatabase(string AlarmCode)
        {
            string stored = "Proc_InsertAlarmHistory";
            DLReport.LogAlarm(stored, AlarmCode);
        }
    }
}
