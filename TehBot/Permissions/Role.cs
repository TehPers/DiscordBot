namespace TehPers.Discord.TehBot.Permissions {

    public class Role {

        public string Name { get; set; }

        public string Parent { get; set; }

        public Role() { }

        public Role(string name, string parent) {
            Name = name;
            Parent = parent;
        }

        public Role(string name) : this(name, (string) null) { }

        public Role(string name, Role parent) : this(name, parent?.Name) { }

        public Role SetParent(Role parent) {
            Parent = parent.Name;
            return this;
        }
    }
}
