using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GateWayDataBase.IO
{
    internal sealed class IOLock
    {
        private System.Threading.ReaderWriterLock m_lock;

        public IOLock()
        {
            m_lock = new System.Threading.ReaderWriterLock();
        }

        public void AcquireReaderLock()
        {
            m_lock.AcquireReaderLock(-1);

            //System.Threading.Monitor.Enter(this);
        }

        public void ReleaseReaderLock()
        {
            m_lock.ReleaseReaderLock();

            //System.Threading.Monitor.Exit(this);
        }

        public void AcquireWriterLock()
        {
            m_lock.AcquireWriterLock(-1);

            //System.Threading.Monitor.Enter(this);
        }

        public void ReleaseWriterLock()
        {
            m_lock.ReleaseWriterLock();

            //System.Threading.Monitor.Exit(this);
        }

    }
}
