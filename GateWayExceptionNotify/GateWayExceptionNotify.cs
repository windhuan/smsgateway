using SMSGateWayAddIns;
using SMSGateWayCore.Message;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Timers;

namespace GateWayExceptionNotify
{
    internal class GateWayExceptionNotify : SMSGateWayAddIns.ControllerAddInBase
    {
        private Thread SendThread = null;
        private SmtpClient smtpClient;

        [ImportProperty("SMTP服务器")]
        public string Server { get; set; }

        [ImportProperty("端口号")]
        public int Port { get; set; }

        [ImportProperty("SSL")]
        public bool SSL { get; set; }

        [ImportProperty("发送账号")]
        public string User { get; set; }

        [ImportProperty("发送密码")]
        public string Password { get; set; }

        [ImportProperty("通知地址")]
        public string Email { get; set; }

        [ImportProperty("通知间隔(秒)")]
        public int Interval { get; set; }

        private object errorslock = new object();
        private List<SMSGateWayCore.Message.ErrorMessage> Errors = new List<SMSGateWayCore.Message.ErrorMessage>();

        public GateWayExceptionNotify()
        {
            Interval = 60;
            Server = "smtp.qq.com";
            Port = 587;
            SSL = true;
        }

        public override string[] ProtocolRequired
        {
            get { return null; }
        }

        public override void OnLoad()
        {
            if (Interval <= 0)
            {
                Interval = 1;
            }

            lock (errorslock)
            {
                Errors.Clear();
            }

            SendThread = new Thread(SendThreadRun);
            SendThread.IsBackground = true;
            SendThread.Start();
        }

        private void SendThreadRun()
        {
            DateTime lastExecuteTime = DateTime.MinValue;
            var waitTime = Interval * 1000;
            while (true)
            {
                if ((DateTime.Now - lastExecuteTime).TotalMilliseconds > waitTime)
                {
                    lastExecuteTime = DateTime.Now;
                    try
                    {
                        string errorlog = string.Empty;
                        lock (errorslock)
                        {
                            if (Errors.Count == 0)
                            {
                                return;
                            }
                            foreach (var log in Errors)
                            {
                                errorlog += log.ErrorTime.ToString() + Environment.NewLine + log.Exception.ToString() + Environment.NewLine;
                            }
                            Errors.Clear();
                        }
                        smtpClient = new SmtpClient(Server, Port);
                        smtpClient.EnableSsl = SSL;
                        smtpClient.Timeout = 60000;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(User, Password);

                        MailMessage mm = new MailMessage();
                        mm.From = new MailAddress(User);
                        var emails = Email.Split(',');
                        foreach (var to in emails)
                        {
                            mm.To.Add(new MailAddress(to));
                        }

                        mm.Subject = base.ConfigName + " 网关错误报告 报告时间：" + DateTime.Now.ToString();
                        mm.BodyEncoding = UTF8Encoding.UTF8;
                        mm.Body = errorlog;
                        smtpClient.Send(mm);
                    }
                    catch (Exception ex)
                    {
                        SendMessage(new ErrorMessage(ex));
                    }
                }
                Thread.Sleep(100);
            }
        }

        public override void OnUnLoad()
        {
            if (SendThread != null)
            {
                SendThread.Abort();
                SendThread = null;
            }
        }

        public override void ErrorMessage(SMSGateWayCore.Message.ErrorMessage pMessage, CancelEventArgs args)
        {
            lock (errorslock)
            {
                Errors.Add(pMessage);
            }
        }

        public override string TypeName
        {
            get { return "网关异常通知插件"; }
        }
    }
}