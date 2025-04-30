# gptLog

A desktop utility for collecting, organizing, and saving ChatGPT conversation snippets.

## Purpose

gptLog is a narrow-scope desktop utility that helps a single user manually collect, organize, and save ChatGPT conversation snippets. The application is designed with the following key goals:

- **Minimal UI** - Optimized for keyboard workflow and clipboard-centric operations
- **Zero web-scraping** - User copies text via ChatGPT's "Copy" button
- **Unambiguous data** - Each message is explicitly tagged as User or Assistant
- **Reliable storage** - JSON file with line-level fidelity and metadata

## Features

- Clipboard monitoring for easy message capture
- Support for both User and Assistant message types
- Message organization with move up/down functionality
- Visual distinction between message types (blue border for User, red border for Assistant)
- Conversation metadata storage (title, creation date, modification date)
- Custom dialog system for errors and confirmations
- Configurable font settings
- Keyboard shortcuts for common operations
- Backward compatibility with legacy file formats
- Cross-platform compatibility

## Tech Stack

- **Language/Runtime**: C# 10 / .NET 8
- **UI Framework**: Avalonia UI 11
- **Architecture**: MVVM pattern
- **MVVM Library**: CommunityToolkit.Mvvm
- **Logging**: Serilog
- **Platforms**: Windows, macOS, Linux

## File Format

gptLog saves conversations in a JSON format that preserves line-level fidelity and includes metadata:

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

### File Format Details

- **Metadata**
  - `title` - Optional title for the conversation
  - `created_at` - UTC timestamp of creation (ISO 8601)
  - `last_modified_at` - UTC timestamp of last modification (ISO 8601)
- **Messages**
  - `role` - Either "user" or "assistant" (lowercase)
  - `lines` - Array of string lines
    - Empty lines in the middle of text appear as `""`
    - Leading and trailing whitespace-only lines are automatically trimmed
    - Empty messages (containing only whitespace) result in an empty lines array

### File Operations

- Files are saved with UTF-8 BOM encoding
- Safe file saving with temporary files and backups
- Filename suggestion based on conversation title (if available)
- Backward compatibility with legacy v0.1 format

## User Interface

- Main window with title field, messages list, and clipboard preview
- Clipboard panel for adding new messages from clipboard content
- Message cards with controls for moving, inserting, and deleting messages
- Visual distinction between User messages (blue border) and Assistant messages (red border)
- Custom dialog system for errors and confirmations

## Application Settings

Application settings are stored in `appsettings.json` and include:

```json
{
    "AppSettings": {
        "FontFamily": "Segoe UI",
        "FontSize": 12,
        "TitleFontSize": 15
    }
}
```

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.