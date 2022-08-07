
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Coflnet.Sky.Base.Models
{
    [DataContract]
    public class Flip
    {
        [IgnoreDataMember]
        [JsonIgnore]
        public int Id { get; set; }
        public int AuctionId { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; set; }
    }
}