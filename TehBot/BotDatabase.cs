using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using SQLite.CodeFirst;
using TehPers.Discord.TehBot.Database;
using TehPers.Discord.TehBot.Permissions;
using TehPers.Discord.TehBot.Permissions.Tables;

namespace TehPers.Discord.TehBot {

    public class BotDatabase : DbContext {
        public BotDatabase(DbConnection connection) : base(connection, true) { }

        #region Tables
        public DbSet<Permission> Permissions { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<RoleAssignment> RoleAssignments { get; set; }
        #endregion

        public void CreateSchema() {
            Type contextType = this.GetType();
            IEnumerable<Type> models = (from p in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        where p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                                        select p.PropertyType.GenericTypeArguments[0]).Distinct();

            var commands = (from m in models
                            let cmd = this.CreateTableFromModel(m)
                            select new { Model = m, Command = cmd.command, Dependencies = cmd.dependencies }).ToHashSet();

            HashSet<Type> createdModels = new HashSet<Type>();
            while (commands.Any()) {
                var command = commands.FirstOrDefault(cmd => !cmd.Dependencies.Except(createdModels).Any());
                if (command == null)
                    throw new Exception("Table could not be created because foreign key dependencies were not created");

                command.Command.ExecuteNonQuery();
                createdModels.Add(command.Model);
                commands.Remove(command);
            }
        }

        public (SQLiteCommand command, List<Type> dependencies) CreateTableFromModel<TModel>() => this.CreateTableFromModel(typeof(TModel));
        public (SQLiteCommand command, List<Type> dependencies) CreateTableFromModel<TModel>(TModel model) => this.CreateTableFromModel(typeof(TModel));
        public (SQLiteCommand command, List<Type> dependencies) CreateTableFromModel(Type model) {
            string tableName = this.GetTableNameFromModel(model);

            var columns = (from p in model.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           where p.CanRead && p.CanWrite
                           let name = p.GetCustomAttributes<ColumnAttribute>().FirstOrDefault()?.Name ?? p.Name
                           let pk = p.GetCustomAttributes<KeyAttribute>().Any()
                           let fk = p.GetCustomAttributes<FKAttribute>().FirstOrDefault()
                           select new {
                               Name = name,
                               PrimaryKey = pk,
                               ForeignKey = fk,
                               Type = this.GetSqlType(p.PropertyType)
                           }).ToList();

            IEnumerable<string> columnDefinitions = from c in columns
                                                        //let keyClause = c.PrimaryKey ? "PRIMARY KEY" : (c.ForeignKey == null ? "" : $"REFERENCES {GetTableNameFromModel(c.ForeignKey.ForeignModel)}({c.ForeignKey.KeyName})")
                                                    let keyClause = c.PrimaryKey ? "PRIMARY KEY" : ""
                                                    let extra = c.Type == "INT" ? "COLLATE NOCASE" : ""
                                                    select $"{c.Name} {c.Type} {keyClause} {extra}";

            string sql = $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitions)});";

            return (new SQLiteCommand(sql, this.Database.Connection as SQLiteConnection), columns.Where(c => c.ForeignKey != null && c.ForeignKey.ForeignModel != model).Select(c => c.ForeignKey.ForeignModel).Distinct().ToList());
        }

        public string GetSqlType(Type column) {
            column = Nullable.GetUnderlyingType(column) ?? column;

            MethodInfo typeToAffinity = typeof(SQLiteConvert).GetMethod("TypeToAffinity", BindingFlags.NonPublic | BindingFlags.Static);
            object result = typeToAffinity.Invoke(null, new object[] { column });
            if (!(result is TypeAffinity affinity))
                return null;

            switch (affinity) {
                case TypeAffinity.Blob:
                    return "BLOB";
                case TypeAffinity.DateTime:
                    return "NUMERIC";
                case TypeAffinity.Double:
                    return "REAL";
                case TypeAffinity.Int64:
                    return "INTEGER";
                case TypeAffinity.Text:
                    return "TEXT";
                default:
                    return "";
            }
        }

        public string GetTableNameFromModel(Type model) => model.GetCustomAttributes<TableAttribute>().FirstOrDefault()?.Name ?? model.Name;
    }
}
