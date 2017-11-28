using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using WFDatabase.Enums;

namespace WFDatabase.Items {
    [Table("mods")]
    public class Mod : Item {
        [Column("rarity")]
        [DisplayName("Rarity")]
        public ModRarity Rarity { get; set; }

        [Column("ranks")]
        [DisplayName("Ranks")]
        public int Ranks { get; set; }

        [Column("damaged")]
        [DisplayName("Damaged")]
        public bool Damaged { get; set; }
    }
}
