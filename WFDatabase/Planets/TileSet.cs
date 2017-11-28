using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WFDatabase.Items;

namespace WFDatabase.Planets {
    [Table("tileSets")]
    public class TileSet {
        [Key]
        [Column("id")]
        public int ID { get; set; }

        [Column("name")]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Column("resources")]
        [DisplayName("Resources")]
        public virtual IEnumerable<Resource> Resources { get; set; }
    }
}
