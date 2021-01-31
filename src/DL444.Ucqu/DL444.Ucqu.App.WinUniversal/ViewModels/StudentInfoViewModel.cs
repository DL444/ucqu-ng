using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal struct StudentInfoViewModel
    {
        public StudentInfoViewModel(StudentInfo info)
        {
            StudentId = info.StudentId;
            Name = info.Name;
            Major = info.Major;
            SecondMajor = info.SecondMajor;
        }

        public string StudentId { get; }
        public string Name { get; }
        public string Major { get; }
        public bool HasSecondMajor => !string.IsNullOrWhiteSpace(SecondMajor);
        public string SecondMajor { get; }
    }
}
