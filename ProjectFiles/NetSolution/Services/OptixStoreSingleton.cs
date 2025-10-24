using FTOptix.Store;
using FTOptix.HMIProject;
using System.Collections.Generic;
using UAManagedCore;
using System.Runtime.CompilerServices;

namespace NETCode.Services
{
    /*
     The OptixDBService class centralizes and manages access to the FT Optix Store and its Table objects, ensuring that:
        - Only one instance of the Store is created during the application's runtime.
        - Access to the database tables is reused and cached, avoiding repeated loading and improving performance. 
    */
    public static class OptixStoreSingleton
    {
        private static Store _store;
        private static readonly string _storePath = "DataStores/Tekno_Local_Database";
        private static Dictionary<string, Table> _tables = new();

        public static Store GetStore()
        {
            if (_store == null)
            {
                _store = Project.Current.Get<Store>(_storePath);
                Log.Info("Store instance created");
            }
            return _store;
        }

        public static Table GetTable(string tableName)
        {
            if (!_tables.ContainsKey(tableName))
            {
                var table = GetStore().Tables.Get<Table>(tableName);
                _tables[tableName] = table;
                Log.Info($"Table {tableName} loaded");
            }
            return _tables[tableName];
        }
    }
}
