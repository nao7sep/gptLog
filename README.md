# gptLog

A desktop utility for collecting, organizing, and saving ChatGPT conversation snippets.

## Overview

gptLog is a narrow-scope desktop application that helps users manually collect, organize, and save ChatGPT conversation snippets. It provides a clipboard-centric workflow where users can copy text from ChatGPT's interface and organize it within a structured conversation format.

## Features

- **Minimal UI** optimized for keyboard workflow and clipboard operations
- **Zero web-scraping** - relies on user copying text via ChatGPT's "Copy" button
- **Explicit message tagging** as User or Assistant
- **Reliable JSON storage** with line-level fidelity and metadata
- **Cross-platform** support for Windows, macOS, and Linux

## Architecture

gptLog follows the MVVM (Model-View-ViewModel) pattern:

- **Model**: Domain classes and JSON serialization helpers
- **ViewModel**: Clipboard management, commands, and observable collections
- **View**: XAML-defined windows and controls with minimal code-behind

The application uses CommunityToolkit.Mvvm for MVVM implementation and Avalonia UI for cross-platform UI rendering.

## Data Model & Storage

Conversations are stored in JSON format with the following structure:

- **Metadata**: Title, creation timestamp, and last modification timestamp
- **Messages**: Array of message objects with role (user/assistant) and content lines

The application preserves line breaks and whitespace within messages while trimming leading and trailing whitespace-only lines. Files are saved with UTF-8 BOM encoding, and the application implements safe file saving with temporary files and backups.

Backward compatibility is maintained for legacy v0.1 format files, which are automatically converted to the new format when saved.

## User Interface

The main window includes:

- Menu bar with save functionality and settings
- Title field for the conversation
- Messages list with color-coded borders (blue for User, red for Assistant)
- Clipboard preview panel with controls to add content as User or Assistant messages

Each message card includes controls for:
- Moving messages up or down
- Inserting new User or Assistant messages
- Deleting messages

## Configuration

Application settings are stored in `appsettings.json` and include:

- Font family (default: "Segoe UI")
- Font size (default: 12pt)
- Title font size (default: 15pt)

## Building & Dependencies

- **Language/Runtime**: C# 10, .NET 8.0
- **UI Framework**: Avalonia 11
- **MVVM Library**: CommunityToolkit.Mvvm
- **Logging**: Serilog for structured logging

Background tasks are used for I/O operations to keep the UI responsive.

## Future Extensions

Potential future enhancements include:

- Keyboard shortcuts
- Drag-and-drop file opening and message reordering
- Search and filtering capabilities
- Undo/redo functionality
- Theme customization
- Export to other formats (Markdown, HTML)
- Multi-file session management
