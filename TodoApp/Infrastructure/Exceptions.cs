using System;

namespace TodoApp.Infrastructure
{
    // Typed exceptions for centralized error handling in CLI
    public class TodoValidationException : Exception
    {
        public TodoValidationException(string message) : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class PersistenceException : Exception
    {
        public PersistenceException(string message) : base(message) { }
    }
}
