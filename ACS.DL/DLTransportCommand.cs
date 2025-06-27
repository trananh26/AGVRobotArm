using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using ACS.Common;
using System.Threading.Tasks;
using System.Configuration;
using System.Security.Claims;
using System.Collections;
using System.ComponentModel.Design;
using System.Security.Policy;

namespace ACS.DL
{
    public class DLTransportCommand
    {
        static SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["DatabaseConnection"]);
        static SqlCommand cmd = new SqlCommand();
        static SqlDataAdapter da;

        public void InsertTransportCommand(string Stored, TransportCommand Transport)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Stored;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@AGVID", Transport.AGVID);
                cmd.Parameters.AddWithValue("@STKID", Transport.STKID);
                cmd.Parameters.AddWithValue("@CommandID", Transport.CommandID);
                cmd.Parameters.AddWithValue("@TrayID", Transport.TrayID);
                cmd.Parameters.AddWithValue("@CommandSource", Transport.CommandSource);
                cmd.Parameters.AddWithValue("@CommandSourceID", Transport.CommandSourceID);
                cmd.Parameters.AddWithValue("@CommandDest", Transport.CommandDest);
                cmd.Parameters.AddWithValue("@CommandDestID", Transport.CommandDestID);
                cmd.Parameters.AddWithValue("@CommandStatus", Transport.CommandStatus);
                cmd.Parameters.AddWithValue("@JobStart", Transport.JobStart);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ee)
            {

            }
        }

        public void UpdateCommandStatus(string Stored, CurrentTransportCommand CurrentJob)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Stored;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@CommandID", CurrentJob.CommandID);
                cmd.Parameters.AddWithValue("@CommandStatus", CurrentJob.CommandStatus);
                cmd.Parameters.AddWithValue("@JobStart", CurrentJob.JobCreat);
                cmd.Parameters.AddWithValue("@JobAssign", CurrentJob.JobAssign);
                if (CurrentJob.CommandStatus == "JOB COMPLETE")
                {
                    cmd.Parameters.AddWithValue("@JobComplete", CurrentJob.JobComplete);
                }

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ee)
            {

            }
        }

        public void DeleteJob(string Stored, string DeleteJobID, DateTime JobCreateTime)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Stored;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@CommandID", DeleteJobID);
                cmd.Parameters.AddWithValue("@CommandStatus", "JOB CANCEL");
                cmd.Parameters.AddWithValue("@JobStart", JobCreateTime);
                cmd.Parameters.AddWithValue("@JobAssign", DateTime.Now);
                cmd.Parameters.AddWithValue("@JobComplete", DateTime.Now);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ee)
            {

            }
        }

        public DataTable GetDataByTable(string stored)
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

        public void UpdateAGVTransportCommand(string query, string ID, string CommandID)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CommandID", CommandID);
                cmd.Parameters.AddWithValue("@ID", ID);
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public static void UpdateOutputState(string query, string State)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@State", State);
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception)
            {

                throw;
            }
        }

        public static void UpdateEqiupmentState(string query, string eqiupmentID, string state)
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@State", state);
                cmd.Parameters.AddWithValue("@State", eqiupmentID);
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
