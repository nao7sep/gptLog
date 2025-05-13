# gptLog

gptLog is a desktop utility that helps users collect, organize, and save ChatGPT conversation snippets in a structured format.

## Overview

gptLog offers a minimalist user interface optimized for keyboard workflows and clipboard-centric operations. It's designed for a single user to manually collect and organize ChatGPT conversation snippets without any web scraping.

### Key Features

- **Minimal UI** - Optimized for keyboard workflow and clipboard-centric operations
- **Zero web-scraping** - User copies text via ChatGPT's "Copy" button
- **Unambiguous data** - Each message is explicitly tagged as User or Assistant
- **Reliable storage** - JSON file with line-level fidelity and metadata
- **Cross-platform** - Runs on Windows, macOS, and Linux
- **Customizable appearance** - Configurable font family and size settings
- **Dialog system** - Consistent styled dialogs for errors and confirmations
- **Comprehensive logging** - Structured logging with Serilog for diagnostics

## Architecture

gptLog is built with C# 10 and .NET 9, using Avalonia UI 11.3 for cross-platform desktop support. The application follows the MVVM (Model-View-ViewModel) architectural pattern with CommunityToolkit.Mvvm.

### Components

| Layer | Responsibility | Notes |
|-------|----------------|-------|
| **Model** | Domain classes (`Message`, `Role` enum), JSON load/save helpers | Pure C#, no UI refs |
| **ViewModel** | Clipboard watcher, commands, unsaved-state flag, `ObservableCollection<Message>` | Implemented with **CommunityToolkit.Mvvm** |
| **View** | XAML-defined windows & controls | Avalonia styling for colored borders |
| **Services** | Dialog service, logging service | Encapsulated functionality for reuse |

## Features

### User Interface

- Message cards with role-specific colored borders (blue for User, red for Assistant)
- Clipboard preview panel with add buttons for User and Assistant messages
- Message reordering with up/down controls
- Message insertion and deletion
- Conversation title editing
- Unsaved changes indicator

### Application Settings

Settings are stored in `appsettings.json` and include:
- Font family (default: "Segoe UI")
- Font size (default: 12pt)
- Title font size (default: 15pt)

### Logging System

- Structured logging with Serilog
- Daily rolling log files stored in the user's AppData folder
- Multiple log levels for detailed diagnostics

## Dependencies

- **Language/Runtime**: C# 10, .NET 9.0
- **UI Framework**: Avalonia 11.3
- **MVVM Library**: CommunityToolkit.Mvvm
- **Configuration**: Microsoft.Extensions.Configuration
- **Logging**: Serilog with Console and File sinks

## License

This project is licensed under the GNU General Public License v3.0 (GPL-3.0).

## Repository

https://github.com/nao7sep/gptLog

## Author

nao7sep (Purrfect Code)