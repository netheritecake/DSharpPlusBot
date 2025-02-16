using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace DSharpBot
{
    public class MinesweeperCommand
    {
        public static Dictionary<ulong, MinesweeperGame> msGames = [];

        [Command("Minesweeper")]
        [Description("Start a minesweeper game")]
        public static async ValueTask MinesweeperStart(CommandContext context)
        {
            MinesweeperGame msGame = new(context.User);
            msGames.Add(context.User.Id, msGame);

            var builder = msGame.BuildMessage();
            await context.RespondAsync(builder);

        }
        
    }

    public class MinesweeperGame
    {
        private readonly DiscordUser _player1;
        private string[,] _board = new string[5,5];
        private bool[,] _isOpen = new bool[5,5];

        public MinesweeperGame(DiscordUser player1)
        {
            _player1 = player1;
            _board = CreateBoard();
            
            // to tell if player has clicked on (opened) the button or not
            _isOpen = new bool[5,5]
            {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };
        }

        public async Task MakeMove(DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs e)
        {
            int row = (int)char.GetNumericValue(e.Id[2]);
            int col = (int)char.GetNumericValue(e.Id[3]);

            Open(row, col);

            if (_board[row, col] == "💣")
            {
                var builder = EndGame($"{_player1.Username} lost!");
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
            }
            else
            {
                var builder = BuildMessage();
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
            }

        }

        public DiscordInteractionResponseBuilder BuildMessage()
        {
            int bombCount = 0;
            var builder = new DiscordInteractionResponseBuilder();
            if (CheckWin())
            {
                return EndGame($"{_player1.Username} won!");
            }
            // build buttons
            for (int i = 0; i < 5; i++)
            {
                DiscordComponent[] components = new DiscordComponent[5];
                for (int j = 0; j < 5; j++)
                {
                    if (_isOpen[i, j])
                    {
                        DiscordComponent button = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"ms{i}{j}", _board[i, j], _isOpen[i, j]);
                        components[j] = button;
                    }
                    else 
                    {
                        DiscordComponent button = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"ms{i}{j}", "​", _isOpen[i, j]);
                        components[j] = button;
                    }

                    // this is just to print the total bomb count
                    if (_board[i, j] == "💣")
                        bombCount++;
                }
                builder.AddComponents(components);
            }

            builder.WithContent($"Game started with {bombCount} bombs.");
            return builder;
        }

        private string[,] CreateBoard()
        {
            string[,] board = new string[5, 5];

            // adding the mines
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    // 20% chance of each cell having a bomb
                    // no upper or lower limits because funny
                    board[i, j] = Random.Shared.Next(0, 5) == 0 ? "💣" : "​";
                }
            }

            // adding the numbers
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (board[i, j] != "💣")
                    {
                        int adjacentMines = CountAdjacentMines(board, i, j);
                        // dont print 0 just print nothing (zero-width space) instead
                        if (adjacentMines == 0)
                            board[i, j] = "​";
                        else
                            board[i, j] = adjacentMines.ToString();
                    }
                }
            }

            return board;
        }

        // this code so ass :broken_heart:
        private void Open(int i, int j)
        {
            if (_isOpen[i, j])
                return;
            
            _isOpen[i, j] = true;
            // if the button is empty then open everything surrounding it
            // recursively
            if (_board[i, j] == "​")
            {
                int[] directions = [-1, 0, 1];

                foreach (int row in directions)
                {
                    foreach (int col in directions)
                    {

                        if (row == 0 && col == 0)
                            continue;

                        int newRow = i + row;
                        int newCol = j + col;

                        if (newRow >= 0 && newRow < 5 && newCol >= 0 && newCol < 5)
                            Open(newRow, newCol);
                    }
                }

            }
        }

        private int CountAdjacentMines(string[,] board, int i, int j)
        {
            int count = 0;
            int[] directions = [-1, 0, 1];

            foreach (int row in directions)
            {
                foreach (int col in directions)
                {
                    int newRow = i + row;
                    int newCol = j + col;

                    if (newRow >= 0 && newRow < 5 && newCol >= 0 && newCol < 5)
                    {
                        if (board[newRow, newCol] == "💣")
                            count++;
                    }
                }
            }

            return count;
        }

        private bool CheckWin()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (_board[i, j] != "💣" && !_isOpen[i, j])
                        return false;
                }
            }
            return true;
        }

        private DiscordInteractionResponseBuilder EndGame(string message)
        {
            var builder = new DiscordInteractionResponseBuilder().WithContent(message);
            // disable every button
            for (int i = 0; i < 5; i++)
            {
                DiscordComponent[] components = new DiscordComponent[5];
                for (int j = 0; j < 5; j++)
                {
                    components[j] = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"ms{i}{j}", _board[i, j], true);
                }
                builder.AddComponents(components);
            }

            MinesweeperCommand.msGames.Remove(_player1.Id);
            return builder;

        }

    }
}
    