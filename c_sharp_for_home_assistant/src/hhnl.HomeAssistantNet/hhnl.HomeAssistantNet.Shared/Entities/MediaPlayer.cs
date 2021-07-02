using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("media_player", "MediaPlayers", typeof(SupportedFeature), true)]
    public abstract class MediaPlayer : Entity
    {
        [Flags]
        public enum SupportedFeature
        {
            SupportPause = 1,
            SupportSeek = 2,
            SupportVolumeSet = 4,
            SupportVolumeMute = 8,
            SupportPreviousTrack = 16,
            SupportNextTrack = 32,

            SupportTurnOn = 128,
            SupportTurnOff = 256,
            SupportPlayMedia = 512,
            SupportVolumeStep = 1024,
            SupportSelectSource = 2048,
            SupportStop = 4096,
            SupportClearPlaylist = 8192,
            SupportPlay = 16384,
            SupportShuffleSet = 32768,
            SupportSelectSoundMode = 65536,
            SupportBrowseMedia = 131072,
            SupportRepeatSet = 262144,
            SupportGrouping = 524288
        }

        protected MediaPlayer(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public SupportedFeature SupportedFeatures => (SupportedFeature)GetAttributeOrDefault<int>("supported_features");

        public bool IsPlaying => State == "playing";
        
        public bool IsPaused => State == "paused";

        public double? Volume => GetAttributeOrDefault<double?>("volume_level");
        
        public bool? IsMuted => GetAttributeOrDefault<bool?>("is_volume_muted");
        
        public bool? IsShuffle => GetAttributeOrDefault<bool?>("shuffle");

        public bool? IsRepeat => GetAttributeOrDefault<bool?>("repeat");

        [RequiresSupportedFeature(SupportedFeature.SupportPause)]
        public async Task PauseAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportPause);
            await HomeAssistantClient.CallServiceAsync("media_player", "media_pause", new { entity_id = UniqueId }, ct);
        }
        
        [RequiresSupportedFeature(SupportedFeature.SupportPlay)]
        public async Task PlayAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportPlay);
            await HomeAssistantClient.CallServiceAsync("media_player", "media_play", new { entity_id = UniqueId }, ct);
        }
        
        [RequiresSupportedFeature(SupportedFeature.SupportPlay | SupportedFeature.SupportPause)]
        public async Task PlayPauseAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportPlay | SupportedFeature.SupportPause);
            await HomeAssistantClient.CallServiceAsync("media_player", "media_play_pause", new { entity_id = UniqueId }, ct);
        }
        
        [RequiresSupportedFeature(SupportedFeature.SupportStop)]
        public async Task StopAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportStop);
            await HomeAssistantClient.CallServiceAsync("media_player", "media_stop", new { entity_id = UniqueId }, ct);
        }
        
        [RequiresSupportedFeature(SupportedFeature.SupportVolumeStep)]
        public async Task VolumeUpAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportVolumeStep);
            await HomeAssistantClient.CallServiceAsync("media_player", "volume_up", new { entity_id = UniqueId }, ct);
        }
        
        [RequiresSupportedFeature(SupportedFeature.SupportVolumeStep)]
        public async Task VolumeDownAsync(CancellationToken ct = default)
        {
            VerifySupportedFeature(SupportedFeature.SupportVolumeStep);
            await HomeAssistantClient.CallServiceAsync("media_player", "volume_down", new { entity_id = UniqueId }, ct);
        }
        
        /// <summary>
        /// Sets the volume of the media player.
        /// </summary>
        /// <param name="volume">The volume value from 0 to 1.</param>
        [RequiresSupportedFeature(SupportedFeature.SupportVolumeSet)]
        public async Task SetVolumeAsync(double volume, CancellationToken ct = default)
        {
            if (volume < 0 || volume > 1)
                throw new ArgumentException("Volume must be a value between 0 and 1.", nameof(volume));
            
            VerifySupportedFeature(SupportedFeature.SupportVolumeSet);
            await HomeAssistantClient.CallServiceAsync("media_player", "volume_set", new
            {
                entity_id = UniqueId,
                volume_level = volume
            },
                ct);
        }

        private void VerifySupportedFeature(SupportedFeature feature)
        {
            if (!SupportedFeatures.HasFlag(feature))
                throw new NotSupportedException($"Device does not support feature '{feature}'.");
        }
    }
}