using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        public async Task<StudentInfo> GetStudentInfoAsync(SignInContext signInContext)
        {
            if (!signInContext.IsValid)
            {
                throw new InvalidOperationException("Currently not signed in.");
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "xsxj/Stu_MyInfo_RPT.aspx").AddSessionCookie(signInContext.SessionId!);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string page = await response.Content.ReadAsStringAsync();
            return ParseStudentInfo(page, signInContext.SignedInUser!);
        }

        private StudentInfo ParseStudentInfo(string page, string studentId)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                throw new FormatException("Page is empty.");
            }
            Regex nameRegex = new Regex("姓.*?名</td>.*?>(.*?)<br>");
            Regex classRegex = new Regex("行政班级</td>.*?>(\\d*)(\\D*)(\\d*)班?<br>");
            Regex secondMajorRegex = new Regex("辅修专业</td>.*?>(.*?)<br>");
            Match classMatch = classRegex.Match(page);
            if (!classMatch.Success)
            {
                throw new FormatException("Unexpected student info format. Cannot find class.");
            }
            string year = $"20{classMatch.FirstGroupValue()}";
            string? secondMajor = null;
            Match secondMajorMatch = secondMajorRegex.Match(page);
            if (secondMajorMatch.Success)
            {
                secondMajor = secondMajorMatch.FirstGroupValue();
            }
            StudentInfo info = new StudentInfo()
            {
                StudentId = studentId,
                Name = nameRegex.Match(page).FirstGroupValue(),
                Year = int.Parse(year),
                Major = classMatch.Groups[2].Value,
                Class = int.Parse(classMatch.Groups[3].Value),
                SecondMajor = string.IsNullOrEmpty(secondMajor) ? null : secondMajor
            };
            return info;
        }
    }
}
