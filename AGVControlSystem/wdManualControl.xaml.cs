using ACS.BL;
using ACS.Common;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AGVControlSystem
{
    /// <summary>
    /// Interaction logic for wdManualControl.xaml
    /// </summary>
    public partial class wdManualControl : Window
    {
        public wdManualControl()
        {
            InitializeComponent();
            instanced = this;
        }
        public List<AGV> lstAGV = new List<AGV>();
        public static wdManualControl instanced;
        public string port_source, tag_source, port_dest, tag_dest, _idSource, _idDest, _trayID, _sourceState, _sourceBay, _destBay;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        public static string Choose;
        private BLTransportCommand oBLTrans = new BLTransportCommand();

        private void btnCall_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(port_source) && !string.IsNullOrEmpty(port_dest))
            {
                if (_sourceBay == _destBay)
                {
                    TransportCommand Transport = new TransportCommand();
                    
                    Transport.AGVID = cboAGV.Text;
                    Transport.STKID = "STK01_MECA2024";
                    Transport.CommandID = DateTime.Now.ToString("ddMMyyyyHHmmss") + "_" + port_source + "_" + port_dest;
                    Transport.CommandSource = port_source;
                    Transport.CommandDest = port_dest;          //thiết bị trả hàng
                    Transport.CommandSourceID = _idSource;
                    Transport.CommandDestID = _idDest;            //Điểm trả hàng
                    Transport.CommandStatus = "JOB CREATE";
                    Transport.JobStart = DateTime.Now;
                    Transport.TrayID = "";

                    oBLTrans.InsertTransportCommand(Transport);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Không thể tạo lệnh giữa 2 thiết bị không cùng khu vận vực vận hành. Vui lòng kiểm tra lại!");
                }    
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btt_Dest_Click(object sender, RoutedEventArgs e)
        {
            Choose = "Choose_Dest";
            wdEqiupment EQP = new wdEqiupment();
            EQP.ShowDialog();
        }

        private void btt_Source_Click(object sender, RoutedEventArgs e)
        {
            Choose = "Choose_Source";
            wdEqiupment EQP = new wdEqiupment();
            EQP.ShowDialog();
        }
    }

}
