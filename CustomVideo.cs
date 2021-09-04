using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using System;
using System.Collections.Generic;
using System.Web;
using UnityEngine.UI;

namespace YoutubePlayer.CustomVideo
{
    public class Player
    {
        private const string youtubePanel = "youtubePanel";
        private const string trendingPanel = "trendingPanel";
        private const string videoPanel = "videoPanel";
        private const string customVideo = "customVideo";
        private const string stopVideo = "stopVideo";
        private const string ytplay = "ytplay";
        private const string ytsearch = "ytsearch";
        private const string yttrending = "yttrending";
        private const string Default = "Default";
        private const string Movie = "Movie";
        private const string Music = "Music";
        private const string Gaming = "Gaming";

        private const string defaultVideoPerm = "bp.videoDefault";
        private const string stopVideoPerm = "bp.videoStop";
        private const string customVideoPerm = "bp.videoCustom";

        private bool VideoPermission(ShPlayer player, ShEntity videoPlayer, string permission, bool checkLimit = false)
        {
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

            return videoPlayer && player.InActionRange(videoPlayer) && (player.InOwnApartment || player.svPlayer.HasPermission(permission));
        }

        [Target(GameSourceEvent.PlayerVideoPanel, ExecutionMode.Override)]
        public void OnVideoPanel(ShPlayer player, ShEntity videoEntity)
        {
            List<LabelID> options = new List<LabelID>();
            if (VideoPermission(player, videoEntity, "yp.play"))
            {
                options.Add(new LabelID("&cYouTube", youtubePanel));
            }
            if (VideoPermission(player, videoEntity, defaultVideoPerm))
            {
                int index = 0;
                foreach (VideoOption option in player.manager.svManager.videoOptions)
                {
                    options.Add(new LabelID(option.label, index.ToString()));
                    index++;
                }
            }
            if (VideoPermission(player, videoEntity, customVideoPerm))
            {
                options.Add(new LabelID("&eCustom Video URL", customVideo));
            }
            if (VideoPermission(player, videoEntity, stopVideoPerm))
            {
                options.Add(new LabelID("&4Stop Video", stopVideo));
            }
            string title = "&7Video Panel";
            player.svPlayer.SendOptionMenu(title, videoEntity.ID, videoPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
        }

        [Target(GameSourceEvent.PlayerOptionAction, ExecutionMode.Test)]
        public bool OnOptionAction(ShPlayer player, int targetID, string menuID, string optionID, string actionID)
        {
            ShEntity videoEntity = EntityCollections.FindByID(targetID);
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
                    else if (VideoPermission(player, videoEntity, defaultVideoPerm, true) && int.TryParse(optionID, out int index))
                    {
                        videoEntity.svEntity.SvStartDefaultVideo(index);
                        player.svPlayer.DestroyMenu(videoPanel);
                    }
                    else if (optionID == youtubePanel && VideoPermission(player, videoEntity, "yp.play"))
                    {
                        List<LabelID> options = new List<LabelID>();

                        if (VideoPermission(player, videoEntity, "yp.play"))
                        {
                            options.Add(new LabelID("Youtube - Play via link", ytplay));
                        }
                        if (VideoPermission(player, videoEntity, "yp.search"))
                        {
                            options.Add(new LabelID("Youtube - Play via search", ytsearch));
                        }
                        if (VideoPermission(player, videoEntity, "yp.trending"))
                        {
                            options.Add(new LabelID("Youtube - Trending", yttrending));
                        }

                        string title = "YouTube Panel";
                        player.svPlayer.SendOptionMenu(title, videoEntity.ID, youtubePanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    return false;
                case youtubePanel:
                    if (optionID == ytplay && VideoPermission(player, videoEntity, "yp.play"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You have provide video link");
                        player.svPlayer.SendInputMenu("Link", targetID, ytplay, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == ytsearch && VideoPermission(player, videoEntity, "yp.search"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You have provide video name");
                        player.svPlayer.SendInputMenu("Name", targetID, ytsearch, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == yttrending && VideoPermission(player, videoEntity, "yp.trending"))
                    {
                        List<LabelID> options = new List<LabelID>();
                        options.Add(new LabelID("General", Default));
                        options.Add(new LabelID("Gaming", Gaming));
                        options.Add(new LabelID("Music", Music));
                        options.Add(new LabelID("Movie", Movie));

                        string title = "Trending Type";
                        player.svPlayer.SendOptionMenu(title, videoEntity.ID, trendingPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    return false;
                case trendingPanel:

                    if (!VideoPermission(player, videoEntity, "yp.trending", true))
                        return false;

                    if (optionID == Default)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending");
                        player.svPlayer.DestroyMenu();
                    }
                    else if (optionID == Gaming)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=gaming");
                        player.svPlayer.DestroyMenu();
                    }
                    else if (optionID == Music)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=music");
                        player.svPlayer.DestroyMenu();
                    }
                    else if (optionID == Movie)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=movies");
                        player.svPlayer.DestroyMenu();
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
                case ytplay:
                    if (VideoPermission(player, videoEntity, "yp.play") && input.StartsWith("https://"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/play?url=" + input);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission and start with 'https://'");
                    }
                    return false;
                case ytsearch:
                    if (VideoPermission(player, videoEntity, "yp.search"))
                    {
                        var encoded = HttpUtility.UrlEncode(input);
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Please wait a moment");
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/search?query=" + encoded);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission");
                    }
                    return false;
                case yttrending:
                    if (VideoPermission(player, videoEntity, "yp.trending"))
                    {
                        var url = @input;
                        var uri = new Uri(url);
                        var query = HttpUtility.ParseQueryString(uri.Query);
                        var videoId = query["v"];
                        videoEntity.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/play/" + videoId);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission");
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
}
