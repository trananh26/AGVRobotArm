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

namespace RobotControlSystem
{
    /// <summary>
    /// Interaction logic for wdEqiupment.xaml
    /// </summary>
    public partial class wdEqiupment : Window
    {
        private string module;
        private string Choose_state = wdManualControl.Choose;
        private DataTable Eqiupment_Infor = new DataTable();
        private string _SourceState, _DestState, _DestID;
        public List<AGV> lstAGV = new List<AGV>();
        public wdEqiupment()
        {
            InitializeComponent();
            Equipment_check();
        }

        /// <summary>
        /// Lấy danh sách thiết bị
        /// </summary>
        private void Equipment_check()
        {
            try
            {
                Eqiupment_Infor = BLLayout.LoadEqiupment();
                dtg_equip.ItemsSource = Eqiupment_Infor.DefaultView;
                dtg_equip.IsReadOnly = true;

            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }

        }

        /// <summary>
        /// Chọn thiết bị
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (_SourceState == "Empty" || string.IsNullOrEmpty(_SourceState) && (_DestID == "6102" || _DestID == "2106"))
            {
                MessageBox.Show("Không thể tạo lệnh vận chuyển từ nơi không có hàng. Vui kiểm tra lại", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (_DestID == "6102" || _DestID == "2106")
            {
                MessageBox.Show("Không thể tạo lệnh vận chuyển trả hàng tới băng tải Input. Vui lòng chọn ô chứa khác", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            }
            else
            {
                this.Close();
            }
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            wdManualControl.instanced.txb_Source.Text = "";
            wdManualControl.instanced.txb_Dest.Text = "";
            wdManualControl.instanced.port_source = "";
            wdManualControl.instanced._idSource = "";
            wdManualControl.instanced._trayID = "";
            wdManualControl.instanced.port_dest = "";
            wdManualControl.instanced._idDest = "";
            this.Close();
        }

        /// <summary>
        /// chọn thiết bị
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dtg_equip_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid gd = (DataGrid)sender;
            DataRowView row_selected = gd.SelectedItem as DataRowView;
            if (row_selected != null && Choose_state == "Choose_Source")
            {
                wdManualControl.instanced.port_source = row_selected["Name"].ToString();
                wdManualControl.instanced._idSource = row_selected["Node"].ToString();
                wdManualControl.instanced._trayID = "";// row_selected["TRAYID"].ToString();
                wdManualControl.instanced.txb_Source.Text = wdManualControl.instanced.port_source;
                _SourceState = row_selected["State"].ToString();
                txb_Search.Text = row_selected["Name"].ToString();
                wdManualControl.instanced._sourceBay = row_selected["BayID"].ToString();
                foreach (AGV _agv in lstAGV)
                {
                    if (_agv.BAYID == row_selected["BayID"].ToString())
                    {
                        wdManualControl.instanced.cboAGV.Text = _agv.ID;
                    }
                }
            }
            else if (row_selected != null && Choose_state == "Choose_Dest")
            {
                wdManualControl.instanced.port_dest = row_selected["Name"].ToString();
                wdManualControl.instanced._idDest = row_selected["Node"].ToString();
                wdManualControl.instanced.txb_Dest.Text = wdManualControl.instanced.port_dest;
                _DestState = row_selected["State"].ToString();
                _DestID = row_selected["Node"].ToString();
                txb_Search.Text = row_selected["Name"].ToString();
                wdManualControl.instanced._destBay = row_selected["BayID"].ToString();
                foreach (AGV _agv in lstAGV)
                {
                    if (_agv.BAYID == row_selected["BayID"].ToString())
                    {
                        wdManualControl.instanced.cboAGV.Text = _agv.ID;
                    }
                }
            }
        }

        private void txb_Search_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
