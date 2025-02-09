using System;
using System.ComponentModel;
using DSharpPlus.Commands;

namespace DSharpBot
{
    public class PingCommand
    {
        [Command("ping")]
        [Description("Check latency")]
        public static async ValueTask Ping(CommandContext context) 
        {
            string? guildId = Environment.GetEnvironmentVariable("GUILD_ID");
            if (guildId != null)
            {
                var latency = context.Client.GetConnectionLatency(ulong.Parse(guildId));
                await context.RespondAsync($"Pong!, Latency is {latency.Milliseconds}ms");
            }
            else
            {
                // Handle the case where GUILD_ID is not set
                await context.RespondAsync("Error: GUILD_ID environment variable is not set.");
            }
        }
    }
}