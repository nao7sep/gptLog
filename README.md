# gptLog

A lightweight desktop utility for collecting, organizing, and saving ChatGPT conversation snippets.

## Overview

gptLog is a narrow-scope desktop utility that helps a single user manually collect, organize, and save ChatGPT conversation snippets. It features a minimal UI optimized for keyboard workflow and clipboard-centric operations.

### Key Design Goals

- **Minimal UI** – Optimized for keyboard workflow, clipboard-centric
- **Zero web-scraping** – User copies text via ChatGPT's "Copy" button
- **Unambiguous data** – Each message is explicitly tagged as *User* or *Assistant*
- **Reliable storage** – JSON file with line-level fidelity and metadata

## Features

- Simple clipboard monitoring for easy message capture
- Clear visual distinction between user and assistant messages
- Message reordering, insertion, and deletion
- Conversation metadata including title and timestamps
- Safe file saving with temporary files and backups
- Cross-platform support (Windows, macOS, Linux)

## System Requirements

- .NET 8.0 Runtime
- Supported platforms: Windows, macOS, Linux

## Installation

1. Download the latest release for your platform from the releases page
2. Extract the archive to your preferred location
3. Run the gptLog executable

## Usage

### Basic Workflow

1. Start a conversation in ChatGPT
2. Use ChatGPT's "Copy" button to copy a message
3. In gptLog, click "+U" to add as a User message or "+A" to add as an Assistant message
4. Continue collecting messages as needed
5. Save your conversation with the "Save" button

### Interface Elements

- **Title field**: Set a title for your conversation
- **Messages List**: View and manage collected messages
- **Clipboard Preview**: See what's currently in your clipboard
- **Message Controls**:
  - ↑/↓: Move messages up or down
  - +U/+A: Insert new User or Assistant message before the selected message
  - 🗑: Delete the selected message

### File Operations

- **New**: Start a new conversation (Ctrl+N)
- **Open**: Load an existing conversation (Ctrl+O)
- **Save**: Save the current conversation (Ctrl+S)

## File Format

gptLog uses a JSON file format with the following structure:

```json
{
    "metadata": {
        "title": "Example Conversation",
        "created_at": "2025-04-25T01:30:45Z",
        "last_modified_at": "2025-04-25T02:15:30Z"
    },
    "messages": [
        {
            "role": "user",
            "lines": [
                "Hello, how are you?"
            ]
        },
        {
            "role": "assistant",
            "lines": [
                "I'm doing great—how can I help you?"
            ]
        }
    ]
}
```

### Format Details

- **metadata**: Object containing conversation metadata
  - **title**: Optional title for the conversation
  - **created_at**: UTC timestamp of creation (ISO 8601)
  - **last_modified_at**: UTC timestamp of last modification (ISO 8601)
- **messages**: Array of message objects
  - **role**: "user" or "assistant"
  - **lines**: Array of string lines preserving original formatting

## Configuration

Application settings are stored in `appsettings.json`:

```json
{
    "AppSettings": {
        "FontFamily": "Segoe UI",
        "FontSize": 12,
        "TitleFontSize": 15
    }
}
```

## Development

### Tech Stack

- **Language/Runtime**: C# 10, .NET 8.0
- **UI Framework**: Avalonia 11
- **Architecture**: MVVM pattern
- **MVVM Library**: CommunityToolkit.Mvvm
- **Logging**: Serilog for structured logging

### Project Structure

- **Model**: Domain classes, JSON load/save helpers
- **ViewModel**: Clipboard watcher, commands, state management
- **View**: XAML-defined windows & controls

## Future Plans

Potential future enhancements include:

1. Keyboard shortcuts
2. Drag-and-drop file opening
3. Drag-and-drop message reordering
4. Search and filter functionality
5. Undo/redo stack
6. Theme customization
7. Export to other formats (Markdown, HTML)
8. Multi-file session management

## License

This project is licensed under the GNU General Public License v3.0 (GPL-3.0) - see the [LICENSE](LICENSE) file for details.
