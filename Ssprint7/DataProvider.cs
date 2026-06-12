using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Ssprint7
{
    internal class DataProvider
    {
        private static DataProvider _instance;
        public static DataProvider Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DataProvider();
                return _instance;
            }
        }

        private string connectionString = "Server=zephyr.proxy.rlwy.net;Port=58816;Database=railway;Uid=root;Pwd=gYvXqmHUpZYSPXozPtWHmcxTPfArOoyJ;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}