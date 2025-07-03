using ACS.BL;
using ACS.Common;
using RobotControlSystem.View;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using iTextSharp.text;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using ActUtlTypeLib;
using System.Net.NetworkInformation;
using Org.BouncyCastle.Ocsp;

namespace RobotControlSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Load_Map();
            AGV_check();

            DispatcherTimer Timer_CheckEqiupment = new DispatcherTimer();
            Timer_CheckEqiupment.Interval = TimeSpan.FromSeconds(0.5);
            Timer_CheckEqiupment.Tick += Timer_CheckEqiupment_Tick;
            Timer_CheckEqiupment.Start();

            DispatcherTimer Timer_CheckCommand = new DispatcherTimer();
            Timer_CheckCommand.Interval = TimeSpan.FromSeconds(2);
            Timer_CheckCommand.Tick += Timer_CheckCommand_Tick;
            Timer_CheckCommand.Start();

            DispatcherTimer Timer_CheckConnection = new DispatcherTimer();
            Timer_CheckConnection.Interval = TimeSpan.FromSeconds(1);
            Timer_CheckConnection.Tick += Timer_CheckConnection_Tick;
            Timer_CheckConnection.Start();

            instanced = this;
        }

        private SerialPort Robot = new SerialPort();
        private SerialPort AGV = new SerialPort();
        private SerialPort AI = new SerialPort();
        private uc_STK _stk = new uc_STK();
        private List<Node> lstNode = new List<Node>();
        private List<Link> lstLink = new List<Link>();
        private List<AGV> lstAGV = new List<AGV>();
        private AGV_Slim[] AGV_Slim = new AGV_Slim[10000];
        private uc_Tag[] uc_Tag = new uc_Tag[10000];
        private uc_Eqiupment[] uc_Eqiupment = new uc_Eqiupment[10000];
        private DataTable dtMaps = new DataTable();
        private List<CurrentTransportCommand> lstCurrentJob = new List<CurrentTransportCommand>();
        private List<Eqiupment> lstEqiupment = new List<Eqiupment>();

        private string _deleteJobID, _jobState;
        private DateTime _deleteJobCreateTime;
        private BLTransportCommand oBLTrans = new BLTransportCommand();
        public static MainWindow instanced;
        public string _callAGVID, _callDest;
        Queue<string> Lora_Receive = new Queue<string>();
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_MS = 1000;
        private const string TRAINING_HISTORY_FILE = "training_history.json";
        private string templateFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateData");

        private string WaitPoint = "9999";
        private string Output_OK_Point = "4106";
        private string Output_NG_Point = "5106";
        private string Arm_TransferDest = "";

        private bool detectResult = true; // Biến này để kiểm tra kết quả phát hiện đối tượng
        private bool RobotArm_isReady = true;
        private bool isTransfer_OKMaterial = true;


        // Thêm biến để lưu trữ ảnh gốc
        private ImageSource originalImage = null;

        private string bt1 = "M2100";   //báo hàng tại vị trí chờ gắp
        private string bt2_OK = "M2201";
        private string bt2_NG = "M2202";
        private string bt4_OK = "M2401";
        private string bt4_NG = "M2402";
        private string bt4_OK_RUN = "M2501";
        private string bt4_NG_RUN = "M2502";
        private string bt1_RUN = "M2505";
        private string OK_FullTray = "M2601";
        private string NG_FullTray = "M2602";

        private int s_bt1;
        private int s_bt2_OK;
        private int s_bt2_NG;
        private int s_bt4_OK;
        private int s_bt4_NG;

        private ActUtlType PLC = new ActUtlType();
        private string IPCamera = "192.168.3.150";
        private string IP_PLC = "192.168.3.250";


        // Cache cho query để tránh tạo lại string
        private static readonly string _cachedAGVQuery = @"
            SELECT A.ID AS AGVID, A.BAYID AS BAYID,A.FULLSTATE,A.CURRENTNODEID AS CURRENTTAG, 
                                    B.XPOS AS X_POS, YPOS AS Y_POS, A.ALARMSTATE AS ALARM, STATUS,
                                    A.RUNSTATE AS RUNSTATE, A.CONNECTIONSTATE AS CONNECTIONSTATE,
                                    TRANSPORTCOMMANDID, Direction, DESTNODEID, PATH
                                    FROM NA_R_VEHICLE A,NA_R_NODE B
                                    WHERE A.CURRENTNODEID = B.ID";

        /// <summary>
        /// Query tối ưu để lấy thông tin AGV với các tùy chọn filtering
        /// </summary>
        /// <param name="onlyConnected">Chỉ lấy AGV đang kết nối</param>
        /// <param name="onlyActive">Chỉ lấy AGV đang hoạt động</param>
        /// <returns>Query string được tối ưu</returns>
        private string GetOptimizedAGVQuery(bool onlyConnected = true, bool onlyActive = false)
        {
            // Sử dụng cached query nếu có thể
            if (onlyConnected && !onlyActive)
            {
                return _cachedAGVQuery;
            }

            var query = @"
                SELECT A.ID AS AGVID, A.BAYID AS BAYID,A.FULLSTATE,A.CURRENTNODEID AS CURRENTTAG, 
                                    B.XPOS AS X_POS, YPOS AS Y_POS, A.ALARMSTATE AS ALARM, STATUS,
                                    A.RUNSTATE AS RUNSTATE, A.CONNECTIONSTATE AS CONNECTIONSTATE,
                                    TRANSPORTCOMMANDID, Direction, DESTNODEID, PATH
                                    FROM NA_R_VEHICLE A,NA_R_NODE B
                                    WHERE A.CURRENTNODEID = B.ID";

            return query;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //Grid_Vision.Visibility = Visibility.Visible;
                Grid_Map.Visibility = Visibility.Visible;
                Grid_Robot.Visibility = Visibility.Hidden;
                Grid_CommandHistory.Visibility = Visibility.Hidden;
                Grid_CurrentCommand.Visibility = Visibility.Hidden;

                ConnectAGV();
                LoadTransportCommand();
                CountCommand();

                //_stk.Background = Brushes.LightGreen;
                _stk.Height = 360;
                _stk.Width = 820;
                Canvas.SetLeft(_stk, 340);
                Canvas.SetTop(_stk, 175);
                cvs_Map.Children.Add(_stk);
                CallAGVStartUp();

                //ConnectPLC();
            }
            catch (Exception ee)
            {

                MessageBox.Show(ee.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Auto Reconnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_CheckConnection_Tick(object sender, EventArgs e)
        {
            //if (PingPLC())
            //{
            //    GetPLCParam();
            //}
            if (!AGV.IsOpen)
            {

                AGV.PortName = clsFileIO.ReadValue("COM_AGV");
                AGV.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_AGV"));
                AGV.Open();

                AGV.DataReceived += Arduino_DataReceived;
            }
            if (!Robot.IsOpen)
            {
                Robot.PortName = clsFileIO.ReadValue("COM_ROBOT");
                Robot.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_ROBOT"));
                Robot.Open();
                Robot.DataReceived += Robot_DataReceived;
            }

            if (!AI.IsOpen)
            {
                AI.PortName = clsFileIO.ReadValue("COM_AI");
                AI.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_AI"));
                AI.Open();
                AI.DataReceived += AI_DataReceived;
            }

        }


        private void ConnectPLC()
        {
            try
            {
                PLC.ActLogicalStationNumber = 25;
                PLC.Open();
                //PLC.SetDevice("D5100", 0);
                //PLC.SetDevice("D5200", 1);
                //PLC.SetDevice("M1", 1);
                MessageBox.Show("Kết nối PLC thành công", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Không kết nối được với PLC. Vui lòng kiểm tra lại kết nối", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool PingPLC()
        {
            try
            {
                Ping PLCPing = new Ping();
                PingReply Reply = PLCPing.Send(IP_PLC);
                // check when the ping is not success
                if (Reply.Status != IPStatus.Success)
                {
                    AlarmLog.LogAlarmToDatabase("04");
                    MessageBox.Show("Không kết nối được với PLC. Vui lòng kiểm tra lại kết nối", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                AlarmLog.LogAlarmToDatabase("04");
                MessageBox.Show("Không kết nối được với PLC. Vui lòng kiểm tra lại kết nối", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông số PLC
        /// </summary>
        private void GetPLCParam()
        {
            //Kiểm tra trạng thái băng tải 1
            PLC.GetDevice(bt1, out s_bt1);
            if (s_bt1 == 1 && RobotArm_isReady)
            {
                string mess = BLRobotArmControl.GetControlPointByID("IP_Material");
                SendRobotArmControlCommand(mess);
            }

            //Kiểm tra trạng thái băng tải 2 OK
            PLC.GetDevice(bt2_OK, out s_bt2_OK);
            if (s_bt2_OK == 0)
            {
                BLRobotArmControl.UpdateAllTrayState("OK", "EMPTY");
            }

            //Kiểm tra trạng thái băng tải 2 NG
            PLC.GetDevice(bt2_NG, out s_bt2_NG);
            if (s_bt2_NG == 0)
            {
                BLRobotArmControl.UpdateAllTrayState("NG", "EMPTY");
            }

            //Kiểm tra trạng thái băng tải 4 OK
            PLC.GetDevice(bt4_OK, out s_bt4_OK);
            if (s_bt4_OK == 1)
            {
                BLTransportCommand.UpdateEqiupmentState("B1_CNV01_OP01", "FULL");
            }
            else if (s_bt4_OK == 0)
            {
                BLTransportCommand.UpdateEqiupmentState("B1_CNV01_OP01", "EMPTY");
            }
            //Kiểm tra trạng thái băng tải 4 NG
            PLC.GetDevice(bt4_NG, out s_bt4_NG);
            if (s_bt4_NG == 1)
            {
                BLTransportCommand.UpdateEqiupmentState("B1_CNV01_OP02", "FULL");
            }
            else if (s_bt4_NG == 0)
            {
                BLTransportCommand.UpdateEqiupmentState("B1_CNV01_OP02", "EMPTY");
            }
            //_eq.ID == "B1STK01_MC02" || _eq.ID == "B1_CNV01_OP01"
        }

        /// <summary>
        /// Gửi yêu cầu điều khiển cánh tay robot
        /// </summary>
        /// <param name="mess"></param>
        private void SendRobotArmControlCommand(string mess)
        {
            mess = "m" + mess.Substring(1, mess.Length - 1);
            Robot.Write(mess);
        }

        /// <summary>
        /// Xử lý dữ liệu nhận từ Robot
        /// </summary>
        /// <param name="data"></param>
        private void RobotDataAnalys(string data)
        {
            //Báo arm đã sẵn sàng ở vị trí home
            if (data.Contains("A1"))
            {
                RobotArm_isReady = true;
            }
            //Báo arm đang bận
            else if (data.Contains("A2"))
            {
                RobotArm_isReady = false;
            }
            //Báo lấy hàng thành công
            else if (data.Contains("A3"))
            {
                DataTable dt = new DataTable();

                if (!RobotArm_isReady && detectResult)
                {
                    dt = BLRobotArmControl.GetControlPointByType("OK");
                    if (dt.Rows.Count > 0)
                    {
                        string mess = dt.Rows[0]["ControlPoint"].ToString();
                        Arm_TransferDest = dt.Rows[0]["PointID"].ToString();
                        SendRobotArmControlCommand(mess);
                    }

                }
                if (!RobotArm_isReady && !detectResult)
                {
                    dt = BLRobotArmControl.GetControlPointByType("NG");
                    if (dt.Rows.Count > 0)
                    {
                        string mess = dt.Rows[0]["ControlPoint"].ToString();
                        Arm_TransferDest = dt.Rows[0]["PointID"].ToString();
                        SendRobotArmControlCommand(mess);
                    }
                }
            }
            //Báo tra hàng thành công
            else if (data.Contains("A4"))
            {
                //Cập nhật thêm 1 ô hàng OK hoặc NG full OK hoặc NG
                if (isTransfer_OKMaterial)
                {
                    BLRobotArmControl.UpdateTrayState(Arm_TransferDest, "FULL");
                }
                else
                {
                    BLRobotArmControl.UpdateTrayState(Arm_TransferDest, "FULL");
                }


                //Check xem có cái nào full chưa thì gọi nâng hạ tới
                if (CheckFullSlotTrayByType("OK"))
                {
                    PLC.SetDevice(OK_FullTray, 1); // full 1 slot OK
                }
                else if (CheckFullSlotTrayByType("NG"))
                {
                    PLC.SetDevice(NG_FullTray, 1); // full 1 slot NG
                }
                RobotArm_isReady = true;
            }
        }

        private bool CheckFullSlotTrayByType(string Type)
        {
            return BLRobotArmControl.CheckFullSlotTrayByType(Type);
        }

        /// <summary>
        /// Load danh sách lệnh đang chạy
        /// </summary>
        private void LoadTransportCommand()
        {
            try
            {
                lstCurrentJob.Clear();
                foreach (AGV _agv in lstAGV)
                {
                    ///Lấy danh sách lệnh đang CREAT theo kind và gán cho AGV
                    DataTable dtCommand = new DataTable();
                    dtCommand = oBLTrans.GetQueueCommandbyAGV(_agv.ID);
                    if (dtCommand.Rows.Count > 0)
                    {
                        CurrentTransportCommand _crCommand = new CurrentTransportCommand();
                        _crCommand.AGVID = dtCommand.Rows[0]["AGVID"].ToString();
                        _crCommand.STKID = dtCommand.Rows[0]["STKID"].ToString();
                        _crCommand.CommandID = dtCommand.Rows[0]["CommandID"].ToString();
                        _crCommand.TrayID = dtCommand.Rows[0]["TrayID"].ToString();
                        _crCommand.ProductID = dtCommand.Rows[0]["ProductID"].ToString();
                        _crCommand.CommandSource = dtCommand.Rows[0]["CommandSource"].ToString();
                        _crCommand.CommandDest = dtCommand.Rows[0]["CommandDest"].ToString();
                        _crCommand.CommandSourceID = dtCommand.Rows[0]["CommandSourceID"].ToString();
                        _crCommand.CommandDestID = dtCommand.Rows[0]["CommandDestID"].ToString();
                        _crCommand.CommandStatus = dtCommand.Rows[0]["CommandStatus"].ToString();
                        _crCommand.JobCreat = DateTime.Parse(dtCommand.Rows[0]["JobCreat"].ToString());
                        _crCommand.JobAssign = DateTime.Parse(dtCommand.Rows[0]["JobAssign"].ToString());
                        lstCurrentJob.Add(_crCommand);
                    }
                }
            }
            catch (Exception)
            {


            }

        }

        /// <summary>
        /// Xử lý log vận hành
        /// </summary>
        /// <param name="data"></param>
        private void ArduinoDataAnalys(string data)
        {
            try
            {
                //while (Lora_Receive.Count > 0)
                //{
                //    data = Lora_Receive.Dequeue();
                if (data.Length > 10)
                {
                    //A123T001234
                    string AGVID = data.Substring(1, 3);
                    string CodeType = data.Substring(4, 1);
                    string Subcode = data.Substring(5, 2);
                    string Code = data.Substring(7, 4);
                    UpdateAGVByLog(AGVID, CodeType, Subcode, Code);
                }
                //}
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        #region Điều khiển AGV theo vị trí và trạng thái
        /// <summary>
        /// Update lại thông tin AGV mỗi lần nhận log
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="codeType"></param>
        /// <param name="subcode"></param>
        /// <param name="code"></param>
        private void UpdateAGVByLog(string ID, string codeType, string subcode, string code)
        {
            switch (codeType)
            {
                case "T":
                    BLLayout.UpdateAGVLocation(ID, code);
                    PathAGV(ID, code);
                    OrderAGVByCommand(ID, code);

                    break;
                case "C":
                    break;
                case "E":
                    BLLayout.UpdateAGVAlarm(ID, code);
                    break;
                case "R":
                    BLLayout.UpdateAGVAlarm(ID, "NOALARM");
                    break;
                case "S":
                    string RunStop = code.Substring(2, 1);
                    string FullEmpty = code.Substring(3, 1);
                    BLLayout.UpdateAGVState(ID, RunStop, FullEmpty);
                    CheckAGVDest(ID, RunStop);
                    break;
                case "O": ///Báo các trường hợp dừng sau quay
                    //CallAGVAfterTurn(ID, codeType, subcode, code);
                    break;
                case "U":
                    //A123U011234
                    //Gọi chạy --> Order turn
                    UpdateCommandStatusByLocation(ID, code);
                    BLControlAGV.UpdateAGVPath(ID, "");
                    Update_AGV();
                    //CallAGV(ID, WaitPoint);//đã gọi rồi
                    PathAGV(ID, code);
                    OrderAGVByCommand(ID, code);

                    LoadCurentTransportCommand();
                    LoadHistoryTransportCommand();
                    break;
                case "L":
                    //A123L011234
                    //Gọi chạy --> Order turn
                    UpdateCommandStatusByLocation(ID, code);
                    BLControlAGV.UpdateAGVPath(ID, "");
                    Update_AGV();
                    PathAGV(ID, code);
                    OrderAGVByCommand(ID, code);

                    LoadCurentTransportCommand();
                    LoadHistoryTransportCommand();
                    break;
                default:
                    break;

            }
            Update_AGV();
        }

        /// <summary>
        /// Kiểm tra xe đến điểm và dừng chưa
        /// </summary>
        /// <param name="IDAGV"></param>
        /// <param name="runStop"></param>
        private void CheckAGVDest(string IDAGV, string runStop)
        {
            //AGV đã tới đích
            foreach (AGV _agv in lstAGV)
            {
                //nếu AGV đang thực hiện lệnh
                if (_agv.ID == IDAGV && !string.IsNullOrEmpty(_agv.TransportCommand) && runStop == "0")
                {
                    //Nếu đích là 4 OK
                    if (_agv.NODE == Output_OK_Point)
                    {
                        //điều khiển băng tải quay
                        PLC.SetDevice(bt4_OK_RUN, 1); // Băng tải 4 OK quay
                    }

                    //Nếu đích là 4 NG
                    if (_agv.NODE == Output_NG_Point)
                    {
                        // điều khiển băng tải quay
                        PLC.SetDevice(bt4_NG_RUN, 1); // Băng tải 4 NG quay
                    }
                }
            }
        }


        /// <summary>
        /// Path lại đường đi cho AGV
        /// </summary>
        /// <param name="iD"></param>
        /// <param name="code"></param>
        private void PathAGV(string IDAGV, string code)
        {
            foreach (AGV _agv in lstAGV)
            {

                if (_agv.Dest != WaitPoint && _agv.ID == IDAGV && !_agv.Path.Contains(code))
                {
                    SendOrderCommand(_agv.ID, "AO04" + code); //AGV reset Queue
                    string _NewPath = GetPathForAGV(code, _agv.Dest);
                    _agv.Path = _NewPath;
                    BLControlAGV.UpdateAGVPath(IDAGV, _NewPath);
                }
            }
        }

        /// <summary>
        /// Lấy path di chuyển cho AGV theo path mới
        /// </summary>
        /// <param name="code"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        private string GetPathForAGV(string crNode, string destNode)
        {
            List<string> result = new List<string>();
            PredictLine a = new PredictLine();
            result = a.GetPath(crNode, destNode);
            string result_string = string.Empty;
            for (int i = 0; i < result.Count(); i++)
            {
                if (i == 0)
                {
                    result_string = result[i].ToString();
                }
                else
                {
                    result_string += "," + result[i].ToString();
                }
            }

            return result_string;
        }

        /// <summary>
        /// Vẽ lên đường đi AGV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AGV_Click(object sender, EventArgs e)
        {

            try
            {
                string AGV = int.Parse(((AGV_Slim)sender).AGV_Name).ToString("000");


                //Line line= new Line();

                //foreach (Link link in lstLink)
                //{
                //    line = new Line { StrokeThickness = 2, Stroke = System.Windows.Media.Brushes.Blue, ToolTip = link.ID };
                //    //line.ToolTip= string.Format("Link: {0}", link.ID);
                //    line.X1 = link.StartX;
                //    line.Y1 = link.StartY;
                //    line.X2 = link.EndX;
                //    line.Y2 = link.EndY;
                //    cvs_Map.Children.Add(line);                 
                //}

            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Tạo yêu cầu điều hướng AGV
        /// </summary>
        private void OrderAGVByCommand(string IDAGV, string CrNode)
        {
            // Nếu đang chạy lệnh thì tìm đường đi mà order
            // Chạy free thì chạy quanh map
            try
            {
                foreach (AGV _agv in lstAGV)
                {
                    if (_agv.ID == IDAGV)
                    {
                        _agv.PreNode = _agv.NODE;
                        _agv.NODE = CrNode;
                        _agv.CrNode = CrNode;
                        string command = "";
                        //AGV đang có lệnh
                        if (_agv.Dest != WaitPoint)
                        {
                            string[] _nodes = _agv.Path.Split(',');
                            List<PathNode> lstPath = new List<PathNode>();

                            for (int i = 0; i < _nodes.Count(); i++)
                            {
                                string[] dataNodes = System.IO.File.ReadAllLines("Nodes.txt");
                                foreach (string dataNode in dataNodes)
                                {
                                    string[] s = dataNode.Split('\t');
                                    if (s[0] == _nodes[i].ToString())
                                    {
                                        PathNode _node = new PathNode();
                                        _node.Node = s[0];
                                        _node.X = int.Parse(s[1]);
                                        _node.Y = int.Parse(s[2]);

                                        lstPath.Add(_node);
                                    }
                                }
                            }

                            for (int i = 0; i < lstPath.Count; i++)
                            {
                                if (lstPath[i].Node == CrNode)
                                {
                                    if (lstPath[i].Node != _agv.Dest && lstPath[i + 1].Node != _agv.Dest)
                                    {
                                        if (lstPath[i].Y == lstPath[i + 1].Y)
                                        {
                                            //Đang đi ngang thì quay
                                            if (int.Parse(lstPath[i].Node) > int.Parse(lstPath[i + 1].Node))
                                            {
                                                if (lstPath[i + 2].X == lstPath[i + 1].X && lstPath[i + 2].Y > lstPath[i + 1].Y)
                                                {
                                                    //Quay trái
                                                    command = "AO01" + lstPath[i + 1].Node;
                                                }
                                                if (lstPath[i + 2].X == lstPath[i + 1].X && lstPath[i + 2].Y < lstPath[i + 1].Y)
                                                {
                                                    //Quay phải
                                                    command = "AO02" + lstPath[i + 1].Node;
                                                }
                                            }
                                            else
                                            {
                                                if (lstPath[i + 2].X == lstPath[i + 1].X && lstPath[i + 2].Y < lstPath[i + 1].Y)
                                                {
                                                    //Quay trái
                                                    command = "AO01" + lstPath[i + 1].Node;
                                                }
                                                if (lstPath[i + 2].X == lstPath[i + 1].X && lstPath[i + 2].Y > lstPath[i + 1].Y)
                                                {
                                                    //Quay phải
                                                    command = "AO02" + lstPath[i + 1].Node;
                                                }
                                            }
                                        }

                                        if (lstPath[i].X == lstPath[i + 1].X)
                                        {
                                            if (int.Parse(lstPath[i].Node) < int.Parse(lstPath[i + 1].Node))
                                            {
                                                //đang đi dọc thì quay
                                                if (lstPath[i + 2].X > lstPath[i + 1].X && lstPath[i + 2].Y == lstPath[i + 1].Y)
                                                {
                                                    //Quay trái
                                                    command = "AO01" + lstPath[i + 1].Node;
                                                }
                                                if (lstPath[i + 2].X < lstPath[i + 1].X && lstPath[i + 2].Y == lstPath[i + 1].Y)
                                                {
                                                    //Quay phải
                                                    command = "AO02" + lstPath[i + 1].Node;
                                                }
                                            }
                                            else
                                            {
                                                //đang đi dọc thì quay
                                                if (lstPath[i + 2].X < lstPath[i + 1].X && lstPath[i + 2].Y == lstPath[i + 1].Y)
                                                {
                                                    //Quay trái
                                                    command = "AO01" + lstPath[i + 1].Node;
                                                }
                                                if (lstPath[i + 2].X > lstPath[i + 1].X && lstPath[i + 2].Y == lstPath[i + 1].Y)
                                                {
                                                    //Quay phải
                                                    command = "AO02" + lstPath[i + 1].Node;
                                                }
                                            }
                                        }
                                    }

                                    if (_agv.Path.Contains(CrNode) && _agv.Dest != CrNode)
                                    {
                                        //Trái: O01, phải: O02
                                        switch (CrNode)
                                        {
                                            ////2101-> T2100
                                            //case "2101":
                                            //    command = "AO012100";
                                            //    break;
                                            //2102-> T2101
                                            case "2102":
                                                command = "AO012101";
                                                break;

                                            case "3105":
                                                command = "AO023106";
                                                break;

                                            case "5106":
                                                command = "AO016106";
                                                break;
                                            case "6106":
                                                command = "AO016107";
                                                break;
                                            ////2100-> T3100
                                            //case "2100":
                                            //    command = "AO013100";
                                            //    break;
                                            ////2101-> T3101
                                            //case "2101":
                                            //    command = "AO013101";
                                            //    break;

                                            ////7100-> T8100
                                            //case "7100":
                                            //    command = "AO018100";
                                            //    break;
                                            ////8106-> T8107
                                            //case "8106":
                                            //    command = "AO018107";
                                            //    break;
                                            //3107-> T2107
                                            case "3107":
                                                command = "AO012107";
                                                break;
                                            default:
                                                break;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(command))
                                        SendOrderCommand(_agv.ID, command);

                                    if (lstPath[i].Node != _agv.Dest && lstPath[i + 1].Node == _agv.Dest)
                                    {
                                        if (!string.IsNullOrEmpty(_agv.TransportCommand))
                                        {
                                            CallAGV(_agv.ID, _agv.Dest);
                                        }
                                        else
                                            SendOrderCommand(_agv.ID, "AO00" + _agv.Dest);
                                    }
                                }
                            }
                        }
                        //AGV trống lệnh
                        else
                        {
                            //Trái: O01, phải: O02
                            switch (CrNode)
                            {
                                //2101-> T2100
                                case "2101":
                                    command = "AO012100";
                                    break;
                                //3100-> T4100
                                case "2100":
                                    command = "AO013100";
                                    break;
                                case "3105":
                                    command = "AO023106";
                                    break;
                                //4106-> T4107
                                case "5106":
                                    command = "AO016106";
                                    break;
                                //3106-> T3107
                                case "6106":
                                    command = "AO016107";
                                    break;
                                //3107-> T2107
                                case "3107":
                                    command = "AO012107";
                                    break;
                                ////6101-> T6100
                                //case "6101":
                                //    command = "AO016100";
                                //    break;
                                ////7100-> T8100
                                //case "7100":
                                //    command = "AO018100";
                                //    break;
                                ////7101-> T7100
                                //case "7101":
                                //    command = "AO017100";
                                //    break;
                                ////8106-> T8107
                                //case "8106":
                                //    command = "AO018107";
                                //    break;
                                ////7107-> T6107
                                //case "7107":
                                //    command = "AO016107";
                                //    break;
                                ////5101-> P4101
                                //case "5101":
                                //    command = "AO024101";
                                //    break;
                                ////5106-> P6106
                                //case "5106":
                                //    command = "AO026106";
                                //    break;
                                default:
                                    break;
                            }

                            if (!string.IsNullOrEmpty(command))
                            {
                                SendOrderCommand(_agv.ID, command);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                PathAGV(IDAGV, CrNode);
                OrderAGVByCommand(IDAGV, CrNode);
            }
        }

        /// <summary>
        /// gọi AGV đi tiếp sau khi quay xong
        /// </summary>
        /// <param name="iD"></param>
        /// <param name="codeType"></param>
        /// <param name="subcode"></param>
        /// <param name="code"></param>
        private void CallAGVAfterTurn(string IDAGV, string codeType, string subcode, string code)
        {
            foreach (AGV _agv in lstAGV)
            {
                if (_agv.ID == IDAGV)
                {
                    if (subcode + code == "030101")
                    {
                        CallAGV(IDAGV, _agv.Dest);
                    }
                }
            }
        }

        #region Xử lý các sự kiện di chuyển AGV
        /// <summary>
        /// Kéo lên trạng thái AGV khi khởi tạo form
        /// </summary>
        private void AGV_check()
        {
            DataTable AGV_load = new DataTable();

            try
            {
                // Sử dụng query tối ưu
                string dbcomman = GetOptimizedAGVQuery(onlyConnected: true, onlyActive: false);

                AGV_load.Clear();
                AGV_load = BLLayout.ReadAGVCurrentParam(dbcomman);

                Load_AGV(AGV_load);
            }
            catch (Exception ex)
            {
                // Log lỗi thay vì bỏ qua
                System.Diagnostics.Debug.WriteLine($"Lỗi khi load AGV: {ex.Message}");
            }
        }

        /// <summary>
        /// Kéo dữ liệu AGV lên với error handling cải tiến
        /// </summary>
        /// <param name="AGV_load">DataTable chứa dữ liệu AGV</param>
        private void Load_AGV(DataTable AGV_load)
        {
            try
            {
                if (AGV_load == null || AGV_load.Rows.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Không có dữ liệu AGV để load");
                    return;
                }

                for (int i = 0; i < AGV_load.Rows.Count; i++)
                {
                    try
                    {
                        AGV AGV_startup = new AGV();

                        // Sử dụng safe conversion với default values
                        AGV_startup.ID = AGV_load.Rows[i]["AGVID"]?.ToString() ?? "";
                        AGV_startup.BAYID = AGV_load.Rows[i]["BAYID"]?.ToString() ?? "";
                        AGV_startup.STATE = AGV_load.Rows[i]["FULLSTATE"]?.ToString() ?? "EMPTY";
                        AGV_startup.STATUS = AGV_load.Rows[i]["RUNSTATE"]?.ToString() ?? "IDLE";
                        AGV_startup.BATTERY = 100; // Default battery level
                        AGV_startup.NODE = AGV_load.Rows[i]["CURRENTTAG"]?.ToString() ?? "";

                        // Safe conversion cho tọa độ X, Y
                        int xPos, yPos;
                        AGV_startup.X = int.TryParse(AGV_load.Rows[i]["X_POS"]?.ToString(), out xPos) ? xPos : 0;
                        AGV_startup.Y = int.TryParse(AGV_load.Rows[i]["Y_POS"]?.ToString(), out yPos) ? yPos : 0;

                        AGV_startup.ALARM = AGV_load.Rows[i]["ALARM"]?.ToString() ?? "NOALARM";
                        AGV_startup.CONNECTSTATE = AGV_load.Rows[i]["CONNECTIONSTATE"]?.ToString() ?? "DISCONNECTED";
                        AGV_startup.RUNSTATE = AGV_load.Rows[i]["RUNSTATE"]?.ToString() ?? "IDLE";

                        // Thông tin transport command
                        AGV_startup.TransportCommand = AGV_load.Rows[i]["TRANSPORTCOMMANDID"]?.ToString() ?? "";
                        AGV_startup.Direction = AGV_load.Rows[i]["Direction"]?.ToString() ?? "";
                        AGV_startup.Dest = AGV_load.Rows[i]["DESTNODEID"]?.ToString() ?? "";
                        AGV_startup.Path = AGV_load.Rows[i]["PATH"]?.ToString() ?? "";

                        // Chỉ thêm AGV có ID hợp lệ
                        if (!string.IsNullOrEmpty(AGV_startup.ID))
                        {
                            lstAGV.Add(AGV_startup);
                        }
                    }
                    catch (Exception rowEx)
                    {
                        // Log lỗi cho từng row nhưng tiếp tục xử lý các row khác
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý AGV row {i}: {rowEx.Message}");
                    }
                }

                // Chỉ update UI nếu có dữ liệu
                if (lstAGV.Count > 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Add_AGV();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi load AGV: {ex.Message}");
                // Có thể thêm notification cho user ở đây
            }
        }

        /// <summary>
        /// Thêm AGV vào map
        /// </summary>
        private void Add_AGV()
        {
            try
            {

                foreach (var agv in lstAGV)
                {
                    int ID_AGV = 0;


                    int.TryParse(agv.ID.ToString(), out ID_AGV);
                    if (ID_AGV < 10000)
                    {

                        AGV_Slim[ID_AGV] = new AGV_Slim();
                        AGV_Slim[ID_AGV].Height = 40;
                        AGV_Slim[ID_AGV].Width = 30;
                        AGV_Slim[ID_AGV].color_Baterry = System.Windows.Media.Brushes.Orange;
                        AGV_Slim[ID_AGV].AGV_Name = ID_AGV.ToString();

                        if (agv.ALARM == "NOALARM")
                        {
                            if (agv.STATUS == "RUN")
                                AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Lime;
                            else if (agv.STATUS == "PARK")
                                AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Yellow;
                            else if (agv.STATUS == "CHARGE")
                                AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.PaleGoldenrod;
                            else if (agv.STATUS == "IDLE")
                                AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.PaleVioletRed;
                        }

                        else
                            AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Red;
                        if (agv.STATE == "FULL")
                            AGV_Slim[ID_AGV].colorTray = System.Windows.Media.Brushes.Black;
                        else if (agv.STATE == "EMPTY")
                        {
                            AGV_Slim[ID_AGV].colorTray = AGV_Slim[ID_AGV].colorbackgroud;
                            AGV_Slim[ID_AGV].rtgSlottray.StrokeThickness = 0;
                        }
                        Canvas.SetLeft(AGV_Slim[ID_AGV], agv.X - 15);
                        Canvas.SetTop(AGV_Slim[ID_AGV], agv.Y - 20);
                        cvs_Map.Children.Add(AGV_Slim[ID_AGV]);


                        AGV_Slim[ID_AGV].AGVClick += new EventHandler(AGV_Click);

                    }
                }
            }
            catch (Exception ee)
            {

                //MessageBox.Show(ee.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //=========================================================================
        private void Update_AGV()
        {

            try
            {
                DataTable dtAGV = new DataTable();
                dtAGV = BLLayout.Load_AGV();

                lstAGV.Clear();

                for (int i = 0; i < dtAGV.Rows.Count; i++)
                {
                    AGV AGV_startup = new AGV();

                    //int.TryParse(AGV_startup.ID.ToString(), out ID_AGV);
                    AGV_startup.ID = dtAGV.Rows[i]["ID"].ToString();
                    AGV_startup.BAYID = dtAGV.Rows[i]["BAYID"].ToString();
                    AGV_startup.STATE = dtAGV.Rows[i]["FULLSTATE"].ToString();
                    AGV_startup.STATUS = dtAGV.Rows[i]["RUNSTATE"].ToString();
                    AGV_startup.BATTERY = 100;
                    AGV_startup.NODE = dtAGV.Rows[i]["CURRENTNODEID"].ToString();
                    AGV_startup.X = Convert.ToInt32(dtAGV.Rows[i]["X_POS"]);
                    AGV_startup.Y = Convert.ToInt32(dtAGV.Rows[i]["Y_POS"]);
                    AGV_startup.ALARM = dtAGV.Rows[i]["ALARMSTATE"].ToString();
                    AGV_startup.CONNECTSTATE = dtAGV.Rows[i]["CONNECTIONSTATE"].ToString();
                    AGV_startup.RUNSTATE = dtAGV.Rows[i]["RUNSTATE"].ToString();
                    AGV_startup.TransportCommand = dtAGV.Rows[i]["TRANSPORTCOMMANDID"].ToString();
                    AGV_startup.Direction = dtAGV.Rows[i]["Direction"].ToString();
                    AGV_startup.Dest = dtAGV.Rows[i]["DESTNODEID"].ToString();
                    AGV_startup.Path = dtAGV.Rows[i]["PATH"].ToString();
                    for (int j = 0; j < dtMaps.Rows.Count; j++)
                    {
                        if (AGV_startup.NODE == dtMaps.Rows[j]["FRTAG"].ToString())
                        {
                            AGV_startup.NEXTNODE = dtMaps.Rows[j]["TONODE"].ToString();
                            AGV_startup.NEXT_X = Convert.ToInt32(dtMaps.Rows[j]["TO_X"]);
                            AGV_startup.NEXT_Y = Convert.ToInt32(dtMaps.Rows[j]["TO_Y"]);
                        }
                    }
                    lstAGV.Add(AGV_startup);
                }
                this.Dispatcher.Invoke(() =>
                {
                    AGV_Update();
                });

            }
            catch (Exception e)
            {
                AlarmLog.LogAlarmToDatabase("08");
                MessageBox.Show(e.ToString());
            }
        }

        private void AGV_Update()
        {
            double AGV_count = lstAGV.Count;
            double AGV_full = 0;
            double AGV_Empty = 0;
            double AGV_connect = 0;
            double AGV_Run = 0;

            try
            {
                foreach (var agv in lstAGV)
                {
                    int ID_AGV = 0;

                    int.TryParse(agv.ID.ToString(), out ID_AGV);

                    AGV_Slim[ID_AGV].color_Baterry = System.Windows.Media.Brushes.LimeGreen;

                    if (agv.ALARM == "NOALARM")
                    {
                        if (agv.STATUS == "RUN")
                            AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Lime;
                        else if (agv.STATUS == "PARK")
                            AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Yellow;
                        else if (agv.STATUS == "CHARGE")
                            AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.PaleGoldenrod;
                        else if (agv.STATUS == "IDLE")
                            AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.PaleVioletRed;
                    }

                    else
                        AGV_Slim[ID_AGV].colorbackgroud = System.Windows.Media.Brushes.Red;
                    if (agv.STATE == "FULL")
                    {
                        AGV_Slim[ID_AGV].colorTray = System.Windows.Media.Brushes.Black;
                        AGV_full++;
                    }

                    else if (agv.STATE == "EMPTY")
                    {
                        AGV_Slim[ID_AGV].colorTray = AGV_Slim[ID_AGV].colorbackgroud;
                        AGV_Slim[ID_AGV].rtgSlottray.StrokeThickness = 0;
                        AGV_Empty++;
                    }

                    if (agv.CONNECTSTATE == "CONNECT")
                        AGV_connect++;

                    if (agv.RUNSTATE == "RUN")
                        AGV_Run++;

                    ////================update huong di chuyen cho ag
                    switch (agv.Direction)
                    {
                        case "L":
                            AGV_Slim[ID_AGV].Direction_AGV = 270;
                            break;
                        case "R":
                            AGV_Slim[ID_AGV].Direction_AGV = 90;
                            break;
                        case "B":
                            AGV_Slim[ID_AGV].Direction_AGV = 180;
                            break;
                        case "F":
                            AGV_Slim[ID_AGV].Direction_AGV = 0;
                            break;
                    }

                    Canvas.SetLeft(AGV_Slim[ID_AGV], agv.X - 14);
                    Canvas.SetTop(AGV_Slim[ID_AGV], agv.Y - 18);


                    AGV_Slim[ID_AGV].ToolTip = string.Format("Vehicle ID: {0}\nBAY ID: {1}\nStatus: {2}\nTrCmdId: {3}", agv.ID, agv.BAYID, agv.STATUS, agv.TransportCommand);

                    ToolTipService.SetShowDuration(AGV_Slim[ID_AGV], 2000000);
                }

                AGV_Connect.TotalValue = AGV_count;
                AGV_FullState.TotalValue = AGV_count;
                AGV_RunState.TotalValue = AGV_count;

                AGV_FullState.FullValue = AGV_full;
                AGV_FullState.EmptyValue = AGV_count - AGV_full;
                double empty_rate = (AGV_Empty) / (AGV_count) * 360;
                AGV_FullState.AngleValue = empty_rate;

                AGV_Connect.FullValue = AGV_connect;    //Full=Connect   Empty=Disconnect
                AGV_Connect.EmptyValue = AGV_count - AGV_connect;
                AGV_Connect.AngleValue = AGV_Connect.EmptyValue / AGV_count * 360;

                AGV_RunState.FullValue = AGV_Run;       //Full=Run      Empty=Stop
                AGV_RunState.EmptyValue = AGV_count - AGV_Run;
                AGV_RunState.AngleValue = AGV_RunState.EmptyValue / AGV_count * 360;

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
            }
        }
        //=========================================================================

        /// <summary>
        /// Load kéo map lên
        /// </summary>
        private void Load_Map()
        {
            try
            {
                DataTable dtMap = new DataTable();
                dtMap = BLLayout.LoadMapConfig();
                dtMaps = dtMap;

                for (int i = 0; i < dtMap.Rows.Count; i++)
                {
                    try
                    {
                        Node node = new Node();
                        node.ID = dtMap.Rows[i]["FRTAG"].ToString();
                        node.TYPE = dtMap.Rows[i]["TYPE"].ToString();
                        node.X = Convert.ToInt32(dtMap.Rows[i]["FROM_X"]);
                        node.Y = Convert.ToInt32(dtMap.Rows[i]["FROM_Y"]);
                        node.Direction = dtMap.Rows[i]["Direction"].ToString();
                        lstNode.Add(node);

                        Link horlink = new Link();
                        horlink.ID = dtMap.Rows[i]["LINK_ID"].ToString();
                        horlink.Source = horlink.ID.Substring(0, 4);
                        horlink.Dest = horlink.ID.Substring(5, 4);
                        horlink.Distance = dtMap.Rows[i]["DIS"].ToString();
                        horlink.StartX = Convert.ToInt32(dtMap.Rows[i]["FROM_X"]);
                        horlink.StartY = Convert.ToInt32(dtMap.Rows[i]["FROM_Y"]);
                        horlink.EndX = Convert.ToInt32(dtMap.Rows[i]["TO_X"]);
                        horlink.EndY = Convert.ToInt32(dtMap.Rows[i]["TO_Y"]);
                        lstLink.Add(horlink);
                    }
                    catch (Exception)
                    {
                        AlarmLog.LogAlarmToDatabase("09");
                    }
                }
                Draw_Map();
            }
            catch (Exception)
            {

            }

        }

        /// <summary>
        /// Vẽ Map
        /// </summary>
        private void Draw_Map()
        {

            try
            {
                foreach (Link link in lstLink)
                {
                    Line line = new Line { StrokeThickness = 2, Stroke = System.Windows.Media.Brushes.Gray, ToolTip = link.ID };
                    //line.ToolTip= string.Format("Link: {0}", link.ID);
                    line.X1 = link.StartX;
                    line.Y1 = link.StartY;
                    line.X2 = link.EndX;
                    line.Y2 = link.EndY;
                    cvs_Map.Children.Add(line);
                }
                Draw_Node();
            }
            catch (Exception)
            {
                AlarmLog.LogAlarmToDatabase("09");
            }
        }

        /// <summary>
        /// Vẽ node
        /// </summary>
        private void Draw_Node()
        {
            try
            {
                foreach (var node in lstNode)
                {
                    int Node_ID = 0;
                    int.TryParse(node.ID.ToString(), out Node_ID);

                    if (node.TYPE == "COMMON")
                    {

                        uc_Tag[Node_ID] = new uc_Tag();
                        uc_Tag[Node_ID].Height = 6;
                        uc_Tag[Node_ID].Width = 6;

                        switch (node.Direction)
                        {
                            case "L":
                                uc_Tag[Node_ID].rotation_tag.Angle = 180;
                                break;
                            case "R":
                                uc_Tag[Node_ID].rotation_tag.Angle = 0;
                                break;
                            case "B":
                                uc_Tag[Node_ID].rotation_tag.Angle = 90;
                                break;
                            case "F":
                                uc_Tag[Node_ID].rotation_tag.Angle = 270;
                                break;
                        }

                        if (Node_ID < 10000)
                        {
                            uc_Tag[Node_ID].colorbackgroud = node.TYPE != "COMMON" ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DimGray;
                            uc_Tag[Node_ID].ToolTip = string.Format("Node: {0}", node.ID);
                            ToolTipService.SetShowDuration(uc_Tag[Node_ID], 20000);
                            Canvas.SetLeft(uc_Tag[Node_ID], node.X - 3);
                            Canvas.SetTop(uc_Tag[Node_ID], node.Y - 3);

                            cvs_Map.Children.Add(uc_Tag[Node_ID]);
                            uc_Tag[Node_ID].Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        uc_Eqiupment[Node_ID] = new uc_Eqiupment();
                        uc_Eqiupment[Node_ID].Height = 10;
                        uc_Eqiupment[Node_ID].Width = 10;


                        if (Node_ID < 10000)
                        {
                            uc_Eqiupment[Node_ID].colorbackgroud = node.TYPE != "COMMON" ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DimGray;
                            uc_Eqiupment[Node_ID].ToolTip = string.Format("Node: {0}", node.ID);
                            ToolTipService.SetShowDuration(uc_Eqiupment[Node_ID], 20000);
                            Canvas.SetLeft(uc_Eqiupment[Node_ID], node.X - 5);
                            Canvas.SetTop(uc_Eqiupment[Node_ID], node.Y - 5);

                            cvs_Map.Children.Add(uc_Eqiupment[Node_ID]);
                            uc_Eqiupment[Node_ID].Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception)
            {
                AlarmLog.LogAlarmToDatabase("09");
            }
        }

        #endregion

        #region Xử lý lệnh vận chuyển
        /// <summary>
        /// định kỳ kiểm tra gán lệnh cho AGV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_CheckCommand_Tick(object sender, EventArgs e)
        {
            try
            {
                //1.Kiểm tra xem AGV đang chạy chờ không
                //2.Kéo danh sách lệnh theo loại AGV và gán cho nó
                //3.Gọi AGV chạy thực hiện lệnh
                //Ktra trạng thái AGV, nếu AGV đang chạy free và ko có lệnh thì tìm lệnh theo kind cho nó

                foreach (AGV _agv in lstAGV)
                {
                    if (_agv.Dest == WaitPoint && string.IsNullOrEmpty(_agv.TransportCommand))
                    {
                        ///Lấy danh sách lệnh đang CREAT theo kind và gán cho AGV
                        DataTable dtCommand = new DataTable();
                        dtCommand = oBLTrans.GetQueueCommandbyAGV(_agv.ID);
                        lstCurrentJob.Clear();
                        if (dtCommand.Rows.Count > 0)
                        {
                            CurrentTransportCommand _crCommand = new CurrentTransportCommand();
                            _crCommand.AGVID = dtCommand.Rows[0]["AGVID"].ToString();
                            _crCommand.STKID = dtCommand.Rows[0]["STKID"].ToString();
                            _crCommand.CommandID = dtCommand.Rows[0]["CommandID"].ToString();
                            _crCommand.TrayID = dtCommand.Rows[0]["TrayID"].ToString();
                            _crCommand.ProductID = dtCommand.Rows[0]["ProductID"].ToString();
                            _crCommand.CommandSource = dtCommand.Rows[0]["CommandSource"].ToString();
                            _crCommand.CommandDest = dtCommand.Rows[0]["CommandDest"].ToString();
                            _crCommand.CommandSourceID = dtCommand.Rows[0]["CommandSourceID"].ToString();
                            _crCommand.CommandDestID = dtCommand.Rows[0]["CommandDestID"].ToString();
                            _crCommand.CommandStatus = "JOB START";
                            _crCommand.JobCreat = DateTime.Parse(dtCommand.Rows[0]["JobCreat"].ToString());
                            _crCommand.JobAssign = DateTime.Now;

                            lstCurrentJob.Add(_crCommand);
                            //Cập nhật lệnh cho AGV
                            oBLTrans.UpdateCommandStatus(_crCommand);
                            oBLTrans.UpdateAGVTransport(_agv.ID, _crCommand.CommandID);
                            BLLayout.UpdateAGVDest(_crCommand.AGVID, _crCommand.CommandSourceID);
                            LoadCurentTransportCommand();
                            break;
                        }
                    }

                }

            }
            catch (Exception ee)
            {

                // MessageBox.Show(ee.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái của thiết bị để tạo lệnh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_CheckEqiupment_Tick(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = new DataTable();
                dt = BLLayout.GetEqiupmentState();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        Eqiupment _eq = new Eqiupment();
                        _eq.ID = dr["ID"].ToString();
                        _eq.Name = dr["Name"].ToString();
                        _eq.Node = dr["Node"].ToString();
                        _eq.ToID = dr["ToID"].ToString();
                        _eq.ToName = dr["ToName"].ToString();
                        _eq.ToNode = dr["ToNode"].ToString();
                        _eq.Kind = dr["Kind"].ToString();
                        _eq.State = dr["State"].ToString();
                        _eq.BayID = dr["BayID"].ToString();


                        if ((_eq.ID == "B1_CNV01_OP02" || _eq.ID == "B1_CNV01_OP01") && _eq.State == "FULL")
                        {
                            CreateTransportCommand(_eq);
                        }
                    }
                }
            }
            catch (Exception)
            {


            }

        }

        /// <summary>
        /// Tạo lệnh vận chuyển
        /// </summary>
        /// <param name="dtPort"></param>
        private void CreateTransportCommand(Eqiupment _eq)
        {
            //1.Tạo lệnh theo loại AGV
            //2.Kiểm tra đã có lệnh từ 2 điểm này đang chờ chưa
            //3.Nếu chưa có thì tạo
            bool _createJob = true;
            string Port_Source = _eq.Name;
            string ID_Source = _eq.Node;
            string Port_Dest = _eq.ToName;
            string ID_Dest = _eq.ToNode;
            foreach (AGV _agv in lstAGV)
            {
                if (_agv.BAYID == _eq.BayID)
                {
                    TransportCommand Transport = new TransportCommand();
                    Transport.AGVID = _agv.ID;
                    Transport.STKID = "STK01_MECA2025";
                    Transport.CommandID = DateTime.Now.ToString("ddMMyyyyHHmmss") + "_" + Port_Source + "_" + Port_Dest;
                    Transport.CommandSource = Port_Source;
                    Transport.CommandDest = Port_Dest;
                    Transport.CommandSourceID = ID_Source;
                    Transport.CommandDestID = ID_Dest;
                    Transport.CommandStatus = "JOB CREATE";
                    Transport.JobStart = DateTime.Now;
                    Transport.TrayID = "";

                    DataTable dtCrCommand = new DataTable();
                    dtCrCommand = oBLTrans.GetQueueCommand();
                    if (dtCrCommand.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dtCrCommand.Rows)
                        {
                            if (dr["CommandSource"].ToString() == Port_Source)
                            {
                                _createJob = false;
                                break;
                            }
                        }
                    }
                    if (_createJob)
                    {
                        oBLTrans.InsertTransportCommand(Transport);
                        CountCommand();
                        LoadCurentTransportCommand();
                    }
                }

            }

        }

        /// <summary>
        /// Tính tổng lệnh vào ra kho
        /// </summary>
        private void CountCommand()
        {
            int count_load = 0;
            int count_unload = 0;
            DataTable dt = new DataTable();
            dt = oBLTrans.GetHistoryTransportCommand();
            foreach (DataRow dr in dt.Rows)
            {
                if (DateTime.Parse(dr["JobCreat"].ToString()).ToString("dd/MM/yyyy") == DateTime.Now.ToString("dd/MM/yyyy"))
                {
                    if (dr["AGVID"].ToString() == "001")
                        count_load++;
                    if (dr["AGVID"].ToString() == "002")
                        count_unload++;
                }
            }

            lbl_LoadCount.Content = count_load.ToString();
            lbl_UnloadCount.Content = count_unload.ToString();
        }

        /// <summary>
        /// Cập nhật trạng thái lệnh vận chuyển
        /// </summary>
        private void UpdateCommandStatusByLocation(string IDAGV, string Location)
        {
            // Nhận được vị trí AGV thì tiến hành cập nhật lại trạng thái lệnh vận chuyển
            // Đúng ra phải xử lý bằng U Code và L code
            try
            {
                LoadTransportCommand();

                foreach (CurrentTransportCommand CurrentJob in lstCurrentJob)
                {
                    if (CurrentJob.AGVID == IDAGV)
                    {
                        if (CurrentJob.CommandSourceID == Location && CurrentJob.CommandStatus == "JOB START") // lấy hàng
                        {
                            CurrentJob.CommandStatus = "TRANSFERING DEST";
                            oBLTrans.UpdateCommandStatus(CurrentJob);
                            BLLayout.UpdateAGVDest(IDAGV, CurrentJob.CommandDestID);
                        }
                        else if (CurrentJob.CommandDestID == Location && CurrentJob.CommandStatus == "TRANSFERING DEST") //trả hàng  
                        {
                            CurrentJob.CommandStatus = "JOB COMPLETE";
                            CurrentJob.JobComplete = DateTime.Now;
                            oBLTrans.UpdateCommandStatus(CurrentJob);

                            oBLTrans.UpdateAGVTransport(IDAGV, "");
                            BLControlAGV.UpdateAGVPath(IDAGV, "");
                            //if (Location == InputPoint)
                            //{
                            //    BLTransportCommand.UpdateOutputState("EMPTY");
                            //}
                            BLLayout.UpdateAGVDest(IDAGV, WaitPoint);
                            SendOrderCommand(IDAGV, "AO03" + WaitPoint);

                            foreach (AGV _agv in lstAGV)
                            {
                                if (_agv.ID == IDAGV)
                                {
                                    _agv.Dest = WaitPoint;
                                    OrderAGVByCommand(IDAGV, Location);
                                }
                            }

                            BLLayout.UpdateAGVState(IDAGV, "2", "0");//Empty--idle

                            //Loại job ra khỏi danh sách đang thực hiện

                            //var itemRemove = lstCurrentJob.Where(x => x.AGVID == IDAGV);
                            //foreach (var item in itemRemove)
                            //{
                            //    if (item.CommandID == CurrentJob.CommandID)
                            //    {
                            //        lstCurrentJob.Remove(item);
                            //    }
                            //}
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                //MessageBox.Show(ee.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        /// <summary>
        /// Gọi AGV di chuyển khi khởi tạo phần mềm
        /// </summary>
        private void CallAGVStartUp()
        {
            foreach (AGV _agv in lstAGV)
            {
                string mess = _agv.ID + "AC00" + _agv.Dest;
                //Call AGV
                //Arduino.Write(mess + "x");
            }
        }

        /// <summary>
        /// Gửi C Code gọi AGV
        /// </summary>
        /// <param name="v"></param>
        private void CallAGV(string AGVID, string Dest)
        {
            string mess = AGVID + "AC00" + Dest;
            {
                AGV.Write(mess + "x");
            }


            //Update lại dest AGV
            foreach (AGV _agv in lstAGV)
            {
                if (_agv.ID == AGVID)
                {
                    _agv.Dest = Dest;

                    BLLayout.UpdateAGVDest(AGVID, Dest);
                }
            }
        }

        /// <summary>
        /// Order AGV quay
        /// </summary>
        /// <param name="AGVID"></param>
        /// <param name="Command"></param>
        private void SendOrderCommand(string AGVID, string Command)
        {
            if (!string.IsNullOrEmpty(Command))
            {
                string mess = AGVID + Command;
                {
                    AGV.Write(mess + "x");
                }
            }
        }

        #endregion


        /// <summary>
        /// Kết nối AGV
        /// </summary>
        private void ConnectAGV()
        {

            ///Arduino: Xe số 1
            ///STM: Xe số 2

            AGV.PortName = clsFileIO.ReadValue("COM_AGV");
            AGV.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_AGV"));
            AGV.Open();

            AGV.DataReceived += Arduino_DataReceived;

            Robot.PortName = clsFileIO.ReadValue("COM_ROBOT");
            Robot.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_ROBOT"));
            Robot.Open();

            Robot.DataReceived += Robot_DataReceived;


            AI.PortName = clsFileIO.ReadValue("COM_AI");
            AI.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE_AI"));
            AI.Open();

            AI.DataReceived += AI_DataReceived;

            //Arduino1.PortName = clsFileIO.ReadValue("COM_ARDUINO1");
            //Arduino1.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE"));
            //Arduino1.Open();

            //Arduino1.DataReceived += Arduino1_DataReceived;

            //SWMPort.PortName = clsFileIO.ReadValue("COM_SWMPORT");
            //SWMPort.BaudRate = int.Parse(clsFileIO.ReadValue("BAURATE"));
            //SWMPort.Open();
            //SWMPort.DataReceived += SWMPort_DataReceived;

        }


        /// <summary>
        /// Nhận dữ liệu từ Robot và xử lý
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Robot_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (Robot.BytesToRead > 500)
                {
                    Robot.DiscardInBuffer();
                    return;
                }
                string data = Robot.ReadTo("x");
                //Lora_Receive.Enqueue(data);

                this.Dispatcher.Invoke(() =>
                {

                    data = data.Trim();
                    RobotDataAnalys(data);
                });

            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString(), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nhận kq từ tool xử lý ảnh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AI_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (Robot.BytesToRead > 500)
                {
                    Robot.DiscardInBuffer();
                    return;
                }
                string data = Robot.ReadTo("x");
                //Lora_Receive.Enqueue(data);

                this.Dispatcher.Invoke(() =>
                {

                    data = data.Trim();
                    if (data.Contains("OK"))
                    {
                        detectResult = true;
                    }
                    else if (data.Contains("NG"))
                    {
                        detectResult = false;
                    }
                });

            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString(), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nhận dữ liệu AGV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Arduino_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (AGV.BytesToRead > 500)
                {
                    AGV.DiscardInBuffer();
                    return;
                }
                string data = AGV.ReadTo("x");
                //Lora_Receive.Enqueue(data);

                this.Dispatcher.Invoke(() =>
                {

                    data = data.Trim();
                    ArduinoDataAnalys(data);
                });

            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString(), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Lấy danh sách lịch sử lệnh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCommandHistory_Click(object sender, RoutedEventArgs e)
        {
            Grid_Map.Visibility = Visibility.Hidden;
            //Grid_Vision.Visibility = Visibility.Hidden;
            Grid_Robot.Visibility = Visibility.Hidden;
            Grid_CommandHistory.Visibility = Visibility.Visible;
            Grid_CurrentCommand.Visibility = Visibility.Hidden;
            LoadHistoryTransportCommand();
        }

        /// <summary>
        /// Lấy danh sách lệnh đang chạy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCrCommand_Click(object sender, RoutedEventArgs e)
        {
            Grid_Map.Visibility = Visibility.Hidden;
            //Grid_Vision.Visibility = Visibility.Hidden;
            Grid_CommandHistory.Visibility = Visibility.Hidden;
            Grid_CurrentCommand.Visibility = Visibility.Visible;
            Grid_Robot.Visibility = Visibility.Hidden;
            LoadCurentTransportCommand();
        }

        private void dtgCrCommand_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid gd = (DataGrid)sender;
            DataRowView row_selected = gd.SelectedItem as DataRowView;
            if (row_selected != null)
            {
                _deleteJobID = row_selected["CommandID"].ToString();
                _deleteJobCreateTime = DateTime.Parse(row_selected["JobCreat"].ToString());
                _jobState = row_selected["CommandStatus"].ToString();
            }
        }

        private void dtgCommandHistory_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid gd = (DataGrid)sender;
            DataRowView row_selected = gd.SelectedItem as DataRowView;
            if (row_selected != null)
            {
                _deleteJobID = row_selected["CommandID"].ToString();
                _deleteJobCreateTime = DateTime.Parse(row_selected["JobCreat"].ToString());
                _jobState = row_selected["CommandStatus"].ToString();
            }
        }

        /// <summary>
        /// Xóa lệnh đnag đc chọn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteCommand_Click(object sender, RoutedEventArgs e)
        {
            if (_jobState == "JOB CREATE")
            {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa lệnh vận chuyển này?", "Xóa lệnh", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    oBLTrans.DeleteJob(_deleteJobID, _deleteJobCreateTime);
                    LoadCurentTransportCommand();
                    LoadHistoryTransportCommand();
                }
            }
            else
                MessageBox.Show("Không thể xóa lệnh đang trong quá trình thực hiện!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);

        }

        /// <summary>
        /// Load danh sách lệnh đang chạy và chờ chạy
        /// </summary>
        private void LoadCurentTransportCommand()
        {
            DataTable dt = new DataTable();
            dt = oBLTrans.GetQueueCommand();
            dtgCrCommand.ItemsSource = dt.DefaultView;
        }

        /// <summary>
        /// Load danh sách lịch sử lệnh
        /// </summary>
        private void LoadHistoryTransportCommand()
        {
            DataTable dt = new DataTable();
            dt = oBLTrans.GetHistoryTransportCommand();
            dtgCommandHistory.ItemsSource = dt.DefaultView;
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            //Grid_Vision.Visibility = Visibility.Visible;
            Grid_Map.Visibility = Visibility.Hidden;
            Grid_CommandHistory.Visibility = Visibility.Hidden;
            Grid_CurrentCommand.Visibility = Visibility.Hidden;
            Grid_Robot.Visibility = Visibility.Hidden;
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnManualControl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wdManualCallAGV frm = new wdManualCallAGV();
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(_callDest) && !string.IsNullOrEmpty(_callAGVID))
                {
                    foreach (AGV _agv in lstAGV)
                    {
                        if (_agv.ID == _callAGVID)
                        {
                            if (string.IsNullOrEmpty(_agv.TransportCommand))
                            {
                                BLLayout.UpdateAGVDest(_callAGVID, _callDest);
                                BLControlAGV.UpdateAGVPath(_callAGVID, "");
                                SendOrderCommand(_callAGVID, "AO03" + _callDest);
                                _agv.Dest = _callDest;
                                OrderAGVByCommand(_callAGVID, _agv.NODE);

                            }
                            else
                            {
                                MessageBox.Show("AGV " + _callAGVID + " đang thực hiện lệnh vận chuyển. Không thể cưỡng chế di chuyển!");
                            }
                        }
                    }

                }
            }
            catch (Exception)
            {

            }

        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // BLTransportCommand.UpdateOutputState("FULL");
            }
            catch (Exception)
            {

            }

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void btnAGV_Click(object sender, RoutedEventArgs e)
        {

            //Grid_Vision.Visibility = Visibility.Hidden;
            Grid_Map.Visibility = Visibility.Visible;
            Grid_CommandHistory.Visibility = Visibility.Hidden;
            Grid_CurrentCommand.Visibility = Visibility.Hidden;
            Grid_Robot.Visibility = Visibility.Hidden;
        }

        private void btn_Pass_Click(object sender, RoutedEventArgs e)
        {
            //lblImageStatus.Content = "OK";
            //lblImageStatus.Foreground = System.Windows.Media.Brushes.Green;
            detectResult = true; // Đánh dấu là đã nhận dạng thành công

            PLC.SetDevice(bt1_RUN, 1);
        }

        private void btn_Fail_Click(object sender, RoutedEventArgs e)
        {
            //lblImageStatus.Content = "NG";
            //lblImageStatus.Foreground = System.Windows.Media.Brushes.Red;
            detectResult = false; // Đánh dấu là nhận dạng thất bại

            PLC.SetDevice(bt1_RUN, 1);
        }




        private void btn_Train_Click(object sender, RoutedEventArgs e)
        {
            ImageSource sourceImage = null;
            if (originalImage != null)
            {
                sourceImage = originalImage;
            }
            //else if (img_InputImage.Source != null)
            //{
            //    sourceImage = img_InputImage.Source;
            //}

            var trainWindow = new wdTrainModel(sourceImage);
            if (trainWindow.ShowDialog() == true)
            {
                // TODO: Xử lý kết quả huấn luyện
                MessageBox.Show("Huấn luyện thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btn_TakeImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_OpenImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Title = "Chọn ảnh"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();

                    // Lưu ảnh gốc trước khi xử lý
                    originalImage = bitmap;
                    //img_InputImage.Source = bitmap;

                    // Thực hiện nhận dạng
                    DetectComponents(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi mở ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void DetectComponents(string imagePath)
        {
            try
            {
                if (!Directory.Exists(templateFolder))
                {
                    MessageBox.Show("Chưa có dữ liệu template!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var templateFiles = Directory.GetFiles(templateFolder, "*.png");
                if (templateFiles.Length == 0)
                {
                    MessageBox.Show("Không có template nào!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                using (Image<Bgr, byte> img = new Image<Bgr, byte>(imagePath))
                {
                    var results = new List<object>();
                    var rectsToDraw = new List<(string label, System.Drawing.Rectangle rect)>();
                    foreach (var file in templateFiles)
                    {
                        string label = System.IO.Path.GetFileName(file).Split('_')[0];
                        using (Image<Bgr, byte> template = new Image<Bgr, byte>(file))
                        {
                            using (Image<Gray, float> result = img.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                            {
                                double minVal = 0, maxVal = 0;
                                System.Drawing.Point minLoc = new System.Drawing.Point(), maxLoc = new System.Drawing.Point();
                                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
                                if (maxVal > 0.8)
                                {
                                    // Vẽ rectangle lên ảnh gốc
                                    CvInvoke.Rectangle(
                                        img,
                                        new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y, template.Width, template.Height),
                                        new MCvScalar(0, 255, 0), 2);

                                    results.Add(new
                                    {
                                        ComponentType = label,
                                        Confidence = maxVal,
                                        Location = new System.Windows.Rect(maxLoc.X, maxLoc.Y, template.Width, template.Height)
                                    });
                                    rectsToDraw.Add((label, new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y, template.Width, template.Height)));
                                }
                            }
                        }
                    }
                    //img_InputImage.Source = ConvertToBitmapSource(img.ToBitmap());
                    //dtg_Result.ItemsSource = results;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhận dạng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    using (Bitmap img = (Bitmap)eventArgs.Frame.Clone())
                    {
                        IntPtr hBitmap = img.GetHbitmap();
                        try
                        {
                            System.Windows.Media.Imaging.BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions()
                            );
                            //img_Camera.Source = bitmapSource;
                        }
                        finally
                        {
                            DeleteObject(hBitmap);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xử lý frame camera: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void dtgVision_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void btnRobot_Click(object sender, RoutedEventArgs e)
        {

            Grid_Robot.Visibility = Visibility.Visible;
            Grid_Map.Visibility = Visibility.Hidden;
            //Grid_Vision.Visibility = Visibility.Hidden;
            Grid_CommandHistory.Visibility = Visibility.Hidden;
            Grid_CurrentCommand.Visibility = Visibility.Hidden;
        }


        private void btnAddCommand_Click(object sender, RoutedEventArgs e)
        {
            wdManualControl frm = new wdManualControl();
            frm.lstAGV = lstAGV;
            frm.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
