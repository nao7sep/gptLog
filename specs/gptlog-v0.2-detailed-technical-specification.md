# **gptLog v0.2 — Detailed Technical Specification**

> **Version**: v0.2
> **Target tech stack**: C# 10 / .NET 8 + Avalonia UI 11, MVVM pattern, Windows | macOS | Linux

---

## 1. Purpose

*gptLog* is a narrow‑scope desktop utility that **helps a single user** manually collect, organise and save ChatGPT conversation snippets.
Key design goals:

1. **Minimal UI** – optimised for keyboard workflow, clipboard‑centric.
2. **Zero web‑scraping** – user copies text via ChatGPT's "Copy" button.
3. **Unambiguous data** – each message is explicitly tagged as *User* or *Assistant*.
4. **Reliable storage** – JSON file with line‑level fidelity and metadata.

---

## 2. High‑Level Architecture

| Layer | Responsibility | Notes |
|-------|----------------|-------|
| **Model** | Domain classes (`Message`, `Role` enum), JSON load/save helpers | Pure C#, no UI refs |
| **ViewModel** | Clipboard watcher, commands (add / delete / move / insert), unsaved‑state flag, `ObservableCollection<Message>` | Implemented with **CommunityToolkit.Mvvm** |
| **View** | XAML‑defined windows & controls; no code‑behind except bootstrapping | Avalonia styling for coloured borders |

---

## 3. Data Model

```csharp
public enum Role
{
    User,
    Assistant
}

public sealed class Message
{
    public Role Role { get; init; }
    public string Text { get; init; } = string.Empty;

    public string PreviewText =>
        $"{(Role == Role.User ? "User" : "Assistant")}: {TrimMessageText(Text)}";

    private string Trimmed => TrimMessageText(Text);

    /// <summary>
    /// Takes a message text, normalizes whitespace, trims it and adds ellipsis if it's longer than the specified length
    /// </summary>
    /// <param name="text">The text to trim</param>
    /// <param name="maxLength">Maximum length (default: 100)</param>
    /// <returns>Trimmed and formatted text</returns>
    public static string TrimMessageText(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalised = Regex.Replace(text, @"\s+", " ").Trim();
        return normalised.Length <= maxLength
            ? normalised
            : normalised[..maxLength].TrimEnd() + "...";
    }
}
```

---

## 4. File Format

### 4.1 Extension
`*.json`

### 4.2 Schema

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

* `metadata` — Object containing conversation metadata
  * `title` — Optional title for the conversation (string)
  * `created_at` — UTC timestamp of when the conversation was first created (ISO 8601)
  * `last_modified_at` — UTC timestamp of when the conversation was last modified (ISO 8601)
* `messages` — Array of message objects
  * `role` — `"user"` | `"assistant"` (lower‑case string)
  * `lines` — array of **string lines**
    * Empty lines appear as `""`
    * Trailing newline in the source text **does not** create an extra empty element

### 4.3 Backward Compatibility

The application supports loading files in the legacy v0.1 format (where the root element is an array of messages). When a legacy file is loaded:

1. The metadata will be initialized with default values
2. The title will be set to null
3. The file will be saved in the new format when saved

### 4.4 File Operations

* Files are saved with UTF-8 BOM encoding
* Safe file saving with temporary files and backups
* Filename suggestion based on conversation title (if available)

Serialization helpers must:

1. Split on newlines using StringReader when saving
2. Normalise to OS newline on load (`Environment.NewLine`)

---

## 5. User Interface Specification

### 5.1 Main Window Layout (wireframe)

```
┌──────────────────────────────────────────────────────────────┐
│ Menu:  [ Save ] [ FileName.json ] [ *UNSAVED* ]  [ ⚙ ]      │
├──────────────────────────────────────────────────────────────┤
│ Title: [ Conversation Title                              ]   │
├──────────────────────────────────────────────────────────────┤
│ Clipboard Preview (multi‑line TextBox, read‑only)           │
│  ┌─────┐  ┌─────────┐  ┌─────────────────────────────────┐  │
│  │ +U │  │ +A      │  │ ☐ Clear clipboard after paste   │  │
│  └─────┘  └─────────┘  └─────────────────────────────────┘  │
├──────────────────────────────────────────────────────────────┤
│ Messages List (ListBox)                                     │
│  ┌────────────────────────────────────────────────────┐     │
│  │ ⬤ blue border  User: Hello, how are y...  ↑  ↓  ✚U ✚A 🗑 │
│  ├────────────────────────────────────────────────────┤     │
│  │ ⬤ red border   Assistant: I'm doing gr... ↑  ↓  ✚U ✚A 🗑│
│  └────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────┘
```

