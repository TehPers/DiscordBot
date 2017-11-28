using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WFDatabase.Items {
    public abstract class Item {
        [Key]
        [Column("id")]
        public int ID { get; set; }

        [Column("creditValue")]
        [DisplayName("Credit Value")]
        public int? CreditValue { get; set; }
        
        [Column("name")]
        [DisplayName("Name")]
        public string Name { get; set; }

        public override string ToString() => this.Name;
    }
}
