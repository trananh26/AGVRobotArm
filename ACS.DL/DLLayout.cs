using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Security.Claims;
using System.Runtime.Remoting.Messaging;

namespace ACS.DL
{
    public class DLLayout
    {
        private static string ConnectionString = ConfigurationSettings.AppSettings["DatabaseConnection"];
        private static SqlConnection conn = new SqlConnection(ConnectionString);
        private static SqlCommand cmd = new SqlCommand();
        private static SqlDataAdapter da;


        public static DataTable GetDataTable(string stored)
        {
            DataTable dt = new DataTable();
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                da = new SqlDataAdapter(stored, conn);
                da.Fill(dt);
                conn.Close();
            }
            catch (Exception ee)
            {

            }
            return dt;
        }

        /// <summary>
        /// Cap nhat Alarm
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ID"></param>
        /// <param name="alarm"></param>
        public static void UpdateAGVAlarm(string query, string ID, string alarm)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Alarm", alarm);
                command.Parameters.AddWithValue("@ID", ID);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        /// <summary>
        /// Cập nhật lại dest AGV
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ID"></param>
        /// <param name="dest"></param>
        public static void UpdateAGVDest(string query, string ID, string dest)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Dest", dest);
                command.Parameters.AddWithValue("@ID", ID);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        /// <summary>
        /// Cập nhật vị trí AGV
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ID"></param>
        /// <param name="location"></param>
        public static void UpdateAGVLocation(string query, string ID, string location)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Location", location);
                command.Parameters.AddWithValue("@ID", ID);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        /// <summary>
        /// Cập nhật trạng thái chạy
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ID"></param>
        /// <param name="runStop"></param>
        /// <param name="fullEmpty"></param>
        public static void UpdateAGVState(string query, string ID, string runStop, string fullEmpty)
        {
             using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RunState", runStop);
                command.Parameters.AddWithValue("@FullState", fullEmpty);
                command.Parameters.AddWithValue("@ID", ID);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
