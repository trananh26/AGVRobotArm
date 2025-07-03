using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.DL;

namespace ACS.BL
{
	public class BLRobotArmControl
	{
		public static bool CheckFullSlotTrayByType(string type)
		{
			string cmd = @"SELECT * FROM dbo.RobotConfig WHERE PointType = '" + type + "' AND PointState = 'FULL'";
			return DLLayout.GetDataTable(cmd).Rows.Count == 4;
		}

		public static string GetControlPointByID(string PointID)
		{
			string cmd = @"SELECT * FROM dbo.RobotConfig WHERE PointID = 'IP_Material'";
			return DLLayout.GetDataTable(cmd).Rows[0]["PointDetail"].ToString();
		}

		public static DataTable GetControlPointByType(string PointType)
		{
			string cmd = "EXEC dbo.Proc_GetSlotByMaterialStatus @MaterialStatus = '" + PointType + "'";
			return DLLayout.GetDataTable(cmd);
		}

        public static void UpdateAllTrayState(string EqiupType, string State)
        {
            string Stored = "Proc_UpdateAllTrayState";
            DLRobotArmControl.UpdateAllTrayState(Stored, EqiupType, State);
        }

        public static void UpdateTrayState(string arm_TransferDest, string State)
        {
			string Stored = "Proc_UpdateRobotArmState";
			DLRobotArmControl.UpdateTrayState(Stored, arm_TransferDest, State);
		}
    }
}
