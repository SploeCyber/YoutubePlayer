using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Required;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using YouTubeApiSharp;

namespace YoutubePlayer
{
    public class Important
    {
        private const string videoPanel = "videoPanel";
        private const string title = "&7Video Panel";
        private const string customVideo = "customVideo";
        private const string stopVideo = "stopVideo";

        private const string youtubePanel = "youtubePanel";
        private const string youtubePlay = "youtubePlay";

        // default permission
        private const string defaultVideoPerm = "bp.videoDefault";
        private const string stopVideoPerm = "bp.videoStop";
        private const string customVideoPerm = "bp.videoCustom";

        // youtubeplayer permission
        private const string YTPlayPerm = "yt.play";


        private bool VideoPermission(ShPlayer player, ShEntity videoPlayer, string permission, bool checkLimit = false)
        {
            if (!videoPlayer) return false;

            if (checkLimit)
            {
                const int videoLimit = 3;

                int videoCount = 0;
                foreach (var e in videoPlayer.svEntity.sector.controlled)
                {
                    if (e != videoPlayer && !string.IsNullOrWhiteSpace(e.svEntity.videoURL))
                        videoCount++;
                }

                if (videoCount >= videoLimit)
                {
                    player.svPlayer.SendGameMessage($"Video limit of {videoLimit} reached");
                    return false;
                }
            }
            return player.InActionRange(videoPlayer) && (player.InOwnApartment || player.svPlayer.HasPermission(permission));
        }


        [Target(GameSourceEvent.PlayerVideoPanel, ExecutionMode.Override)]
        public void OnVideoPanel(ShPlayer player, ShEntity videoEntity)
        {
            var options = new List<LabelID>();

            if (VideoPermission(player, videoEntity, YTPlayPerm))
            {
                options.Add(new LabelID("&cYouTube", youtubePanel));
            }

            if (VideoPermission(player, videoEntity, customVideoPerm))
            {
                options.Add(new LabelID("&6Custom Video URL", customVideo));
            }

            if (VideoPermission(player, videoEntity, stopVideoPerm))
            {
                options.Add(new LabelID("&4Stop Video", stopVideo));
            }

            if (VideoPermission(player, videoEntity, defaultVideoPerm))
            {
                int index = 0;
                foreach (var option in player.manager.svManager.videoOptions)
                {
                    options.Add(new LabelID(option.label, index.ToString()));
                    index++;
                }
            }

            player.svPlayer.SendOptionMenu(title, videoEntity.ID, videoPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
        }


        [Target(GameSourceEvent.PlayerOptionAction, ExecutionMode.Test)]
        public bool OnOptionAction(ShPlayer player, int targetID, string menuID, string optionID, string actionID)
        {
            var videoEntity = EntityCollections.FindByID(targetID);

            switch (menuID)
            {
                case videoPanel:

                    if (optionID == customVideo && VideoPermission(player, videoEntity, customVideoPerm))
                    {
                        player.svPlayer.SendGameMessage("Only direct video links supported - Can upload to Imgur or Discord and link that");
                        player.svPlayer.SendInputMenu("Direct MP4/WEBM URL", targetID, customVideo, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == stopVideo && VideoPermission(player, videoEntity, stopVideoPerm))
                    {
                        videoEntity.svEntity.SvStopVideo();
                        player.svPlayer.DestroyMenu(videoPanel);
                    }
                    else if (VideoPermission(player, videoEntity, defaultVideoPerm, true) && int.TryParse(optionID, out var index))
                    {
                        videoEntity.svEntity.SvStartDefaultVideo(index);
                        player.svPlayer.DestroyMenu(videoPanel);
                    }
                    else if (optionID == youtubePanel && VideoPermission(player, videoEntity, YTPlayPerm))
                    {
                        List<LabelID> options = new List<LabelID>();

                        if (VideoPermission(player, videoEntity, YTPlayPerm))
                        {
                            options.Add(new LabelID("Play via link", "ytplay"));
                        }
                        player.svPlayer.SendOptionMenu("YouTube Panel", videoEntity.ID, youtubePanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    return false;

                case youtubePanel:
                    if (optionID == "ytplay" && VideoPermission(player, videoEntity, YTPlayPerm))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You have provide video link");
                        player.svPlayer.SendInputMenu("Link", targetID, youtubePlay, InputField.ContentType.Standard, 128);
                    }
                    return false;
                default:
                    return true;
            }
        }


        [Target(GameSourceEvent.PlayerSubmitInput, ExecutionMode.Test)]
        public bool OnSubmitInput(ShPlayer player, int targetID, string menuID, string input)
        {
            ShEntity videoEntity = EntityCollections.FindByID(targetID);

            switch (menuID)
            {
                case youtubePlay:
                    IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(input);
                    VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4);
                    if (video.RequiresDecryption) { DownloadUrlResolver.DecryptDownloadUrl(video); }

                    if (VideoPermission(player, videoEntity, YTPlayPerm, true) && input.StartsWith("https://"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo(video.DownloadUrl);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You must have permission and start with 'https://'");
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
}
