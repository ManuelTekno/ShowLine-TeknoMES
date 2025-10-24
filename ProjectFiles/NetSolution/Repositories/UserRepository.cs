using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Services;
using UAManagedCore;

namespace NETCode.Repositories;

public class UserRepository : OptixRepositoryBase<Users>, IOptixRepository<Users>
{
    //Constructor
    public UserRepository()
        : base(
            "users",
            new string[] { "user_name", "user_password", "rol", "date_created", "last_login_date" },
            (resultSet, row) => new Users
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                UserName = resultSet[row, 1].ToString(),
                UserPassword = resultSet[row, 2].ToString(),
                Rol = resultSet[row, 3].ToString(),
                DateCreated = resultSet[row, 4] != null ? Convert.ToDateTime(resultSet[row, 4]) : (DateTime?)null,
                LastLoginDate = resultSet[row, 5] != null ? Convert.ToDateTime(resultSet[row, 5]) : (DateTime?)null
            }
            
        )
    { Log.Info("User Repository instance created"); }
    public int? GetIdByName(string name)
    {
        string query = $"SELECT id FROM users WHERE user_name = '{name}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null; // Not found

        return Convert.ToInt32(resultSet[0, 0]);
    }
    public void Insert(Users entity)
    {
        var values = new object[]
        {
            entity.UserName,
            entity.UserPassword,
            entity.Rol,
            entity.DateCreated,
            entity.LastLoginDate
        };
        base.Insert(entity, values);
    }
    public void Update(Users entity)
    {
        string query = $"UPDATE users SET user_name = '{entity.UserName}', user_password = '{entity.UserPassword}', rol = '{entity.Rol}', last_login_date = '{entity.LastLoginDate:yyyy-MM-dd HH:mm:ss}' WHERE id = {entity.Id}";
        ExecuteQuery(query);
    }

    public string ValidateUserCredentials(string username, string password)
    {
        string query = $"SELECT rol FROM users WHERE user_name = '{username}' AND user_password = '{password}' LIMIT 1";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null; // No match found

        return resultSet[0, 0]?.ToString();
    }

}
