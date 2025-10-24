using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Services;
using UAManagedCore;

namespace NETCode.Repositories;
public class StationRepository : OptixRepositoryBase<Station>, IOptixRepository<Station>
{
    public StationRepository()
        : base(
            "stations",
            new string[] { "name", "description", "dependency" , "plctagname" },
            (resultSet, row) => new Station
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                Name = resultSet[row, 1].ToString(),
                Description = resultSet[row, 2].ToString(),
                Dependency = Convert.ToInt32(resultSet[row, 3]),
                PLCTagName = resultSet[row, 4].ToString()
            }
            
        )
    { Log.Info("Station Repository instance created"); }

    public void Insert(Station entity)
    {
        var values = new object[] { entity.Name, entity.Description, entity.Dependency , entity.PLCTagName };
        base.Insert(entity, values);
    }

    public void Update(Station entity)
    {
        string query = $"UPDATE stations SET dependency = '{entity.Dependency}', description = '{entity.Description}', name = '{entity.Name}' , plctagname = '{entity.PLCTagName}' WHERE id = {entity.Id}";
        ExecuteQuery(query);
    }

    public int? GetIdByName(string name)
    {
        string query = $"SELECT id FROM stations WHERE name = '{name}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null; // Not found

        return Convert.ToInt32(resultSet[0, 0]);
    }
    public int? GetDependencyByName(string stationName)
    {
        string query = $"SELECT dependency FROM stations WHERE name = '{stationName}'";
        var result = ExecuteQuery(query);
        if (result.GetLength(0) == 0)
            return null;

        return Convert.ToInt32(result[0, 0]);
    }
    public List<Station> GetStationsWithPlcTags()
    {
        return GetAll()
            .Where(s => !string.IsNullOrEmpty(s.PLCTagName))
            .ToList();
    }

    public bool ExistsById(int id)
    {
        string query = $"SELECT 1 FROM stations WHERE id = {id} LIMIT 1";
        Log.Info($"[StationRepo] SQL: {query}");
        var rs = ExecuteQuery(query);
        return rs.GetLength(0) > 0;
    }

    public bool IsTerminalById(int stationId)
    {
        // Terminal = no children (no station depends on this id)
        string query = $"SELECT 1 FROM stations WHERE dependency = {stationId} LIMIT 1";
        var rs = ExecuteQuery(query);
        return rs.GetLength(0) == 0;
    }

    public bool IsTerminalByName(string stationName)
    {
        // Resolve id first (fast path)
        string idQuery = $"SELECT id FROM stations WHERE name = '{stationName}' LIMIT 1";
        var idRs = ExecuteQuery(idQuery);
        if (idRs.GetLength(0) == 0)
            throw new InvalidOperationException($"Station '{stationName}' not found.");

        int stationId = Convert.ToInt32(idRs[0, 0]);
        return IsTerminalById(stationId);
    }

    // (Optional) Initial station check (no dependency)
    public bool IsInitialById(int stationId)
    {
        string query = $"SELECT dependency FROM stations WHERE id = {stationId} LIMIT 1";
        var rs = ExecuteQuery(query);
        if (rs.GetLength(0) == 0)
            throw new InvalidOperationException($"Station id {stationId} not found.");

        return rs[0, 0] is DBNull;
    }

    public bool IsInitialByName(string stationName)
    {
        string query = $"SELECT dependency FROM stations WHERE name = '{stationName}' LIMIT 1";
        var rs = ExecuteQuery(query);
        if (rs.GetLength(0) == 0)
            throw new InvalidOperationException($"Station '{stationName}' not found.");

        return rs[0, 0] is DBNull;
    }


}
