using BrokeProtocol.API;

namespace YoutubePlayer
{
    public class Core : Plugin
    {
        public Core()
        {
            Info = new PluginInfo("YouTubePlayer", "yp")
            {
                Description = "Play videos from YouTube",
                Website = "https://github.com/SploeCyber/YoutubePlayer"
            };

        }
    }
}
