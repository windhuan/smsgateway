using SMSGateWayCore.Message;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Xml.Serialization;

namespace GateWayStatistics
{
    class GateWayStatistics : SMSGateWayAddIns.ControllerAddInBase
    {
        static string ConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\SMSGateWay\";
        private StatisticsData SData;
        private StatisticsWindow _StatisticsWindow;
        private Window _MainWindow;
        private Timer _SpeedTimer;
        bool _AllowClose = false;

        int BeSubmitCount = 0;
        int BeDeliverCount = 0;
        int SubmitCount = 0;
        int DeliverCount = 0;
        public override string[] ProtocolRequired
        {
            get { return null; }
        }

        public override void OnLoad()
        {
            LoadData();
            _AllowClose = false;
            SData.ConfigName = this.ConfigName;
            if (_SpeedTimer == null)
            {
                _SpeedTimer = new Timer(1000);
                _SpeedTimer.Elapsed += _SpeedTimer_Elapsed;
            }

            BeSubmitCount = 0;
            BeDeliverCount = 0;
            SubmitCount = 0;
            DeliverCount = 0;

            _SpeedTimer.Enabled = true;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _StatisticsWindow = new StatisticsWindow();
                _StatisticsWindow.DataContext = SData;
                _StatisticsWindow.btInit.Click += btInit_Click;
                _MainWindow = new Window();
                _MainWindow.Title = this.ConfigName + "统计";
                _MainWindow.Width = 230;
                _MainWindow.Height = 225;
                _MainWindow.WindowStyle = WindowStyle.None;
                _MainWindow.Content = _StatisticsWindow;
                _MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _MainWindow.MouseLeftButtonDown += _MainWindow_MouseLeftButtonDown;
                _MainWindow.ResizeMode = ResizeMode.NoResize;
                _MainWindow.Closing += _MainWindow_Closing;
                _MainWindow.Show();

            });
        }

        void _SpeedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SData.SubmitSpeed = SubmitCount - BeSubmitCount;
            SData.DeliverSpeed = DeliverCount - BeDeliverCount;
            BeSubmitCount = SubmitCount;
            BeDeliverCount = DeliverCount;
            SData.RaisePropertyNotify();
        }

        void _MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_AllowClose;
        }

        void btInit_Click(object sender, RoutedEventArgs e)
        {
            SData.DeliverCurrentCount = 0;
            SData.DeliverTotalCount = 0;
            SData.ReportCurrentCount = 0;
            SData.ReportTotalCount = 0;
            SData.SubmitTotalCount = 0;
            SData.SubmitCurrentCount = 0;
            SData.RaisePropertyNotify();
            SaveData();
        }
        void _MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _MainWindow.DragMove();
        }

        public override void OnUnLoad()
        {
            SaveData();
            _AllowClose = true;
            if (_SpeedTimer != null)
            {
                _SpeedTimer.Enabled = false;
            }
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _MainWindow.Close();
            });
        }

        protected override void DeliverMessage(SMSGateWayCore.Message.SMSDeliverMessage pMessage, CancelEventArgs args)
        {
            SData.DeliverCurrentCount++;
            SData.DeliverTotalCount++;
            DeliverCount++;
            SData.RaisePropertyNotify();
        }

        protected override void ReprotMessage(SMSGateWayCore.Message.SMSReportMessage pMessage, CancelEventArgs args)
        {
            SData.ReportCurrentCount++;
            SData.ReportTotalCount++;
            DeliverCount++;
            SData.RaisePropertyNotify();
        }
        protected override void StatusMessage(SMSStatusMessage pMessage, CancelEventArgs args)
        {
            if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.Complete)
            {
                SData.SubmitCurrentCount++;
                SData.SubmitTotalCount++;
                SubmitCount++;
                SData.RaisePropertyNotify();
            }
        }

        public override string TypeName
        {
            get { return "网关统计插件"; }
        }

        private void SaveData()
        {
            if (!Directory.Exists(ConfigPath + @"\Statistics\"))
            {
                Directory.CreateDirectory(ConfigPath + @"\Statistics\");
            }
            using (FileStream fs = File.Create(ConfigPath + @"\Statistics\" + this.ConfigName + "统计.xml"))
            {
                XmlSerializer xmlser = new XmlSerializer(typeof(StatisticsData));
                xmlser.Serialize(fs, SData);
            }
        }

        private void LoadData()
        {
            if (File.Exists(ConfigPath + @"\Statistics\" + this.ConfigName + "统计.xml"))
            {
                using (FileStream fs = File.OpenRead(ConfigPath + @"\Statistics\" + this.ConfigName + "统计.xml"))
                {
                    XmlSerializer xmlser = new XmlSerializer(typeof(StatisticsData));
                    SData = xmlser.Deserialize(fs) as StatisticsData;
                }
            }
            else
            {
                SData = new StatisticsData();
            }
        }
    }
}
