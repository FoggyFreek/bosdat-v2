using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Utilities;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class EnrollmentPricingService(
    ApplicationDbContext context,
    ICourseTypePricingService pricingService) : IEnrollmentPricingService
{
    public async Task<EnrollmentPricingDto?> GetEnrollmentPricingAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken ct = default)
    {
        var enrollment = await context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .Include(e => e.Course.Teacher)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, ct);

        if (enrollment == null)
        {
            return null;
        }

        var currentPricing = await pricingService.GetCurrentPricingAsync(
            enrollment.Course.CourseTypeId, ct);

        if (currentPricing == null)
        {
            return null;
        }

        var isChildPricing = IsoDateHelper.IsChild(enrollment.Student.DateOfBirth);
        var applicableBasePrice = isChildPricing
            ? currentPricing.PriceChild
            : currentPricing.PriceAdult;

        var discountAmount = applicableBasePrice * (enrollment.DiscountPercent / 100m);
        var pricePerLesson = applicableBasePrice - discountAmount;

        var courseName = $"{enrollment.Course.CourseType.Name} - {enrollment.Course.Teacher.FullName}";

        return new EnrollmentPricingDto
        {
            EnrollmentId = enrollment.Id,
            CourseId = enrollment.CourseId,
            CourseName = courseName,
            BasePriceAdult = currentPricing.PriceAdult,
            BasePriceChild = currentPricing.PriceChild,
            IsChildPricing = isChildPricing,
            ApplicableBasePrice = applicableBasePrice,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountAmount = discountAmount,
            PricePerLesson = pricePerLesson
        };
    }
}
