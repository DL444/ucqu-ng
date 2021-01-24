using System;
using System.Collections.Generic;
using System.Linq;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal struct ScoreSetViewModel
    {
        public ScoreSetViewModel(ScoreSet scoreSet)
        {
            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            GradePoint = scoreSet.GradePoint;
            GradePointDisplay = locService.Format("OverallGradePointFormat", scoreSet.GradePoint);
            Terms = new CollectionViewSource()
            {
                IsSourceGrouped = true,
                ItemsPath = new PropertyPath("Courses"),
                Source = scoreSet.Terms.Reverse<Term>().Select(x => new TermViewModel(x, locService)).ToList()
            };
        }

        public double GradePoint { get; }
        public string GradePointDisplay { get; }

        public CollectionViewSource Terms { get; }
    }

    internal struct TermViewModel
    {
        public TermViewModel(Term term, ILocalizationService locService)
        {
            string termNotation = locService.GetString($"TermNumberNotation{term.TermNumber}");
            DisplayName = locService.Format("TermDisplayNameFormat", term.BeginningYear, term.EndingYear, termNotation);
            GradePoint = term.GradePoint;
            GradePointDisplay = locService.Format("TermGradePointFormat", term.GradePoint);
            Courses = term.Courses.Select(x => new CourseViewModel(x)).ToList();
        }

        public string DisplayName { get; }
        public double GradePoint { get; }
        public string GradePointDisplay { get; }
        public List<CourseViewModel> Courses { get; }
    }

    internal struct CourseViewModel
    {
        public CourseViewModel(Course course)
        {
            ShortName = course.ShortName;
            Credit = course.Credit;
            IsInitialTake = course.IsInitialTake;
            Score = course.Score;
            Comment = course.Comment;
            ShortLecturer = course.ShortLecturer;
            GradePoint = course.GradePoint;
        }

        public string ShortName { get; }
        public double Credit { get; }
        public bool IsInitialTake { get; }
        public int Score { get; }
        public string Comment { get; }
        public string ShortLecturer { get; }
        public double GradePoint { get; }
    }
}
