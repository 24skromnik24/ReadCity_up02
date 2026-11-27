using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCity.Data
{
    public static class DbHelper
    {
        //адрес бд
        private static readonly string ConnectionString =
            "Host=localhost;Port=5432;Database=ReadCity;Username=postgres;Password=postgres";

        //метод для создания нового объекта для обращения к бд
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

    }
}
