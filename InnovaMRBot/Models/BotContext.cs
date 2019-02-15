using Microsoft.EntityFrameworkCore;

namespace InnovaMRBot.Models
{
    public class BotContext : DbContext
    {
        public DbSet<ConversationSetting> ConversationSettings { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<MessageReaction> Reactions { get; set; }

        public DbSet<VersionedMergeRequest> VersionedMergeRequests { get; set; }

        public DbSet<Action> Actions { get; set; }

        public BotContext(DbContextOptions<BotContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConversationSetting>().HasMany(c => c.Partisipants).WithOne(e => e.Conversation);
            modelBuilder.Entity<ConversationSetting>().HasMany(c => c.ListOfMerge).WithOne(e => e.ConversationSetting).IsRequired();
            //modelBuilder.Entity<ConversationSetting>().HasOne(c => c.AlertChat).WithOne(e => e.ConversationSettingAlertChat);
            modelBuilder.Entity<ConversationSetting>().HasOne(c => c.MRChat).WithOne(e => e.ConversationSettingMrChat).HasForeignKey<ChatSetting>(c => c.MRChatId).IsRequired();

            modelBuilder.Entity<MergeSetting>().HasMany(c => c.Reactions).WithOne(e => e.MergeSetting);
            modelBuilder.Entity<MergeSetting>().HasMany(c => c.VersionedSetting).WithOne(e => e.MergeSetting);

            modelBuilder.Entity<VersionedMergeRequest>().HasMany(c => c.Reactions).WithOne(e => e.VersionedMergeRequest);
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=innovamrbotdb;Trusted_Connection=True;");
        //}
    }
}
