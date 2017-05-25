using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Discord.TehBot.Commands {
    public class CommandDocs {
        public string Description { get; set; }
        public List<Argument> Arguments { get; set; } = new List<Argument>();

        public class Argument {
            public bool Optional { get; }
            public string Name { get; }
            public string Description { get; }

            public Argument(string name, string description, bool optional = false) {
                Name = name;
                Description = description;
                Optional = optional;
            }
        }
    }
}
