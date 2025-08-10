using System.ComponentModel.DataAnnotations;

namespace TodoApp.Model
{
    public class TodoTask
    {
        [Display(Name = "ID", Order = 0)]
        public int Id { get; set; }

        [Display(Order = 3)]
        public TodoTaskStatus Status { get; set; }

        [Display(Order = 1)]
        public string Name { get; set; }

        [Display(Order = 2)]
        public string Owner { get; set; }

        [Display(Order = 4)]
        public string Description { get; set; }
    }
}
