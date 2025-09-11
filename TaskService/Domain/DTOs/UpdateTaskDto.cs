using System.ComponentModel.DataAnnotations;

namespace TaskService.Domain.DTOs
{
    public class UpdateTaskDto
    {
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? DueDate { get; set; }

        public JobTaskStatus? Status { get; set; }
    }
}
