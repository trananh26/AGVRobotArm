using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.DL
{
    public class DLRobotArmControl
    {
        private static string ConnectionString = ConfigurationSettings.AppSettings["DatabaseConnection"];
        private static SqlConnection conn = new SqlConnection(ConnectionString);
        private static SqlCommand cmd = new SqlCommand();
        private static SqlDataAdapter da;

        public static void UpdateAllTrayState(string stored, string eqiupType, string state)
        {
            throw new NotImplementedException();
        }

        public static void UpdateTrayState(string stored, string PointID, string state)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = stored;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@PointID", PointID);
                cmd.Parameters.AddWithValue("@PointState", state);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ee)
            {

            }
        }
    }
}
