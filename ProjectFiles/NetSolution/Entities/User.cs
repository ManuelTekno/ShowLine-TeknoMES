using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Entities;

public class Users
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string UserPassword { get; set; }
    public string Rol { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
