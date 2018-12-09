namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Location
    {
        // долгота
        [DataMember(Name = "longitude")]
        public float Longitude { get; set; }

        // широта
        [DataMember(Name = "latitude")]
        public float Latitude { get; set; }
    }
}
