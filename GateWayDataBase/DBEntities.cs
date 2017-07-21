using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace GateWayDataBase
{

    public class DBEntities : DbContext
    {
        public static DBEntities Factory(DBType pType, string pConnStr)
        {
            switch (pType)
            {
                case DBType.MSSQL:
                    return new MsSqlDBEntities(new System.Data.SqlClient.SqlConnection(pConnStr));
                case DBType.MYSQL:
                    return new MySqlDBEntities(new MySql.Data.MySqlClient.MySqlConnection(pConnStr));
            }
            return null;
        }

        [DbConfigurationType(typeof(DbConfiguration))]
        class MsSqlDBEntities : DBEntities
        {
            public MsSqlDBEntities(DbConnection conn)
                : base(conn)
            {

            }
        }

        [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
        class MySqlDBEntities : DBEntities
        {
            public MySqlDBEntities(DbConnection conn)
                : base(conn)
            {

            }
        }

        protected DBEntities(DbConnection conn)
            : base(conn, true)
        {

        }

        public DbSet<DeliverSMS> DeliverSMS { get; set; }
        public DbSet<SubmitSMS> SubmitSMS { get; set; }
    }

    public partial class DeliverSMS
    {
        [Key]
        public int ID { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string SrcTerminalID { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string DestTerminalID { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string MsgContent { get; set; }
        [MaxLength(20)]
        public string LinkID { get; set; }
        [MaxLength(10)]
        public string ServiceID { get; set; }
        public System.DateTime DeliverTime { get; set; }
    }

    public partial class SubmitSMS
    {
        [Key]
        public int ID { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string DestTerminalID { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string MsgContent { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string SrcTerminalID { get; set; }
        [MaxLength(10)]
        public string ExpandNo { get; set; }
        [MaxLength(10)]
        public string ServiceID { get; set; }
        [MaxLength(50)]
        public string MsgID { get; set; }
        public Nullable<System.DateTime> SendTime { get; set; }
        [Required]
        public int SendStatus { get; set; }
        [MaxLength(20)]
        public string ReportStatus { get; set; }
        public Nullable<System.DateTime> ReportTime { get; set; }
        [Required]
        public int Priority { get; set; }
        [Required]
        public int SubPriority { get; set; }
        public int? ForeignID { get; set; }
        [MaxLength(20)]
        public string ConfigName { get; set; }
    }
}