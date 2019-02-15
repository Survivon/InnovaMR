using InnovaMRBot.Models;
using System;

namespace InnovaMRBot.Repository
{
    public class UnitOfWork : IDisposable
    {
        private readonly BotContext _dbContext;
        private ConversationSettingRepository _conversationRepository;
        private UserRepository _userRepository;
        private ActionRepository _actionRepository;

        public ConversationSettingRepository Conversations => _conversationRepository ?? (_conversationRepository = new ConversationSettingRepository(_dbContext));

        public UserRepository Users => _userRepository ?? (_userRepository = new UserRepository(_dbContext));

        public ActionRepository Actions => _actionRepository ?? (_actionRepository = new ActionRepository(_dbContext));

        public UnitOfWork(BotContext dbContext) => _dbContext = dbContext;

        public void Save()
        {
            _dbContext.SaveChanges();
        }

        private bool disposed = false;

        public virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
                this.disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
