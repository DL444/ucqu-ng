using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        /// <summary>
        /// Get current signed in user's scores.
        /// </summary>
        /// <param name="isSecondMajor">Whether to get major or second major scores.</param>
        /// <returns>User's scores.</returns>
        /// <exception cref="FormatException">Thrown when page responsed by server cannot be correctly parsed.</exception>
        public async Task<ScoreSet?> GetScoreAsync(bool isSecondMajor)
        {
            if (string.IsNullOrEmpty(signedInUser))
            {
                throw new InvalidOperationException("Currently not signed in.");
            }
            var request = new HttpRequestMessage(HttpMethod.Post, "XSCJ/f_xsgrcj_rpt.aspx").AddSessionCookie(sessionId);
            Dictionary<string, string> content = new Dictionary<string, string>()
            {
                { "SJ", "0" },
                { "SelXNXQ", "0" },
                { "txt_xm", signedInUser },
                { "txt_xs", signedInUser },
                { "sel_xs", signedInUser },
                { "zfx_flag", isSecondMajor ? "1" : "0" }
            };
            request.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string page = await response.Content.ReadAsStringAsync();
            return ParsePage(page, isSecondMajor);
        }

        private ScoreSet? ParsePage(string page, bool isSecondMajor)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return null;
            }
            string studentId = GetScorePageProperty(page, "学号", "&") ?? signedInUser;
            string? name = GetScorePageProperty(page, "姓名", "&");
            if (name == null)
            {
                return null;
            }
            _ = int.TryParse(GetScorePageProperty(page, "年级", "&"), out int admissionYear);
            ScoreSet set = new ScoreSet(studentId, name, admissionYear)
            {
                Major = GetScorePageProperty(page, "专业", "&"),
                ManagementClass = GetScorePageProperty(page, "行政班级", "<"),
                IsSecondMajor = isSecondMajor,
                Terms = GetTerms(page)
            };

            string substring = page;
            int startIndex = page.IndexOf("<table");
            if (startIndex < 0)
            {
                throw new FormatException("Cannot find score tables in the document.");
            }
            substring = substring.Substring(startIndex + 1);
            Regex tableStartRegex = new Regex("<table.*?<table.*?>(.*?)</table(.*)", RegexOptions.CultureInvariant);
            Regex trRegex = new Regex("<tr .*?>(.*?)</tr>", RegexOptions.CultureInvariant);
            Regex tdRegex = new Regex("<td .*?>(.*?)<br></td>", RegexOptions.CultureInvariant);
            foreach (Term term in set.Terms)
            {
                Match tableMatch = tableStartRegex.Match(substring);
                if (!tableMatch.Success)
                {
                    throw new FormatException("Encountered unexpected score table format: cannot locate tables.");
                }
                string currentTable = tableMatch.FirstGroupValue();
                MatchCollection rows = trRegex.Matches(currentTable);
                for (int i = 1; i < rows.Count; i++)
                {
                    string row = rows[i].FirstGroupValue();
                    MatchCollection cols = tdRegex.Matches(row);
                    if (cols.Count < 11)
                    {
                        throw new FormatException("Encountered unexpected score table format: too few columns.");
                    }
                    Course course = new Course(name: cols[1].FirstGroupValue())
                    {
                        Credit = double.Parse(cols[2].FirstGroupValue()),
                        Category = cols[3].FirstGroupValue(),
                        IsInitialTake = !"重修".Equals(cols[5].FirstGroupValue(), StringComparison.Ordinal),
                        IsSecondMajor = "辅修".Equals(cols[7].FirstGroupValue(), StringComparison.Ordinal),
                        Comment = cols[8].FirstGroupValue(),
                        Lecturer = cols[9].FirstGroupValue(),
                        ObtainedTime = DateTimeOffset.Parse($"{cols[10].FirstGroupValue()} +8")
                    };
                    int score;
                    switch (cols[6].FirstGroupValue())
                    {
                        case "优秀":
                            score = 95;
                            break;
                        case "良好":
                            score = 85;
                            break;
                        case "中等":
                            score = 75;
                            break;
                        case "及格":
                            score = 65;
                            break;
                        case "不及格":
                            score = 50;
                            break;
                        case "合格":
                            score = 85;
                            break;
                        case "不合格":
                            score = 50;
                            break;
                        default:
                            score = (int)double.Parse(cols[6].FirstGroupValue());
                            break;
                    }
                    course.Score = score;
                    term.Courses.Add(course);
                }
                substring = tableMatch.Groups[2].Value;
            }
            return set;
        }

        private string? GetScorePageProperty(string page, string name, string ending)
        {
            Regex regex = new Regex($"{name}：(.*?){ending}", RegexOptions.CultureInvariant);
            Match match = regex.Match(page);
            if (!match.Success)
            {
                return null;
            }
            return match.FirstGroupValue();
        }

        private List<Term> GetTerms(string page)
        {
            Regex regex = new Regex("学年学期：(.*?)<", RegexOptions.CultureInvariant);
            MatchCollection matches = regex.Matches(page);
            List<Term> terms = new List<Term>(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = matches[i].FirstGroupValue();
                int beginYear = int.Parse(raw.Substring(0, 4));
                Regex termIdRegex = new Regex("第(.)", RegexOptions.CultureInvariant);
                Match termIdMatch = termIdRegex.Match(raw);
                if (!termIdMatch.Success)
                {
                    throw new FormatException("Term ID not found.");
                }
                int termId;
                switch (termIdMatch.FirstGroupValue())
                {
                    case "一":
                        termId = 0;
                        break;
                    case "二":
                        termId = 1;
                        break;
                    case "三":
                        termId = 2;
                        break;
                    case "四":
                        termId = 3;
                        break;
                    default:
                        throw new FormatException($"Unexpected term ID: {termIdMatch.FirstGroupValue()}");
                }
                Term term = new Term()
                {
                    BeginningYear = beginYear,
                    TermNumber = termId
                };
                terms.Add(term);
            }
            return terms;
        }
    }
}
