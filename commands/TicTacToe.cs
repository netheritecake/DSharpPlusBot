using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace DSharpBot
{
    public class TicTacToeCommand
    {
        public static Dictionary <ulong, TicTacToeGame> tttGames = [];

        [Command("TicTacToe")]
        [Description("Play TicTacToe")]
        public static async ValueTask TicTacToeStart(CommandContext context, DiscordUser Opponent)
        {
            
            if (context.User == Opponent)
            {
                var response = new DiscordInteractionResponseBuilder().WithContent("You can't play against yourself.").AsEphemeral();
                await context.RespondAsync(response);
                return;
            }
            if (tttGames.ContainsKey(context.User.Id))
            {
                var response = new DiscordInteractionResponseBuilder().WithContent("You are already in a game, finish that one first.").AsEphemeral();
                await context.RespondAsync(response);
                return;
            }
            if (tttGames.ContainsKey(Opponent.Id))
            {
                var response = new DiscordInteractionResponseBuilder().WithContent("Your opponent is already in a game, finish that one first.").AsEphemeral();
                await context.RespondAsync(response);
                return;
            }

            TicTacToeGame tttGame = new(context.User, Opponent);
            tttGames.Add(context.User.Id, tttGame);
            tttGames.Add(Opponent.Id, tttGame);

            var builder = tttGame.BuildMessage();
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
            _currentPlayer = Random.Shared.Next(0, 2) == 0 ? _player1 : _player2;
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

        private bool CheckWin()
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

        private bool CheckDraw()
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

        public DiscordInteractionResponseBuilder EndGame(string message)
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
            TicTacToeCommand.tttGames.Remove(_player1.Id);
            TicTacToeCommand.tttGames.Remove(_player2.Id);
            
            return builder;
        }
    }
}
