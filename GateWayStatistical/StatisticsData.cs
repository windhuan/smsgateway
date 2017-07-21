using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GateWayStatistics
{
    public class StatisticsData:INotifyPropertyChanged
    {
        [XmlIgnore]
        public string ConfigName { get; set; }

        public long SubmitTotalCount { get; set; }
        public long DeliverTotalCount { get; set; }
        public long ReportTotalCount { get; set; }

        [XmlIgnore]
        public long SubmitCurrentCount { get; set; }
        [XmlIgnore]
        public long DeliverCurrentCount { get; set; }
        [XmlIgnore]
        public long ReportCurrentCount { get; set; }
        [XmlIgnore]
        public long SubmitSpeed { get; set; }
        [XmlIgnore]
        public long DeliverSpeed { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyNotify()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("SubmitTotalCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("DeliverTotalCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("ReportTotalCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("SubmitCurrentCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("DeliverCurrentCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("ReportCurrentCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("SubmitSpeed"));
                PropertyChanged(this, new PropertyChangedEventArgs("DeliverSpeed"));
            }
        }
    }
}
