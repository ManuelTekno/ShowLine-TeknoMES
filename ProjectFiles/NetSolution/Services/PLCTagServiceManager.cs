using NETCode.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAManagedCore;
using NETCode.Entities;

namespace NETCode.Services
{
    public static class PLCTagServiceManager
    {
        public static Dictionary<string, IPLCTagService> Services { get; private set; } = new();
        private static readonly OptixDBService _db = OptixDBService.GetInstance();

        public static void Initialize(List<string> stationList, string basePath)
        {
            foreach (var stationTag in stationList)
            {
                Services[stationTag] = new PlcTagServicePerStation(basePath, stationTag);
            }
            Log.Info("Station services initialized successfully.");
        }
        }
    }
