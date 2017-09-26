using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Discord.TehBot.Database {

    [AttributeUsage(AttributeTargets.Property)]
    public class FKAttribute : Attribute {

        public Type ForeignModel { get; set; }

        public string KeyName { get; set; }

        public FKAttribute(Type foreignModel) {
            this.ForeignModel = foreignModel;
            this.KeyName = (from p in foreignModel.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            where p.GetCustomAttributes<KeyAttribute>().Any()
                            select p.GetCustomAttributes<ColumnAttribute>().FirstOrDefault()?.Name ?? p.Name).FirstOrDefault();

            if (this.KeyName == null)
                throw new ArgumentException("Model must contain a primary key containing a KeyAttribute", nameof(foreignModel));
        }

        public FKAttribute(Type foreignModel, string keyName) {
            this.ForeignModel = foreignModel;
            this.KeyName = keyName;
        }
    }
}
