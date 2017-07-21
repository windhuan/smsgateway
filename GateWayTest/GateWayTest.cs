using SMSGateWayCore.Message;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace GateWayTest
{
    public class GateWayTest : SMSGateWayAddIns.ControllerAddInBase
    {
        public new bool IsConnected { get; private set; }
        public override string[] ProtocolRequired
        {
            get { return null; }
        }
        MainTestPage _TestPage;
        Window _MainWindow;
        bool _AllowClose = false;
        public override void OnLoad()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _AllowClose = false;

                _TestPage = new MainTestPage();
                _TestPage.GateWayTest = this;
                _TestPage.DataContext = new SMSGateWayCore.Message.SMSSubmitMessage();


                _MainWindow = new Window();
                _MainWindow.Title = this.ConfigName + "测试";
                _MainWindow.Width = 800;
                _MainWindow.Height = 370;
                _MainWindow.WindowStyle = WindowStyle.None;
                _MainWindow.Content = _TestPage;
                _MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _MainWindow.MouseLeftButtonDown += _MainWindow_MouseLeftButtonDown;
                _MainWindow.Closing += _MainWindow_Closing;
                _MainWindow.ResizeMode = ResizeMode.NoResize;
                _MainWindow.Show();

            });
        }

        void _MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_AllowClose;
        }

        void _MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _MainWindow.DragMove();
        }

        public override void OnUnLoad()
        {
            _AllowClose = true;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _MainWindow.Close();
            });
        }

        public override void OnConnect()
        {
            IsConnected = true;
        }

        public override void OnDisConnect()
        {
            IsConnected = false;
        }

        public override string TypeName
        {
            get { return "测试插件"; }
        }

        protected override void ReprotMessage(SMSReportMessage pMessage, CancelEventArgs args)
        {
            _TestPage.ReprotMessage(pMessage);
        }

        protected override void DeliverMessage(SMSGateWayCore.Message.SMSDeliverMessage pMessage, CancelEventArgs args)
        {
            _TestPage.DeliverMessage(pMessage);
        }

        protected override void StatusMessage(SMSGateWayCore.Message.SMSStatusMessage pMessage, CancelEventArgs args)
        {
            _TestPage.SubmitMessageStatus(pMessage);
        }

        public void Send(object obj)
        {
            SubmitMessage(obj as SMSSubmitMessage, null, "MainTestPage");
        }
    }
}