using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ocapps.Structures
{
    public class Application
    {
        public class AppItem
        {
            public List<string> aliases { get; set; }
            public string filename { get; set; }
            public string url { get; set; }
            public string name { get; set; }
            public string architecture { get; set; }
        }

        public class RootObject
        {
            public List<Dictionary<string, AppItem>> browsers { get; set; }
            [YamlMember(Alias = "vista_apps")] public List<Dictionary<string, AppItem>> VistaApps { get; set; }
            [YamlMember(Alias = "windows_7_apps")] public List<Dictionary<string, AppItem>> Windows7Apps { get; set; }

            [YamlMember(Alias = "Codec_video_audio")]
            public List<Dictionary<string, AppItem>> CodecVideoAudio { get; set; }

            public List<Dictionary<string, AppItem>> Utilities { get; set; }
            public List<Dictionary<string, AppItem>> Other { get; set; }
            public List<Dictionary<string, AppItem>> Office { get; set; }
            public List<Dictionary<string, AppItem>> Programming { get; set; }
        }
    }
}