### 5.2 Colours & Styling

| Element | Style |
|---------|-------|
| **User message border** | `2 px` solid **blue** (`#0066ff`) |
| **Assistant message border** | `2 px` solid **red** (`#cc0000`) |
| Background (both) | Neutral (white or light grey) |
| Card padding / margin | `6 px` / `4 px`, corner radius `4 px` |
| Font family | Configurable (default: "Segoe UI") |
| Font size | Configurable (default: 12pt) |
| Title font size | Configurable (default: 15pt) |

### 5.3 Message Card Controls

| Control | Function | Shortcut |
|---------|----------|----------|
| **↑** | Move selected message **up** | `Ctrl+U` |
| **↓** | Move selected message **down** | `Ctrl+D` |
| **✚U** | Insert **User** message before this card | — |
| **✚A** | Insert **Assistant** message before this card | — |
| **🗑** | Delete this message | `Delete` |

Inactive operations (e.g., moving the first card up) act as no‑op.

### 5.4 Clipboard Panel Behaviour

1. **Polling**: every `500 ms` (DispatcherTimer) or platform event.
2. **Validation**: accept only text with non‑whitespace after `Trim()`.
   * If invalid → `+U`/`+A` buttons disabled (`IsEnabled=false`).
3. On successful add:
   * Obey *"Clear clipboard after paste"* (default **ON**).
   * Clear preview.
   * Mark session **unsaved**.

### 5.5 Window Options

| Option | Function | Default |
|--------|----------|---------|
| **Stay on Top** | Keep window above other windows | OFF |

---

## 6. Command & Shortcut Map

| Command | Key Gesture | Scope |
|---------|-------------|-------|
| Add clipboard as **User** | *(none)* | Clipboard panel |
| Add clipboard as **Assistant** | *(none)* | Clipboard panel |
| Move message up | `Ctrl+U` | ListBox |
| Move message down | `Ctrl+D` | ListBox |
| Delete message | `Delete` | ListBox |
| Save file | `Ctrl+S` | Global |
| Open file | `Ctrl+O` | Global |

---

## 7. State & Persistence Logic

| Flag | Set When | Cleared When |
|------|----------|-------------|
| **Unsaved** | Any add / delete / move / insert since last save, or title change | Successful save |
| **Saved** | App launch & file load | — |

On window close with Unsaved = true, prompt: **"You have unsaved changes. Exit anyway?"**

### 7.1 Application Settings

Application settings are stored in `appsettings.json` and include:

```json
{
  "FontFamily": "Segoe UI",
  "FontSize": 12,
  "TitleFontSize": 15
}
```

---

## 8. Error Handling & Edge Cases

| Scenario | Behaviour |
|----------|-----------|
| Add pressed with invalid clipboard | Command ignored; UI already disabled |
| Delete on empty list | No action |
| Move top item up / bottom item down | No action |
| Invalid JSON on load | Show error dialog; keep current session |
| File write failure | Error dialog; remain Unsaved |
| Legacy format file | Load successfully; convert to new format on save |

### 8.1 Dialog System

The application includes a custom dialog system with two types:
- **OK** dialog - For information and error messages
- **Yes/No** dialog - For confirmation prompts

Dialogs are styled consistently with the main application and support the configured font settings.

---

## 9. Build & Dependency Notes

* **Language / runtime**: C# 10, .NET 8.0
* **UI**: Avalonia 11
* **MVVM library**: `CommunityToolkit.Mvvm` (RelayCommand, ObservableObject)
* **Threading**: I/O on background tasks to keep UI responsive
* **Logging**: Serilog for structured logging
* **Unit tests**: recommended for Model & serialization helpers

---

## 10. Future Extensions (non‑requirements)

1. Drag‑and‑drop re‑ordering
2. Search / filter
3. Undo / redo stack
4. Multi‑file session management
5. Theme customization
6. Export to other formats (Markdown, HTML)

---

*End of gptLog v0.2 specification.*