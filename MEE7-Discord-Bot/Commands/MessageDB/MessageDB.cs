﻿using Discord;
using Discord.WebSocket;
using MEE7.Backend;
using MEE7.Backend.HelperFunctions;
using Newtonsoft.Json;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MEE7.Commands.MessageDB
{
    class MessageDB : Command
    {
        string dbDirPath = $"Commands{Path.DirectorySeparatorChar}MessageDB{Path.DirectorySeparatorChar}";
        string dbPath = $"Commands{Path.DirectorySeparatorChar}MessageDB{Path.DirectorySeparatorChar}db.json";
        object dbLock = new object();

        public MessageDB() : base("messageDB", "makes database stuff without databases because no", false, true)
        {

        }

        public override void Execute(SocketMessage message)
        {
            var split = message.Content.Split(' ');

            lock (dbLock)
            {
                if (split.Length <= 1)
                {
                    return;
                }
                else if (split[1] == "build")
                {
                    if (message.Author.Id != Program.Master.Id)
                    {
                        DiscordNETWrapper.SendText("You are not allowed to do that :/", message.Channel).Wait();
                        return;
                    }

                    if (!File.Exists(dbPath))
                        return;

                    var curGuild = Program.GetGuildFromChannel(message.Channel);
                    DBGuild db = DBFromGuild(curGuild);
                    File.WriteAllText(GetFilePath(db), CreateJson(db));

                    DiscordNETWrapper.SendText("Server db build!", message.Channel).Wait();
                }
                else if (split[1] == "countChannelMessages")
                {
                    DBGuild dbGuild = null;
                    if ((dbGuild = GetGuild(message)) == null)
                        return;

                    string re = "";
                    foreach (var channel in dbGuild.TextChannels.OrderByDescending(x => x.Messages.Count))
                    {
                        re += $"`{channel.Name}`: **{channel.Messages.Count}** messages\n";
                    }
                    DiscordNETWrapper.SendText(re, message.Channel).Wait();
                }
                else if (split[1] == "countUserMessages")
                {
                    DBGuild dbGuild = null;
                    if ((dbGuild = GetGuild(message)) == null)
                        return;

                    string re = "";

                    List<DBMessage> allGuildMessages = new List<DBMessage>();
                    foreach (var channel in dbGuild.TextChannels)
                        allGuildMessages.AddRange(channel.Messages);

                    var grouping = allGuildMessages.GroupBy(x => x.AuthorName);

                    foreach (var g in grouping.OrderByDescending(g => g.Count()))
                        re += $"`{g.Key}`: **{g.Count()}** messages\n";

                    DiscordNETWrapper.SendText(re, message.Channel).Wait();
                }
                else if (split[1] == "plotActivityOverTime")
                {
                    DBGuild dbGuild = null;
                    if ((dbGuild = GetGuild(message)) == null)
                        return;

                    List<DBMessage> allGuildMessages = new List<DBMessage>();
                    try
                    {
                        ulong channelID = Convert.ToUInt64(split[2]);
                        allGuildMessages = dbGuild.TextChannels.First(x => x.Id == channelID).Messages;
                    }
                    catch
                    {
                        allGuildMessages.Clear();
                        foreach (var channel in dbGuild.TextChannels)
                            allGuildMessages.AddRange(channel.Messages);
                    }

                    if (allGuildMessages.Count == 0)
                    {
                        DiscordNETWrapper.SendText("No messages to plot :/", message.Channel).Wait();
                        return;
                    }

                    var messagesGroupedByDay = allGuildMessages.
                        OrderBy(x => x.Timestamp).
                        Select(x => new Tuple<DBMessage, string>(x, x.Timestamp.ToShortDateString())).
                        GroupBy(x => x.Item2).
                        Select(x => new Tuple<string, int>(x.Key, x.Count())).
                        ToArray();

                    Plot plt = new Plot();
                    plt.PlotScatter(
                        messagesGroupedByDay.
                        Select(x => DateTime.Parse(x.Item1).ToOADate()).
                        ToArray(), 
                        messagesGroupedByDay.
                        Select(x => (double)x.Item2).
                        ToArray());
                    plt.Ticks(dateTimeX: true);
                    plt.Legend();
                    plt.Title("Server Activity over Time");
                    plt.YLabel("Messages send on that day");
                    plt.XLabel("Day");

                    DiscordNETWrapper.SendBitmap(plt.GetBitmap(), message.Channel).Wait();
                }
            }
        }

        DBGuild GetGuild(SocketMessage message)
        {
            var curGuild = Program.GetGuildFromChannel(message.Channel);
            if (!File.Exists(GetFilePath(curGuild)))
            {
                DiscordNETWrapper.SendText("I don't have data saved for this server yet :/", message.Channel).Wait();
                return null;
            }
            return ParseJson(File.ReadAllText(GetFilePath(curGuild)));
        }

        string GetFilePath(SocketGuild g) => $"{dbDirPath}{g.Id}db.json";
        string GetFilePath(DBGuild g) => $"{dbDirPath}{g.Id}db.json";

        DBGuild ParseJson(string json) => JsonConvert.DeserializeObject<DBGuild>(json);
        string CreateJson(DBGuild db) => JsonConvert.SerializeObject(db);

        public DBGuild DBFromGuild(SocketGuild guild)
        {
            DBGuild re = new DBGuild();
            re.CreatedAt = guild.CreatedAt.UtcDateTime;
            re.IconUrl = guild.IconUrl;
            re.Id = guild.Id;
            re.Name = guild.Name;
            re.OwnerId = guild.OwnerId;
            re.SplashUrl = guild.SplashUrl;

            re.TextChannels = new List<DBTextChannel>();
            foreach (var textChannel in guild.TextChannels)
            {
                DBTextChannel reChannel = new DBTextChannel();

                reChannel.CreatedAt = textChannel.CreatedAt.UtcDateTime;
                reChannel.GuildId = re.Id;
                reChannel.Id = textChannel.Id;
                reChannel.Name = textChannel.Name;

                reChannel.Messages = new List<DBMessage>();
                foreach (var message in DiscordNETWrapper.EnumerateMessages(textChannel as IMessageChannel))
                {
                    DBMessage reMessage = new DBMessage();

                    reMessage.Attachements = message.Attachments.Select(x => x.Url).ToArray();
                    reMessage.AuthorId = message.Author.Id;
                    reMessage.AuthorName = message.Author.Username;
                    reMessage.Channel = message.Channel.Id;
                    reMessage.Content = message.Content;
                    reMessage.Embeds = message.Embeds.Select(x => new DBEmbed() 
                    { 
                        AuthorURL = x.Author.HasValue ? x.Author.Value.Url : "", 
                        Color = x.Color, 
                        Desc = x.Description, 
                        Fields = x.Fields.Select(y => new DBEmbedField() 
                        { 
                            Title = y.Name,
                            Content = y.Value
                        }).ToList(),
                        Footer = x.Footer.HasValue ? x.Footer.Value.Text : "",
                        ImageUrl = x.Image.HasValue ? x.Image.Value.Url : "",
                        ThumbnailUrl = x.Thumbnail.HasValue ? x.Thumbnail.Value.Url : "",
                        Timestamp = x.Timestamp.HasValue ? x.Timestamp.Value.UtcDateTime : DateTime.MinValue,
                        Title = x.Title
                    }).ToList();
                    reMessage.Id = message.Id;
                    reMessage.IsPinned = message.IsPinned;
                    reMessage.MentionedChannels = message.MentionedChannelIds.ToList();
                    reMessage.MentionedRoles = message.MentionedRoleIds.ToList();
                    reMessage.MentionedUsers = message.MentionedUserIds.ToList();
                    reMessage.Timestamp = message.Timestamp.UtcDateTime;

                    reChannel.Messages.Add(reMessage);
                }

                re.TextChannels.Add(reChannel);
            }

            return re;
        }
    }
}
