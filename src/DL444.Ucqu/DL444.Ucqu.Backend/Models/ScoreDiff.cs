namespace DL444.Ucqu.Backend.Models
{
    public struct ScoreDiffItem
    {
        public string StudentId { get; set; }
        public ScoreDiffType DiffType { get; set; }
        public string ShortName { get; set; }
        public bool IsMakeup { get; set; }
        public int OldScore { get; set; }
        public int NewScore { get; set; }
    }

    public enum ScoreDiffType
    {
        Add = 0,
        Change = 1,
        Remove = 2
    }
}
