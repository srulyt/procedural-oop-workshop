namespace TodoApp.Model
{
    public class TodoTask
    {
        public int Id { get; set; }
        public TodoTaskStatus Status { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
    }
}
