using System.ComponentModel.DataAnnotations;

namespace TaskService.Domain.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public JobTaskStatus Status { get; set; } = JobTaskStatus.New;

        public override string ToString()
        {
            return $"{Title} - '{Description}' - {DueDate} - {Status}";
        }
    }
}
