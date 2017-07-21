using SMSGateWayAddIns;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Timers;

namespace GateWayExceptionNotify
{
    class GateWayExceptionNotify : SMSGateWayAddIns.ControllerAddInBase
    {
        Timer SendTimer = null;
        SmtpClient smtpClient;

        [ImportProperty("邮件地址")]
        public string Email { get; set; }

        [ImportProperty("通知间隔(分)")]
        public int Interval { get; set; }

        object errorslock = new object();
        List<SMSGateWayCore.Message.ErrorMessage> Errors = new List<SMSGateWayCore.Message.ErrorMessage>();


        public GateWayExceptionNotify()
        {
            Interval = 10;
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
            SendTimer = new Timer();
            SendTimer.Interval = Interval * 60 * 1000;
            SendTimer.Elapsed += SendTimer_Elapsed;
            SendTimer.Start();
        }

        void SendTimer_Elapsed(object sender, ElapsedEventArgs e)
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
            smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.EnableSsl = true;
            smtpClient.Timeout = 20000;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential("smsgatewayreport@gmail.com", "User@123");

            MailMessage mm = new MailMessage();
            mm.From = new MailAddress("smsgatewayreport@gmail.com");
            var emails = Email.Split(',');
            foreach (var to in emails)
            {
                mm.To.Add(new MailAddress(to));
            }

            mm.Subject = base.ConfigName + " 网关错误报告 报告时间：" + DateTime.Now.ToString();
            mm.BodyEncoding = UTF8Encoding.UTF8;
            mm.Body = errorlog;
            smtpClient.SendAsync(mm, null);
        }

        public override void OnUnLoad()
        {
            if (SendTimer != null)
            {
                SendTimer.Stop();
                SendTimer = null;
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
