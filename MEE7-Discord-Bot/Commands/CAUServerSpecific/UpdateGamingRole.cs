﻿using Discord.WebSocket;
using MEE7.Backend;
using MEE7.Backend.HelperFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEE7.Commands.CAUServerSpecific
{
    class UpdateGamingRole : Command
    {
        public UpdateGamingRole() : base("", "", false, true)
        {
            Program.OnConnected += Program_OnConnected;
        }

        private void Program_OnConnected()
        {
            SocketGuild uniServer = Program.GetGuildFromID(479950092938248193);
            var gamingRole = uniServer.Roles.First(x => x.Id == 539810100173471744);
            int gamingPos = gamingRole.Position;
            int aboveGamingPos = uniServer.Roles.First(x => x.Id == 479952941827096578).Position; // Using Fachschaftler*in role pos as the pos above gaming roles
            Program.OnGuildMemberUpdated += (SocketGuildUser arg1, SocketGuildUser arg2) =>
            {
                if (arg2.Guild.Id == 479950092938248193 &&
                        arg2.Roles.Any(x => x.Position > gamingPos && x.Position < aboveGamingPos))
                    arg2.AddRoleAsync(gamingRole);
            };
        }

        public override void Execute(SocketMessage message) { }
    }
}