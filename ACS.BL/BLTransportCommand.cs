using ACS.Common;
using ACS.DL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ACS.BL
{
    public class BLTransportCommand
    {
        DLTransportCommand db = new DLTransportCommand();
        /// <summary>
        /// lấy danh sách lệnh chờ
        /// </summary>
        /// <returns></returns>
        public DataTable GetQueueCommand()
        {
            string Stored = @"EXEC Proc_GetQueueCommandHistory";
            return db.GetDataByTable(Stored);
        }

        /// <summary>
        /// Lấy lệnh đang create phù hợp với khu vực chạy của AGV
        /// </summary>
        /// <param name="iD"></param>
        /// <returns></returns>
        public DataTable GetQueueCommandbyAGV(string ID)
        {
            string Stored = @"EXEC Proc_GetQueueCommandHistoryByAGV @AGVID = N'" + ID + "'";
            return db.GetDataByTable(Stored);
        }

        /// <summary>
        /// Lấy full lịch sử vận chuyển
        /// </summary>
        /// <returns></returns>
        public DataTable GetHistoryTransportCommand()
        {
            string Stored = @"EXEC Proc_GetAllCommandHistory";
            return db.GetDataByTable(Stored);
        }

        /// <summary>
        /// Thêm lệnh
        /// </summary>
        /// <param name="Transport"></param>
        public void InsertTransportCommand(TransportCommand Transport)
        {
            string Stored = "Insert_CommandHistory";
            db.InsertTransportCommand(Stored, Transport);
        }

        public void UpdateAGVTransport(string ID, string CommandID)
        {
            string query = "Update NA_R_VEHICLE Set TRANSPORTCOMMANDID = @CommandID Where ID = @ID";
            db.UpdateAGVTransportCommand(query, ID, CommandID);
        }

        public void UpdateCommandStatus(CurrentTransportCommand CurrentJob)
        {
            string Stored = "Update_CommandHistory";
            db.UpdateCommandStatus(Stored, CurrentJob);
        }

        public void DeleteJob(string DeleteJobID, DateTime JobCreateTime)
        {
            string Stored = "Update_CommandHistory";
            db.DeleteJob(Stored, DeleteJobID, JobCreateTime);
        }

        public static void UpdateOutputState(string State)
        {
            string cmd = "Update Eqiupment Set State = @State Where BayID = 'MECA20241'";
            DLTransportCommand.UpdateOutputState(cmd, State);
        }
    }
}
