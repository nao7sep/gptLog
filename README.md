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
- Message organization with move up/down functionality
- Support for both User and Assistant message types
- Visual distinction between message types
- Conversation metadata storage
- Keyboard shortcuts for common operations
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
        "Hello, how are you?",
        ""
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

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.