using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ACS.DL
{
    public class DLControlAGV
    {
        private static string ConnectionString = ConfigurationSettings.AppSettings["DatabaseConnection"];
        private static SqlConnection conn = new SqlConnection(ConnectionString);
        private static SqlCommand cmd = new SqlCommand();
        private static SqlDataAdapter da;

        /// <summary>
        /// Update path mới cho AGV
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ID"></param>
        /// <param name="Path"></param>
        public static void UpdateAGVPath(string query, string ID, string Path)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Path", Path);
                cmd.Parameters.AddWithValue("@ID", ID);
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
