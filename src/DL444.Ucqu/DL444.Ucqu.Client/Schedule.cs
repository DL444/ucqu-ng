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
        public async Task<Schedule?> GetScheduleAsync(int beginningYear, int term)
        {
            if (string.IsNullOrEmpty(signedInUser))
            {
                throw new InvalidOperationException("Currently not signed in.");
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "znpk/Pri_StuSel_rpt.aspx");
            Dictionary<string, string> content = new Dictionary<string, string>()
            {
                { "Sel_XNXQ", $"{beginningYear}{term}" },
                { "rad", "on" },
                { "px", "1" }
            };
            request.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string page = await response.Content.ReadAsStringAsync();
            return ParseSchedule(page);
        }

        private Schedule? ParseSchedule(string page)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return null;
            }
            int peTable = -1;
            int expTable = -1;
            Regex headerRegex = new Regex("<div group=\"group\" class=\"page_group\".*?</div>", RegexOptions.CultureInvariant);
            MatchCollection headerMatches = headerRegex.Matches(page);
            for (int i = 0; i < headerMatches.Count; i++)
            {
                if (headerMatches[i].Value.Contains("体育课"))
                {
                    peTable = i;
                }
                else if (headerMatches[i].Value.Contains("实验"))
                {
                    expTable = i;
                }
            }
            Schedule schedule = new Schedule();
            Regex tableRegex = new Regex("<tbody>.*?</tbody>", RegexOptions.CultureInvariant);
            Regex trRegex = new Regex("<tr >.*?</tr>", RegexOptions.CultureInvariant);
            Regex tdRegex = new Regex("<td .*?>(.*?)<br></td>", RegexOptions.CultureInvariant);
            Regex sessionRegex = new Regex("(.)\\[((.*?)-(.*?)|(.*?))节\\]", RegexOptions.CultureInvariant);
            Regex weeksRegex = new Regex("((..?)|(..?)-(..?))[,;]", RegexOptions.CultureInvariant);
            MatchCollection tableMatches = tableRegex.Matches(page);
            for (int i = 0; i < tableMatches.Count; i++)
            {
                MatchCollection rows = trRegex.Matches(tableMatches[i].Value);
                foreach (Match row in rows)
                {
                    MatchCollection cells = tdRegex.Matches(row.Value);
                    ScheduleEntry entry;
                    string weeksStr, sessionsStr;
                    if (i == peTable)
                    {
                        entry = new ScheduleEntry()
                        {
                            Name = GetCellValue(cells[6]),
                            Lecturer = GetCellValue(cells[7]),
                            Room = GetCellValue(cells[10])
                        };
                        weeksStr = $"{GetCellValue(cells[8])},";
                        sessionsStr = GetCellValue(cells[9]);
                    }
                    else if (i == expTable)
                    {
                        entry = new ScheduleEntry()
                        {
                            Name = $"{GetCellValue(cells[1]).Split(']')[1]}实验",
                            Lecturer = GetCellValue(cells[7]),
                            Room = GetCellValue(cells[11])
                        };
                        weeksStr = $"{GetCellValue(cells[9])},";
                        sessionsStr = GetCellValue(cells[10]);
                    }
                    else
                    {
                        if (GetCellValue(cells[7]).Equals("实践", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        entry = new ScheduleEntry()
                        {
                            Name = GetCellValue(cells[1]).Split(']')[1],
                            Lecturer = GetCellValue(cells[9]),
                            Room = GetCellValue(cells[12])
                        };
                        weeksStr = $"{GetCellValue(cells[10])},";
                        sessionsStr = GetCellValue(cells[11]);
                    }
                    Match sessionMatch = sessionRegex.Match(sessionsStr);
                    if (!sessionMatch.Success)
                    {
                        throw new FormatException($"Encountered unexpected schedule format while parsing session. Got {sessionsStr}");
                    }
                    entry.DayOfWeek = GetDayOfWeek(sessionMatch.FirstGroupValue());
                    if (sessionMatch.Groups[3].Captures.Count == 0)
                    {
                        entry.StartSession = int.Parse(sessionMatch.Groups[5].Value);
                        entry.EndSession = entry.StartSession;
                    }
                    else
                    {
                        entry.StartSession = int.Parse(sessionMatch.Groups[3].Value);
                        entry.EndSession = int.Parse(sessionMatch.Groups[4].Value);
                    }
                    MatchCollection weeksMatch = weeksRegex.Matches(weeksStr);
                    foreach (Match weekItem in weeksMatch)
                    {
                        if (weekItem.Groups[2].Captures.Count == 0)
                        {
                            int startWeek = int.Parse(weekItem.Groups[3].Value);
                            int endWeek = int.Parse(weekItem.Groups[4].Value);
                            for (int week = startWeek; week <= endWeek; week++)
                            {
                                schedule.AddEntry(week, entry);
                            }
                        }
                        else
                        {
                            int week = int.Parse(weekItem.Groups[2].Value);
                            schedule.AddEntry(week, entry);
                        }
                    }
                }
            }
            return schedule;
        }

        private string GetCellValue(Match cell)
        {
            string result = cell.FirstGroupValue();
            if (string.IsNullOrEmpty(result))
            {
                Regex altRegex = new Regex("<td .*? hidevalue='(.*?)'><br></td>", RegexOptions.CultureInvariant);
                return altRegex.Match(cell.Value).FirstGroupValue();
            }
            else
            {
                return result;
            }
        }

        private int GetDayOfWeek(string zhPresent)
        {
            switch (zhPresent)
            {
                case "一":
                    return 1;
                case "二":
                    return 2;
                case "三":
                    return 3;
                case "四":
                    return 4;
                case "五":
                    return 5;
                case "六":
                    return 6;
                case "日":
                    return 7;
                default:
                    return 0;
            }
        }
    }
}
