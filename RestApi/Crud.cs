using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Dapper;

namespace RestApi
{
    public class RequestCrud
    {
        public string table;
        public Dictionary<string, object> row;
        public Statement type;
    }

    public enum Statement
    {
        INSERT = 1,
        UPDATE = 2,
        DELETE = 3,
        MERGE = 4
    }

    public static class Crud
    {
        public static void CrudExec(this IDbConnection connection, RequestCrud data)
        {
            var dialect = DbHelper.GetDialect(connection);
            var tableName = data.table;
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("Requires a table name");
            }

            var pkeys = DbHelper.GetPrimaryKeys(connection, tableName);
            var colsDesc = DbHelper.GetTabDescription(connection, tableName).Where(x => data.row.ContainsKey(x.ColumnName.ToLower())).ToList();

            var sql = string.Empty;
            if (data.type == Statement.INSERT)
            {
                var idColumn = pkeys.FirstOrDefault().ToLower();
                // если 1 ключ и не задан то исключаем (сиквенс)
                if (pkeys.Count() == 1 && !data.row.ContainsKey(idColumn))
                {
                    sql = CreateInsert(tableName, colsDesc.Where(x => x.ColumnName.ToLower() != idColumn).ToList());
                }
                else
                {
                    sql = CreateInsert(tableName, colsDesc);
                }
            }
            else if (data.type == Statement.UPDATE)
            {
                sql = CreateUpdate(tableName, pkeys, colsDesc);
            }
            else if (data.type == Statement.DELETE)
            {
                sql = CreateDelete(tableName, pkeys);
            }
            else if (data.type == Statement.MERGE)
            {
                sql = CreateMergeSql(dialect, tableName, pkeys, colsDesc);
            }
            else
            {
                return;
            }
            var values = PrepareValues(data.row, colsDesc);
            connection.Execute(sql, values);
        }

        private static string CreateMergeSql(Dialect dialect, string tableName, List<string> primaryKeys, List<TableDescription> tabDesc)
        {
            return (dialect == Dialect.Oracle) ? CreateOraMergeSql(tableName, primaryKeys, tabDesc) : CreatePgMergeSql(tableName, primaryKeys, tabDesc);
        }

        private static string CreatePgMergeSql(string tableName, List<string> primaryKeys, List<TableDescription> tabDesc)
        {
            var pkeys = string.Join(", ", primaryKeys.ToArray());
            var colsDesc = tabDesc.Select(x => x.ColumnName).ToArray();
            var colNames = string.Join(", ", colsDesc);
            var colNamesExcluded = "EXCLUDED." + string.Join(", EXCLUDED.", colsDesc);
            var recordsListTemplate = ":P_" + string.Join(",:P_", colsDesc).TrimStart(',');
            var sql = string.Format("INSERT INTO {0}({1}) VALUES ({4}) ON CONFLICT({3}) DO UPDATE SET({1}) = ({2})",
                tableName, colNames, colNamesExcluded, pkeys, recordsListTemplate);
            return sql;
        }

        private static string CreateOraMergeSql(string tableName, List<string> primaryKeys, List<TableDescription> tabDesc)
        {
            var where = string.Empty;
            foreach (var key in primaryKeys)
            {
                where += " AND " + key + " = :P_" + key;
            }
            where = where.Substring(5);

            var updateCols = tabDesc.Where(x => !primaryKeys.Any(p => p == x.ColumnName));
            var set = string.Empty;
            foreach (var col in updateCols)
            {
                set += $", {col.ColumnName} = :P_{col.ColumnName}";
            }
            set = set.TrimStart(',');

            var colsDesc = tabDesc.Select(x => x.ColumnName).ToArray();
            var colNames = string.Join(", ", colsDesc);
            var values = ":P_" + string.Join(",:P_", colsDesc).TrimStart(',');

            var sql = string.Format("merge into {0} using DUAL on ({1}) when matched then update set {2} when not matched then insert({3}) values ({4})",
                tableName, where, set, colNames, values);
            return sql;
        }

        private static string CreateInsert(string tableName, List<TableDescription> tabDesc)
        {
            var colsDesc = tabDesc.Select(x => x.ColumnName).ToArray();
            var colNames = string.Join(", ", colsDesc);
            var values = ":P_" + string.Join(",:P_", colsDesc).TrimStart(',');
            var sql = string.Format("INSERT INTO {0}({1}) VALUES ({2})", tableName, colNames, values);
            return sql;
        }

        private static string CreateUpdate(string tableName, List<string> primaryKeys, List<TableDescription> tabDesc)
        {
            var updateCols = tabDesc.Where(x => !primaryKeys.Any(p => p == x.ColumnName));
            var set = string.Empty;
            foreach (var col in updateCols)
            {
                set += $", {col.ColumnName} = :P_{col.ColumnName}";
            }
            set = set.TrimStart(',');

            var where = string.Empty;
            foreach (var key in primaryKeys)
            {
                where += " AND " + key + " = :P_" + key;
            }
            where = where.Substring(5);
            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", tableName, set, where);
            return sql;
        }

        private static string CreateDelete(string tableName, List<string> primaryKeys)
        {
            var where = string.Empty;
            foreach (var key in primaryKeys)
            {
                where += " AND " + key + " = :P_" + key;
            }
            where = where.Substring(5);
            var sql = string.Format("DELETE FROM {0} WHERE {1}", tableName, where);
            return sql;
        }

        private static Dictionary<string, object> PrepareValues(Dictionary<string, object> row, List<TableDescription> tabDesc)
        {
            var values = new Dictionary<string, object>();
            foreach (var prop in row)
            {
                var key = prop.Key.ToLower();
                var desc = tabDesc.Where(x => x.ColumnName.ToLower() == key).FirstOrDefault();
                if (desc != null)
                {
                    dynamic value;
                    string[] dbDateTypes = { "date", "timestamp", "timestamp without time zone", "timestamp with local time zone" };
                    string[] dbNumberTypes = { "numeric", "smallint", "integer", "bigint", "decimal", "numeric", "real", "double precision", "serial", "bigserial" };
                    if (dbDateTypes.Contains(desc.DataType.ToLower()))
                    {
                        var propVal = Convert.ToString(prop.Value);
                        value = RestHelper.GetDateTime(propVal);
                    }
                    else if (dbNumberTypes.Contains(desc.DataType.ToLower()))
                    {
                        var propVal = Convert.ToString(prop.Value);
                        value = RestHelper.GetDouble(propVal);
                    }
                    else
                    {
                        value = prop.Value;
                    }
                    values.Add("P_" + key.ToLower(), value);
                }
            }
            return values;
        }

    }

}
