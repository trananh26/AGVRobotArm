using ACS.BL;
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
    /// Interaction logic for wdManualCallAGV.xaml
    /// </summary>
    public partial class wdManualCallAGV : Window
    {
        
        public wdManualCallAGV()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable dtMap = new DataTable();
            dtMap = BLLayout.LoadAllNode();
            cboNode.Items.Add("9999");
            foreach (DataRow dr in dtMap.Rows)
            {
                cboNode.Items.Add(dr[0].ToString());
            }

        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.instanced._callDest = cboNode.Text;
            MainWindow.instanced._callAGVID = cboAGVID.Text;
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.instanced._callDest = "";
            MainWindow.instanced._callAGVID = "";
            this.Close();
        }
    }
}
