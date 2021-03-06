﻿using Abyssal.Common;
using AbyssalSpotify;
using Disqord;
using Humanizer;
using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Lament.Helpers;

namespace Lament.Modules
{
    [Name("Spotify")]
    [Description(
        "Commands that relate to Spotify, a digital music service that gives you access to millions of songs.")]
    public class SpotifyModule : LamentModuleBase
    {
        private readonly SpotifyClient _spotify;

        public SpotifyModule(SpotifyClient spotify)
        {
            _spotify = spotify;
        }

        [Command("track", "spotify", "sp")]
        [Description("Searches the Spotify database for a song.")]
        [RunMode(RunMode.Parallel)]
        public async Task TrackAsync(
            [Name("Track Name")]
            [Description("The track to search for.")]
            [Remainder]
            string trackQuery = null)
        {
            SpotifyTrack track;
            if (trackQuery != null)
            {
                var tracks = await _spotify.SearchAsync(trackQuery, SearchType.Track).ConfigureAwait(false);
                track = tracks.Tracks.Items.FirstOrDefault();
            }
            else
            {
                var spotifyActivity =
                    Context.User.Presence.Activities.FirstOrDefault(d => d is SpotifyActivity spot) as
                        SpotifyActivity;
                if (spotifyActivity == null)
                {
                    await ReplyAsync("You didn't supply a track, and you're not currently listening to anything!");
                    return;
                }

                track = await _spotify.GetTrackAsync(spotifyActivity.TrackId).ConfigureAwait(false);
            }

            if (track == null)
            {
                await ReplyAsync("Cannot find a track by that name.");
                return;
            }
            await ReplyAsync(embed: CreateTrackEmbed(track).Build());
        }

        [Command("album")]
        [Description("Searches the Spotify database for an album.")]
        [RunMode(RunMode.Parallel)]
        public async Task Command_SearchAlbumAsync(
            [Name("Album Name")]
            [Description("The album name to search for.")]
            [Remainder]
            string? albumQuery = null)
        {
            SpotifyAlbum album;
            if (albumQuery == null)
            {
                if (!(Context.User.Presence.Activities.FirstOrDefault(d => d is SpotifyActivity) is SpotifyActivity spotifyActivity))
                {
                    await ReplyAsync("You didn't supply an album name, and you're not currently listening to anything!");
                    return;
                }

                var track = await _spotify.GetTrackAsync(spotifyActivity.TrackId).ConfigureAwait(false);
                album = await track.Album.GetFullEntityAsync().ConfigureAwait(false);
            }
            else
            {
                try
                {
                    var result = await _spotify.SearchAsync(albumQuery, SearchType.Album).ConfigureAwait(false);
                    var sa0 = result.Albums.Items.FirstOrDefault();

                    if (sa0 == null)
                    {
                        await ReplyAsync("Cannot find album by that name.");
                        return;
                    }

                    album = await _spotify.GetAlbumAsync(sa0.Id.Id).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyAsync("An error occurred while searching for the album.");
                    return;
                }
            }

            var embed = new LocalEmbedBuilder
            {
                Color = Context.Color,
                Author = new LocalEmbedAuthorBuilder
                {
                    Name = album.Artists.Select(a => a.Name).Humanize(),
                    IconUrl = album.Images.FirstOrDefault()?.Url,
                    Url = album.Artists.FirstOrDefault()?.Id.Url
                },
                Title = album.Name,
                ThumbnailUrl = album.Images.FirstOrDefault()?.Url,
                Footer = new LocalEmbedFooterBuilder
                {
                    Text = string.Join("\n",
                        album.Copyrights.Distinct().Select(a =>
                            $"[{a.CopyrightType.Humanize()}] {a.CopyrightText}"))
                }
            };

            var tracks = album.Tracks.Items;
            var size = 0;

            var tracksOutput = tracks.TakeWhile((track, len) =>
                {
                    if (size > 2000) return false;
                    var st =
                        $"{len + 1} - {UrlHelper.CreateMarkdownUrl(track.Name, track.Id.Url)} by {track.Artists.Select(ab => ab.Name).Humanize()}";
                    size += st.Length;
                    return size <= 2000;
                }).Select((track, b) =>
                    $"{b + 1} - {UrlHelper.CreateMarkdownUrl(track.Name, track.Id.Url)} by {track.Artists.Select(ab => ab.Name).Humanize()}")
                .ToList();

            if (tracksOutput.Count != tracks.Length)
                tracksOutput.Add($"And {tracks.Length - tracksOutput.Count} more...");

            embed.Description = string.Join("\n", tracksOutput);

            embed.AddField("Release Date", album.ReleaseDate.ToString("D"), true);

            var length = TimeSpan.FromMilliseconds(album.Tracks.Items.Sum(a => a.Duration.TotalMilliseconds));

            embed.AddField("Length", $"{length.Hours} hours, {length.Minutes} minutes", true);
            await ReplyAsync(embed: embed.Build());
        }

        private LocalEmbedBuilder CreateTrackEmbed(SpotifyTrack track)
        {
            var embed = new LocalEmbedBuilder
            {
                Color = Context.Color,
                Author = new LocalEmbedAuthorBuilder
                {
                    Name = track.Artists.Select(a => a.Name).Humanize(),
                    IconUrl = track.Album.Images.FirstOrDefault()?.Url,
                    Url = track.Artists.FirstOrDefault()?.Id.Url
                },
                Title = track.Name,
                ThumbnailUrl = track.Album.Images.FirstOrDefault()?.Url
            };

            embed.AddField("Length", $"{track.Duration.Minutes} minutes, {track.Duration.Seconds} seconds", true);
            embed.AddField("Release Date", track.Album.ReleaseDate.ToString("D"), true);
            embed.AddField("Album", UrlHelper.CreateMarkdownUrl(track.Album.Name, track.Album.Id.Url), true);
            if (track.HasExplicitLyrics) embed.Description = "This track contains explicit lyrics.";

            embed.AddField("\u200B", UrlHelper.CreateMarkdownUrl("Open in Spotify", track.Id.Url));

            return embed;
        }
    }
}