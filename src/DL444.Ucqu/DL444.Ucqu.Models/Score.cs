using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class ScoreSet : IStatusResource, ICosmosResource
    {
        public ScoreSet(string studentId)
        {
            StudentId = studentId;
        }

        public string Id() => $"Score-{StudentId}-{(IsSecondMajor ? "S" : "M")}";
        public string PartitionKey() => StudentId;
        public RecordStatus RecordStatus { get; set; }

        public string StudentId { get; set; }
        public bool IsSecondMajor { get; set; }
        public List<Term> Terms { get; set; } = new List<Term>();
    }

    public class Term
    {
        public int BeginningYear { get; set; }
        [JsonInclude]
        public int EndingYear => BeginningYear + 1;
        public int TermNumber { get; set; }
        [JsonInclude]
        public double GradePoint
        {
            get
            {
                double sumGp = 0.0;
                double sumCredit = 0.0;
                foreach (var course in Courses)
                {
                    if (course.GradePoint == 0.0)
                    {
                        continue;
                    }
                    sumGp += course.GradePoint * course.Credit;
                    sumCredit += course.Credit;
                }
                return sumCredit == 0.0 ? 0.0 : sumGp / sumCredit;
            }
        }

        public List<Course> Courses { get; set; } = new List<Course>();
    }

    public class Course
    {
        public Course(string name) => Name = name;
        
        public string Name { get; set; }
        [JsonInclude]
        public string ShortName => Utilities.GetShortformName(Name);
        public double Credit { get; set; }
        public string? Category { get; set; }
        public bool IsInitialTake { get; set; }
        public bool IsMakeup
            => "补考".Equals(Comment, StringComparison.Ordinal)
            || "补考(缺考)".Equals(Comment, StringComparison.Ordinal);
        public int Score { get; set; }
        public bool IsSecondMajor { get; set; }
        public string? Comment { get; set; }
        public string? Lecturer { get; set; }
        [JsonInclude]
        public string ShortLecturer => Utilities.GetShortformName(Lecturer);
        public DateTimeOffset ObtainedTime { get; set; }
        [JsonInclude]
        public double GradePoint
        {
            get
            {
                if (GradePointTruncated)
                {
                    return Score < 60 ? 0.0 : 1.0;
                }

                if (Score > 90)
                {
                    return 4.0;
                }
                else if (Score < 60)
                {
                    return 0.0;
                }
                else
                {
                    return 1.0 + 0.1 * (Score - 60);
                }
            }
        }

        private bool GradePointTruncated => IsInitialTake == false
            || "补考".Equals(Comment, StringComparison.Ordinal)
            || "缺考".Equals(Comment, StringComparison.Ordinal)
            || "补考(缺考)".Equals(Comment, StringComparison.Ordinal);
    }
}
