using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class StudentInfoViewModel
    {
        public StudentInfoViewModel() : this(new StudentInfo()) { }
        public StudentInfoViewModel(StudentInfo info) => this.info = info;
        
        public string Name => info.Name;
        public string Major => info.Major;
        public bool HasSecondMajor => info.SecondMajor != null;
        public string SecondMajor => info.SecondMajor;

        private StudentInfo info;
    }
}
