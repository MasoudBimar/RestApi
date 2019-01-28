using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Dapper;

namespace RestApi
{
    public enum Dialect
    {
        PostgreSQL,
        Oracle,
    }

    public class TableDescription
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string Comments { get; set; }
    }

    public class DbHelper
    {
        public static Dialect GetDialect(IDbConnection connection)
        {
            return (connection.GetType().Name == "OracleConnection") ? Dialect.Oracle : Dialect.PostgreSQL;
        }

        public static List<TableDescription> GetTabDescription(IDbConnection connection, string tableName)
        {
            var dialect = GetDialect(connection);
            return (dialect == Dialect.Oracle) ? GetOraTabDescription(connection, tableName) : GetPgTabDescription(connection, tableName);
        }

        public static List<string> GetPrimaryKeys(IDbConnection connection, string tableName)
        {
            var dialect = GetDialect(connection);
            return (dialect == Dialect.Oracle) ? GetOraPrimaryKeys(connection, tableName) : GetPgPrimaryKeys(connection, tableName);
        }

        private static List<TableDescription> GetPgTabDescription(IDbConnection connection, string tableName)
        {
            var sql = @"select t1.table_name, t1.column_name, t1.data_type, pgd.description as comments
              from (select c.table_name, c.column_name, c.data_type, c.ordinal_position, st.relid
                      from pg_catalog.pg_statio_all_tables st, information_schema.columns c
                     where c.table_schema = st.schemaname
                       and c.table_name = st.relname
                       and c.table_name = :tableName) t1
              left outer join pg_catalog.pg_description pgd
                on pgd.objoid = t1.relid
               and pgd.objsubid = t1.ordinal_position";
            return connection.Query<TableDescription>(sql, new { tableName }).ToList();
        }

        private static List<string> GetPgPrimaryKeys(IDbConnection connection, string tableName)
        {
            var sql = @"select a.attname
                    from pg_index i
                    join pg_attribute a 
                    on a.attrelid = i.indrelid
                    and a.attnum = any(i.indkey)
                    where i.indrelid = (select oid from pg_class where relname = :tablename)
                    and i.indisprimary";
            return connection.Query<string>(sql, new { tableName }).ToList();
        }

        private static List<TableDescription> GetOraTabDescription(IDbConnection connection, string tableName)
        {
            var sql = @"select lower(T2.TABLE_NAME) as TABLE_NAME, lower(T2.COLUMN_NAME) as COLUMN_NAME, lower(T1.DATA_TYPE) as DATA_TYPE, T2.COMMENTS
              from USER_TAB_COLS T1, USER_COL_COMMENTS T2
             where T1.TABLE_NAME = T2.TABLE_NAME
               and T1.COLUMN_NAME = T2.COLUMN_NAME
               and T1.TABLE_NAME = upper(:tableName)";
            return connection.Query<TableDescription>(sql, new { tableName }).ToList();
        }

        private static List<string> GetOraPrimaryKeys(IDbConnection connection, string tableName)
        {
            string sql = @"select lower(COLS.COLUMN_NAME) as COLUMN_NAME
              from USER_CONSTRAINTS CONS, USER_CONS_COLUMNS COLS
             where CONS.CONSTRAINT_TYPE = 'P'
               and CONS.CONSTRAINT_NAME = COLS.CONSTRAINT_NAME
               and CONS.OWNER = COLS.OWNER
               and upper(COLS.TABLE_NAME) = upper(:tableName)
             order by COLS.TABLE_NAME, COLS.POSITION";
            return connection.Query<string>(sql, new { tableName }).ToList();
        }

        public static Dictionary<string, Type> GetCursorDescColumns(IDbConnection connection, string sql)
        {
            var descTab = new Dictionary<string, Type>();
            using (var reader = connection.ExecuteReader(sql + " where 1=2"))
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    descTab.Add(reader.GetName(i).ToLower(), reader.GetFieldType(i));
                }
            }
            return descTab;
        }

    }
}
