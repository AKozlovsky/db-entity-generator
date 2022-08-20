using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXApplication2
{
    class DB
    {
        List<string> tables = new List<string>();
        List<string> views = new List<string>();
        DataTable columnsOfTable = new DataTable();

        string connStr = "SERVER=localhost;DATABASE=tbname;UID=root;PASSWORD=;";
        string nameColumn = "Tables";

        public DB()
        {
            using (var conn = new MySqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    tables = MakeListTables(conn);
                    views = MakeListViews(conn);
                }
                catch (MySqlException ex)
                {
                    switch (ex.Number)
                    {
                        case 0:
                            MessageBox.Show("Cannot connect to server. Contact administrator");
                            break;

                        case 1045:
                            MessageBox.Show("Invalid username/password, please try again");
                            break;
                    }
                }
                finally
                {
                    if (conn != null)
                        conn.Close();
                }
            }
        }

        public List<string> Tables
        {
            get
            {
                return tables;
            }
        }

        public List<string> Views
        {
            get
            {
                return views;
            }
        }

        private DataTable ShowTables(MySqlConnection conn)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
            string sql = "SHOW FULL TABLES WHERE Table_Type != 'VIEW'";
            dataAdapter.SelectCommand = new MySqlCommand(sql, conn);
            dataAdapter.Fill(table);
            return table;
        }

        private List<string> MakeListTables(MySqlConnection conn)
        {
            List<string> list = new List<string>();
            DataTable table = ShowTables(conn);
            foreach (DataRow row in table.Rows)
                foreach (DataColumn column in table.Columns)
                    if (column.ToString() == nameColumn)
                        list.Add(row[column].ToString());
            return list;
        }

        private DataTable ShowViews(MySqlConnection conn)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
            string sql = "SHOW FULL TABLES WHERE Table_Type = 'VIEW'";
            dataAdapter.SelectCommand = new MySqlCommand(sql, conn);
            dataAdapter.Fill(table);
            return table;
        }

        private List<string> MakeListViews(MySqlConnection conn)
        {
            List<string> list = new List<string>();
            DataTable table = ShowViews(conn);
            foreach (DataRow row in table.Rows)
                foreach (DataColumn column in table.Columns)
                    if (column.ToString() == nameColumn)
                        list.Add(row[column].ToString());
            return list;
        }

        public DataTable GetColumnsOfTable(string tableName)
        {
            DataTable table = new DataTable();
            using (var conn = new MySqlConnection(connStr))
            {
                try
                {
                    conn.Open();        
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
                    string sql = $"SHOW COLUMNS FROM {tableName}";
                    dataAdapter.SelectCommand = new MySqlCommand(sql, conn);
                    dataAdapter.Fill(table);
                }
                catch (MySqlException ex)
                {
                    switch (ex.Number)
                    {
                        case 0:
                            MessageBox.Show("Cannot connect to server. Contact administrator");
                            break;

                        case 1045:
                            MessageBox.Show("Invalid username/password, please try again");
                            break;
                    }
                }
                finally
                {
                    if (conn != null)
                        conn.Close();
                }
            }

            return table;
        }
    }
}
