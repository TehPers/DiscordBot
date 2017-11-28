using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WFDatabase.Enums;
using WFDatabase.Items;

namespace WFDatabase.Planets {
    [Table("planetNodes")]
    public class PlanetNode {
        [Key]
        [Column("id")]
        public int ID { get; set; }

        [Column("planet")]
        [DisplayName("Planet")]
        public Planet Planet { get; set; }

        [Column("name")]
        [DisplayName("Name")]
        public string Name { get; set; }
        
        [Column("missionType")]
        [DisplayName("Mission Type")]
        public MissionType MissionType { get; set; }

        [Column("minLevel")]
        [DisplayName("Min Level")]
        public int? MinLevel { get; set; }

        [Column("maxLevel")]
        [DisplayName("Max Level")]
        public int? MaxLevel { get; set; }

        [Column("faction")]
        [DisplayName("Faction")]
        public Faction Faction { get; set; }

        // TODO: Support open-world, relay, sharkwing, full archwing, normal missions
        [Column("archwing")]
        [DisplayName("Archwing")]
        public bool Archwing { get; set; }

        [Column("relay")]
        [DisplayName("Relay")]
        public bool Relay { get; set; }

        [Column("resources")]
        [DisplayName("Resources")]
        public IEnumerable<Resource> Resources { get; set; }

        public override string ToString() => $"{this.Name} ({this.Planet.Name})";
    }
}
