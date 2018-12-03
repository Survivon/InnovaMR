using InnovaMRBot.Models;
using InnovaMRBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InnovaMRBot.Services
{
    public class StatHtmlBuilder : IStatBuilder
    {
        private const string HEADER_HTML_PATTERN =
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>Statistics</title><style>table, th, td { border: 1px solid black;border-collapse: collapse; } th, td { padding: 5px;text-align: left; } </style></head><body>{0}</body></html>";

        // ex: getalldata 24/11/2018 28/11/2018
        public static ResponseMessage GetAllData(List<MergeSetting> merge, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            var result = new ResponseMessage();

            IEnumerable<IGrouping<string, MergeSetting>> groupedMerges;

            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.Owner.Name);
            }
            else
            {
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.Owner.Name);
            }

            var resultTable = new StringBuilder();

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>MR Owner</th><th>MR url</th><th>Tickets url</th><th>Description</th><th>Publish date</th><th>Count of change</th><th>Reviewers</th></tr>");

            foreach (var groupedMerge in groupedMerges)
            {
                var first = groupedMerge.FirstOrDefault();
                if (first == null) continue;

                resultTable.Append($"<tr><td rowspan=\"{groupedMerge.ToList().Count}\">{groupedMerge.Key}</td><td>{first.MrUrl}</td><td>{string.Join("<br>", first.TicketsUrl)}</td><td>{first.Description}</td><td>{first.PublishDate.Value:MM/dd/yy H:mm:ss}</td><td>{first.CountOfChange}</td><td>{string.Join("<br>", first.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td></tr>");
                foreach (var mergeSetting in groupedMerge.Skip(1))
                {
                    resultTable.Append(
                        $"<tr><td>{mergeSetting.MrUrl}</td><td>{string.Join("<br>", mergeSetting.TicketsUrl)}</td><td>{mergeSetting.Description}</td><td>{mergeSetting.PublishDate.Value:MM/dd/yy H:mm:ss}</td><td>{mergeSetting.CountOfChange}</td><td>{string.Join("<br>", mergeSetting.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td></tr>");
                }
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "alldata");

            result.Message = fileUrl;

            return result;
        }

        // ex: getmrreaction 24/11/2018 28/11/2018
        public static ResponseMessage GetMRReaction(List<MergeSetting> merge, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            var result = new ResponseMessage();

            IEnumerable<IGrouping<string, MergeSetting>> groupedMerges;
            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.Owner.Name);
            }
            else
            {
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.Owner.Name);
            }

            var resultTable = new StringBuilder();

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>MR Owner</th><th>MR url</th><th>Tickets url</th><th>MR Reaction</th><th>Avg reaction</th></tr>");

            foreach (var groupedMerge in groupedMerges)
            {
                var first = groupedMerge.FirstOrDefault();
                if (first == null) continue;

                var lastVersion = GetLastVersion(first);

                resultTable.Append($"<tr><td rowspan=\"{groupedMerge.ToList().Count}\">{groupedMerge.Key}</td><td>{first.MrUrl}</td><td>{string.Join("<br>", first.TicketsUrl)}</td><td>{string.Join("<br>", lastVersion.Reactions.Select(c => $"{c.User.Name} - {c.ReactionInMinutes}"))}</td><td>{lastVersion.Reactions.Sum(c => c.ReactionInMinutes) / lastVersion.Reactions.Count}</td></tr>");
                foreach (var mergeSetting in groupedMerge.Skip(1))
                {
                    lastVersion = GetLastVersion(mergeSetting);
                    resultTable.Append(
                        $"<tr><td>{mergeSetting.MrUrl}</td><td>{string.Join("<br>", mergeSetting.TicketsUrl)}</td><td>{string.Join("<br>", lastVersion.Reactions.Select(c => $"{c.User.Name} - {c.ReactionInMinutes}"))}</td><td>{lastVersion.Reactions.Sum(c => c.ReactionInMinutes) / lastVersion.Reactions.Count}</td></tr>");
                }
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "mrreaction");

            result.Message = fileUrl;

            return result;
        }

        // ex: getusermrreaction 24/11/2018 28/11/2018
        public static ResponseMessage GetUsersMRReaction(List<MergeSetting> merges, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            var result = new ResponseMessage();

            IEnumerable<IGrouping<string, MergeSetting>> groupedMerges;
            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merges.GroupBy(m => m.Owner.Name);
            }
            else
            {
                groupedMerges = merges.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.Owner.Name);
            }

            var resultTable = new StringBuilder();

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>Dev</th><th>Avg reaction time</th></tr>");

            foreach (var groupedMerge in groupedMerges)
            {
                resultTable.Append(
                    $"<tr><td>{groupedMerge.Key}</td><td>{GetAvgReactionToOtherMerge(merges, groupedMerge.Key)}</td></tr>");
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "usermrreaction");

            result.Message = fileUrl;

            return result;
        }

        // ex: getunmarked 24/11/2018 28/11/2018
        public static ResponseMessage GetUnmarkedCountMergePerDay(List<MergeSetting> merge, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            var result = new ResponseMessage();

            IEnumerable<IGrouping<DateTime, MergeSetting>> groupedMerges;
            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.PublishDate.Value.Date);
            }
            else
            {
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.PublishDate.Value.Date);
            }

            var resultTable = new StringBuilder();

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>Date</th><th>Count unmarked mr</th></tr>");

            foreach (var groupedMerge in groupedMerges.OrderBy(c => c.Key))
            {
                resultTable.Append($"<tr><td>{groupedMerge.Key:MM/dd/yyyy}</td><td>{groupedMerge.Count(c => GetLastVersion(c).Reactions.Count < 2)}</td></tr>");
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "unmarkedmrperday");

            result.Message = fileUrl;

            return result;
        }

        private static VersionedMergeRequest GetLastVersion(MergeSetting merge)
        {
            var result = new VersionedMergeRequest()
            {
                PublishDate = merge.PublishDate,
                Reactions = merge.Reactions,
            };

            if (merge.VersionedSetting != null && merge.VersionedSetting.Any())
            {
                return merge.VersionedSetting.FirstOrDefault(c =>
                    c.PublishDate == merge.VersionedSetting.Max(m => m.PublishDate));
            }

            return result;
        }

        private static int GetAvgReactionToOtherMerge(List<MergeSetting> merges, string dev)
        {
            var reaction = 0;

            var allTime = 0;
            var countOfReview = 0;

            foreach (var mergeSetting in merges)
            {
                if (mergeSetting.Reactions.Any(r => r.User.Name.Equals(dev)))
                {
                    allTime += mergeSetting.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                    countOfReview++;
                }

                foreach (var versionedMergeRequest in mergeSetting.VersionedSetting)
                {
                    if (versionedMergeRequest.Reactions.Any(r => r.User.Name.Equals(dev)))
                    {
                        allTime += versionedMergeRequest.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                        countOfReview++;
                    }
                }
            }

            if (countOfReview > 0)
            {
                return allTime / countOfReview;
            }

            return reaction;
        }

        private static string GetFileName(string content, string fileName)
        {
            var result = string.Empty;
            var fullFileName = $"{fileName}{DateTime.Now.Millisecond}.html";

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                //"wwwroot",
                fullFileName);

            if (!File.Exists(path))
            {
                var file = File.Create(path);
                file.Dispose();
            }

            File.WriteAllText(path, content);

            result = $"https://innovamrbot.azurewebsites.net/download/{fullFileName}";

            return result;
        }
    }
}
