using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCity.Models
{
    public static class CurrentUser
    {

        public static int Id { get; private set; }
        public static string FullName { get; private set; }
        public static string RoleName { get; private set; }

        //вход ползователя
        public static void SetUser(int id, string fullName, string roleName)
        {
            Id = id;
            FullName = fullName;
            RoleName = roleName;
        }

        //вход гостя
        public static void SetGuest()
        {
            Id = 0;
            FullName = "Гость";
            RoleName = "Гость";
        }

    }
}
