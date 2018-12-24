using InnovaMRBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace InnovaMRBot.Repository
{
    public class ConversationSettingRepository : IRepository<ConversationSetting>
    {
        private readonly BotContext _dbContext;

        public ConversationSettingRepository(BotContext context)
        {
            this._dbContext = context;
        }

        public IEnumerable<ConversationSetting> GetAll()
        {
            var allConversation = _dbContext.ConversationSettings.Include(c => c.MRChat)
                .Include(c => c.ListOfMerge)
                .ThenInclude(c => c.VersionedSetting).ThenInclude(c => c.Reactions)
                .Include(c => c.ListOfMerge).ThenInclude(c => c.Reactions)
                .Include(c => c.Partisipants)
                .ToList();

            return allConversation;
        }

        public ConversationSetting Get(Guid id)
        {
            return _dbContext.ConversationSettings.FirstOrDefault(c => c.Id.Equals(id));
        }

        public void Create(ConversationSetting item)
        {
            _dbContext.ConversationSettings.Add(item);
        }

        public void Update(ConversationSetting item)
        {
            _dbContext.ConversationSettings.Update(item);
        }

        public void Delete(ConversationSetting item)
        {
            _dbContext.ConversationSettings.Remove(item);
        }
    }
}

