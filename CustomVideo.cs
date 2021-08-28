using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using UnityEngine.UI;

namespace YoutubePlayer.CustomVideo
{
    public class HackingContainer
    {
        public ShPlayer player;
        public ShApartment targetApartment;
        public ShPlayer targetPlayer;

        public HackingContainer(ShPlayer player, int apartmentID, string username)
        {
            this.player = player;
            targetApartment = EntityCollections.FindByID<ShApartment>(apartmentID);
            EntityCollections.TryGetPlayerByNameOrID(username, out targetPlayer);
        }
        public ApartmentPlace ApartmentPlace => targetPlayer.ownedApartments.TryGetValue(targetApartment, out var apartmentPlace) ? apartmentPlace : null;

        public bool IsValid => player && targetApartment && targetPlayer && player.IsMobile && player.InActionRange(targetApartment) && ApartmentPlace != null;

        public bool HackingActive => player.svPlayer.hackingGame != null;
    }
    public class Player
    {
        private const float securityCutoff = 0.99f;
        public ShPlayer player;
        public ShApartment targetApartment;
        public ShPlayer targetPlayer;
        private const string youtubePanel = "youtubePanel";
        private const string trendingPanel = "trendingPanel";
        private const string securityPanel = "securityPanel";
        private const string enterPasscode = "enterPasscode";
        private const string setPasscode = "setPasscode";
        private const string clearPasscode = "clearPasscode";
        private const string upgradeSecurity = "upgradeSecurity";
        private const string hackPanel = "hackPanel";
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
                foreach (VideoOption option in videoEntity.svEntity.videoOptions)
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


        private int SecurityUpgradeCost(float currentLevel) => (int)(15000f * currentLevel * currentLevel + 200f);

        [Target(GameSourceEvent.PlayerOptionAction, ExecutionMode.Override)]
        public void OnOptionAction(ShPlayer player, int targetID, string menuID, string optionID, string actionID)
        {
            switch (menuID)
            {
                case securityPanel:
                    var apartment = EntityCollections.FindByID<ShApartment>(targetID);

                    if (!apartment) return;

                    switch (optionID)
                    {
                        case enterPasscode:
                            player.svPlayer.SendInputMenu("Enter Passcode", targetID, enterPasscode, InputField.ContentType.Password);
                            break;
                        case setPasscode:
                            player.svPlayer.SendInputMenu("Set Passcode", targetID, setPasscode, InputField.ContentType.Password);
                            break;
                        case clearPasscode:
                            if (player.ownedApartments.TryGetValue(apartment, out var apartmentPlace))
                            {
                                apartmentPlace.svPasscode = null;
                                player.svPlayer.SendGameMessage("Apartment Passcode Cleared");
                            }
                            else player.svPlayer.SendGameMessage("No Apartment Owned");
                            break;
                        case upgradeSecurity:
                            if (player.ownedApartments.TryGetValue(apartment, out var securityPlace) && securityPlace.svSecurity < securityCutoff)
                            {
                                int upgradeCost = SecurityUpgradeCost(securityPlace.svSecurity);

                                if (player.MyMoneyCount >= upgradeCost)
                                {
                                    player.TransferMoney(DeltaInv.RemoveFromMe, upgradeCost);
                                    securityPlace.svSecurity += 0.1f;
                                    player.svPlayer.SendGameMessage("Apartment Security Upgraded");
                                    player.svPlayer.SvSecurityPanel(apartment.ID);
                                }
                                else
                                {
                                    player.svPlayer.SendGameMessage("Insufficient funds");
                                }
                            }
                            else player.svPlayer.SendGameMessage("Unable");
                            break;
                        case hackPanel:
                            var options = new List<LabelID>();
                            foreach (var clone in apartment.svApartment.clones.Values)
                            {
                                options.Add(new LabelID($"{clone.svOwner.username} - Difficulty: {clone.svSecurity.ToPercent()}", clone.svOwner.username));
                            }
                            player.svPlayer.SendOptionMenu("&7Places", targetID, hackPanel, options.ToArray(), new LabelID[] { new LabelID("Hack", string.Empty) });
                            break;
                    }
                    break;

                case hackPanel:
                    var hackingContainer = new HackingContainer(player, targetID, optionID);
                    if (hackingContainer.IsValid)
                    {
                        player.svPlayer.StartHackingMenu("Hack Security Panel", targetID, menuID, optionID, hackingContainer.ApartmentPlace.svSecurity);
                        player.StartCoroutine(CheckValidHackingGame(hackingContainer));
                    }
                    break;

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
                    else if (optionID == youtubePanel && player.svPlayer.HasPermission("yp.main"))
                    {
                        List<LabelID> options = new List<LabelID>();

                        if (player.svPlayer.HasPermission("yp.play"))
                        {
                            options.Add(new LabelID("Play via link", ytplay));
                        }
                        if (player.svPlayer.HasPermission("yp.search"))
                        {
                            options.Add(new LabelID("Play via search", ytsearch));
                        }
                        if (player.svPlayer.HasPermission("yp.trending"))
                        {
                            options.Add(new LabelID("Trending", yttrending));
                        }

                        string title = "YouTube Panel";
                        player.svPlayer.SendOptionMenu(title, videoEntity.ID, youtubePanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }

                    break;
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
                        options.Add(new LabelID("Default", Default));
                        options.Add(new LabelID("Gaming", Gaming));
                        options.Add(new LabelID("Music", Music));
                        options.Add(new LabelID("Movie", Movie));

                        string title = "Trending Type";
                        player.svPlayer.SendOptionMenu(title, videoEntityTrending.ID, trendingPanel, options.ToArray(), new LabelID[] { new LabelID("Select", string.Empty) });
                    }
                    break;

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
                    break;

                default:
                    if (targetID >= 0)
                    {
                        player.svPlayer.job.OnOptionMenuAction(targetID, menuID, optionID, actionID);
                    }
                    else
                    {
                        var target = EntityCollections.FindByID<ShPlayer>(-targetID);
                        if (target) target.svPlayer.job.OnOptionMenuAction(player.ID, menuID, optionID, actionID);
                    }
                    break;
            }
        }

