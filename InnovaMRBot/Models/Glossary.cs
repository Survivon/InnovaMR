namespace InnovaMRBot.Models
{
    public static class Glossary
    {
        public static class Sprint
        {
            public const string COMMAND = "/sprint";

            public const string ADD_ACTION = "Add";

            public const string UPDATE_ACTION = "Update";

            public const string REMOVE_ACTION = "Delete";
        }

        public static class Stat
        {
            public const string COMMAND = "/get_stat";

            public const string ALL = "all";

            public const string SPRINT = "sprint";

            public const string DATE = "date";

            public const string GET_ALL_STAT_SUFIX = "_getalldata";

            public const string GET_MRREACTION_STAT_SUFIX = "_getmrreaction";

            public const string GET_UNMARKED_STAT_SUFIX = "_getunmarked";

            public const string GET_UNMARKED_PERUSER_STAT_SUFIX = "_getunmarkedperuser";

            public const string GET_USER_MRREACTION_STAT_SUFIX = "_getusermrreaction";
        }

        public static class InlineAction
        {
            public const string SUCCESS_REACTION = "/success reaction";

            public const string BAD_REACTION = "/bad reaction";

            public const string START_WATCH = "/start watch";

            public const string GET_STAT = "/get stat";

            public const string SUCCESS_REACTION_MR = "/success_reaction_";

            public const string BAD_REACTION_MR = "/bad_reaction_";
        }

        public static class ActionType
        {
            public const string UNMARKED = "unmarked";

            public const string WATCH_NOTIFICATION = "watch";

            public const string REVIEW_NOTIFICATION = "review";

            public const string CLEAR_TEMP_DATA = "cleartempdata";
        }
    }
}
