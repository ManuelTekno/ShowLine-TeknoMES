using FTOptix.HMIProject;
using FTOptix.Store;
using NETCode.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace NETCode.Repositories;
    public abstract class OptixRepositoryBase<T> where T : class, new()
    {
        protected Store MyStore;
        protected Table MyTable;
        protected string StorePath;
        protected string TableName;
        protected string[] DbColumns;
        protected Func<object[,], int, T> MapFunc;

    protected OptixRepositoryBase(string tableName, string[] dbColumns, Func<object[,], int, T> mapFunc)
    {
        TableName = tableName;
        DbColumns = dbColumns;
        MapFunc = mapFunc;
        InitializeStoreAndTable();
    }
        private void InitializeStoreAndTable()
    {
        MyStore = OptixStoreSingleton.GetStore();
        MyTable = OptixStoreSingleton.GetTable(TableName);
    }
        public object[,] ExecuteQuery(string query)
        {
            object[,] resultSet;
            string[] header;
            MyStore.Query(query, out header, out resultSet);
            return resultSet;
        }
        public IEnumerable<T> GetAll()
        {
            var items = new List<T>();
            string query = $"SELECT * FROM {TableName}";
            var resultSet = ExecuteQuery(query);

            for (int i = 0; i < resultSet.GetLength(0); i++)
            {
                items.Add(MapFunc(resultSet, i));
            }

            return items;
        }
        public T GetById(int id, object[,] resultSet = null)
        {
            object[,] objects = resultSet ?? ExecuteQuery($"SELECT * FROM {TableName} WHERE id = {id}");

            if (objects.GetLength(0) == 0) return null;

            return MapFunc(objects, 0);
        }
        public void Insert(T entity, object[] values)
        {
            var insertValues = new object[1, values.Length];
            for (int i = 0; i < values.Length; i++)
                insertValues[0, i] = values[i];

            MyTable.Insert(DbColumns, insertValues);
        }
        public void Update(T entity, int id, string updateQuery)
        {
            ExecuteQuery(updateQuery);
        }
        public void DeleteByName(string name)
        {
            string query = $"DELETE FROM {TableName} WHERE Name = '{name}'";
            ExecuteQuery(query);
        }
        public void DeleteByID(int id)
    {
        string query = $"DELETE FROM {TableName} WHERE id = '{id}'";
        ExecuteQuery(query);
    }

}


