using ACS.DL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ACS.BL
{
    public class BLLayout
    {
        /// <summary>
        /// Lấy trạng thái thiết bị để tạo lệnh
        /// </summary>
        /// <returns></returns>
        public static DataTable GetEqiupmentState()
        {
            string Stored = @"EXEC Proc_GetEqiupmentState";
            return DLLayout.GetDataTable(Stored);
        }

        public static DataTable LoadAllNode()
        {
            string Stored = @"SELECT * FROM dbo.NA_R_NODE";
            return DLLayout.GetDataTable(Stored);
        }

        /// <summary>
        /// Lấy danh sách thiết bị
        /// </summary>
        /// <returns></returns>
        public static DataTable LoadEqiupment()
        {
            string Stored = @"EXEC Proc_GetAllEqiupment";
            return DLLayout.GetDataTable(Stored);
        }

        /// <summary>
        /// Kéo lên config vẽ map
        /// </summary>
        /// <returns></returns>
        public static DataTable LoadMapConfig()
        {
            string Stored = @"EXEC Proc_LoadMapConfig";
            return DLLayout.GetDataTable(Stored);
        }

        /// <summary>
        /// Lấy thông tin AGV
        /// </summary>
        /// <returns></returns>
        public static DataTable Load_AGV()
        {
            string Stored = @"EXEC dbo.Proc_Get_AGVCurrentParametter";
            return DLLayout.GetDataTable(Stored);
        }

        /// <summary>
        /// Load lên thông số hiện tại của AGV
        /// </summary>
        /// <param name="dbcomman"></param>
        /// <returns></returns>
        public static DataTable ReadAGVCurrentParam(string dbcomman)
        {
            return DLLayout.GetDataTable(dbcomman);
        }

        /// <summary>
        /// Cập nhật Alarm AGV
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Alarm"></param>
        public static void UpdateAGVAlarm(string ID, string Alarm)
        {
            string query = "Update NA_R_VEHICLE Set ALARMSTATE = @Alarm Where ID = @ID";
            DLLayout.UpdateAGVAlarm(query, ID, Alarm);
        }

        /// <summary>
        /// Cập nhật dest AGV
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="dest"></param>
        public static void UpdateAGVDest(string ID, string dest)
        {
            string query = "Update NA_R_VEHICLE Set DESTNODEID = @Dest Where ID = @ID";
            DLLayout.UpdateAGVDest(query, ID, dest);
        }

        /// <summary>
        /// Cập nhật vị trí AGV
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="location"></param>
        public static void UpdateAGVLocation(string ID, string location)
        {
            string query = "Update NA_R_VEHICLE Set CURRENTNODEID = @Location Where ID = @ID";
            DLLayout.UpdateAGVLocation(query, ID, location);
        }

        /// <summary>
        /// Cập nhật trạng thái AGV
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="runStop"></param>
        /// <param name="fullEmpty"></param>
        public static void UpdateAGVState(string ID, string runStop, string fullEmpty)
        {
            switch (runStop)
            {
                case "0":   //Dừng
                    runStop = "PARK";
                    break;
                case "1":   //Chạy
                    runStop = "RUN";
                    break;                
                case "2":   //Chạy chờ lệnh
                    runStop = "IDLE";
                    break;
            }

            switch (fullEmpty)
            {
                case "1":   //Chạy
                    fullEmpty = "FULL";
                    break;
                case "0":   //Dừng
                    fullEmpty = "EMPTY";
                    break;
            }

            string query = "Update NA_R_VEHICLE Set RUNSTATE = @RunState, FULLSTATE = @FullState Where ID = @ID";
            DLLayout.UpdateAGVState(query, ID, runStop, fullEmpty);
        }
    }
}
