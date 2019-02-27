using System;
using System.ComponentModel.DataAnnotations.Schema;
using InnovaMRBot.Models.Enum;

namespace InnovaMRBot.Models
{
    public class Log
    {
        public Guid Id { get; set; }

        public string MethodName { get; set; }

        public DateTime ExecDate { get; set; }

        public string Description { get; set; }

        public string UserId { get; set; }

        [NotMapped]
        public LogType Type
        {
            get
            {
                if (string.IsNullOrEmpty(LogType))
                {
                    return Enum.LogType.Info;
                }

                if (LogType.Equals(Enum.LogType.Error.ToString()))
                {
                    return Enum.LogType.Error;
                }
                else if (LogType.Equals(Enum.LogType.Fatal.ToString()))
                {
                    return Enum.LogType.Fatal;
                }
                else if (LogType.Equals(Enum.LogType.Warn.ToString()))
                {
                    return Enum.LogType.Warn;
                }
                else
                {
                    return Enum.LogType.Info;
                }
            }
            set => LogType = value.ToString();
        }

        public string LogType { get; set; }
    }
}
