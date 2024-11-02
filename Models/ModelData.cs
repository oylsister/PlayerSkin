namespace PlayerSkin.Models
{
    public class Settings
    {
        public string? DefaultSkin { get; set; }
        public Dictionary<string, ModelData> ModelList { get; set; } = new Dictionary<string, ModelData>();
    }

    public class ModelData
    {
        public string? Name { get; set; }
        public string? ModelPath { get; set; }
    }
}
