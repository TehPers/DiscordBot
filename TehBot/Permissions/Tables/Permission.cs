using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TehPers.Discord.TehBot.Database;

namespace TehPers.Discord.TehBot.Permissions.Tables {

    [Table("Permissions")]
    public class Permission {

        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Column("Name")]
        public string Name { get; set; }
        
        [Column("Role")]
        [FK(typeof(Role))]
        public int RoleID { get; set; }
    }
}
