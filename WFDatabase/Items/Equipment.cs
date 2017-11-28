using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WFDatabase.Items {
    public abstract class Equipment : Item {
        [Column("mastery")]
        [DisplayName("Mastery")]
        [Range(0, 30)]
        public int Mastery { get; set; }

        [Column("prime")]
        [DisplayName("Prime")]
        public bool Prime { get; set; }

        [Column("ducats")]
        [DisplayName("Ducats")]
        public int? Ducats { get; set; }
    }
}
