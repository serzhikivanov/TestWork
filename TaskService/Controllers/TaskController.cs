using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskService.Domain.Data;
using TaskService.Domain.DTOs;
using TaskService.Domain.Models;

namespace TaskService.Controllers
{   
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TasksController> _logger;

        public TasksController(AppDbContext db, ILogger<TasksController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid task data - {dto}");
                return BadRequest(ModelState);
            }

            var task = new TaskModel
            {
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Status = dto.Status
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Added new task {dto}");

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) 
                return NotFound();

            return Ok(task);
        }

        // GET: api/tasks
        // supports pagination: ?page=1&pageSize=20
        // filter by status: ?status=New
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] TaskStatus? status = null)
        {
            if (page <= 0) 
                page = 1;

            if (pageSize <= 0 || pageSize > 100) 
                pageSize = 20;

            var query = _db.Tasks.AsQueryable();
            if (status.HasValue) 
                query = query.Where(t => (int)t.Status == (int)status.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new { total, page, pageSize, items };
            return Ok(result);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) 
                return NotFound();

            var updated = false;
            if (!string.IsNullOrWhiteSpace(dto.Title) && dto.Title != task.Title)
            {
                task.Title = dto.Title;
                updated = true;
            }

            if (dto.Description != null && dto.Description != task.Description)
            {
                task.Description = dto.Description;
                updated = true;
            }

            if (dto.DueDate.HasValue && dto.DueDate.Value != task.DueDate)
            {
                task.DueDate = dto.DueDate.Value;
                updated = true;
            }

            if (dto.Status.HasValue && dto.Status.Value != task.Status)
            {
                task.Status = dto.Status.Value;
                updated = true;
            }

            if (!updated) 
                return BadRequest("No changes detected.");


            task.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(task);
        }


        // DELETE: api/tasks/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();


            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();


            return NoContent();
        }
    }
}
