# TodoApp - CLI Task Manager

A fully procedural C# console application for managing TODO tasks. This application is designed as an educational tool to demonstrate procedural programming before transitioning to object-oriented principles.

## Features

- **Full CRUD Operations**: Create, Read, Update, and Delete tasks
- **Task Properties**: Each task includes ID, Name, Owner, Status, and Description
- **Filtering**: Filter tasks by status or owner
- **Colored Output**: Status-based color coding for better readability
- **Data Persistence**: JSON file storage with automatic directory creation
- **CLI Interface**: Modern command-line interface similar to dotnet CLI and GitHub CLI

## Prerequisites

- .NET SDK 8.0 or later
- Windows, macOS, or Linux

## Installation and Setup

1. **Clone or download** the project files
2. **Navigate** to the TodoApp directory
3. **Build** the application:
   ```bash
   dotnet build
   ```
4. **Run** the application:
   ```bash
   dotnet run
   ```

## Usage

### Basic Command Structure
```
dotnet run -- <command> [options]
```

### Available Commands

#### 1. Display Help
```bash
dotnet run -- --help
dotnet run
```

#### 2. Add a New Task
```bash
# Add task with all properties
dotnet run -- add --name "Fix login bug" --owner "john" --description "Fix authentication issue in login form"

# Add task with minimal info (owner defaults to "Unassigned", status to "Todo")
dotnet run -- add --name "Update documentation"

# Add task with custom status
dotnet run -- add --name "Review PR" --owner "alice" --status "In Progress" --description "Review authentication refactoring"
```

#### 3. List Tasks

**List all tasks:**
```bash
dotnet run -- list
```

**Filter by status:**
```bash
dotnet run -- list --status "Todo"
dotnet run -- list --status "In Progress"
dotnet run -- list --status "Complete"
```

**Filter by owner:**
```bash
dotnet run -- list --owner "john"
dotnet run -- list --owner "alice"
```

#### 4. Update Tasks
```bash
# Update task name
dotnet run -- update --id 1 --name "Fix critical login bug"

# Update multiple properties
dotnet run -- update --id 2 --owner "bob" --status "In Progress" --description "Updated description"

# Update just the status
dotnet run -- update --id 3 --status "Complete"
```

#### 5. Mark Task as Complete
```bash
dotnet run -- complete --id 1
```

#### 6. Assign Task to Someone
```bash
dotnet run -- assign --id 2 --owner "jane"
```

#### 7. Delete Tasks
```bash
dotnet run -- delete --id 1
```

## Sample Usage Scenarios

### Scenario 1: Creating and Managing a Sprint
```bash
# Add some tasks for a sprint
dotnet run -- add --name "Implement user authentication" --owner "alice" --description "Create login/logout functionality"
dotnet run -- add --name "Design user dashboard" --owner "bob" --description "Create wireframes and mockups"
dotnet run -- add --name "Setup CI/CD pipeline" --owner "charlie" --description "Configure GitHub Actions"

# Check all tasks
dotnet run -- list

# Update task status as work progresses
dotnet run -- update --id 1 --status "In Progress"
dotnet run -- complete --id 2

# Reassign a task
dotnet run -- assign --id 3 --owner "alice"

# Filter to see what's left to do
dotnet run -- list --status "Todo"
```

### Scenario 2: Managing Personal Tasks
```bash
# Add personal tasks
dotnet run -- add --name "Buy groceries" --owner "me" --description "Milk, bread, eggs"
dotnet run -- add --name "Call dentist" --owner "me" --description "Schedule appointment"
dotnet run -- add --name "Review budget" --owner "me" --status "In Progress"

# See my tasks
dotnet run -- list --owner "me"

# Complete tasks
dotnet run -- complete --id 1
dotnet run -- complete --id 2
```

## Data Storage

- **Location**: 
  - Windows: `%APPDATA%\TodoApp\tasks.json`
  - macOS/Linux: `~/.todoapp/tasks.json`
- **Format**: JSON array with task objects
- **Auto-creation**: Directory and file are created automatically if they don't exist

### Sample Data Format
```json
[
  {
    "Id": 1,
    "Name": "Complete project documentation",
    "Owner": "alice",
    "Status": "In Progress",
    "Description": "Write comprehensive documentation for the new feature"
  },
  {
    "Id": 2,
    "Name": "Review pull request",
    "Owner": "bob",
    "Status": "Todo",
    "Description": "Review the authentication refactoring PR"
  }
]
```

## Output Features

- **Color Coding**: 
  - Green: Completed tasks
  - Yellow: In Progress tasks
  - White: Todo tasks
- **Table Format**: Clean, aligned columns for easy reading
- **Truncation**: Long text is truncated with "..." for better table display
- **Error Handling**: Clear error messages with colored output

## Error Handling

The application includes comprehensive error handling for:
- Missing required parameters
- Invalid task IDs
- File I/O operations
- JSON parsing errors
- Directory creation failures

## Development Notes

This application is intentionally built using a fully procedural approach with all code contained in the `Main` method. This design demonstrates:
- Code organization challenges in procedural programming
- Difficulty in maintaining large procedural codebases
- Repetitive patterns that could benefit from object-oriented design
- The need for better separation of concerns

This serves as the perfect starting point for a workshop on transitioning from procedural to object-oriented programming.

## Building and Running

### Development Mode
```bash
# Build the project
dotnet build

# Run in development
dotnet run

# Run with arguments
dotnet run -- add --name "Test task" --owner "developer"
```

### Release Mode
```bash
# Build for release
dotnet build --configuration Release

# Publish for distribution
dotnet publish --configuration Release --output ./publish
```

### Create Alias (Optional)
For easier usage, you can create an alias:

**Windows (PowerShell):**
```powershell
function todoapp { dotnet run --project "C:\path\to\TodoApp" -- $args }
```

**macOS/Linux (Bash):**
```bash
alias todoapp='dotnet run --project /path/to/TodoApp --'
```

Then you can use it like:
```bash
todoapp add --name "My task" --owner "me"
todoapp list
```

## System Requirements

- .NET SDK 8.0+
- Windows 10+, macOS 10.14+, or modern Linux distribution
- Minimum 50MB disk space
- Read/write permissions for application data directory
