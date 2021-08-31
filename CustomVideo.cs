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

        private bool VideoPermission(ShPlayer player, ShEntity videoPlayer, PermEnum permission)
        {
            return videoPlayer && player.InActionRange(videoPlayer) && (player.InOwnApartment || player.svPlayer.HasPermissionBP(permission));
        }

        [Target(GameSourceEvent.PlayerVideoPanel, ExecutionMode.Override)]
        public void OnVideoPanel(ShPlayer player, ShEntity videoEntity)
        {
            List<LabelID> options = new List<LabelID>();
            if (player.svPlayer.HasPermission("yp.main"))
            {
                options.Add(new LabelID("&cYouTube", youtubePanel));
            }
            if (VideoPermission(player, videoEntity, PermEnum.VideoDefault))
            {
                int index = 0;
                foreach (VideoOption option in player.manager.svManager.videoOptions)
                {
                    options.Add(new LabelID(option.label, index.ToString()));
                    index++;
                }
            }
            if (VideoPermission(player, videoEntity, PermEnum.VideoCustom))
            {
                options.Add(new LabelID("&eCustom Video URL", customVideo));
            }
            if (VideoPermission(player, videoEntity, PermEnum.VideoStop))
            {
                options.Add(new LabelID("&4Stop Video", stopVideo));
            }
            string title = "&7Video Panel";
            player.svPlayer.SendOptionMenu(title, videoEntity.ID, videoPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
        }

        [Target(GameSourceEvent.PlayerOptionAction, ExecutionMode.Test)]
        public bool OnOptionAction(ShPlayer player, int targetID, string menuID, string optionID, string actionID)
        {
            switch (menuID)
            {
                case videoPanel:
                    ShEntity videoEntity = EntityCollections.FindByID(targetID);
                    if (optionID == customVideo && VideoPermission(player, videoEntity, PermEnum.VideoCustom))
                    {
                        player.svPlayer.SendGameMessage("Only direct video links supported - Can upload to Imgur or Discord and link that");
                        player.svPlayer.SendInputMenu("Direct MP4/WEBM URL", targetID, customVideo, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == stopVideo && VideoPermission(player, videoEntity, PermEnum.VideoStop))
                    {
                        videoEntity.svEntity.SvStopVideo();
                        player.svPlayer.DestroyMenu(videoPanel);
                    }
                    else if (VideoPermission(player, videoEntity, PermEnum.VideoDefault) && int.TryParse(optionID, out int index))
                    {
                        videoEntity.svEntity.SvStartDefaultVideo(index);
                        player.svPlayer.DestroyMenu(videoPanel);
                    }
                    else if (optionID == youtubePanel && player.svPlayer.HasPermission("yp.play"))
                    {
                        List<LabelID> options = new List<LabelID>();

                        if (player.svPlayer.HasPermission("yp.play"))
                        {
                            options.Add(new LabelID("Youtube - Play via link", ytplay));
                        }
                        if (player.svPlayer.HasPermission("yp.search"))
                        {
                            options.Add(new LabelID("Youtube - Play via search", ytsearch));
                        }
                        if (player.svPlayer.HasPermission("yp.trending"))
                        {
                            options.Add(new LabelID("Youtube - Trending", yttrending));
                        }

                        string title = "YouTube Panel";
                        player.svPlayer.SendOptionMenu(title, videoEntity.ID, youtubePanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    return false;
                case youtubePanel:
                    ShEntity videoEntityPanel = EntityCollections.FindByID(targetID);
                    if (optionID == ytplay && player.svPlayer.HasPermission("yp.play"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You have provide video link");
                        player.svPlayer.SendInputMenu("Link", targetID, ytplay, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == ytsearch && player.svPlayer.HasPermission("yp.search"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  You have provide video name");
                        player.svPlayer.SendInputMenu("Name", targetID, ytsearch, InputField.ContentType.Standard, 128);
                    }
                    else if (optionID == yttrending && player.svPlayer.HasPermission("yp.trending"))
                    {
                        var videoEntityTrending = EntityCollections.FindByID(targetID);
                        List<LabelID> options = new List<LabelID>();
                        options.Add(new LabelID("General", Default));
                        options.Add(new LabelID("Gaming", Gaming));
                        options.Add(new LabelID("Music", Music));
                        options.Add(new LabelID("Movie", Movie));

                        string title = "Trending Type";
                        player.svPlayer.SendOptionMenu(title, videoEntityTrending.ID, trendingPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    return false;
                case trendingPanel:
                    ShEntity trendingPanel2 = EntityCollections.FindByID(targetID);
                    if (optionID == Default)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        trendingPanel2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending");
                    }
                    else if (optionID == Gaming)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        trendingPanel2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=gaming");
                    }
                    else if (optionID == Music)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        trendingPanel2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=music");
                    }
                    else if (optionID == Movie)
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        trendingPanel2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/trending?type=movies");
                    }
                    return false;
                default:
                    return true;
            }
        }

        [Target(GameSourceEvent.PlayerSubmitInput, ExecutionMode.Test)]
        public bool OnSubmitInput(ShPlayer player, int targetID, string menuID, string input)
        {
            switch (menuID)
            {
                case ytplay:
                    ShEntity videoEntity2 = EntityCollections.FindByID(targetID);

                    if (player.svPlayer.HasPermission("yp.play") && input.StartsWith("https://"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        videoEntity2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/play?url=" + input);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission and start with 'https://'");
                    }
                    return false;
                case ytsearch:
                    ShEntity videoEntity3 = EntityCollections.FindByID(targetID);
                    if (player.svPlayer.HasPermission("yp.search"))
                    {
                        var encodename = HttpUtility.UrlEncode(input);
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        videoEntity3.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/search?query=" + encodename);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission");
                    }
                    return false;
                case yttrending:
                    ShEntity videoEntity4 = EntityCollections.FindByID(targetID);

                    if (player.svPlayer.HasPermission("yp.trending"))
                    {
                        var url = @input;
                        var uri = new Uri(url);
                        var query = HttpUtility.ParseQueryString(uri.Query);
                        var videoId = query["v"];
                        videoEntity4.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/play/" + videoId);
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
