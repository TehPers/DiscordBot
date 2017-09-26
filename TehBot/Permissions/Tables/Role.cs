using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TehPers.Discord.TehBot.Database;

namespace TehPers.Discord.TehBot.Permissions.Tables {

    [Table("Roles")]
    public class Role {

        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Column("Name")]
        public string Name { get; set; }
        
        [Column("Guild")]
        public long? GuildID { get; set; }

        [Column("Parent")]
        [FK(typeof(Role))]
        public int? ParentID { get; set; }
    }
}
