
using System;
using System.Runtime.CompilerServices;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;

namespace InnovaMRBot.Services
{
    public class Logger
    {
        private readonly UnitOfWork _dbContext;

        public Logger(UnitOfWork dbContext)
        {
            _dbContext = dbContext;
        }

        public void Info(string description, string userId, [CallerMemberName] string methodName = "")
        {
            var log = new Log()
            {
                Description = description,
                UserId = userId,
                Type = LogType.Info,
                ExecDate = DateTime.UtcNow,
                MethodName = methodName,
            };

            _dbContext.Logs.Create(log);
            _dbContext.Save();
        }

        public void Warn(string description, string userId, [CallerMemberName] string methodName = "")
        {
            var log = new Log()
            {
                Description = description,
                UserId = userId,
                Type = LogType.Warn,
                ExecDate = DateTime.UtcNow,
                MethodName = methodName,
            };

            _dbContext.Logs.Create(log);
            _dbContext.Save();
        }

        public void Error(string description, string userId, [CallerMemberName] string methodName = "")
        {
            var log = new Log()
            {
                Description = description,
                UserId = userId,
                Type = LogType.Error,
                ExecDate = DateTime.UtcNow,
                MethodName = methodName,
            };

            _dbContext.Logs.Create(log);
            _dbContext.Save();
        }

        public void Fatal(string description, string userId, [CallerMemberName] string methodName = "")
        {
            var log = new Log()
            {
                Description = description,
                UserId = userId,
                Type = LogType.Fatal,
                ExecDate = DateTime.UtcNow,
                MethodName = methodName,
            };

            _dbContext.Logs.Create(log);
            _dbContext.Save();
        }
    }
}
