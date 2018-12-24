using InnovaMRBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InnovaMRBot.Repository
{
    public class UserRepository : IRepository<User>
    {
        private readonly BotContext _dbContext;

        public UserRepository(BotContext context)
        {
            this._dbContext = context;
        }

        public IEnumerable<User> GetAll()
        {
            return _dbContext.Users.ToList();
        }

        public User Get(Guid id)
        {
            return _dbContext.Users.FirstOrDefault(c => c.UserId.Equals(id));
        }

        public void Create(User item)
        {
            _dbContext.Users.Add(item);
        }

        public void Update(User item)
        {
            _dbContext.Users.Update(item);
        }

        public void Delete(User item)
        {
            _dbContext.Users.Remove(item);
        }
    }
}
