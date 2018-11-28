﻿using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiscordBot.Commands
{
    public class AnimeQuote : Command
    {
        public AnimeQuote() : base("animeQuote", "Get a random anime quote", false)
        {

        }

        public override async Task execute(SocketMessage commandmessage)
        {
            string[] Files = Directory.GetFiles(Global.CurrentExecutablePath + "\\Resources\\Anime Quotes");
            List<string> SendableFiles = new List<string>();
            foreach (string s in Files)
            {
                if (Path.GetExtension(s) == ".jpg" || Path.GetExtension(s) == ".png" || Path.GetExtension(s) == ".jpeg" ||
                    Path.GetExtension(s) == ".gif" || Path.GetExtension(s) == ".mp4")
                    SendableFiles.Add(s);
            }
            string filepath = SendableFiles[Global.RDM.Next(SendableFiles.Count)];
            await Global.SendFile(filepath, commandmessage.Channel);
        }
    }
}
