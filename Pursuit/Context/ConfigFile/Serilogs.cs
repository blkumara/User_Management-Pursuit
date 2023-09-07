using Pursuit.Model;

namespace Pursuit.Context.ConfigFile
{
    public interface ISerilogs
    {
        ICollection<String>? Using { get; set; }
        MinimumLevel? MinimumLevel { get; set; }
        ICollection<WriteTo>? WriteTo { get; set; }
       
       
    }
    public class Serilogs : ISerilogs
    {
        public ICollection<String>? Using { get; set; } = null!;
        public MinimumLevel? MinimumLevel { get; set; } = null!;

        public ICollection<WriteTo>? WriteTo { get; set; } = null!;
       
    }
}
