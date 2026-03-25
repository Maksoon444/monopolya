using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MonopolyWPF
{
    public partial class MainWindow : Window
    {
        private MonopolyGame game = null!;
        private DispatcherTimer gameTimer = null!;
        private DispatcherTimer movementTimer = null!;
        private Dictionary<int, Border> cellBorders = null!;
        private Dictionary<int, Canvas> cellCanvases = null!;
        private int playerCount = 2;
        private int startMoney = 1500;
        private int animationSpeed = 300;

        private Player? movingPlayer = null;
        private int targetPosition = 0;
        private int currentStep = 0;
        private int totalSteps = 0;
        private List<int> pathPositions = new List<int>();
        private bool isMoving = false;

        private Dictionary<Player, Border> playerMarkers = new Dictionary<Player, Border>();

        private Border chatBorder = null!;
        private StackPanel chatPanel = null!;
        private ListBox chatListBox = null!;
        private TextBox chatInputTextBox = null!;
        private Button sendChatButton = null!;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            CreateBoard();
            CreateCenterChat();
            UpdatePlayersAvatarPanel();
            UpdateUI();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddChatMessage("Система", "Добро пожаловать в Монополию! Используйте чат для общения с другими игроками.");
        }

        private void InitializeGame()
        {
            game = new MonopolyGame(playerCount, startMoney);
            game.StartNewGame();

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += GameTimer_Tick;

            movementTimer = new DispatcherTimer();
            movementTimer.Interval = TimeSpan.FromMilliseconds(animationSpeed);
            movementTimer.Tick += MovementTimer_Tick;

            cellBorders = new Dictionary<int, Border>();
            cellCanvases = new Dictionary<int, Canvas>();
            playerMarkers.Clear();
        }

        private void CreateBoard()
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            int cellSize = 100;
            int boardSize = 8;

            for (int row = 0; row < boardSize; row++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });
            }

            cellBorders.Clear();
            cellCanvases.Clear();

            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    int position = GetPositionFromCoordinates(row, col, boardSize);

                    if (position >= 0 && position < game.Board.Count)
                    {
                        var cellCanvas = new Canvas
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Background = Brushes.Transparent,
                            Margin = new Thickness(1)
                        };

                        var cellBorder = CreateCellBorder(position, row, col, cellSize);
                        cellBorders[position] = cellBorder;

                        var cellPanel = CreateCellPanel(position);
                        cellBorder.Child = cellPanel;

                        Canvas.SetZIndex(cellBorder, 0);
                        cellCanvas.Children.Add(cellBorder);

                        cellCanvas.MouseLeftButtonUp += CellBorder_MouseLeftButtonUp;
                        cellCanvas.Tag = position;

                        cellCanvases[position] = cellCanvas;

                        Grid.SetRow(cellCanvas, row);
                        Grid.SetColumn(cellCanvas, col);
                        BoardGrid.Children.Add(cellCanvas);
                    }
                    else
                    {
                        var emptyBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            Background = Brushes.Transparent,
                            Margin = new Thickness(1),
                            Width = cellSize,
                            Height = cellSize
                        };

                        Grid.SetRow(emptyBorder, row);
                        Grid.SetColumn(emptyBorder, col);
                        BoardGrid.Children.Add(emptyBorder);
                    }
                }
            }

            CreatePlayerMarkers();
        }

        private void CreatePlayerMarkers()
        {
            playerMarkers.Clear();

            foreach (var player in game.Players)
            {
                var playerMarker = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = GetPlayerColor(game.Players.IndexOf(player)),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(3),
                    Tag = "PlayerMarker",
                    ToolTip = player.Name,
                    Visibility = Visibility.Visible
                };

                var markerText = new TextBlock
                {
                    Text = player.Name.Substring(0, 1),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                playerMarker.Child = markerText;
                playerMarkers[player] = playerMarker;
            }
        }

        private void CreateCenterChat()
        {
            int boardSize = 8;
            int centerStartRow = 2;
            int centerEndRow = 5;
            int centerStartCol = 2;
            int centerEndCol = 5;

            var chatContainer = new Grid();
            chatContainer.SetValue(Grid.RowSpanProperty, centerEndRow - centerStartRow + 1);
            chatContainer.SetValue(Grid.ColumnSpanProperty, centerEndCol - centerStartCol + 1);
            Grid.SetRow(chatContainer, centerStartRow);
            Grid.SetColumn(chatContainer, centerStartCol);

            chatBorder = new Border
            {
                BorderBrush = Brushes.Blue,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(240, 173, 216, 230)),
                Margin = new Thickness(5),
                Padding = new Thickness(10)
            };

            chatPanel = new StackPanel();

            chatPanel.Children.Add(new TextBlock
            {
                Text = "💬 ЧАТ ИГРОКОВ",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            chatListBox = new ListBox
            {
                Height = 200,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 11
            };

            ScrollViewer.SetVerticalScrollBarVisibility(chatListBox, ScrollBarVisibility.Auto);
            chatPanel.Children.Add(chatListBox);

            var inputPanel = new Grid();
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            chatInputTextBox = new TextBox
            {
                Height = 30,
                FontSize = 12,
                Padding = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.White
            };
            chatInputTextBox.KeyDown += ChatInputTextBox_KeyDown;
            Grid.SetColumn(chatInputTextBox, 0);

            sendChatButton = new Button
            {
                Content = "📨",
                Width = 40,
                Height = 30,
                FontSize = 16,
                Background = Brushes.LightGreen,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "Отправить сообщение (Enter)"
            };
            sendChatButton.Click += SendChatButton_Click;
            Grid.SetColumn(sendChatButton, 1);

            inputPanel.Children.Add(chatInputTextBox);
            inputPanel.Children.Add(sendChatButton);
            chatPanel.Children.Add(inputPanel);

            chatBorder.Child = chatPanel;
            chatContainer.Children.Add(chatBorder);

            Canvas.SetZIndex(chatContainer, 200);

            BoardGrid.Children.Add(chatContainer);
        }

        private void CellBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int position)
            {
                ShowCellInfo(position);
            }
            else if (sender is Canvas canvas && canvas.Tag is int pos)
            {
                ShowCellInfo(pos);
            }
        }

        private Border CreateCellBorder(int position, int row, int col, int cellSize)
        {
            return new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = GetCellColor(position),
                Tag = position,
                Width = cellSize - 2,
                Height = cellSize - 2,
                Cursor = Cursors.Hand,
                ToolTip = GetCellToolTip(position)
            };
        }

        private StackPanel CreateCellPanel(int position)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 95
            };

            if (position >= 0 && position < game.Board.Count)
            {
                var cell = game.Board[position];

                string icon = GetCellIcon(cell.Type);
                panel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 2)
                });

                var nameTextBlock = new TextBlock
                {
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 90,
                    MaxHeight = 40,
                    LineHeight = 12,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                string wrappedName = WrapText(cell.Name, 12);
                nameTextBlock.Text = wrappedName;

                panel.Children.Add(nameTextBlock);

                if (cell.Type == CellType.Property)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"${100 + position * 15}",
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkGreen,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }

                if (cell.Type == CellType.Chance || cell.Type == CellType.CommunityChest ||
                    cell.Type == CellType.Lucky || cell.Type == CellType.Unlucky ||
                    cell.Type == CellType.Gift || cell.Type == CellType.Penalty)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = "?",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Purple,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
            }

            return panel;
        }

        private string WrapText(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxCharsPerLine)
                return text;

            string result = "";
            int wordsPerLine = 2;
            string[] words = text.Split(' ');

            for (int i = 0; i < words.Length; i += wordsPerLine)
            {
                string line = string.Join(" ", words.Skip(i).Take(wordsPerLine));
                result += line;
                if (i + wordsPerLine < words.Length)
                    result += "\n";
            }

            return result;
        }

        private string GetCellIcon(CellType type)
        {
            return type switch
            {
                CellType.Start => "🏁",
                CellType.Property => "🏘️",
                CellType.Chance => "❓",
                CellType.CommunityChest => "📦",
                CellType.Jail => "⛓️",
                CellType.FreeParking => "🅿️",
                CellType.GoToJail => "🚓",
                CellType.Tax => "💰",
                CellType.Lucky => "🍀",
                CellType.Unlucky => "💢",
                CellType.Gift => "🎁",
                CellType.Penalty => "⚠️",
                _ => "⬜"
            };
        }

        private string GetCellToolTip(int position)
        {
            if (position < 0 || position >= game.Board.Count) return "";

            var cell = game.Board[position];
            string ownerInfo = cell.Owner != null ? $"Владелец: {cell.Owner.Name}" : "Нет владельца";
            string priceInfo = cell.Type == CellType.Property ? $"\nЦена: ${100 + position * 15}" : "";

            return $"{cell.Name}\nТип: {GetCellTypeName(cell.Type)}{priceInfo}\n{ownerInfo}";
        }

        private string GetCellTypeName(CellType type)
        {
            return type switch
            {
                CellType.Start => "Старт",
                CellType.Property => "Недвижимость",
                CellType.Chance => "Шанс",
                CellType.CommunityChest => "Казна",
                CellType.Jail => "Тюрьма",
                CellType.FreeParking => "Бесплатная стоянка",
                CellType.GoToJail => "Отправка в тюрьму",
                CellType.Tax => "Налог",
                CellType.Lucky => "Удача",
                CellType.Unlucky => "Неудача",
                CellType.Gift => "Подарок",
                CellType.Penalty => "Штраф",
                _ => "Неизвестно"
            };
        }

        private int GetPositionFromCoordinates(int row, int col, int size)
        {
            if (row == 0) return col;
            if (col == size - 1) return size + row - 1;
            if (row == size - 1) return 3 * size - 3 - col;
            if (col == 0) return 4 * size - 5 - row;
            return -1;
        }

        private SolidColorBrush GetCellColor(int position)
        {
            if (position < 0 || position >= game.Board.Count)
                return Brushes.Gray;

            var cell = game.Board[position];
            if (cell == null) return Brushes.Gray;

            if (cell.Owner != null)
            {
                var ownerColor = GetPlayerColor(game.Players.IndexOf(cell.Owner));
                return new SolidColorBrush(Color.FromArgb(180, ownerColor.Color.R, ownerColor.Color.G, ownerColor.Color.B));
            }

            return cell.Type switch
            {
                CellType.Start => new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                CellType.Property => new SolidColorBrush(Color.FromRgb(200, 220, 255)),
                CellType.Chance => new SolidColorBrush(Color.FromRgb(255, 200, 150)),
                CellType.CommunityChest => new SolidColorBrush(Color.FromRgb(255, 255, 200)),
                CellType.Jail => new SolidColorBrush(Color.FromRgb(255, 150, 150)),
                CellType.FreeParking => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CellType.GoToJail => new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                CellType.Tax => new SolidColorBrush(Color.FromRgb(255, 200, 220)),
                CellType.Lucky => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                CellType.Unlucky => new SolidColorBrush(Color.FromRgb(169, 169, 169)),
                CellType.Gift => new SolidColorBrush(Color.FromRgb(238, 130, 238)),
                CellType.Penalty => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                _ => Brushes.White
            };
        }

        private void UpdatePlayersAvatarPanel()
        {
            PlayersAvatarPanel.Children.Clear();

            foreach (var player in game.Players)
            {
                var avatarBorder = new Border
                {
                    Width = 45,
                    Height = 45,
                    CornerRadius = new CornerRadius(22.5),
                    Background = GetPlayerColor(game.Players.IndexOf(player)),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(5),
                    ToolTip = $"{player.Name} (${player.Money})"
                };

                var avatarText = new TextBlock
                {
                    Text = player.Name.Substring(0, 1),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                avatarBorder.Child = avatarText;
                PlayersAvatarPanel.Children.Add(avatarBorder);
            }
        }

        private void UpdatePlayerPositions()
        {
            foreach (var canvas in cellCanvases.Values)
            {
                var markersToRemove = canvas.Children.OfType<Border>()
                    .Where(b => b.Tag?.ToString() == "PlayerMarker")
                    .ToList();

                foreach (var marker in markersToRemove)
                {
                    canvas.Children.Remove(marker);
                }
            }

            foreach (var player in game.Players)
            {
                if (player != null && cellCanvases.ContainsKey(player.Position) && playerMarkers.ContainsKey(player))
                {
                    var canvas = cellCanvases[player.Position];
                    var marker = playerMarkers[player];

                    int playerIndex = game.Players.IndexOf(player);
                    double offsetX = 5 + (playerIndex * 25) % 70;
                    double offsetY = 5 + (playerIndex * 15) % 60;

                    Canvas.SetLeft(marker, offsetX);
                    Canvas.SetTop(marker, offsetY);

                    Canvas.SetZIndex(marker, 100 + playerIndex);

                    canvas.Children.Add(marker);
                }
            }
        }

        private SolidColorBrush GetPlayerColor(int index)
        {
            return index switch
            {
                0 => new SolidColorBrush(Color.FromRgb(255, 80, 80)),
                1 => new SolidColorBrush(Color.FromRgb(80, 80, 255)),
                2 => new SolidColorBrush(Color.FromRgb(80, 255, 80)),
                3 => new SolidColorBrush(Color.FromRgb(255, 255, 80)),
                _ => Brushes.Gray
            };
        }

        private void UpdateUI()
        {
            try
            {
                if (game == null || game.Players == null || game.Players.Count == 0)
                    return;

                var currentPlayer = game.CurrentPlayer;
                if (currentPlayer != null)
                {
                    CurrentPlayerText.Text = $"{currentPlayer.Name}";
                    MoneyText.Text = $"Деньги: ${currentPlayer.Money}";
                    PositionText.Text = $"Позиция: {currentPlayer.Position}";
                    PropertiesCountText.Text = $"Недвижимость: {currentPlayer.Properties.Count}";

                    CurrentPlayerAvatar.Background = GetPlayerColor(game.Players.IndexOf(currentPlayer));
                    var avatarText = new TextBlock
                    {
                        Text = currentPlayer.Name.Substring(0, 1),
                        FontSize = 36,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    CurrentPlayerAvatar.Child = avatarText;
                }

                PlayersListBox.Items.Clear();
                foreach (var player in game.Players)
                {
                    if (player != null)
                    {
                        string jailStatus = player.InJail ? " 🔒" : "";
                        string properties = player.Properties.Count > 0 ? $" 🏠{player.Properties.Count}" : "";
                        PlayersListBox.Items.Add($"● {player.Name}: ${player.Money}{jailStatus}{properties}");
                    }
                }

                foreach (var kvp in cellBorders)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.Background = GetCellColor(kvp.Key);
                    }
                }

                if (!isMoving)
                {
                    UpdatePlayerPositions();
                }

                UpdatePlayersAvatarPanel();
                UpdateDiceDisplay();

                if (game.CheckGameOver())
                {
                    ShowGameOver();
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Ошибка обновления UI: {ex.Message}");
            }
        }

        private void UpdateDiceDisplay()
        {
            var diceResult = game.LastDiceRoll;
            int dice1 = diceResult.Item1;
            int dice2 = diceResult.Item2;

            Dice1Text.Text = GetDiceSymbol(dice1);
            Dice2Text.Text = GetDiceSymbol(dice2);

            Dice1Border.Background = GetDiceColor(dice1);
            Dice2Border.Background = GetDiceColor(dice2);
        }

        private string GetDiceSymbol(int value)
        {
            return value switch
            {
                1 => "⚀",
                2 => "⚁",
                3 => "⚂",
                4 => "⚃",
                5 => "⚄",
                6 => "⚅",
                _ => "⚀"
            };
        }

        private SolidColorBrush GetDiceColor(int value)
        {
            return value switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 200, 200)),
                2 => new SolidColorBrush(Color.FromRgb(200, 200, 255)),
                3 => new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                4 => new SolidColorBrush(Color.FromRgb(255, 255, 200)),
                5 => new SolidColorBrush(Color.FromRgb(255, 200, 255)),
                6 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                _ => Brushes.White
            };
        }

        private void AddLogMessage(string message)
        {
            try
            {
                if (LogListBox != null)
                {
                    LogListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                    }
                }
            }
            catch { }
        }

        private void AddChatMessage(string sender, string message)
        {
            try
            {
                if (chatListBox != null)
                {
                    string formattedMessage = $"[{DateTime.Now:HH:mm}] {sender}: {message}";

                    var listBoxItem = new ListBoxItem();
                    listBoxItem.Content = formattedMessage;

                    if (sender == "Красный")
                        listBoxItem.Foreground = Brushes.Red;
                    else if (sender == "Синий")
                        listBoxItem.Foreground = Brushes.Blue;
                    else if (sender == "Зеленый")
                        listBoxItem.Foreground = Brushes.Green;
                    else if (sender == "Желтый")
                        listBoxItem.Foreground = Brushes.Orange;
                    else if (sender == "Система")
                        listBoxItem.Foreground = Brushes.Gray;
                    else
                        listBoxItem.Foreground = Brushes.Black;

                    chatListBox.Items.Add(listBoxItem);

                    if (chatListBox.Items.Count > 0)
                    {
                        chatListBox.ScrollIntoView(chatListBox.Items[chatListBox.Items.Count - 1]);
                    }
                }
            }
            catch { }
        }

        private void ShowCellInfo(int position)
        {
            if (position < 0 || position >= game.Board.Count) return;

            var cell = game.Board[position];
            string ownerInfo = cell.Owner != null ? $"Владелец: {cell.Owner.Name}" : "Свободно";
            string priceInfo = cell.Type == CellType.Property ? $"\nЦена: ${100 + position * 15}" : "";
            string playersHere = GetPlayersAtPosition(position);

            MessageBox.Show(
                $"Клетка: {cell.Name}\n" +
                $"Тип: {GetCellTypeName(cell.Type)}{priceInfo}\n" +
                $"{ownerInfo}\n" +
                $"Игроки здесь: {playersHere}",
                "Информация о клетке",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private string GetPlayersAtPosition(int position)
        {
            var playersHere = game.Players.Where(p => p.Position == position).Select(p => p.Name).ToList();
            return playersHere.Count > 0 ? string.Join(", ", playersHere) : "Нет";
        }

        private void StartPlayerMovement(Player player, int steps)
        {
            if (isMoving) return;

            movingPlayer = player;
            totalSteps = steps;
            currentStep = 0;
            pathPositions.Clear();

            int currentPos = player.Position;
            for (int i = 1; i <= steps; i++)
            {
                int nextPos = (currentPos + i) % game.Board.Count;
                pathPositions.Add(nextPos);
            }

            isMoving = true;
            movementTimer.Start();

            RollDiceButton.IsEnabled = false;
            BuyPropertyButton.IsEnabled = false;
            EndTurnButton.IsEnabled = false;
        }

        private void MovementTimer_Tick(object? sender, EventArgs e)
        {
            if (!isMoving || movingPlayer == null || currentStep >= totalSteps)
            {
                movementTimer.Stop();
                isMoving = false;

                if (movingPlayer != null)
                {
                    movingPlayer.Position = targetPosition;

                    if (currentStep == totalSteps && movingPlayer.Position < currentStep)
                    {
                        movingPlayer.Money += 200;
                        AddLogMessage($"💰 {movingPlayer.Name} прошел через СТАРТ и получил $200!");
                    }

                    game.HandleCurrentCellAction();

                    string cardEffect = game.GetLastCardEffect();
                    if (!string.IsNullOrEmpty(cardEffect))
                    {
                        AddLogMessage($"❓ {cardEffect}");
                    }

                    UpdateUI();

                    if (game.CurrentCell.Type == CellType.Property && game.CurrentCell.Owner == null)
                    {
                        BuyPropertyButton.IsEnabled = true;
                    }

                    EndTurnButton.IsEnabled = true;
                }

                return;
            }

            targetPosition = pathPositions[currentStep];
            movingPlayer.Position = targetPosition;

            UpdatePlayerPositions();

            currentStep++;
        }

        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isMoving) return;

                game.RollDice();

                var diceResult = game.LastDiceRoll;
                int dice1 = diceResult.Item1;
                int dice2 = diceResult.Item2;
                int total = dice1 + dice2;

                string diceMessage = $"{game.CurrentPlayer.Name} бросил кубики: {GetDiceSymbol(dice1)} + {GetDiceSymbol(dice2)} = {total}";
                AddLogMessage($"🎲 {diceMessage}");

                StartPlayerMovement(game.CurrentPlayer, total);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Ошибка: {ex.Message}");
                RollDiceButton.IsEnabled = true;
            }
        }

        private void BuyPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (game.BuyCurrentProperty())
                {
                    string buyMessage = $"{game.CurrentPlayer.Name} купил {game.CurrentCell.Name}";
                    AddLogMessage($"💰 {buyMessage}");
                    BuyPropertyButton.IsEnabled = false;
                    UpdateUI();
                }
                else
                {
                    AddLogMessage($"❌ Недостаточно денег для покупки {game.CurrentCell.Name}");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Ошибка при покупке: {ex.Message}");
            }
        }

        private void EndTurnButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isMoving) return;

                game.NextPlayer();

                RollDiceButton.IsEnabled = true;
                BuyPropertyButton.IsEnabled = false;
                EndTurnButton.IsEnabled = false;

                string turnMessage = $"Ход перешел к {game.CurrentPlayer.Name}";
                AddLogMessage($"⏭️ {turnMessage}");

                if (game.CurrentPlayer.InJail)
                {
                    string jailMessage = $"{game.CurrentPlayer.Name} в тюрьме! Осталось ходов: {game.CurrentPlayer.JailTurns}";
                    AddLogMessage($"🔒 {jailMessage}");
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                AddLogMessage($"Ошибка при завершении хода: {ex.Message}");
                RollDiceButton.IsEnabled = true;
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                game = new MonopolyGame(playerCount, startMoney);
                game.StartNewGame();

                RollDiceButton.IsEnabled = true;
                BuyPropertyButton.IsEnabled = false;
                EndTurnButton.IsEnabled = false;

                LogListBox.Items.Clear();
                if (chatListBox != null)
                {
                    chatListBox.Items.Clear();
                }

                AddLogMessage("🔄 Новая игра началась!");
                AddLogMessage($"👥 Игроки: {string.Join(", ", game.Players.Select(p => p.Name))}");
                AddLogMessage($"💰 Начальный капитал: ${startMoney}");

                AddChatMessage("Система", "Добро пожаловать! Чат только для игроков.");

                CreateBoard();
                CreatePlayerMarkers();
                CreateCenterChat();
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании новой игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
        }

        private void ShowGameOver()
        {
            try
            {
                if (game.Players == null || game.Players.Count == 0) return;

                var winner = game.Players.OrderByDescending(p => p.Money).FirstOrDefault();
                if (winner != null)
                {
                    string gameOverMessage = $"Победитель: {winner.Name} с ${winner.Money}! Недвижимость: {winner.Properties.Count} объектов";
                    AddLogMessage($"🏆 ИГРА ОКОНЧЕНА! {gameOverMessage}");

                    var result = MessageBox.Show(
                        $"🏆 ИГРА ОКОНЧЕНА! 🏆\n\n" +
                        $"Победитель: {winner.Name}\n" +
                        $"Итоговый капитал: ${winner.Money}\n" +
                        $"Недвижимость: {winner.Properties.Count} объектов\n\n" +
                        $"Хотите сыграть еще раз?",
                        "Конец игры",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        NewGameButton_Click(this, new RoutedEventArgs());
                    }
                }

                RollDiceButton.IsEnabled = false;
                BuyPropertyButton.IsEnabled = false;
                EndTurnButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                AddLogMessage($"Ошибка при завершении игры: {ex.Message}");
            }
        }

        private void SendChatButton_Click(object sender, RoutedEventArgs e)
        {
            SendChatMessage();
        }

        private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                SendChatMessage();
                e.Handled = true;
            }
        }

        private void SendChatMessage()
        {
            string message = chatInputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message) && game?.CurrentPlayer != null)
            {
                AddChatMessage(game.CurrentPlayer.Name, message);
                chatInputTextBox.Clear();
            }
        }

        private void ShowLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogPopup.IsOpen = true;
        }

        private void ShowLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LogPopup.IsOpen = ShowLogMenuItem.IsChecked;
        }

        private void CloseLogPopupButton_Click(object sender, RoutedEventArgs e)
        {
            LogPopup.IsOpen = false;
            ShowLogMenuItem.IsChecked = false;
        }

        private void SetSpeedSlow_Click(object sender, RoutedEventArgs e)
        {
            animationSpeed = 500;
            movementTimer.Interval = TimeSpan.FromMilliseconds(animationSpeed);
            SpeedSlowMenuItem.IsChecked = true;
            SpeedNormalMenuItem.IsChecked = false;
            SpeedFastMenuItem.IsChecked = false;
            AddLogMessage("Скорость анимации: МЕДЛЕННО");
        }

        private void SetSpeedNormal_Click(object sender, RoutedEventArgs e)
        {
            animationSpeed = 300;
            movementTimer.Interval = TimeSpan.FromMilliseconds(animationSpeed);
            SpeedSlowMenuItem.IsChecked = false;
            SpeedNormalMenuItem.IsChecked = true;
            SpeedFastMenuItem.IsChecked = false;
            AddLogMessage("Скорость анимации: НОРМАЛЬНО");
        }

        private void SetSpeedFast_Click(object sender, RoutedEventArgs e)
        {
            animationSpeed = 150;
            movementTimer.Interval = TimeSpan.FromMilliseconds(animationSpeed);
            SpeedSlowMenuItem.IsChecked = false;
            SpeedNormalMenuItem.IsChecked = false;
            SpeedFastMenuItem.IsChecked = true;
            AddLogMessage("Скорость анимации: БЫСТРО");
        }

        private void SetPlayers2_Click(object sender, RoutedEventArgs e)
        {
            playerCount = 2;
            Players2MenuItem.IsChecked = true;
            Players3MenuItem.IsChecked = false;
            Players4MenuItem.IsChecked = false;
            AddLogMessage("Установлено 2 игрока");
        }

        private void SetPlayers3_Click(object sender, RoutedEventArgs e)
        {
            playerCount = 3;
            Players2MenuItem.IsChecked = false;
            Players3MenuItem.IsChecked = true;
            Players4MenuItem.IsChecked = false;
            AddLogMessage("Установлено 3 игрока");
        }

        private void SetPlayers4_Click(object sender, RoutedEventArgs e)
        {
            playerCount = 4;
            Players2MenuItem.IsChecked = false;
            Players3MenuItem.IsChecked = false;
            Players4MenuItem.IsChecked = true;
            AddLogMessage("Установлено 4 игрока");
        }

        private void SetMoney1000_Click(object sender, RoutedEventArgs e)
        {
            startMoney = 1000;
            Money1000MenuItem.IsChecked = true;
            Money1500MenuItem.IsChecked = false;
            Money2000MenuItem.IsChecked = false;
            AddLogMessage($"Начальные деньги: ${startMoney}");
        }

        private void SetMoney1500_Click(object sender, RoutedEventArgs e)
        {
            startMoney = 1500;
            Money1000MenuItem.IsChecked = false;
            Money1500MenuItem.IsChecked = true;
            Money2000MenuItem.IsChecked = false;
            AddLogMessage($"Начальные деньги: ${startMoney}");
        }

        private void SetMoney2000_Click(object sender, RoutedEventArgs e)
        {
            startMoney = 2000;
            Money1000MenuItem.IsChecked = false;
            Money1500MenuItem.IsChecked = false;
            Money2000MenuItem.IsChecked = true;
            AddLogMessage($"Начальные деньги: ${startMoney}");
        }

        private void RulesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string rules = "ПРАВИЛА ИГРЫ МОНОПОЛИЯ\n\n" +
                          "1. Игроки по очереди бросают кубики и перемещаются по полю.\n" +
                          "2. При проходе через СТАРТ игрок получает $200.\n" +
                          "3. Если игрок попадает на свободную недвижимость, он может её купить.\n" +
                          "4. Клетки ШАНС, КАЗНА, УДАЧА, НЕУДАЧА, ПОДАРОК и ШТРАФ дают случайные бонусы или штрафы.\n" +
                          "5. Попав на клетку ОТПРАВЛЯЙТЕСЬ В ТЮРЬМУ, игрок пропускает 3 хода.\n" +
                          "6. НАЛОГИ уменьшают деньги игрока.\n" +
                          "7. Игра заканчивается, когда один из игроков банкротится.\n\n" +
                          "УДАЧНОЙ ИГРЫ!";

            MessageBox.Show(rules, "Правила игры", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "МОНОПОЛИЯ ДЕЛЮКС\n\n" +
                "Версия 3.2\n\n" +
                "✨ Новые возможности:\n" +
                "- Чат в центре игрового поля\n" +
                "- Игроки поверх клеток\n" +
                "- Анимация движения\n" +
                "- Тюрьма в углу поля\n" +
                "- Разнообразное расположение клеток\n" +
                "- Полностью видимые названия\n\n" +
                "Разработано на C# WPF\n" +
                "© 2024 Все права защищены",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}