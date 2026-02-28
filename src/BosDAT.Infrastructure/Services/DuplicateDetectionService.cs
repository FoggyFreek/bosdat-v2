using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _context;

    public DuplicateDetectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DuplicateCheckResultDto> CheckForDuplicatesAsync(
        CheckDuplicatesDto dto,
        CancellationToken cancellationToken = default)
    {
        var duplicates = new List<DuplicateMatchDto>();

        var query = _context.Students.AsQueryable();

        // Exclude the current student when editing
        if (dto.ExcludeId.HasValue)
        {
            query = query.Where(s => s.Id != dto.ExcludeId.Value);
        }

        var potentialMatches = await query.ToListAsync(cancellationToken);

        foreach (var student in potentialMatches)
        {
            var (score, reasons) = CalculateMatchScore(dto, student);

            if (score >= 50) // Threshold for considering it a potential duplicate
            {
                duplicates.Add(new DuplicateMatchDto
                {
                    Id = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,
                    Phone = student.Phone,
                    Status = student.Status,
                    ConfidenceScore = score,
                    MatchReason = string.Join("; ", reasons)
                });
            }
        }

        // Sort by confidence score descending
        var sortedDuplicates = duplicates
            .OrderByDescending(d => d.ConfidenceScore)
            .ToList();

        return new DuplicateCheckResultDto
        {
            HasDuplicates = sortedDuplicates.Count > 0,
            Duplicates = sortedDuplicates
        };
    }

    private static (int Score, List<string> Reasons) CalculateMatchScore(CheckDuplicatesDto dto, Student student)
    {
        var score = 0;
        var reasons = new List<string>();

        score += CheckEmailMatch(dto, student, reasons);
        score += CheckNameMatch(dto, student, reasons);
        score += CheckPhoneMatch(dto, student, reasons);
        score += CheckDateOfBirthMatch(dto, student, reasons);

        return (Math.Min(score, 100), reasons);
    }

    private static int CheckEmailMatch(CheckDuplicatesDto dto, Student student, List<string> reasons)
    {
        if (string.Equals(dto.Email, student.Email, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Exact email match");
            return 100;
        }
        return 0;
    }

    private static int CheckNameMatch(CheckDuplicatesDto dto, Student student, List<string> reasons)
    {
        var inputFullName = $"{dto.FirstName} {dto.LastName}".ToLowerInvariant();
        var studentFullName = $"{student.FirstName} {student.LastName}".ToLowerInvariant();

        if (inputFullName == studentFullName)
        {
            reasons.Add("Exact name match");
            return 60;
        }

        var nameSimilarity = CalculateNameSimilarity(dto.FirstName, dto.LastName, student.FirstName, student.LastName);
        if (nameSimilarity >= 0.8)
        {
            reasons.Add("Similar name");
            return 40;
        }

        if (nameSimilarity >= 0.6)
        {
            reasons.Add("Partially similar name");
            return 20;
        }

        return 0;
    }

    private static int CheckPhoneMatch(CheckDuplicatesDto dto, Student student, List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(student.Phone))
            return 0;

        var normalizedInputPhone = NormalizePhoneNumber(dto.Phone);
        var normalizedStudentPhone = NormalizePhoneNumber(student.Phone);
        var normalizedStudentPhoneAlt = student.PhoneAlt != null ? NormalizePhoneNumber(student.PhoneAlt) : null;

        if (normalizedInputPhone == normalizedStudentPhone || normalizedInputPhone == normalizedStudentPhoneAlt)
        {
            reasons.Add("Phone number match");
            return 50;
        }

        return 0;
    }

    private static int CheckDateOfBirthMatch(CheckDuplicatesDto dto, Student student, List<string> reasons)
    {
        if (!dto.DateOfBirth.HasValue || !student.DateOfBirth.HasValue)
            return 0;

        if (dto.DateOfBirth.Value != student.DateOfBirth.Value)
            return 0;

        var nameSimilarity = CalculateNameSimilarity(dto.FirstName, dto.LastName, student.FirstName, student.LastName);
        if (nameSimilarity >= 0.5)
        {
            reasons.Add("Same date of birth with similar name");
            return 40;
        }

        return 0;
    }

    private static double CalculateNameSimilarity(string firstName1, string lastName1, string firstName2, string lastName2)
    {
        var firstNameSimilarity = CalculateStringSimilarity(firstName1.ToLowerInvariant(), firstName2.ToLowerInvariant());
        var lastNameSimilarity = CalculateStringSimilarity(lastName1.ToLowerInvariant(), lastName2.ToLowerInvariant());

        // Weight last name slightly more as it's typically more unique
        return (firstNameSimilarity * 0.4) + (lastNameSimilarity * 0.6);
    }

    private static double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        if (s1 == s2)
            return 1;

        // Use Levenshtein distance for similarity
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s2[j - 1] == s1[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
