# Procedural to Object-Oriented Programming Workshop

This repository contains materials for a hands-on workshop that teaches the transition from procedural to object-oriented programming using a real-world C# console application.

## Workshop Overview

This workshop demonstrates the evolution from procedural programming to object-oriented design by refactoring a fully functional CLI TODO application. Participants will experience firsthand the challenges of procedural code and the benefits that OOP principles provide.

## Repository Structure

```
procedural-oop-workshop/
├── TodoApp/                     # The procedural C# console application
│   ├── Program.cs               # All application logic in one Main method
│   ├── TodoApp.csproj           # .NET 8 project file
│   └── README.md                # Usage instructions and examples
└── README.md                    # This file - workshop overview
```

## The TodoApp - Starting Point

The `TodoApp` directory contains a fully functional CLI task manager built using **intentionally poor** procedural design:

### Features
- ✅ Complete CRUD operations for tasks
- ✅ Task properties: ID, Name, Owner, Status, Description
- ✅ Modern CLI interface (similar to dotnet CLI)
- ✅ JSON file persistence
- ✅ Colored console output
- ✅ Filtering by status and owner
- ✅ Comprehensive error handling

### Procedural Design "Anti-Patterns"
The application deliberately demonstrates problematic procedural patterns:
- **Monolithic Main method** - All 400+ lines of code in one method
- **No separation of concerns** - UI, business logic, and data access mixed together
- **Repeated code patterns** - Similar logic duplicated across operations
- **Difficult to test** - No isolated units to test independently
- **Hard to extend** - Adding new features requires modifying the giant Main method
- **Poor maintainability** - Finding and fixing bugs is challenging

## Getting Started

### Prerequisites
- .NET SDK 8.0 or later
- Visual Studio Code or preferred IDE
- Basic understanding of C# syntax

### Quick Start
1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/procedural-oop-workshop.git
   cd procedural-oop-workshop
   ```

2. **Explore the procedural application:**
   ```bash
   cd TodoApp
   dotnet run
   ```

3. **Try adding some tasks:**
   ```bash
   dotnet run -- add --name "Learn OOP" --owner "student" --description "Complete the workshop"
   dotnet run -- list
   ```

4. **Review the code:**
   - Open `TodoApp/Program.cs` 
   - Notice everything is in the Main method
   - Identify repetitive patterns and mixed concerns

## Educational Value

This workshop provides hands-on experience with:

### Procedural Programming Challenges
- **Code organization** difficulties in large procedural programs
- **Maintainability** issues as codebase grows
- **Code reuse** limitations without proper abstraction
- **Testing** challenges with tightly coupled code

### Object-Oriented Solutions
- **Encapsulation** - Bundling data and methods together
- **Inheritance** - Building on existing functionality
- **Polymorphism** - Multiple implementations of common interfaces
- **Abstraction** - Hiding complex implementation details

### Real-World Application
- Working with a functional application (not toy examples)
- Gradual refactoring approach
- Maintaining functionality while improving design
- Modern C# and .NET practices

## Workshop Outcomes

By the end of this workshop, participants will:

1. **Understand** the limitations of purely procedural programming
2. **Recognize** when and why to apply OOP principles
3. **Practice** refactoring procedural code to object-oriented design
4. **Experience** the benefits of proper separation of concerns
5. **Learn** common design patterns and their applications
6. **Gain** confidence in designing maintainable software architecture

## Instructor Notes

The procedural TodoApp is designed to be:
- **Functional** - All features work correctly
- **Messy** - Intentionally poor organization for educational impact
- **Realistic** - Based on common procedural programming mistakes
- **Extensible** - Perfect foundation for OOP refactoring exercises

The key to this workshop is letting participants experience the pain points of procedural programming before introducing OOP solutions.

## Additional Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [Object-Oriented Programming Principles](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/oop)

## Contributing

This workshop is designed for educational purposes. If you find improvements or have suggestions, please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request with your improvements

## License

This educational material is provided as-is for learning purposes. Feel free to use and adapt for your own workshops and training sessions.
