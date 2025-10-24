using System;
using System.Collections.Generic;
using NETCode.Services;
using UAManagedCore;
using NETCode.Core;

namespace NETCode.Services;

/// <summary>
/// Represents the container for services related to a specific station.
/// </summary>
public class StationService
{
    public IPLCTagService PLCTagService { get; set; }
    public OptixDBService DBService { get; set; }
    // Future services can be added here (e.g., DB, logging, etc.)
}

/// <summary>
/// Centralized manager that holds all instantiated station services.
/// </summary>
public static class StationServiceManager
{
    public static Dictionary<string, StationService> Services { get; private set; } = new();

    /// <summary>
    /// Initializes the services for all stations.
    /// </summary>
    /// <param name="stationTags">List of station identifiers (e.g., Station1, Station2)</param>
    /// <param name="basePath">Base node path in the model (e.g., Model/MES_Stations)</param>
    public static void Initialize(List<string> stationTags, string basePath)
    {
        var dbService = OptixDBService.GetInstance(); // shared instance
        foreach (var tag in stationTags)
        {
            Services[tag] = new StationService
            {
                PLCTagService = new PlcTagServicePerStation(basePath, tag),
                DBService = dbService
            };
        }

        Log.Info("Station services initialized with isolated DB instances.");
    }
}
