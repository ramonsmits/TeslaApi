using Newtonsoft.Json;

namespace TeslaApi
{
    public class SoftwareUpdate
    {
        [JsonProperty(PropertyName = "download_perc")]
        public int DownloadPercentage { get; set; }

        /// <summary>
        /// Expected install duration in seconds
        /// </summary>
        [JsonProperty(PropertyName = "expected_duration_sec")]
        public int ExpectedDuration { get; set; }

        [JsonProperty(PropertyName = "install_perc")]
        public int InstallPercentage { get; set; }
        
        /// <summary>
        /// Known values: {empty string}, available, scheduled, installing
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
    }
}
