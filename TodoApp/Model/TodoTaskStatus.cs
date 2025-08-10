
using System.ComponentModel.DataAnnotations;

namespace TodoApp.Model
{
    public enum TodoTaskStatus
    {
        [Display(Name = "Todo")]
        Todo,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Complete")]
        Complete
    }
}
