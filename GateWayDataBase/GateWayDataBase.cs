using SMSGateWayAddIns;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.Data.EntityClient;
using System.Data.Entity.Validation;
using SMSGateWayCore;
using System.ComponentModel;
using SMSGateWayCore.Message;
using EntityFramework.Utilities;
using GateWayDataBase.IO;

namespace GateWayDataBase
{
    public class GateWayDataBase : ControllerAddInBase
    {
        private Thread SendThread = null;
        private Thread UploadThread = null;

        [ImportProperty("连接字符串")]
        public string ConnectStr { get; set; }

        [ImportProperty("数据库类型")]
        public DBType ConnDBType { get; set; }

        [ImportProperty("查询数据间隔(毫秒)")]
        public int WaitTime { get; set; }

        [ImportProperty("上传数据间隔(毫秒)")]
        public int UploadTime { get; set; }

        [ImportProperty("每次查询数量")]
        public int SearchTopCount { get; set; }

        [ImportProperty("是否自动创建数据库")]
        public bool AutoCreate { get; set; }

        public override string TypeName
        {
            get { return "通用数据库"; }
        }

        public override string[] ProtocolRequired
        {
            get { return null; }
        }

        private static string ConfigCachePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\SMSGateWay\Cache\";

        private string _StatusUpdateCahcePath;
        private object _StatusUpdateCahceLock = new object();
        private FileAppender _StatusUpdateCahceAppender;

        private string _ReportUpdateCahcePath;
        private object _ReportUpdateCahceLock = new object();
        private FileAppender _ReportUpdateCahceAppender;

        private string _DeliverUpdateCahcePath;
        private object _DeliverUpdateCahceLock = new object();
        private FileAppender _DeliverUpdateCahceAppender;

        private bool isLoaded = false;
        private int _CurrentSendCount = 0;

        public GateWayDataBase()
        {
            SearchTopCount = 100;
            WaitTime = 1000;
            UploadTime = 5000;
            ConnDBType = global::GateWayDataBase.DBType.MSSQL;
            AutoCreate = false;
            ConnectStr = "";
            //Data Source=XX;Initial Catalog=XX;UID=XX;Password=XX
            //Server=45.63.55.79;Database=wshop;Uid=root;Pwd=272312297
        }

