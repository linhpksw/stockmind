namespace stockmind.Utils
{
    public sealed class EmailOptions
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? From { get; set; }
        public int? TimeoutSeconds { get; set; }
    }
}
