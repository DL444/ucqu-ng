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
        public async Task<ExamSchedule> GetExamScheduleAsync(SignInContext signInContext, int beginningYear, int term)
        {
            if (!signInContext.IsValid)
            {
                throw new InvalidOperationException("Currently not signed in.");
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "KSSW/stu_ksap_rpt.aspx").AddSessionCookie(signInContext.SessionId!);
            Dictionary<string, string> content = new Dictionary<string, string>()
            {
                { "sel_xnxq", $"{beginningYear}{term}" }
            };
            request.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string page = await response.Content.ReadAsStringAsync();
            return ParseExams(page, signInContext.SignedInUser!);
        }

        private ExamSchedule ParseExams(string page, string signedInUser)
        {
            if (string.IsNullOrEmpty(page))
            {
                throw new FormatException("Page is empty.");
            }
            ExamSchedule examSchedule = new ExamSchedule(signedInUser);
            Regex trRegex = new Regex("<tr class=.>.*?</tr>", RegexOptions.CultureInvariant);
            Regex tdRegex = new Regex("<td .*?>(.*?)<br></td>", RegexOptions.CultureInvariant);
            Regex timeRegex = new Regex("(.*?)\\((.*?)周 星期(.)\\)(.*)-(.*)", RegexOptions.CultureInvariant);
            MatchCollection rows = trRegex.Matches(page);
            foreach (Match? row in rows)
            {
                if (row == null)
                {
                    continue;
                }
                MatchCollection cols = tdRegex.Matches(row.Value);
                Exam exam = new Exam(
                    name: cols[1].FirstGroupValue(),
                    location: cols[6].FirstGroupValue(),
                    seating: int.Parse(cols[7].FirstGroupValue())
                )
                {
                    Credit = double.Parse(cols[2].FirstGroupValue()),
                    Category = cols[3].FirstGroupValue(),
                    Type = cols[4].FirstGroupValue(),
                };
                string timeStr = cols[5].FirstGroupValue();
                Match timeMatch = timeRegex.Match(timeStr);
                if (!timeMatch.Success)
                {
                    throw new FormatException($"Unable to parse exam time. Got {timeStr}");
                }
                exam.Week = int.Parse(timeMatch.Groups[2].Value);
                exam.DayOfWeek = GetDayOfWeek(timeMatch.Groups[3].Value);
                DateTimeOffset date = DateTimeOffset.Parse($"{timeMatch.FirstGroupValue()} +8");
                TimeSpan startTimeSpan = TimeSpan.Parse(timeMatch.Groups[4].Value);
                TimeSpan endTimeSpan = TimeSpan.Parse(timeMatch.Groups[5].Value);
                exam.StartTime = date.Add(startTimeSpan);
                exam.EndTime = date.Add(endTimeSpan);
                examSchedule.Exams.Add(exam);
            }
            return examSchedule;
        }
    }
}
