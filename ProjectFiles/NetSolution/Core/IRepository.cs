using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NETCode.Core;

public interface IOptixRepository<T> where T : class
{
    /// <summary>
    /// Retrieves all records from the table.
    /// </summary>
    IEnumerable<T> GetAll();

    /// <summary>
    /// Retrieves a record by Id. Optionally accepts a resultSet to avoid extra queries.
    /// </summary>
    T GetById(int id, object[,] resultSet = null);

    /// <summary>
    /// Retrieves the Id using the Name field.
    /// </summary>
    void Insert(T entity);

    /// <summary>
    /// Deletes a record by Id.
    /// </summary>
    /// 
    void DeleteByID(int id);
    void DeleteByName(string name);

    /// <summary>
    /// Updates a record by Id.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Updates a record by Name.
    /// </summary>
}
