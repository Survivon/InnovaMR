using InnovaMRBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = InnovaMRBot.Models.Action;

namespace InnovaMRBot.Repository
{
    public class ActionRepository : IRepository<Action>
    {
        private readonly BotContext _dbContext;

        public ActionRepository(BotContext context)
        {
            this._dbContext = context;
        }

        public IEnumerable<Action> GetAll()
        {
            return _dbContext.Actions.ToList();
        }

        public Action Get(Guid id)
        {
            return _dbContext.Actions.FirstOrDefault(a => a.Id == id);
        }

        public void Create(Action item)
        {
            _dbContext.Actions.Add(item);
        }

        public void Update(Action item)
        {
            _dbContext.Actions.Update(item);
        }

        public void Delete(Action item)
        {
            _dbContext.Actions.Remove(item);
        }
    }
}
