using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.HMIProject;
using NETCode.Services;
using NETCode.Stations;
using FTOptix.Alarm;

public class StartupLogic : BaseNetLogic
{
    // =========================
    // Fields & Constants
    // =========================
    private readonly List<Station_Base> _stations = new();
    private readonly List<PeriodicTask> _periodicTasks = new();
    // ==== Pick-To-Light (single station) ====
    private PickToLightBroadcasterRaw _ptl;
    private PeriodicTask _ptlTaskSingle;



    private OptixDBService _dbService;
    private static Store _store;

    private bool _storeOnline = false;

    private const string StorePath = "DataStores/Tekno_Local_Database";
    private const string PLCBasePath = "CommDrivers/AB/ShowLine/Tags/Controller Tags";

    private const int TimeoutMs = 20000;      // Maximum wait time for Store to come online (20 seconds)
    private const int CheckIntervalMs = 500;  // Poll interval to check Store status (ms)

    // Unique PTL tags (NOT the station UDT)
    //Test_MES_System_v20/CommDrivers/AB/ShowLine/Tags/Controller Tags/PTL_Scheduled
    private const string PTL_NODE_PATH = "Model/PLCTags/Controller Tags/PTL_Sheduled";
    const string PTL_TAG_WRITE = "WriteBins_STRING";   // STRING (MES → PLC)
    const string PTL_TAG_REQUEST = "RequestNext_BOOL";   // BOOL   (PLC  → MES)
    const string PTL_TAG_COMPLETE = "Complete_BOOL";      // BOOL   (PLC  → MES)
    const string PTL_TAG_ACK = "Ack_BOOL";           // BOOL   (MES → PLC) or null to disable

    public override void Start()
    {
        Log.Info("StartupLogic: Initializing stations...");

        // Resolve Store object and wait until it is online (Status == 1)
        _store = Project.Current.Get<Store>(StorePath);
        int waitedMs = 0;

        while (waitedMs < TimeoutMs)
        {
            _storeOnline = (int)_store.Status == 1; // 1 = Online
            if (_storeOnline)
                break;

            Log.Info("StartupLogic: Waiting for Store to come online...");
            System.Threading.Thread.Sleep(CheckIntervalMs);
            waitedMs += CheckIntervalMs;
        }

        if (!_storeOnline)
        {
            Log.Error("StartupLogic: Store did not come online within the timeout window.");
            return;
        }

        Log.Info("StartupLogic: Store is online. Continuing initialization...");

        // Initialize repositories/services
        _dbService = OptixDBService.GetInstance();

        // Pull station definitions (with PLC tags) from DB
        var stations = _dbService.StationRepo.GetStationsWithPlcTags();

        // Initialize station services (PLC tag access)
        var stationTags = stations.Select(s => s.PLCTagName).ToList();
        StationServiceManager.Initialize(stationTags, PLCBasePath);

        // Build station objects and launch periodic execution cycles
        foreach (var s in stations)
        {
            var tag = s.PLCTagName;
            var name = s.Name;

            var service = StationServiceManager.Services[tag];
            var station = new Station_Base(tag, name, service.PLCTagService);
            _stations.Add(station);

            Log.Info($"StartupLogic: Launching periodic task for tag '{tag}' (DB name: '{name}').");
            var task = new PeriodicTask(() => station.RunCycle(), 1000, LogicObject);
            task.Start();
            _periodicTasks.Add(task);
        }

        Log.Info("StartupLogic: Stations initialized successfully.");

        // ==== Start one PTL periodic task ====
        try
        {
            // Validate node exists
            var node = Project.Current.Get(PTL_NODE_PATH);
            if (node == null)
            {
                Log.Error($"StartupLogic: PTL node not found at '{PTL_NODE_PATH}'. Check the path in the Project Explorer.");
            }
            else
            {
                _ptl = new PickToLightBroadcasterRaw(
                    PTL_NODE_PATH,
                    PTL_TAG_WRITE,
                    PTL_TAG_REQUEST,
                    PTL_TAG_COMPLETE,
                    PTL_TAG_ACK // or null to disable ACK pulse
                );

                _ptlTaskSingle = new PeriodicTask(() => _ptl.Tick(), 1000, LogicObject); // 1s scan
                _ptlTaskSingle.Start();

                Log.Info($"StartupLogic: PTL broadcaster started at '{PTL_NODE_PATH}'.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"StartupLogic: PTL broadcaster failed to start: {ex.Message}");
        }

    }

    public override void Stop()
    {
        // Dispose all periodic tasks on shutdown
        foreach (var task in _periodicTasks)
            task.Dispose();

        Log.Info("StartupLogic: All station periodic tasks have been stopped.");
        if (_ptlTaskSingle != null)
        {
            _ptlTaskSingle.Dispose();
            _ptlTaskSingle = null;
        }
        _ptl = null;

    }

    // =========================
    // Commands
    // =========================

    /// <summary>
    /// Re-reads stations from the database and restarts periodic tasks.
    /// </summary>
    [ExportMethod]
    public void RefreshStations()
    {
        Log.Info("StartupLogic: Refreshing station list...");

        // Stop and clear current stations/tasks
        foreach (var task in _periodicTasks)
            task.Dispose();

        _stations.Clear();
        _periodicTasks.Clear();

        // Read stations from DB again
        var stations = _dbService.StationRepo.GetStationsWithPlcTags();

        // Reinitialize services with fresh tag list
        var stationTags = stations.Select(s => s.PLCTagName).ToList();
        StationServiceManager.Initialize(stationTags, PLCBasePath);

        // Recreate station objects and periodic tasks
        foreach (var s in stations)
        {
            var tag = s.PLCTagName;
            var name = s.Name;

            var service = StationServiceManager.Services[tag];
            var station = new Station_Base(tag, name, service.PLCTagService);
            _stations.Add(station);

            Log.Info($"StartupLogic: Launching periodic task for tag '{tag}' (DB name: '{name}').");
            var task = new PeriodicTask(() => station.RunCycle(), 1000, LogicObject);
            task.Start();
            _periodicTasks.Add(task);
        }

        Log.Info("StartupLogic: Stations refreshed successfully.");
    }

    /// <summary>
    /// Removes a station by its PLC tag (stops its periodic task and removes it from the lists).
    /// </summary>
    [ExportMethod]
    public void RemoveStation(string stationTag)
    {
        var index = _stations.FindIndex(s => s._stationTag == stationTag);
        if (index == -1)
        {
            Log.Warning($"StartupLogic: Station '{stationTag}' not found.");
            return;
        }

        // Stop the associated periodic task
        _periodicTasks[index].Dispose();

        // Remove entries
        _stations.RemoveAt(index);
        _periodicTasks.RemoveAt(index);

        Log.Info($"StartupLogic: Station '{stationTag}' removed successfully.");
    }


    //Helpers
    private static string BuildPlcNodePath(string basePath, string stationTag)
    {
        // Example result: "Model/CommDrivers/AB/ShowLine/Tags/Controller Tags/STP110_Station"
        return $"Model/{basePath}/{stationTag}";
    }

}
