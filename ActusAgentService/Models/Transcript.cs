namespace ActusAgentService.Models
{
    public class Transcript
    {
        public string ChannelId { get; set; }
        public DateTime StartTime { get; set; }
        public string Text { get; set; }
        public float[] Embedding { get; set; }
        //public List<object> Embedding { get; set; }
    }
}
