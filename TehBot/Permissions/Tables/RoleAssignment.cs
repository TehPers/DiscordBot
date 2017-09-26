using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TehPers.Discord.TehBot.Database;

namespace TehPers.Discord.TehBot.Permissions.Tables {

    [Table("RoleAssignments")]
    public class RoleAssignment {

        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Column("UserID")]
        public long? UserID { get; set; }
        
        [Column("Role")]
        [FK(typeof(Role))]
        public int RoleID { get; set; }
    }
}
