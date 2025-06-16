using ACS.DL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ACS.BL
{
    public class BLControlAGV
    {
        /// <summary>
        /// Lưu path mới cho AGV
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Path"></param>
        public static void UpdateAGVPath(string ID, string Path)
        {
            string query = "Update NA_R_VEHICLE Set PATH = @Path Where ID = @ID";
            DLControlAGV.UpdateAGVPath(query, ID, Path);
        }
    }
}
