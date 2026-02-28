using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class CourseTaskService(IUnitOfWork unitOfWork) : ICourseTaskService
{
    public async Task<IEnumerable<CourseTaskDto>> GetByCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<CourseTask>().Query()
            .Where(t => t.CourseId == courseId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new CourseTaskDto
            {
                Id = t.Id,
                CourseId = t.CourseId,
                Title = t.Title,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<CourseTaskDto?> CreateAsync(Guid courseId, CreateCourseTaskDto dto, CancellationToken ct = default)
    {
        var courseExists = await unitOfWork.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
        {
            return null;
        }

        var task = new CourseTask
        {
            CourseId = courseId,
            Title = dto.Title
        };

        await unitOfWork.Repository<CourseTask>().AddAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CourseTaskDto
        {
            Id = task.Id,
            CourseId = task.CourseId,
            Title = task.Title,
            CreatedAt = task.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await unitOfWork.Repository<CourseTask>().GetByIdAsync(taskId, ct);
        if (task == null)
        {
            return false;
        }

        await unitOfWork.Repository<CourseTask>().DeleteAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