        public override void OnLoad()
        {
            isLoaded = true;
            _StatusUpdateCahcePath = ConfigCachePath + this.ConfigName + "_StatusCache";
            _StatusUpdateCahceAppender = new FileAppender(_StatusUpdateCahcePath + ".current");

            _ReportUpdateCahcePath = ConfigCachePath + this.ConfigName + "_ReportCache";
            _ReportUpdateCahceAppender = new FileAppender(_ReportUpdateCahcePath + ".current");

            _DeliverUpdateCahcePath = ConfigCachePath + this.ConfigName + "_DeliverCache";
            _DeliverUpdateCahceAppender = new FileAppender(_DeliverUpdateCahcePath + ".current");

            if (!Directory.Exists(ConfigCachePath))
            {
                Directory.CreateDirectory(ConfigCachePath);
            }

            _CurrentSendCount = 0;
            try
            {
                using (var _Entities = DBEntities.Factory(ConnDBType, ConnectStr))
                {
                    if (AutoCreate)
                    {
                        if (!_Entities.Database.Exists())
                        {
                            _Entities.Database.Create();
                        }
                    }
                    else
                    {
                        if (!_Entities.Database.Exists())
                        {
                            throw new Exception("没有找到数据库");
                        }
                    }

                    SendThread = new Thread(SendThreadRun);
                    SendThread.IsBackground = true;
                    SendThread.Start();

                    UploadThread = new Thread(UploadThreadRun);
                    UploadThread.IsBackground = true;
                    UploadThread.Start();
                }
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        public override void OnUnLoad()
        {
            isLoaded = false;
            if (SendThread != null)
            {
                while (SendThread.ThreadState != ThreadState.Stopped)
                {
                    Thread.Sleep(100);
                    continue;
                }
                SendThread = null;
            }

            if (UploadThread != null)
            {
                while (UploadThread.ThreadState != ThreadState.Stopped)
                {
                    Thread.Sleep(100);
                    continue;
                }
                UploadThread = null;
                UploadStatusData();
                UploadDeliverData();
                UploadReportData();
            }
        }

        private void SendThreadRun()
        {
            DateTime lastExecuteTime = DateTime.MinValue;
            while (isLoaded)
            {
                if ((DateTime.Now - lastExecuteTime).TotalMilliseconds > WaitTime)
                {
                    lastExecuteTime = DateTime.Now;
                    if (_CurrentSendCount < 0)
                    {
                        _CurrentSendCount = 0;
                    }
                    try
                    {
                        if (IsConnected && isLoaded)
                        {
                            if (_CurrentSendCount > (SearchTopCount * 2))
                            {
                                continue;
                            }

                            using (var _Entities = DBEntities.Factory(ConnDBType, ConnectStr))
                            {
                                DateTime now = DateTime.Now;
                                var sendingsms = _Entities.SubmitSMS.AsNoTracking().Where(p => p.SendStatus == -1 && (p.SendTime == null || now > p.SendTime) && (p.ConfigName == null || p.ConfigName == this.ConfigName)).OrderByDescending(p => p.Priority).ThenBy(p => p.SubPriority).Take(SearchTopCount).ToList();
                                //0 complete
                                //-1 nosend
                                //-2 waitsend
                                //-3 sending
                                //-4 timeout
                                if (sendingsms.Count > 0)
                                {
                                    sendingsms.ForEach(p => p.SendStatus = -2);
                                    EFBatchOperation.For(_Entities, _Entities.SubmitSMS).UpdateAll(sendingsms,
                                       p =>
                                       {
                                           p.ColumnsToUpdate(p1 => p1.SendStatus);
                                       });

                                    foreach (var data in sendingsms)
                                    {
                                        SubmitSMS syncObject = data;
                                        SMSSubmitMessage message = new SMSSubmitMessage();
                                        //消息接收号码
                                        message.DestTerminalID = syncObject.DestTerminalID;
                                        //资费代码
                                        message.FeeCode = 0;
                                        //被计费用户的号码
                                        message.FeeTerminalID = string.Empty;
                                        //资费类别
                                        message.FeeType = FeeTypes.Free;
                                        //计费用户类型字段
                                        message.FeeUserType = FeeUserTypes.Termini;
                                        //消息内容
                                        message.MsgContent = syncObject.MsgContent;
                                        //是否要求返回状态确认报告
                                        message.NeedReport = true;
                                        //业务标识
                                        message.ServiceID = syncObject.ServiceID;
                                        //消息发送号码
                                        message.SrcTerminalID = syncObject.SrcTerminalID;
                                        //修改短信默认编码
                                        message.Encoding = SMSEncoding.CODING_UCS2;
                                        if (syncObject.ExpandNo != null)
                                        {
                                            message.SrcTerminalID += syncObject.ExpandNo;
                                        }
                                        SubmitMessage(message, null, syncObject);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SendMessage(new ErrorMessage(ex));
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void UploadThreadRun()
        {
            DateTime lastExecuteTime = DateTime.MinValue;
            while (isLoaded)
            {
                if ((DateTime.Now - lastExecuteTime).TotalMilliseconds > UploadTime)
                {
                    lastExecuteTime = DateTime.Now;
                    UploadStatusData();
                    UploadDeliverData();
                    UploadReportData();
                }
                Thread.Sleep(100);
            }
        }

        private void UploadStatusData()
        {
            try
            {
                if (File.Exists(_StatusUpdateCahceAppender.FileName))
                {
                    lock (_StatusUpdateCahceLock)
                    {
                        _StatusUpdateCahceAppender.Dispose();
                        File.Move(_StatusUpdateCahceAppender.FileName, _StatusUpdateCahcePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".upload");
                    }
                }

                DirectoryInfo dir = new DirectoryInfo(ConfigCachePath);
                foreach (var file in dir.GetFiles(this.ConfigName + "*.upload").OrderBy(p => p.CreationTime))
                {
                    if (!file.FullName.StartsWith(_StatusUpdateCahcePath))
                    {
                        continue;
                    }
                    List<SubmitSMS> updateDatas = ReadCacheFromFile<SubmitSMS>(file.FullName);
                    Dictionary<int, SubmitSMS> updateDataDic = new Dictionary<int, SubmitSMS>();
                    foreach (var data in updateDatas)
                    {
                        updateDataDic[data.ID] = data;
                    }

                    using (var _Entities = DBEntities.Factory(ConnDBType, ConnectStr))
                    {
                        EFBatchOperation.For(_Entities, _Entities.SubmitSMS).UpdateAll(updateDataDic.Values.ToArray(),
                            p =>
                            {
                                p.ColumnsToUpdate(
                                    p1 => p1.MsgID,
                                    p1 => p1.SendStatus,
                                    p1 => p1.SendTime);
                            });
                    }
                    File.Delete(file.FullName);
                }
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        private void UploadReportData()
        {
            try
            {
                if (File.Exists(_ReportUpdateCahceAppender.FileName))
                {
                    lock (_ReportUpdateCahceLock)
                    {
                        _ReportUpdateCahceAppender.Dispose();
                        File.Move(_ReportUpdateCahceAppender.FileName, _ReportUpdateCahcePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".upload");
                    }
                }

                DirectoryInfo dir = new DirectoryInfo(ConfigCachePath);
                foreach (var file in dir.GetFiles("*.upload").OrderBy(p => p.CreationTime))
                {
                    if (!file.FullName.StartsWith(_ReportUpdateCahcePath))
                    {
                        continue;
                    }
                    List<SubmitSMS> updateDatas = ReadCacheFromFile<SubmitSMS>(file.FullName);
                    List<SubmitSMS> updateDatas2 = new List<SubmitSMS>();
                    foreach (var item in updateDatas)
                    {
                        if (updateDatas2.FirstOrDefault(p => p.MsgID == item.MsgID) == null)
                        {
                            updateDatas2.Add(item);
                        }
                    }
                    using (var _Entities = DBEntities.Factory(ConnDBType, ConnectStr))
                    {
                        EFBatchOperation.For(_Entities, _Entities.SubmitSMS).UpdateAll(updateDatas2.ToArray(),
                            p =>
                            {
                                p.ColumnsToKey(p1 => p1.MsgID);
                                p.ColumnsToUpdate(p1 => p1.ReportStatus, p1 => p1.ReportTime);
                            });
                    }
                    File.Delete(file.FullName);
                }
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        private void UploadDeliverData()
        {
            try
            {
                if (File.Exists(_DeliverUpdateCahceAppender.FileName))
                {
                    lock (_DeliverUpdateCahceLock)
                    {
                        _DeliverUpdateCahceAppender.Dispose();
                        File.Move(_DeliverUpdateCahceAppender.FileName, _DeliverUpdateCahcePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".upload");
                    }
                }

                DirectoryInfo dir = new DirectoryInfo(ConfigCachePath);
                foreach (var file in dir.GetFiles(this.ConfigName + "*.upload").OrderBy(p => p.CreationTime))
                {
                    if (!file.FullName.StartsWith(_DeliverUpdateCahcePath))
                    {
                        continue;
                    }
                    //避免用户回复乱码,导致数据库插入失败后,全部回复信息无法写入数据库
                    try
                    {
                        List<DeliverSMS> updateDatas = ReadCacheFromFile<DeliverSMS>(file.FullName);
                        using (var _Entities = DBEntities.Factory(ConnDBType, ConnectStr))
                        {
                            EFBatchOperation.For(_Entities, _Entities.DeliverSMS).InsertAll(updateDatas.ToArray());
                        }
                        File.Delete(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        SendMessage(new ErrorMessage(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        protected override void DeliverMessage(SMSDeliverMessage pMessage, CancelEventArgs args)
        {
            try
            {
                RecordDeliverUpdateCache(new DeliverSMS()
                {
                    DeliverTime = DateTime.Now,
                    DestTerminalID = pMessage.DestTerminalID,
                    LinkID = pMessage.LinkID,
                    MsgContent = pMessage.MsgContent,
                    ServiceID = pMessage.ServiceID,
                    SrcTerminalID = pMessage.SrcTerminalID
                });
                pMessage.AssignProcessResult(true);
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        protected override void ReprotMessage(SMSReportMessage pMessage, CancelEventArgs args)
        {
            try
            {
                RecordReportUpdateCache(new SubmitSMS()
                {
                    MsgID = pMessage.MsgID.ToString(),
                    ReportStatus = pMessage.Status,
                    ReportTime = DateTime.Now
                });
                pMessage.AssignProcessResult(true);
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        protected override void StatusMessage(SMSStatusMessage pMessage, CancelEventArgs args)
        {
            try
            {
                if (pMessage.PkNumber == 1 && pMessage.aSyncState is SubmitSMS)
                {
                    var submitsms = pMessage.aSyncState as SubmitSMS;

                    if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.WaitSend)
                    {
                        Interlocked.Increment(ref _CurrentSendCount);
                        //if (submitsms.SendStatus == -1)
                        //{
                        //    submitsms.SendStatus = -2;
                        //    RecordStatusUpdateCache(submitsms);
                        //}
                    }

                    if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.Sending)
                    {
                        if (submitsms.SendStatus == -2)
                        {
                            submitsms.SendStatus = -3;
                            submitsms.SendTime = DateTime.Now;
                            RecordStatusUpdateCache(submitsms);
                        }
                    }

                    if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.Timeout)
                    {
                        submitsms.SendStatus = -1;
                        submitsms.SendTime = null;
                        RecordStatusUpdateCache(submitsms);
                    }

                    if (pMessage.Status == SMSGateWayCore.SubmitMessageStatus.Complete)
                    {
                        Interlocked.Decrement(ref _CurrentSendCount);
                        submitsms.MsgID = pMessage.MsgID.ToString();
                        submitsms.SendStatus = (int)pMessage.RespStatus;
                        submitsms.SendTime = DateTime.Now;
                        RecordStatusUpdateCache(submitsms);
                    }
                }
            }
            catch (Exception ex)
            {
                SendMessage(new ErrorMessage(ex));
            }
        }

        private void RecordStatusUpdateCache(SubmitSMS pSubmitSMS)
        {
            string tempstr = Newtonsoft.Json.JsonConvert.SerializeObject(pSubmitSMS) + Environment.NewLine;
            lock (_StatusUpdateCahceLock)
            {
                _StatusUpdateCahceAppender.WriteAllText(tempstr, true);
            }
        }

        private void RecordReportUpdateCache(SubmitSMS pSubmitSMS)
        {
            string tempstr = Newtonsoft.Json.JsonConvert.SerializeObject(pSubmitSMS) + Environment.NewLine;
            lock (_ReportUpdateCahceLock)
            {
                _ReportUpdateCahceAppender.WriteAllText(tempstr, true);
            }
        }

        private void RecordDeliverUpdateCache(DeliverSMS pDeliverSMS)
        {
            string tempstr = Newtonsoft.Json.JsonConvert.SerializeObject(pDeliverSMS) + Environment.NewLine;
            lock (_DeliverUpdateCahceLock)
            {
                _DeliverUpdateCahceAppender.WriteAllText(tempstr, true);
            }
        }

        private List<T> ReadCacheFromFile<T>(string filename)
        {
            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
            List<T> datas = new List<T>();
            foreach (var line in lines)
            {
                try
                {
                    if (line.Trim() != string.Empty)
                    {
                        datas.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(line));
                    }
                }
                catch (Exception ex)
                {
                    SendMessage(new ErrorMessage(ex));
                }
            }
            return datas;
        }
    }
}