        private IEnumerator CheckValidHackingGame(HackingContainer hackingContainer)
        {
            while (hackingContainer.HackingActive && hackingContainer.IsValid)
            {
                yield return null;
            }

            if (hackingContainer.HackingActive) hackingContainer.player.svPlayer.SvHackingStop(true);
        }

        [Target(GameSourceEvent.PlayerSubmitInput, ExecutionMode.Override)]
        public void OnSubmitInput(ShPlayer player, int targetID, string menuID, string input)
        {
            switch (menuID)
            {
                case enterPasscode:
                    var a1 = EntityCollections.FindByID<ShApartment>(targetID);

                    foreach (var a in a1.svApartment.clones.Values)
                    {
                        if (a.svPasscode != null && a.svPasscode == input)
                        {
                            player.svPlayer.SvEnterDoor(targetID, a.svOwner, true);
                            return;
                        }
                    }
                    player.svPlayer.SendGameMessage("Passcode: No Match");
                    break;

                case setPasscode:
                    var a2 = EntityCollections.FindByID<ShApartment>(targetID);
                    if (a2 && player.ownedApartments.TryGetValue(a2, out var ap2))
                    {
                        ap2.svPasscode = input;
                        player.svPlayer.SendGameMessage("Apartment Passcode Set");
                        return;
                    }
                    player.svPlayer.SendGameMessage("No Apartment Owned");
                    break;

                case customVideo:
                    var videoEntity = EntityCollections.FindByID(targetID);

                    if (VideoPermission(player, videoEntity, PermEnum.VideoCustom) && input.StartsWith("https://"))
                    {
                        videoEntity.svEntity.SvStartCustomVideo(input);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("Must have permission ");
                    }
                    break;

                case ytplay:
                    ShEntity videoEntity2 = EntityCollections.FindByID(targetID);

                    if (player.svPlayer.HasPermission("yp.play") && input.StartsWith("https://www.youtube.com/"))
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Resolving url....");
                        var url = @input;
                        var uri = new Uri(url);
                        var query = HttpUtility.ParseQueryString(uri.Query);
                        var videoId = query["v"];
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        videoEntity2.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/play/" + videoId);
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission and start with 'https://www.youtube.com/'");
                    }
                    break;

                case ytsearch:
                    ShEntity videoEntity3 = EntityCollections.FindByID(targetID);
                    if (player.svPlayer.HasPermission("yp.search"))
                    {
                        var encodename = HttpUtility.UrlEncode(input);
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Fetching data from youtube....");
                        videoEntity3.svEntity.SvStartCustomVideo("https://ytproxy.sploecyber.repl.co/api/search?query=" + encodename;
                    }
                    else
                    {
                        player.svPlayer.SendGameMessage("〔<color=#546eff>YouTubePlayer</color>〕 |  Must have permission");
                    }
                    break;

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
                    break;
            }
        }
    }
}
