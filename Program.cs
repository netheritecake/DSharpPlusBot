﻿using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;


namespace DSharpBot
{
    class Program
    {
        static async Task Main(string[] args)
        {   
            
            string? botToken = Environment.GetEnvironmentVariable("TOKEN");
            

            if (botToken is null)
            {
                throw new InvalidOperationException("TOKEN environment variable is not set.");
            }


            // Create a new DiscordClient
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(botToken, DiscordIntents.All);
            
            // Configure the event handlers
            builder.ConfigureEventHandlers(
                b => b.HandleComponentInteractionCreated(async (s, e) =>
                {
                    // TicTacToe button clicks
                    if (e.Id == "tttDelete")
                    {
                        if (TicTacToeCommand.tttGames.TryGetValue(e.User.Id, out TicTacToeGame? tttGame))
                        {
                            var builder = tttGame.EndGame($"{e.User.Username} resigned.");
                            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
                        }
                            
                        else
                            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, 
                            new DiscordInteractionResponseBuilder().WithContent("You are not in a Tic-Tac-Toe game!").AsEphemeral());
                    }
                    else if (e.Id.StartsWith("ttt"))
                    {   
                        if (TicTacToeCommand.tttGames.TryGetValue(e.User.Id, out TicTacToeGame? tttGame))
                            await tttGame.MakeMove(e);
                        else
                            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, 
                            new DiscordInteractionResponseBuilder().WithContent("You are not in a Tic-Tac-Toe game!").AsEphemeral());
                    }

                    // Minesweeper button clicks
                    else if (e.Id.StartsWith("ms"))
                    {   
                        var creatorId = e.Id[5..];
                        if (MinesweeperCommand.msGames.TryGetValue(e.User.Id, out MinesweeperGame? msGame))
                        {   
                            if (creatorId == msGame._player1.Id.ToString())
                                await msGame.MakeMove(e);
                            else
                                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, 
                                new DiscordInteractionResponseBuilder().WithContent("This is NOT your board.").AsEphemeral());
                        }
                            
                        else
                            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, 
                            new DiscordInteractionResponseBuilder().WithContent("You are not in a Minesweeper game!").AsEphemeral());
                    }
                })
            );

            // Registering the commands
            builder.UseCommands((serviceProvider, extension) => 
            {
                extension.AddCommands([typeof(PingCommand), typeof(TicTacToeCommand), typeof(MinesweeperCommand)]);
                TextCommandProcessor textCommandProcessor = new(new()
                {
                    PrefixResolver = new DefaultPrefixResolver(true, "!").ResolvePrefixAsync
                });

                extension.AddProcessor(textCommandProcessor);
            }, new CommandsConfiguration()
            {
                RegisterDefaultCommandProcessors = true,
            });
   
            
            DiscordClient client = builder.Build();
            await client.ConnectAsync();
            await Task.Delay(-1);

        }
    }

}
