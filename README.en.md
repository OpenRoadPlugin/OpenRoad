[![fr](https://img.shields.io/badge/lang-fr-green.svg)](README.md)
[![en](https://img.shields.io/badge/lang-en-red.svg)](README.en.md)

# Open Asphalte

<p align="center">
  <img src="OAS_Logo.png" alt="Open Asphalte" width="400"/>
</p>

**Modular Plugin for AutoCAD** Roadworks and Urban Planning

[![AutoCAD 2025+](https://img.shields.io/badge/AutoCAD-2025+-blue.svg)](https://www.autodesk.com/products/autocad)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

---

## Overview

Open Asphalte is an **extensible framework** for AutoCAD, designed for roadworks and urban planning professionals. Its modular architecture allows adding new features easily **without ever modifying the core program**.

### Philosophy

> **The core never changes.** Modules are added, the core remains intact.

- **Modular Architecture** Add features simply by dropping DLLs
- **Automatic Discovery** Modules are detected at startup without configuration
- **Dynamic Interface** Menu and ribbon generated automatically based on installed modules
- **Multilingual** French, English, Spanish
- **Zero Configuration** Works right after installation

### Fundamental Principle

If a module is not installed, it **exists nowhere**:
- Not in the menu
- Not in the ribbon
- Not in the commands
- Not in memory

The program automatically adapts to the present modules.

---

## Installation

### Prerequisites

- **AutoCAD 2025** or higher
- Windows 10/11

### Quick Install

1. **Download** the latest version from [Releases](https://github.com/openasphalteplugin/openasphalte/releases)
2. **Install the Core** Directly with the .exe
3. Launch AutoCAD and download the modules you wish to use.

### File Structure

```
OpenAsphalte/
  OAS.Core.dll          # Plugin Core (mandatory)
  Modules/              # Modules folder (created automatically)
    OAS.Georeferencement.dll
    OAS.StreetView.dll
    OAS.Cota2Lign.dll
    OAS.DynamicSnap.dll
    ...
```

---

## 🚀 Usage

### System Commands

These commands are **always available**, even without any installed module:

| Command | Description |
|---------|-------------|
| OAS_HELP | Displays the list of available commands |
| OAS_VERSION | Version information and loaded modules |
| OAS_SETTINGS | Opens the settings window |
| OAS_MODULES | Opens the module manager |
| OAS_RELOAD | Reloads the configuration |
| OAS_UPDATE | Checks for updates |

### Automatic Interface

Open Asphalte automatically generates:
- A **menu** with the localized application name
- A **ribbon tab** with the localized application name

The interface adapts dynamically:
- Module installed: Visible in menu and ribbon
- Module missing: No trace in the interface

---

## Modules

Modules extend Open Asphalte's capabilities. They are **automatically discovered** at startup.

### Module Management

- **Installation**: Check the desired modules in the *Modules* tab of the settings (`OAS_SETTINGS`) and click *Install*.
- **Update**: The *Update* button appears when a new version is available.
- **Uninstallation**: Click *Uninstall* to remove a module.
  > **Note**: Effective uninstallation happens at the next AutoCAD restart (unlocked file deletion).

### Official Modules

| Module | Description | Documentation |
|--------|-------------|---------------|
| **Georeferencing** | Coordinate systems and transformations | [See doc](docs/modules/georeferencement.md) |
| **Street View** | Dynamic AutoCAD ↔ Google Maps link | [See doc](docs/modules/streetview.md) |
| **Dimensioning** | Road dimensioning tools (Between 2 lines) | [See doc](docs/modules/cota2lign.md) |
| **Dynamic Snap** | Intelligent snapping engine (System) | [See doc](docs/modules/dynamicsnap.md) |

### Installing a Module

**Option A: Using the built-in Module Manager**

1. Open AutoCAD
2. Type **OAS_MODULES**
3. Select the module to install
4. Restart AutoCAD

The module will automatically appear in the interface!

**Option B: Manual Installation**

1. Download the module .dll file (e.g., `OAS.Georeferencement.dll`)
2. Place it in the **Modules/** folder (next to OAS.Core.dll)
3. Restart AutoCAD

The module will automatically appear in the interface!

### Removing a Module

1. Close AutoCAD
2. Delete the .dll file from the Modules/ folder
3. Restart AutoCAD

The module will completely disappear from the interface.

### Creating Your Own Modules

Check the **[Developer Guide](docs/guides/developer_guide.md)** to create your custom modules.

---

## 🌐 Supported Languages

- 🇫🇷 Français (default)
- 🇬🇧 **English**
- 🇪🇸 Español

Change the language with `OAS_SETTINGS` or in the configuration file.
All **Core** texts (UI, system commands, logs) are localized.

---

## 🛠️ Configuration

Configuration is stored in:
```
%APPDATA%\Open Asphalte\config.json
```

### Available Settings

| Setting | Description | Default |
|---------|-------------|---------|
| language | Language (fr, en, es) | fr |
| devMode | Developer mode (detailed logs) | false |
| checkUpdatesOnStartup | Check for updates on startup | true |
| mainMenuName | Custom menu and ribbon name | Open Asphalte |

### Custom Menu Name

During installation, you can customize the main menu name displayed in AutoCAD. If you enter a name (e.g., "MyCompany"), the menu and ribbon will display "MyCompany - OA".

You can also modify this setting manually in the `config.json` file:

```json
{
  "mainMenuName": "MyCompany - OA"
}
```

---

## Architecture

```
OpenAsphalte/
  src/
    OAS.Core/                 # Plugin Core (NEVER MODIFY)
      Plugin.cs               # Entry point IExtensionApplication
      Abstractions/           # Interfaces for creating modules
        IModule.cs            # Module interface
        ModuleBase.cs         # Module base class
        CommandBase.cs        # Command base class
        CommandInfoAttribute.cs
      Discovery/              # Automatic module discovery
      Configuration/          # Configuration management
      Localization/           # Translation system
      Logging/                # Unified logs
      Services/               # Shared services
        GeometryService.cs
        LayerService.cs
      UI/                     # Dynamic menu and ribbon construction
      Commands/               # System commands (OAS_HELP, OAS_SETTINGS...)

  templates/                  # Templates for creating new modules
    OAS.Module.Template.csproj
    ModuleTemplate.cs
    CommandTemplate.cs

  bin/
    OAS.Core.dll              # Main compiled DLL
    Modules/                  # Modules folder (external DLLs)
```

### Loading Flow

```
AutoCAD starts
  NETLOAD OAS.Core.dll
    1. Load configuration
    2. Initialize localization
    3. Scan Modules/ folder
       For each found OAS.*.dll:
         - Search for IModule classes
         - Validate dependencies
         - Load translations
         - Call Initialize()
    4. Generation of dynamic menu
    5. Generation of dynamic ribbon
    6. Ready!
```

---

## Compilation

### Developer Prerequisites

- Visual Studio 2022 or VS Code with C#
- .NET 8.0 SDK
- AutoCAD 2025 (for reference DLLs)

### Build Core

```bash
cd src/OAS.Core
dotnet build -c Release
```

The file OAS.Core.dll will be generated in bin/.

---

## 🤝 Contribution

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md).

### How to Contribute

1. Fork the project
2. Create a branch (git checkout -b feature/my-feature)
3. Commit (git commit -m 'Add my feature')
4. Push (git push origin feature/my-feature)
5. Open a Pull Request

---

## License

This project is licensed under **[Apache 2.0](LICENSE)** - free to use, modify, and distribute under the terms of the license.
See also the [NOTICE](NOTICE) file for mentions and trademarks.

### Disclaimer

This software is provided **"as is"**, without warranty of any kind, express or implied.

**Open Asphalte and its contributors shall not be held liable** for:
- Any direct, indirect, incidental, or consequential damages
- Any loss of data or profits
- Any business interruption
- Any injury resulting from the use or inability to use this software

Using this plugin in AutoCAD is done **at your own risk**. Always verify your drawings and data before any critical operation.

---

## Support

- Issues: [GitHub Issues](https://github.com/openasphalteplugin/openasphalte/issues)
- Discussions: [GitHub Discussions](https://github.com/openasphalteplugin/openasphalte/discussions)

---

## ✨ Partners
