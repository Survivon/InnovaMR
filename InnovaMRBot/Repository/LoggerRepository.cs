using InnovaMRBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InnovaMRBot.Repository
{
    public class LoggerRepository : IRepository<Log>
    {
        private readonly BotContext _dbContext;

        public LoggerRepository(BotContext context)
        {
            this._dbContext = context;
        }

        public IEnumerable<Log> GetAll()
        {
            return _dbContext.Logs.ToList();
        }

        public Log Get(Guid id)
        {
            return _dbContext.Logs.FirstOrDefault();
        }

        public void Create(Log item)
        {
            _dbContext.Logs.Add(item);
        }

        public void Update(Log item)
        {
            _dbContext.Logs.Update(item);
        }

        public void Delete(Log item)
        {
            _dbContext.Logs.Remove(item);
        }
    }
}
