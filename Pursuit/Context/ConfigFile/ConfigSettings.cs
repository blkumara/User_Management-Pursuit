namespace Pursuit.Context.ConfigFile
{
    public interface IConfigSettings
    {
        string? DomainName { get; set; }
     
    }
    public class ConfigSettings : IConfigSettings
    {
        public string? DomainName { get; set; }

    }
}
