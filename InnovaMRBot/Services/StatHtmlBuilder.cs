using InnovaMRBot.Helpers;
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
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>Statistics</title><style>table, th, td { border: 1px solid black;border-collapse: collapse; } th, td { padding: 5px;text-align: left; } tr:nth-child(even) {background: #d1e2ff} tr:nth-child(odd) { background: #ffebd1} </style></head><body>{0}</body></html>";

        // ex: getalldata 24/11/2018 28/11/2018
        public static string GetAllData(List<MergeSetting> merge, List<User> usersList, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            IEnumerable<IGrouping<string, MergeSetting>> groupedMerges;

            MapUsers(merge, usersList);

            var resultTable = new StringBuilder();

            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.Owner.Name);
            }
            else
            {
                resultTable.Append($"<h2>Stat from {start:MM/dd/yy} to {end:MM/dd/yy}</h2>");
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.Owner.Name);
            }


            resultTable.Append("<table style=\"width: 100 % \">" +
                               "<tr><th>MR Owner</th>" +
                               "<th>MR Link</th>" +
                               "<th>Tickets Links</th>" +
                               "<th>Description</th>" +
                               "<th>Publish Date</th>" +
                               "<th>Count of Change</th>" +
                               "<th>Reviewers</th>" +
                               "<th>Version Publish Date</th>" +
                               "<th>Version Description</th>" +
                               "<th>Version Reaction</th>" +
                               "</tr>");

            foreach (var groupedMerge in groupedMerges)
            {
                var first = groupedMerge.FirstOrDefault();
                if (first == null) continue;

                var totalRowCount = GetTotalCount(merge.Where(c => c.Owner.Name.Equals(groupedMerge.Key)).ToList());

                var firstVersion = first.VersionedSetting.FirstOrDefault();

                var countOfRowsForFirstVersioned = first.VersionedSetting.Count;
                countOfRowsForFirstVersioned = countOfRowsForFirstVersioned == 0 ? 1 : countOfRowsForFirstVersioned;

                if (firstVersion == null)
                {
                    resultTable.Append($"<tr>" +
                                       $"<td rowspan=\"{totalRowCount}\">{groupedMerge.Key}</td>" +
                                       $"<td>{first.MrUrl}</td>" +
                                       $"<td>{string.Join("<hr><br>", first.TicketsUrl.Split(';').ToList())}</td>" +
                                       $"<td>{first.Description}</td><td>{first.PublishDate.Value:MM/dd/yy H:mm:ss}</td><td>{first.CountOfChange}</td>" +
                                       $"<td>{string.Join("<hr><br>", first.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                                       $"<td></td>" +
                                       $"<td></td>" +
                                       $"<td></td>" +
                                       $"</tr>");
                }
                else
                {
                    resultTable.Append($"<tr>" +
                                       $"<td rowspan=\"{totalRowCount}\">" +
                                       $"{groupedMerge.Key}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{first.MrUrl}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{string.Join("<br>", first.TicketsUrl.Split(';').ToList())}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{first.Description}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{first.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{first.CountOfChange}</td>" +
                                       $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{string.Join("<hr><br>", first.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                                       $"<td>{firstVersion.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                                       $"<td>{firstVersion.Description}</td>" +
                                       $"<td>{string.Join("<hr><br>", firstVersion.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                                       $"</tr>");

                    foreach (var versionedMergeRequest in first.VersionedSetting.Skip(1))
                    {
                        resultTable.Append(
                            $"<tr>" +
                            $"<td>{versionedMergeRequest.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                            $"<td>{versionedMergeRequest.Description}</td>" +
                            $"<td>{string.Join("<hr><br>", versionedMergeRequest.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                            $"</tr>");
                    }
                }

                foreach (var mergeSetting in groupedMerge.Skip(1))
                {
                    var firstVersioned = mergeSetting.VersionedSetting.FirstOrDefault();
                    countOfRowsForFirstVersioned = mergeSetting.VersionedSetting.Count;
                    countOfRowsForFirstVersioned = countOfRowsForFirstVersioned == 0 ? 1 : countOfRowsForFirstVersioned;

                    if (firstVersioned == null)
                    {
                        resultTable.Append(
                            $"<tr>" +
                            $"<td>{mergeSetting.MrUrl}</td>" +
                            $"<td>{string.Join("<hr><br>", mergeSetting.TicketsUrl.Split(';').ToList())}</td>" +
                            $"<td>{mergeSetting.Description}</td>" +
                            $"<td>{mergeSetting.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                            $"<td>{mergeSetting.CountOfChange}</td>" +
                            $"<td>{string.Join("<hr><br>", mergeSetting.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                            $"<td></td>" +
                            $"<td></td>" +
                            $"<td></td>" +
                            $"</tr>");
                    }
                    else
                    {

                        resultTable.Append(
                            $"<tr>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{mergeSetting.MrUrl}</td>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{string.Join("<br>", mergeSetting.TicketsUrl.Split(';').ToList())}</td>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{mergeSetting.Description}</td>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{mergeSetting.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{mergeSetting.CountOfChange}</td>" +
                            $"<td rowspan=\"{countOfRowsForFirstVersioned}\">{string.Join("<hr><br>", mergeSetting.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                            $"<td>{firstVersioned.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                            $"<td>{firstVersioned.Description}</td>" +
                            $"<td>{string.Join("<hr><br>", firstVersioned.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                            $"</tr>");

                        foreach (var versionedMergeRequest in mergeSetting.VersionedSetting.Skip(1))
                        {
                            resultTable.Append(
                                $"<tr>" +
                                $"<td>{versionedMergeRequest.PublishDate.Value:MM/dd/yy H:mm:ss}</td>" +
                                $"<td>{versionedMergeRequest.Description}</td>" +
                                $"<td>{string.Join("<hr><br>", versionedMergeRequest.Reactions.Select(c => $"{c.User.Name} in {c.ReactionTime:MM/dd/yy H:mm:ss}"))}</td>" +
                                $"</tr>");
                        }
                    }
                }
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "alldata");

            return fileUrl;
        }

        // ex: getmrreaction 24/11/2018 28/11/2018
        public static string GetMRReaction(List<MergeSetting> merge, List<User> usersList, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            IEnumerable<IGrouping<string, MergeSetting>> groupedMerges;

            MapUsers(merge, usersList);

            var resultTable = new StringBuilder();
            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.Owner.Name);
            }
            else
            {
                resultTable.Append($"<h2>Stat from {start:MM/dd/yy} to {end:MM/dd/yy}</h2>");
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.Owner.Name);
            }

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>MR Owner</th><th>MR Link</th><th>Tickets Links</th><th>MR Reaction</th><th>Avg Reaction</th></tr>");

            foreach (var groupedMerge in groupedMerges)
            {
                var first = groupedMerge.FirstOrDefault();
                if (first == null) continue;

                var lastVersion = GetLastVersion(first);

                var lastVersionCount = lastVersion.Reactions.Count;
                lastVersionCount = lastVersionCount == 0 ? 1 : lastVersionCount;

                resultTable.Append($"<tr>" +
                                   $"<td rowspan=\"{groupedMerge.ToList().Count}\">{groupedMerge.Key}</td>" +
                                   $"<td>{first.MrUrl}</td>" +
                                   $"<td>{string.Join("<br>", first.TicketsUrl.Split(';').ToList())}</td>" +
                                   $"<td>{string.Join("<br>", lastVersion.Reactions.Select(c => $"{c.User.Name} - {c.ReactionInMinutes.MinutesToCorrectTimeConverter()}"))}</td>" +
                                   $"<td>{(lastVersion.Reactions.Sum(c => c.ReactionInMinutes) / lastVersionCount).MinutesToCorrectTimeConverter()}</td>" +
                                   $"</tr>");

                foreach (var mergeSetting in groupedMerge.Skip(1))
                {
                    lastVersion = GetLastVersion(mergeSetting);

                    lastVersionCount = lastVersion.Reactions.Count;
                    lastVersionCount = lastVersionCount == 0 ? 1 : lastVersionCount;

                    resultTable.Append(
                        $"<tr>" +
                        $"<td>{mergeSetting.MrUrl}</td>" +
                        $"<td>{string.Join("<br>", mergeSetting.TicketsUrl.Split(';').ToList())}</td>" +
                        $"<td>{string.Join("<br>", lastVersion.Reactions.Select(c => $"{c.User.Name} - {c.ReactionInMinutes.MinutesToCorrectTimeConverter()}"))}</td>" +
                        $"<td>{(lastVersion.Reactions.Sum(c => c.ReactionInMinutes) / lastVersionCount).MinutesToCorrectTimeConverter()}</td>" +
                        $"</tr>");
                }
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "mrreaction");

            return fileUrl;
        }

        // ex: getusermrreaction 24/11/2018 28/11/2018
        public static string GetUsersMRReaction(List<MergeSetting> merges, List<User> usersList, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            MapUsers(merges, usersList);

            var resultTable = new StringBuilder();
            if (start != default(DateTimeOffset) && end != default(DateTimeOffset))
            {
                resultTable.Append($"<h2>Stat from {start:MM/dd/yy} to {end:MM/dd/yy}</h2>");
            }

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>Dev</th><th>Avg Response Time</th></tr>");

            foreach (var user in usersList)
            {
                resultTable.Append(
                    $"<tr>" +
                    $"<td>{user.Name}</td>" +
                    $"<td>{GetAvgReactionToOtherMerge(merges, user.Name, start, end).MinutesToCorrectTimeConverter()}</td>" +
                    $"</tr>");

            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "usermrreaction");

            return fileUrl;
        }

        // ex: getunmarked 24/11/2018 28/11/2018
        public static string GetUnmarkedCountMergePerDay(List<MergeSetting> merge, List<User> usersList, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            IEnumerable<IGrouping<DateTime, MergeSetting>> groupedMerges;

            MapUsers(merge, usersList);

            var resultTable = new StringBuilder();
            if (start == default(DateTimeOffset) || end == default(DateTimeOffset))
            {
                groupedMerges = merge.GroupBy(m => m.PublishDate.Value.Date);
            }
            else
            {
                resultTable.Append($"<h2>Stat from {start:MM/dd/yy} to {end:MM/dd/yy}</h2>");
                groupedMerges = merge.Where(m => m.PublishDate < end && m.PublishDate > start)
                    .GroupBy(m => m.PublishDate.Value.Date);
            }

            resultTable.Append("<table style=\"width: 100 % \"><tr><th>Date</th><th>Count MR Without Review</th></tr>");

            foreach (var groupedMerge in groupedMerges.OrderBy(c => c.Key))
            {
                resultTable.Append($"<tr><td>{groupedMerge.Key:MM/dd/yyyy}</td><td>{groupedMerge.Count(c => GetLastVersion(c).Reactions.Count < 2)}</td></tr>");
            }

            resultTable.Append("</table>");

            var resultMessage = resultTable.ToString();

            var allHtml = HEADER_HTML_PATTERN.Replace("{0}", resultMessage);

            var fileUrl = GetFileName(allHtml, "unmarkedmrperday");

            return fileUrl;
        }

        private static void MapUsers(List<MergeSetting> merge, List<User> usersList)
        {
            if (usersList == null || !usersList.Any()) return;

            foreach (var mergeSetting in merge)
            {
                mergeSetting.Owner = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSetting.OwnerId));
                foreach (var versionedMergeRequest in mergeSetting.VersionedSetting)
                {
                    foreach (var reaction in versionedMergeRequest.Reactions)
                    {
                        reaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(reaction.UserId));
                    }
                }

                foreach (var mergeSettingReaction in mergeSetting.Reactions)
                {
                    mergeSettingReaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSettingReaction.UserId));
                }
            }
        }

        private static int GetTotalCount(List<MergeSetting> merges)
        {
            var result = 0;

            foreach (var mergeSetting in merges)
            {
                if (!mergeSetting.VersionedSetting.Any())
                {
                    result++;
                }
                else
                {
                    result += mergeSetting.VersionedSetting.Count;
                }
            }

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

        private static int GetAvgReactionToOtherMerge(List<MergeSetting> merges, string dev, DateTimeOffset start = default(DateTimeOffset), DateTimeOffset end = default(DateTimeOffset))
        {
            var reaction = 0;

            var allTime = 0;
            var countOfReview = 0;

            foreach (var mergeSetting in merges)
            {
                if (mergeSetting.Reactions.Any(r => r.User.Name.Equals(dev)))
                {
                    if (start == default(DateTimeOffset) && end == default(DateTimeOffset))
                    {
                        allTime += mergeSetting.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                        countOfReview++;
                    }
                    else if (mergeSetting.PublishDate > start && mergeSetting.PublishDate < end)
                    {
                        allTime += mergeSetting.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                        countOfReview++;
                    }
                }

                foreach (var versionedMergeRequest in mergeSetting.VersionedSetting)
                {
                    if (versionedMergeRequest.Reactions.Any(r => r.User.Name.Equals(dev)))
                    {
                        if (start == default(DateTimeOffset) && end == default(DateTimeOffset))
                        {
                            allTime += versionedMergeRequest.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                            countOfReview++;
                        }
                        else if (versionedMergeRequest.PublishDate > start && versionedMergeRequest.PublishDate < end)
                        {
                            allTime += versionedMergeRequest.Reactions.FirstOrDefault(r => r.User.Name.Equals(dev)).ReactionInMinutes;
                            countOfReview++;
                        }
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
            var fullFileName = $"{fileName}{Guid.NewGuid().ToString()}.html";

            var directoryPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp",
                fullFileName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(path))
            {
                var file = File.Create(path);
                file.Dispose();
            }

            File.WriteAllText(path, content);

            return path;
        }
    }
}
