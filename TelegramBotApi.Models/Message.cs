
namespace TelegramBotApi.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Attachment;

    [DataContract]
    public class Message
    {
        [DataMember(Name = "message_id")]
        public int Id { get; set; }

        [DataMember(Name = "from")]
        public User Sender { get; set; }

        [DataMember(Name = "date")]
        public long Date { get; set; }

        [DataMember(Name = "chat")]
        public Chat Chat { get; set; }

        [DataMember(Name = "forward_from")]
        public User ForwardSender { get; set; }

        [DataMember(Name = "forward_signature")]
        public string ForwardSignature { get; set; }

        [DataMember(Name = "forward_from_message_id")]
        public int ForwardFromMessageId { get; set; }

        [DataMember(Name = "forward_date")]
        public long ForwardDate { get; set; }

        [DataMember(Name = "reply_to_message")]
        public Message ReplyMessage { get; set; }

        [DataMember(Name = "edit_date")]
        public int EditDate { get; set; }

        [DataMember(Name = "media_group_id")]
        public string MediaGroupId { get; set; }

        [DataMember(Name = "author_signature")]
        public string AuthorSignature { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "entities")]
        public List<MessageEntity> Entities { get; set; }

        [DataMember(Name = "caption_entities")]
        public List<MessageEntity> CaptionEntities { get; set; }

        [DataMember(Name = "audio")]
        public Audio Audio { get; set; }

        //VideoNote

        //Game

        //Invoice

        //SuccessfulPayment

        //connected_website

        //passport_data

        [DataMember(Name = "animation")]
        public Animation Animation { get; set; }

        [DataMember(Name = "document")]
        public Document Document { get; set; }

        [DataMember(Name = "photo")]
        public List<PhotoSize> Photo { get; set; }

        [DataMember(Name = "sticker")]
        public Sticker Sticker { get; set; }

        [DataMember(Name = "video")]
        public Video Video { get; set; }

        [DataMember(Name = "voice")]
        public Voice Voice { get; set; }

        [DataMember(Name = "caption")]
        public string Caption { get; set; }

        [DataMember(Name = "contact")]
        public Contact Contact { get; set; }

        [DataMember(Name = "location")]
        public Location Location { get; set; }

        [DataMember(Name = "venue")]
        public Venue Venue { get; set; }

        [DataMember(Name = "new_chat_member")]
        public User NewChatMember { get; set; }

        [DataMember(Name = "left_chat_member")]
        public User LeftChatMember { get; set; }

        [DataMember(Name = "new_chat_title")]
        public string NewChatTitle { get; set; }

        [DataMember(Name = "new_chat_photo")]
        public List<PhotoSize> NewChatPhoto { get; set; }

        [DataMember(Name = "delete_chat_photo")]
        public bool IsDeleteChatPhoto { get; set; }

        [DataMember(Name = "group_chat_created")]
        public bool IsGroupChatCreated { get; set; }

        [DataMember(Name = "supergroup_chat_created")]
        public bool IsSupergroupChatCreated { get; set; }

        [DataMember(Name = "channel_chat_created")]
        public bool IsChannelChatCreated { get; set; }

        [DataMember(Name = "migrate_to_chat_id")]
        public int MigrateToChatId { get; set; }

        [DataMember(Name = "migrate_from_chat_id")]
        public int MigrateFromChatId { get; set; }

        [DataMember(Name = "pinned_message")]
        public Message PinnedMessage { get; set; }
    }
}
