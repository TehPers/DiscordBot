using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using WFDatabase.Enums;

namespace WFDatabase.Items {
    [Table("resources")]
    public class Resource : Item {
        [Column("rarity")]
        [DisplayName("Rarity")]
        public ResourceRarity? Rarity { get; set; }
    }
}
