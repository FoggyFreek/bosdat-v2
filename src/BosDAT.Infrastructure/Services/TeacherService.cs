using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class TeacherService(IUnitOfWork unitOfWork, ApplicationDbContext context) : ITeacherService
{
    public async Task<List<TeacherListDto>> GetAllAsync(
        bool? activeOnly,
        int? instrumentId,
        Guid? courseTypeId,
        CancellationToken ct = default)
    {
        IQueryable<Teacher> query = unitOfWork.Teachers.Query()
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherCourseTypes)
                .ThenInclude(tlt => tlt.CourseType);

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId.Value));
        }

        if (courseTypeId.HasValue)
        {
            query = query.Where(t => t.TeacherCourseTypes.Any(tlt => tlt.CourseTypeId == courseTypeId.Value));
        }

        return await query
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(t => new TeacherListDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                IsActive = t.IsActive,
                Role = t.Role,
                Instruments = t.TeacherInstruments.Select(ti => ti.Instrument.Name).ToList(),
                CourseTypes = t.TeacherCourseTypes.Select(tlt => tlt.CourseType.Name).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<TeacherDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, ct);

        if (teacher == null)
        {
            return null;
        }

        return MapToDto(teacher);
    }

    public async Task<(TeacherDto Teacher, List<CourseListDto> Courses)?> GetWithCoursesAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetWithCoursesAsync(id, ct);

        if (teacher == null)
        {
            return null;
        }

        var teacherDto = MapToDto(teacher);
        var courses = teacher.Courses.Select(c => new CourseListDto
        {
            Id = c.Id,
            TeacherName = teacher.FullName,
            CourseTypeName = c.CourseType.Name,
            InstrumentName = c.CourseType.Instrument.Name,
            RoomName = c.Room?.Name,
            DayOfWeek = c.DayOfWeek,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Frequency = c.Frequency,
            WeekParity = c.WeekParity,
            Status = c.Status,
            EnrollmentCount = c.Enrollments.Count
        }).ToList();

        return (teacherDto, courses);
    }

    public async Task<TeacherDto> CreateAsync(CreateTeacherDto dto, CancellationToken ct = default)
    {
        // Check for duplicate email
        var existing = await unitOfWork.Teachers.GetByEmailAsync(dto.Email, ct);
        if (existing != null)
        {
            throw new InvalidOperationException("A teacher with this email already exists");
        }

        // Validate lesson types
        if (dto.CourseTypeIds.Count > 0)
        {
            var courseTypes = await context.CourseTypes
                .Where(lt => dto.CourseTypeIds.Contains(lt.Id))
                .ToListAsync(ct);

            // Check all lesson types exist
            if (courseTypes.Count != dto.CourseTypeIds.Count)
            {
                throw new InvalidOperationException("One or more lesson types not found");
            }

            // Check all lesson types are active
            var inactiveCourseTypes = courseTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveCourseTypes.Count > 0)
            {
                throw new InvalidOperationException($"Cannot assign inactive lesson types: {string.Join(", ", inactiveCourseTypes.Select(lt => lt.Name))}");
            }

            // Check lesson type instruments match teacher's instruments
            var mismatchedCourseTypes = courseTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedCourseTypes.Count > 0)
            {
                throw new InvalidOperationException($"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedCourseTypes.Select(lt => lt.Name))}");
            }
        }

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Prefix = dto.Prefix,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            HourlyRate = dto.HourlyRate,
            Role = dto.Role,
            Notes = dto.Notes,
            IsActive = true
        };

        await unitOfWork.Teachers.AddAsync(teacher, ct);

        // Add instruments
        foreach (var instrumentId in dto.InstrumentIds)
        {
            context.TeacherInstruments.Add(new TeacherInstrument
            {
                TeacherId = teacher.Id,
                InstrumentId = instrumentId
            });
        }

        // Add lesson types
        foreach (var courseTypeId in dto.CourseTypeIds)
        {
            context.TeacherCourseTypes.Add(new TeacherCourseType
            {
                TeacherId = teacher.Id,
                CourseTypeId = courseTypeId
            });
        }

        await unitOfWork.SaveChangesAsync(ct);

        // Reload with instruments and lesson types
        var createdTeacher = await unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(teacher.Id, ct);

        return MapToDto(createdTeacher!);
    }

    public async Task<TeacherDto?> UpdateAsync(Guid id, UpdateTeacherDto dto, CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, ct);

        if (teacher == null)
        {
            return null;
        }

        // Check for duplicate email (excluding current teacher)
        var existing = await unitOfWork.Teachers.GetByEmailAsync(dto.Email, ct);
        if (existing != null && existing.Id != id)
        {
            throw new InvalidOperationException("A teacher with this email already exists");
        }

        // Validate new lesson types
        if (dto.CourseTypeIds.Count > 0)
        {
            var courseTypes = await context.CourseTypes
                .Where(lt => dto.CourseTypeIds.Contains(lt.Id))
                .ToListAsync(ct);

            // Check all lesson types exist
            if (courseTypes.Count != dto.CourseTypeIds.Count)
            {
                throw new InvalidOperationException("One or more lesson types not found");
            }

            // Check all lesson types are active
            var inactiveCourseTypes = courseTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveCourseTypes.Count > 0)
            {
                throw new InvalidOperationException($"Cannot assign inactive lesson types: {string.Join(", ", inactiveCourseTypes.Select(lt => lt.Name))}");
            }

            // Check lesson type instruments match teacher's new instruments
            var mismatchedCourseTypes = courseTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedCourseTypes.Count > 0)
            {
                throw new InvalidOperationException($"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedCourseTypes.Select(lt => lt.Name))}");
            }
        }

        teacher.FirstName = dto.FirstName;
        teacher.LastName = dto.LastName;
        teacher.Prefix = dto.Prefix;
        teacher.Email = dto.Email;
        teacher.Phone = dto.Phone;
        teacher.Address = dto.Address;
        teacher.PostalCode = dto.PostalCode;
        teacher.City = dto.City;
        teacher.HourlyRate = dto.HourlyRate;
        teacher.IsActive = dto.IsActive;
        teacher.Role = dto.Role;
        teacher.Notes = dto.Notes;

        // Update instruments
        var currentInstruments = teacher.TeacherInstruments.ToList();
        var instrumentsToRemove = currentInstruments.Where(ti => !dto.InstrumentIds.Contains(ti.InstrumentId)).ToList();
        var instrumentsToAdd = dto.InstrumentIds.Where(iid => !currentInstruments.Any(ti => ti.InstrumentId == iid));

        foreach (var ti in instrumentsToRemove)
        {
            context.TeacherInstruments.Remove(ti);
        }

        foreach (var instrumentId in instrumentsToAdd)
        {
            context.TeacherInstruments.Add(new TeacherInstrument
            {
                TeacherId = teacher.Id,
                InstrumentId = instrumentId
            });
        }

        // Get instrument IDs being removed
        var removedInstrumentIds = instrumentsToRemove.Select(ti => ti.InstrumentId).ToHashSet();

        // Update lesson types (also cascade-remove any lesson types whose instrument was removed)
        var currentCourseTypes = teacher.TeacherCourseTypes.ToList();
        var courseTypesToRemove = currentCourseTypes
            .Where(tlt => !dto.CourseTypeIds.Contains(tlt.CourseTypeId) || removedInstrumentIds.Contains(tlt.CourseType.InstrumentId))
            .ToList();
        var courseTypesToAdd = dto.CourseTypeIds
            .Where(ltid => !currentCourseTypes.Any(tlt => tlt.CourseTypeId == ltid));

        foreach (var tlt in courseTypesToRemove)
        {
            context.TeacherCourseTypes.Remove(tlt);
        }

        foreach (var courseTypeId in courseTypesToAdd)
        {
            context.TeacherCourseTypes.Add(new TeacherCourseType
            {
                TeacherId = teacher.Id,
                CourseTypeId = courseTypeId
            });
        }

        await unitOfWork.SaveChangesAsync(ct);

        // Reload with updated instruments and lesson types
        var updatedTeacher = await unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, ct);

        return MapToDto(updatedTeacher!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetByIdAsync(id, ct);

        if (teacher == null)
        {
            return false;
        }

        // Instead of deleting, deactivate
        teacher.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }

    public async Task<List<TeacherAvailabilityDto>?> GetAvailabilityAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetByIdAsync(id, ct);
        if (teacher == null)
        {
            return null;
        }

        var availability = await unitOfWork.Teachers.GetAvailabilityAsync(id, ct);

        return availability.Select(a => new TeacherAvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = a.DayOfWeek,
            FromTime = a.FromTime,
            UntilTime = a.UntilTime
        }).ToList();
    }

    public async Task<List<TeacherAvailabilityDto>?> UpdateAvailabilityAsync(
        Guid id,
        List<UpdateTeacherAvailabilityDto> dtos,
        CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetWithAvailabilityAsync(id, ct);
        if (teacher == null)
        {
            return null;
        }

        // Validate: max 7 entries
        if (dtos.Count > 7)
        {
            throw new InvalidOperationException("Maximum of 7 availability entries allowed (one per day)");
        }

        // Validate: no duplicate days
        var duplicateDays = dtos.GroupBy(d => d.DayOfWeek).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateDays.Any())
        {
            throw new InvalidOperationException($"Duplicate days are not allowed: {string.Join(", ", duplicateDays)}");
        }

        // Validate: time range (UntilTime >= FromTime + 1 hour, unless both 00:00 for unavailable)
        foreach (var dto in dtos)
        {
            var isUnavailable = dto.FromTime == TimeOnly.MinValue && dto.UntilTime == TimeOnly.MinValue;
            if (!isUnavailable)
            {
                var minEndTime = dto.FromTime.AddHours(1);
                if (dto.UntilTime < minEndTime)
                {
                    throw new InvalidOperationException($"End time must be at least 1 hour after start time for {dto.DayOfWeek}. Use 00:00-00:00 to mark as unavailable.");
                }
            }
        }

        // Remove existing availability
        var existingAvailability = teacher.Availability.ToList();
        foreach (var existing in existingAvailability)
        {
            context.TeacherAvailabilities.Remove(existing);
        }

        // Add new availability
        var newAvailability = new List<TeacherAvailability>();
        foreach (var dto in dtos)
        {
            var availability = new TeacherAvailability
            {
                Id = Guid.NewGuid(),
                TeacherId = id,
                DayOfWeek = dto.DayOfWeek,
                FromTime = dto.FromTime,
                UntilTime = dto.UntilTime
            };
            newAvailability.Add(availability);
            context.TeacherAvailabilities.Add(availability);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return newAvailability
            .OrderBy(a => a.DayOfWeek)
            .Select(a => new TeacherAvailabilityDto
            {
                Id = a.Id,
                DayOfWeek = a.DayOfWeek,
                FromTime = a.FromTime,
                UntilTime = a.UntilTime
            })
            .ToList();
    }

    public async Task<List<CourseTypeSimpleDto>?> GetAvailableCourseTypesAsync(
        Guid id,
        string? instrumentIds,
        CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetByIdAsync(id, ct);
        if (teacher == null)
        {
            return null;
        }

        // Parse instrument IDs from query string (comma-separated)
        var instrumentIdList = string.IsNullOrEmpty(instrumentIds)
            ? new List<int>()
            : instrumentIds.Split(',').Select(int.Parse).ToList();

        if (instrumentIdList.Count == 0)
        {
            return new List<CourseTypeSimpleDto>();
        }

        return await context.CourseTypes
            .Include(lt => lt.Instrument)
            .Where(lt => lt.IsActive && instrumentIdList.Contains(lt.InstrumentId))
            .OrderBy(lt => lt.Instrument.Name)
            .ThenBy(lt => lt.Name)
            .Select(lt => new CourseTypeSimpleDto
            {
                Id = lt.Id,
                Name = lt.Name,
                InstrumentId = lt.InstrumentId,
                InstrumentName = lt.Instrument.Name,
                DurationMinutes = lt.DurationMinutes,
                Type = lt.Type
            })
            .ToListAsync(ct);
    }

    private static TeacherDto MapToDto(Teacher teacher)
    {
        return new TeacherDto
        {
            Id = teacher.Id,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Prefix = teacher.Prefix,
            FullName = teacher.FullName,
            Email = teacher.Email,
            Phone = teacher.Phone,
            Address = teacher.Address,
            PostalCode = teacher.PostalCode,
            City = teacher.City,
            HourlyRate = teacher.HourlyRate,
            IsActive = teacher.IsActive,
            Role = teacher.Role,
            Notes = teacher.Notes,
            Instruments = teacher.TeacherInstruments.Select(ti => new InstrumentDto
            {
                Id = ti.Instrument.Id,
                Name = ti.Instrument.Name,
                Category = ti.Instrument.Category,
                IsActive = ti.Instrument.IsActive
            }).ToList(),
            CourseTypes = teacher.TeacherCourseTypes.Select(tlt => new CourseTypeSimpleDto
            {
                Id = tlt.CourseType.Id,
                Name = tlt.CourseType.Name,
                InstrumentId = tlt.CourseType.InstrumentId,
                InstrumentName = tlt.CourseType.Instrument?.Name ?? "",
                DurationMinutes = tlt.CourseType.DurationMinutes,
                Type = tlt.CourseType.Type
            }).ToList(),
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}
