namespace WpfLibrary1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class Officer
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Patronymic { get; set; }
        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        public int? RankId { get; set; }
        [ForeignKey(nameof(RankId))]
        public OfficerRank? Rank { get; set; }

        public int? DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }
        [MaxLength(200)]
        public string? Email { get; set; }
        [MaxLength(50)]
        public string? Phone { get; set; }
        public DateTime? HiredDate { get; set; }

        public bool CanBeLead { get; set; } = true;

        public ICollection<CaseRecord>? CasesLed { get; set; }
        public ICollection<Evidence>? CollectedEvidence { get; set; }
        public User? User { get; set; }
    }

    public partial class Officer
    {
        [NotMapped]
        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Patronymic))
                    return $"{LastName} {FirstName}".Trim();
                return $"{LastName} {FirstName} {Patronymic}".Trim();
            }
        }
    }

    public class Department
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(250)]
        public string? Street { get; set; }
        [MaxLength(50)]
        public string? House { get; set; }
        [MaxLength(100)]
        public string? City { get; set; }
        [NotMapped]
        public string? Address
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(Street)) parts.Add(Street);
                if (!string.IsNullOrWhiteSpace(House)) parts.Add(House);
                if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
                return parts.Count == 0 ? null : string.Join(", ", parts);
            }
        }
        [MaxLength(50)]
        public string? Phone { get; set; }

        public ICollection<Officer>? Officers { get; set; }
    }

    public enum CaseStatus
    {
        Open = 0,
        InProgress = 1,
        Closed = 2,
        Archived = 3
    }

    public class CaseRecord
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string CaseNumber { get; set; } = string.Empty;
        [MaxLength(250)]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int StatusId { get; set; }
        [NotMapped]
        public CaseStatus Status
        {
            get => (CaseStatus)StatusId;
            set => StatusId = (int)value;
        }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int? LeadOfficerId { get; set; }
        [ForeignKey(nameof(LeadOfficerId))]
        public Officer? LeadOfficer { get; set; }

        public int? LocationId { get; set; }
        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }

        public ICollection<Suspect>? Suspects { get; set; }
        public ICollection<CaseStatusHistory>? StatusHistories { get; set; }
        public ICollection<Evidence>? EvidenceItems { get; set; }
    }

    public partial class Suspect
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Patronymic { get; set; }
        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [NotMapped]
        public string AppearedInOtherCasesText { get; set; } = "Нет";

        public ICollection<CaseRecord>? Cases { get; set; }
    }

    public partial class Suspect
    {
        [NotMapped]
        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Patronymic))
                    return $"{LastName} {FirstName}".Trim();
                return $"{LastName} {FirstName} {Patronymic}".Trim();
            }
        }
    }

    public class CaseStatusHistory
    {
        [Key]
        public int Id { get; set; }
        public int CaseRecordId { get; set; }
        [ForeignKey(nameof(CaseRecordId))]
        public CaseRecord? CaseRecord { get; set; }

        public int StatusId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    public class Evidence
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Tag { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CollectedAt { get; set; }

        public int? CollectedByOfficerId { get; set; }
        [ForeignKey(nameof(CollectedByOfficerId))]
        public Officer? CollectedBy { get; set; }

        public int? CaseRecordId { get; set; }
        [ForeignKey(nameof(CaseRecordId))]
        public CaseRecord? CaseRecord { get; set; }
    }

    public class Location
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(250)]
        public string? Street { get; set; }
        [MaxLength(50)]
        public string? House { get; set; }
        [MaxLength(100)]
        public string? City { get; set; }
        
        public ICollection<CaseRecord>? Cases { get; set; }
    }

    public class OfficerRank
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Officer>? Officers { get; set; }
    }

    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<User>? Users { get; set; }
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        public byte[]? PasswordHash { get; set; }


        public int? OfficerId { get; set; }
        [ForeignKey(nameof(OfficerId))]
        public Officer? Officer { get; set; }

        public int? RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }
    }
}
