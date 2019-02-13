using InnovaMRBot.Services;
using System;
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public static class StatisticGlossary
    {
        public static Dictionary<string, Func<List<MergeSetting>, List<User>, User, DateTimeOffset, DateTimeOffset, string>> StatisticCommand = new Dictionary<string, Func<List<MergeSetting>, List<User>, User, DateTimeOffset, DateTimeOffset, string>>
        {
            { Glossary.Stat.GET_ALL_STAT_SUFIX, StatHtmlBuilder.GetAllData },
            { Glossary.Stat.GET_MRREACTION_STAT_SUFIX, StatHtmlBuilder.GetMRReaction },
            { Glossary.Stat.GET_UNMARKED_STAT_SUFIX, StatHtmlBuilder.GetUnmarkedCountMergePerDay },
            { Glossary.Stat.GET_UNMARKED_PERUSER_STAT_SUFIX, StatHtmlBuilder.GetUnmarkedMergePerUser },
            { Glossary.Stat.GET_USER_MRREACTION_STAT_SUFIX, StatHtmlBuilder.GetUsersMRReaction },
        };

    }
}
