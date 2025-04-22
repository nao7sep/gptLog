# **gptLog v0.1 — Detailed Technical Specification**

> **Version**: v0.1
> **Target tech stack**: C# 10 / .NET 8 + Avalonia UI 11, MVVM pattern, Windows | macOS | Linux

---

## 1. Purpose

*gptLog* is a narrow‑scope desktop utility that **helps a single user** manually collect, organise and save ChatGPT conversation snippets.
Key design goals:

1. **Minimal UI** – optimised for keyboard workflow, clipboard‑centric.
2. **Zero web‑scraping** – user copies text via ChatGPT’s “Copy” button.
3. **Unambiguous data** – each message is explicitly tagged as *User* or *Assistant*.
4. **Reliable storage** – JSON file with line‑level fidelity.

---

## 2. High‑Level Architecture

| Layer | Responsibility | Notes |
|-------|----------------|-------|
| **Model** | Domain classes (`Message`, `Role` enum), JSON load/save helpers | Pure C#, no UI refs |
| **ViewModel** | Clipboard watcher, commands (add / delete / move / insert), unsaved‑state flag, `ObservableCollection<Message>` | Implemented with **CommunityToolkit.Mvvm** (or other specified) |
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
        $"{(Role == Role.User ? "User" : "Assistant")}: {Trimmed}";

    private string Trimmed
    {
        get
        {
            var normalised = Regex.Replace(Text, @"\s+", " ");
            return normalised.Length <= 50
                ? normalised
                : normalised[..50] + "...";
        }
    }
}
```

---

## 4. File Format

### 4.1 Extension
`*.json`

### 4.2 Schema

```json
[
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
      "I’m doing great—how can I help you?"
    ]
  }
]
```

* `role` — `"user"` | `"assistant"` (lower‑case string)
* `lines` — array of **string lines**
  * Empty lines appear as `""`
  * Trailing newline in the source text **does not** create an extra empty element

Serialization helpers must:

1. Split on `\r\n?|\n` when saving.
2. Normalise to OS newline on load (`Environment.NewLine`).

---

## 5. User Interface Specification

### 5.1 Main Window Layout (wireframe)

```
┌──────────────────────────────────────────────────────────────┐
│ Menu:  [ Save ] [ FileName.json ] [ *UNSAVED* ]             │
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
| **User message border** | `2 px` solid **blue** (`#0066ff`) |
| **Assistant message border** | `2 px` solid **red** (`#cc0000`) |
| Background (both) | Neutral (white or light grey) |
| Card padding / margin | `6 px` / `4 px`, corner radius `4 px` |

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

1. **Polling**: every `500 ms` (DispatcherTimer) or platform event.
2. **Validation**: accept only text with non‑whitespace after `Trim()`.
   * If invalid → `+U`/`+A` buttons disabled (`IsEnabled=false`).
3. On successful add:
   * Obey *“Clear clipboard after paste”* (default **ON**).
   * Clear preview.
   * Mark session **unsaved**.

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

---

## 7. State & Persistence Logic

| Flag | Set When | Cleared When |
|------|----------|-------------|
| **Unsaved** | Any add / delete / move / insert since last save | Successful save |
| **Saved** | App launch & file load | — |

On window close with Unsaved = true, prompt: **“You have unsaved changes. Exit anyway?”**

---

## 8. Error Handling & Edge Cases

| Scenario | Behaviour |
|----------|-----------|
| Add pressed with invalid clipboard | Command ignored; UI already disabled |
| Delete on empty list | No action |
| Move top item up / bottom item down | No action |
| Invalid JSON on load | Show error dialog; keep current session |
| File write failure | Error dialog; remain Unsaved |

---

## 9. Build & Dependency Notes

* **Language / runtime**: C# 10, .NET 8.0
* **UI**: Avalonia 11
* **MVVM library**: `CommunityToolkit.Mvvm` (RelayCommand, ObservableObject)
* **Threading**: I/O on background tasks to keep UI responsive
* **Unit tests**: recommended for Model & serialization helpers

---

## 10. Future Extensions (non‑requirements)

1. Drag‑and‑drop re‑ordering
2. Search / filter
3. Undo / redo stack
4. Multi‑file session management

---

*End of gptLog v0.1 specification.*