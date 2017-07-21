using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GateWayDataBase.IO
{
    internal class FileAppender : IDisposable
    {
        private readonly IOLock io_lock = new IOLock();

        private string f_name = string.Empty;//文件全路径
        private Encoding f_encode = Encoding.UTF8;//文件编码

        public string FileName
        {
            get
            {
                return f_name;
            }
        }

        private FileStream f_stream = null;
        private StreamWriter writer = null;

        private bool isAppend = false;//是否为追加模式
        private readonly int reTimes = 5;//尝试读取次数

        //为避免在写文件的过程将writer释放
        private readonly object obj_lock = new object();

        public FileAppender(string filename)
        {
            this.f_name = filename.Replace("/", "\\");
        }
        public FileAppender(string filename, Encoding encode)
            : this(filename)
        {
            this.f_encode = encode;
        }

        public void WriteAllText(string content, bool append)
        {
            if (f_stream == null || isAppend != append)
            {
                isAppend = append;

                Reset();
            }
            io_lock.AcquireWriterLock();
            lock (obj_lock)
            {
                try
                {
                    writer.Write(content);
                    writer.Flush();
                }
                finally
                {
                    io_lock.ReleaseWriterLock();
                }
            }
        }
        public void Dispose()
        {
            Close();
        }

        private void Reset()
        {
            Close();
            OpenFile(isAppend);
        }
        private void Close()
        {
            lock (obj_lock)
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                    writer = null;
                }
                if (f_stream != null)
                {
                    f_stream.Close();
                    f_stream.Dispose();
                    f_stream = null;
                }
         
            }
        }

        private void OpenFile(bool append)
        {
            Exception ex = null;
            for (int i = 0; i < reTimes; i++)
            {
                try
                {
                    f_stream = new FileStream(f_name,
                        (append ? FileMode.Append : FileMode.Create),
                        (append ? FileAccess.Write : FileAccess.ReadWrite),
                        FileShare.Read);
                    break;
                }
                catch (Exception e) { ex = e; Thread.Sleep(500); }
            }
            if (f_stream == null)
                throw ex;
            else
                writer = new StreamWriter(f_stream, f_encode);
        }

    }
}
