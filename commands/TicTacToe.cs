using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;

namespace DSharpBot
{
    public class TicTacToeCommand
    {
        public static Dictionary <ulong, TicTacToeGame> games = [];

        [Command("TicTacToe")]
        [Description("Play TicTacToe")]
        public static async ValueTask TicTacToeStart(CommandContext context, DiscordUser Opponent)
        {
            
            if (context.User == Opponent)
            {
                await context.RespondAsync("You can't play against yourself!");
                return;
            }
            if (games.ContainsKey(context.User.Id))
            {
                await context.RespondAsync("You are already in a game, finish that one first.");
                return;
            }
            TicTacToeGame game = new(context.User, Opponent);
            games.Add(context.User.Id, game);
            games.Add(Opponent.Id, game);

            var builder = game.BuildMessage();

            await context.RespondAsync(builder);
        }

        
    }

    public class TicTacToeGame
    {
        private readonly DiscordUser _player1;
        private readonly DiscordUser _player2;
        private DiscordUser _currentPlayer;
        private string[,] _board = new string[3,3];
        private bool[,] _isDisabled = new bool[3,3];

        public TicTacToeGame(DiscordUser player1, DiscordUser player2)
        {
            _player1 = player1;
            _player2 = player2;
            Random random = new Random();
            _currentPlayer = random.Next(0, 2) == 0 ? _player1 : _player2;
            _board = new string[,] 
            {
                { "​", "​", "​" },
                { "​", "​", "​" },
                { "​", "​", "​" }
            };
            _isDisabled = new bool[,] 
            {
                { false, false, false },
                { false, false, false },
                { false, false, false }
            };
        }

        public async Task MakeMove(DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs e)
        {
            if (e.User == _currentPlayer)
            {
                int row = (int)char.GetNumericValue(e.Id[3]);
                int col = (int)char.GetNumericValue(e.Id[4]);

                _isDisabled[row, col] = true;
                if (_currentPlayer == _player1)
                {
                    _board[row, col] = "X";
                    _currentPlayer = _player2;
                }
                else
                {
                    _board[row, col] = "O";
                    _currentPlayer = _player1;
                }
                    
                    
                var builder = BuildMessage();
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                builder);

            }
            else
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("It is NOT your turn.").AsEphemeral());
            }
            
        }

        public async Task Resign(DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs e)
        {
            TicTacToeCommand.games.Remove(_player1.Id);
            TicTacToeCommand.games.Remove(_player2.Id);
            
            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder().WithContent($"{e.User.Username} Resigned!").AsEphemeral());
        }
        
        public DiscordInteractionResponseBuilder BuildMessage()
        {
            if (CheckWin())
            {
                if (_currentPlayer == _player1)
                    return EndGame($"{_player2.Username} won!");
                else
                    return EndGame($"{_player1.Username} won!");
            }

            if (CheckDraw())
            {
                return EndGame("Booo you guys suck (game drawn)");
            }

            var builder = new DiscordInteractionResponseBuilder();
            builder.WithContent($"Game started! {_currentPlayer.Username} to move.");
            
            // first loop creates a row on each iteration (so 3 rows)
            // second loop adds a button to each row (so 3 buttons per row)
            for (int i = 0; i < 3; i++)
            {
                DiscordComponent[] components = new DiscordComponent[3];
                for (int j = 0; j < 3; j++)
                {
                    DiscordComponent button = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"ttt{i}{j}", _board[i, j],_isDisabled[i, j]);
                    components[j] = button;
                }

                builder.AddComponents(components);
            }
            builder.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Danger, "tttDelete", "End Game")
            );

            return builder;
        }

        public bool CheckWin()
        {
            for (int i = 0; i < 3; i++)
            {   
                // horizontal
                if (_board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2] && _board[i, 0] != "​")
                    return true;
                // vertical
                if (_board[0, i] == _board[1, i] && _board[1, i] == _board[2, i] && _board[0, i] != "​")
                    return true;
            }
            // diagonals
            if (_board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2] && _board[0, 0] != "​")
                return true;
            if (_board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0] && _board[0, 2] != "​")
                return true;
            return false;
        }

        public bool CheckDraw()
        {
            // if theres an empty spot on the board, that means the game isnt over yet
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_board[i, j] == "​")
                        return false;
                }
            }
            return true;
        }

        private DiscordInteractionResponseBuilder EndGame(string message)
        {
            var builder = new DiscordInteractionResponseBuilder().WithContent(message);

            // disabling every button since the game has ended
            for (int i = 0; i < 3; i++)
            {
                DiscordComponent[] components = new DiscordComponent[3];
                for (int j = 0; j < 3; j++)
                {
                    components[j] = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"ttt{i}{j}", _board[i, j], true);
                }
                builder.AddComponents(components);
            }

            // Remove the game from active games
            TicTacToeCommand.games.Remove(_player1.Id);
            TicTacToeCommand.games.Remove(_player2.Id);
            
            return builder;
        }
    }
}
