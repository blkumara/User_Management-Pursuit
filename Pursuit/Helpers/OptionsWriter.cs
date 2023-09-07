using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;

namespace Pursuit.Helpers
{
    public interface IOptionsWriter
    {
        void UpdateOptions(Action<JObject> callback, bool reload = true);
    }

    public class OptionsWriter : IOptionsWriter
    {
        private readonly Microsoft.Extensions.Hosting.IHostingEnvironment environment;
        private readonly IConfigurationRoot configuration;
        private readonly string file;

        public OptionsWriter(
            Microsoft.Extensions.Hosting.IHostingEnvironment environment,
            IConfigurationRoot configuration,
            string file)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.file = file;
        }

        public void UpdateOptions(Action<JObject> callback, bool reload = true)
        {
            IFileProvider fileProvider = this.environment.ContentRootFileProvider;
            IFileInfo fi = fileProvider.GetFileInfo(this.file);
            JObject config = fileProvider.ReadJsonFileAsObject(fi);
            callback(config);
            using (var stream = File.OpenWrite(fi.PhysicalPath))
            {
                stream.SetLength(0);
                config.WriteTo(stream);
            }

            this.configuration.Reload();
        }
    }
}
