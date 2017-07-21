using SMSGateWayCore.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GateWayTest
{
    /// <summary>
    /// Interaction logic for MainTestPage.xaml
    /// </summary>
    public partial class MainTestPage : UserControl
    {

        public GateWayTest GateWayTest { get; set; }
        ScrollViewer _ScrollViewer = null;
        public MainTestPage()
        {
            InitializeComponent();
            this.Loaded += MainTestPage_Loaded;
        }

        void MainTestPage_Loaded(object sender, RoutedEventArgs e)
        {
            _ScrollViewer = FindScrollViewer(scrollviewer);
        }

        public void ReprotMessage(SMSReportMessage pMessage)
        {
            WriteLog(pMessage.ToString());
        }

        public void DeliverMessage(SMSGateWayCore.Message.SMSDeliverMessage pMessage)
        {
            WriteLog(pMessage.ToString());
        }

        public void SubmitMessageStatus(SMSGateWayCore.Message.SMSStatusMessage pMessage)
        {
            if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.Complete)
            {
                WriteLog(pMessage.ToString());
            }
        }

        private void WriteLog(string log)
        {
            Action action = () =>
            {
                string logmsg = string.Format("{0}\n{1}\n\n", DateTime.Now, log);

                new Run(logmsg, OutputParagraph.ContentEnd)
                {
                    Foreground = Brushes.Green
                };
                if (_ScrollViewer != null)
                {
                    _ScrollViewer.ScrollToEnd();
                }
            };

            if (this.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    action();
                });

            }
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            if (GateWayTest.IsConnected)
            {
                GateWayTest.Send(this.DataContext);
            }
            else
            {
                MessageBox.Show("网关没有连接成功!");
            }
        }

        public static ScrollViewer FindScrollViewer(FlowDocumentScrollViewer flowDocumentScrollViewer)
        {
            if (VisualTreeHelper.GetChildrenCount(flowDocumentScrollViewer) == 0)
            {
                return null;
            }

            DependencyObject firstChild = VisualTreeHelper.GetChild(flowDocumentScrollViewer, 0);
            if (firstChild == null)
            {
                return null;
            }

            Decorator border = VisualTreeHelper.GetChild(firstChild, 0) as Decorator;

            if (border == null)
            {
                return null;
            }

            return border.Child as ScrollViewer;
        }
    }
}