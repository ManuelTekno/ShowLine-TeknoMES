# ShowLine-TeknoMES

Prototype for Tekno MES integrating PLC comms, SQL storage, and FT Optix UI.

## ✨ Features
- FT Optix HMI/SCADA project with Allen-Bradley driver and station logic
- PLC ↔ MES handshake by command/response + result payloads
- SQL persistence for units, operations and results

## 🧱 Repository layout
- `Test_MES_System_v20.optix` – Main Optix project
- `Test_MES_System_v20.optix.design` – Design-time artifacts
- `DesignTimeNodes/CommDrivers/AB/ShowLine` – AB comm driver config
- `Nodes/`, `ProjectFiles/`, `ApplicationFiles/DA/`, `IDEVersion.txt`

## ✅ Requirements
- **FactoryTalk Optix** (same IDE version listed in `IDEVersion.txt`)
- **MySQL 8.x** (or compatible)
- **PLC Allen-Bradley** (EtherNet/IP) with reachable IP

## 🔧 Quick Start (Local)
1. Clone repo  
   ```bash
   git clone https://github.com/ManuelTekno/ShowLine-TeknoMES.git
