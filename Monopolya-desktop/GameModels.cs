using System;
using System.Collections.Generic;
using System.Linq;

namespace MonopolyWPF
{
    public enum CellType
    {
        Start,
        Property,
        Chance,
        CommunityChest,
        Jail,
        FreeParking,
        GoToJail,
        Tax,
        Lucky,
        Unlucky,
        Gift,
        Penalty
    }

    public class Cell
    {
        public string Name { get; set; }
        public CellType Type { get; set; }
        public int Position { get; set; }
        public Player? Owner { get; set; }

        public Cell(int position, string name, CellType type)
        {
            Position = position;
            Name = name;
            Type = type;
            Owner = null;
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int Money { get; set; }
        public bool InJail { get; set; }
        public int JailTurns { get; set; }
        public List<string> Properties { get; set; }

        public Player(string name)
        {
            Name = name;
            Position = 0;
            Money = 1500;
            InJail = false;
            JailTurns = 0;
            Properties = new List<string>();
        }

        public void Move(int steps, int boardSize)
        {
            if (boardSize <= 0) throw new ArgumentException("Board size must be positive");
            Position = (Position + steps) % boardSize;
        }
    }

    public class Dice
    {
        private Random random;

        public Dice()
        {
            random = new Random();
        }

        public int Roll()
        {
            return random.Next(1, 7);
        }

        public (int, int) RollTwoDice()
        {
            return (Roll(), Roll());
        }
    }

    public class MonopolyGame
    {
        private List<Player> players;
        private List<Cell> board;
        private Dice dice;
        private int currentPlayerIndex;
        private (int, int) lastDiceRoll;
        private string lastCardEffect = "";

        public List<Player> Players => players;
        public List<Cell> Board => board;

        public Player CurrentPlayer
        {
            get
            {
                if (players == null || players.Count == 0 || currentPlayerIndex >= players.Count)
                    throw new InvalidOperationException("No active players");
                return players[currentPlayerIndex];
            }
        }

        public Cell CurrentCell
        {
            get
            {
                var player = CurrentPlayer;
                if (board == null || player.Position >= board.Count)
                    throw new InvalidOperationException("Invalid player position");
                return board[player.Position];
            }
        }

        public (int, int) LastDiceRoll => lastDiceRoll;

        public MonopolyGame(int playerCount = 2, int startMoney = 1500)
        {
            dice = new Dice();
            players = new List<Player>();
            board = CreateBoard();
            currentPlayerIndex = 0;

            string[] colors = { "Красный", "Синий", "Зеленый", "Желтый" };
            for (int i = 0; i < Math.Min(playerCount, 4); i++)
            {
                var player = new Player(colors[i]);
                player.Money = startMoney;
                players.Add(player);
            }
        }

        private List<Cell> CreateBoard()
        {
            var board = new List<Cell>();

            // Верхний ряд (0-7) - разнообразные клетки
            board.Add(new Cell(0, "СТАРТ", CellType.Start));
            board.Add(new Cell(1, "Бульвар Арбат", CellType.Property));
            board.Add(new Cell(2, "Шанс", CellType.Chance));
            board.Add(new Cell(3, "Улица Тверская", CellType.Property));
            board.Add(new Cell(4, "Удача", CellType.Lucky));
            board.Add(new Cell(5, "Площадь Революции", CellType.Property));
            board.Add(new Cell(6, "Казна", CellType.CommunityChest));
            board.Add(new Cell(7, "Налог", CellType.Tax));

            // Правый ряд (8-14) - разнообразные клетки
            board.Add(new Cell(8, "ТЮРЬМА", CellType.Jail)); // Тюрьма в углу
            board.Add(new Cell(9, "Улица Пушкинская", CellType.Property));
            board.Add(new Cell(10, "Подарок", CellType.Gift));
            board.Add(new Cell(11, "Проспект Мира", CellType.Property));
            board.Add(new Cell(12, "Неудача", CellType.Unlucky));
            board.Add(new Cell(13, "Шанс", CellType.Chance));
            board.Add(new Cell(14, "Улица Чехова", CellType.Property));

            // Нижний ряд (15-22) - разнообразные клетки
            board.Add(new Cell(15, "Бесплатная стоянка", CellType.FreeParking));
            board.Add(new Cell(16, "Штраф", CellType.Penalty));
            board.Add(new Cell(17, "Улица Горького", CellType.Property));
            board.Add(new Cell(18, "Казна", CellType.CommunityChest));
            board.Add(new Cell(19, "Удача", CellType.Lucky));
            board.Add(new Cell(20, "Вокзал", CellType.Property));
            board.Add(new Cell(21, "Налог на роскошь", CellType.Tax));
            board.Add(new Cell(22, "Подарок", CellType.Gift));

            // Левый ряд (23-28) - разнообразные клетки
            board.Add(new Cell(23, "Отправляйтесь в тюрьму", CellType.GoToJail));
            board.Add(new Cell(24, "Улица Ленина", CellType.Property));
            board.Add(new Cell(25, "Неудача", CellType.Unlucky));
            board.Add(new Cell(26, "Проспект Победы", CellType.Property));
            board.Add(new Cell(27, "Штраф", CellType.Penalty));
            board.Add(new Cell(28, "Шанс", CellType.Chance));

            return board;
        }

        public void StartNewGame()
        {
            foreach (var player in players)
            {
                player.Position = 0;
                player.InJail = false;
                player.JailTurns = 0;
                player.Properties.Clear();
            }

            foreach (var cell in board)
            {
                cell.Owner = null;
            }

            currentPlayerIndex = 0;
        }

        public void RollDice()
        {
            lastDiceRoll = dice.RollTwoDice();
            lastCardEffect = "";
        }

        public void MoveCurrentPlayer()
        {
            if (players.Count == 0) return;

            int total = lastDiceRoll.Item1 + lastDiceRoll.Item2;

            if (CurrentPlayer.InJail)
            {
                HandleJailTurn();
                return;
            }

            int oldPosition = CurrentPlayer.Position;
            CurrentPlayer.Move(total, board.Count);

            if (oldPosition + total >= board.Count)
            {
                CurrentPlayer.Money += 200;
            }
        }

        public void HandleCurrentCellAction()
        {
            HandleCellAction(CurrentCell);
        }

        private void HandleJailTurn()
        {
            CurrentPlayer.JailTurns--;

            if (lastDiceRoll.Item1 == lastDiceRoll.Item2)
            {
                CurrentPlayer.InJail = false;
                CurrentPlayer.JailTurns = 0;

                int total = lastDiceRoll.Item1 + lastDiceRoll.Item2;
                int oldPosition = CurrentPlayer.Position;
                CurrentPlayer.Move(total, board.Count);

                if (oldPosition + total >= board.Count)
                {
                    CurrentPlayer.Money += 200;
                }
            }
            else if (CurrentPlayer.JailTurns <= 0)
            {
                CurrentPlayer.Money -= 50;
                CurrentPlayer.InJail = false;

                int total = lastDiceRoll.Item1 + lastDiceRoll.Item2;
                int oldPosition = CurrentPlayer.Position;
                CurrentPlayer.Move(total, board.Count);

                if (oldPosition + total >= board.Count)
                {
                    CurrentPlayer.Money += 200;
                }
            }
        }

        private void HandleCellAction(Cell cell)
        {
            if (cell == null) return;

            switch (cell.Type)
            {
                case CellType.Chance:
                    DrawChanceCard();
                    break;

                case CellType.CommunityChest:
                    DrawCommunityChestCard();
                    break;

                case CellType.Lucky:
                    int luckAmount = new Random().Next(50, 201);
                    CurrentPlayer.Money += luckAmount;
                    lastCardEffect = $"Удача! +${luckAmount}";
                    break;

                case CellType.Unlucky:
                    int unluckAmount = new Random().Next(30, 151);
                    CurrentPlayer.Money -= unluckAmount;
                    lastCardEffect = $"Неудача! -${unluckAmount}";
                    break;

                case CellType.Gift:
                    int giftAmount = new Random().Next(40, 121);
                    CurrentPlayer.Money += giftAmount;
                    lastCardEffect = $"Подарок! +${giftAmount}";
                    break;

                case CellType.Penalty:
                    int penaltyAmount = new Random().Next(20, 101);
                    CurrentPlayer.Money -= penaltyAmount;
                    lastCardEffect = $"Штраф! -${penaltyAmount}";
                    break;

                case CellType.GoToJail:
                    CurrentPlayer.Position = 8; // Позиция тюрьмы
                    CurrentPlayer.InJail = true;
                    CurrentPlayer.JailTurns = 3;
                    lastCardEffect = "Отправляйтесь в тюрьму!";
                    break;

                case CellType.Tax:
                    int tax = cell.Name.Contains("роскошь") ? 100 : 50;
                    CurrentPlayer.Money -= tax;
                    lastCardEffect = $"Налог -${tax}";
                    break;
            }
        }

        private void DrawChanceCard()
        {
            string[] chances = {
                "Аванс на работе! Получите $100",
                "Штраф за превышение скорости. Заплатите $50",
                "Вы выиграли в лотерею! Получите $150",
                "Ремонт дома. Заплатите $80",
                "Перейдите на СТАРТ",
                "Отправляйтесь в тюрьму",
                "Получите $50 от каждого игрока"
            };

            Random rand = new Random();
            string chance = chances[rand.Next(chances.Length)];
            ApplyChanceEffect(chance);
            lastCardEffect = chance;
        }

        private void ApplyChanceEffect(string chance)
        {
            if (string.IsNullOrEmpty(chance)) return;

            if (chance.Contains("Получите $100"))
                CurrentPlayer.Money += 100;
            else if (chance.Contains("Заплатите $50"))
                CurrentPlayer.Money -= 50;
            else if (chance.Contains("Получите $150"))
                CurrentPlayer.Money += 150;
            else if (chance.Contains("Заплатите $80"))
                CurrentPlayer.Money -= 80;
            else if (chance.Contains("Перейдите на СТАРТ"))
            {
                CurrentPlayer.Position = 0;
                CurrentPlayer.Money += 200;
            }
            else if (chance.Contains("Отправляйтесь в тюрьму"))
            {
                CurrentPlayer.Position = 8;
                CurrentPlayer.InJail = true;
                CurrentPlayer.JailTurns = 3;
            }
            else if (chance.Contains("Получите $50 от каждого игрока"))
            {
                foreach (var p in players.Where(p => p != CurrentPlayer && p.Money >= 50))
                {
                    p.Money -= 50;
                    CurrentPlayer.Money += 50;
                }
            }
        }

        private void DrawCommunityChestCard()
        {
            string[] chests = {
                "Наследство! Получите $100",
                "Оплатите лечение. Заплатите $50",
                "День рождения! Получите $10 от каждого игрока",
                "Налоговая проверка. Заплатите $75",
                "Возврат налогов. Получите $20",
                "Благотворительность. Заплатите $30"
            };

            Random rand = new Random();
            string chest = chests[rand.Next(chests.Length)];
            ApplyChestEffect(chest);
            lastCardEffect = chest;
        }

        private void ApplyChestEffect(string chest)
        {
            if (string.IsNullOrEmpty(chest)) return;

            if (chest.Contains("Получите $100"))
                CurrentPlayer.Money += 100;
            else if (chest.Contains("Заплатите $50"))
                CurrentPlayer.Money -= 50;
            else if (chest.Contains("Получите $10 от каждого игрока"))
            {
                foreach (var p in players.Where(p => p != CurrentPlayer && p.Money >= 10))
                {
                    p.Money -= 10;
                    CurrentPlayer.Money += 10;
                }
            }
            else if (chest.Contains("Заплатите $75"))
                CurrentPlayer.Money -= 75;
            else if (chest.Contains("Получите $20"))
                CurrentPlayer.Money += 20;
            else if (chest.Contains("Заплатите $30"))
                CurrentPlayer.Money -= 30;
        }

        public string GetLastCardEffect()
        {
            return lastCardEffect;
        }

        public bool BuyCurrentProperty()
        {
            var cell = CurrentCell;
            if (cell.Type == CellType.Property && cell.Owner == null)
            {
                int price = 100 + cell.Position * 15;
                if (CurrentPlayer.Money >= price)
                {
                    CurrentPlayer.Money -= price;
                    CurrentPlayer.Properties.Add(cell.Name);
                    cell.Owner = CurrentPlayer;
                    return true;
                }
            }
            return false;
        }

        public void NextPlayer()
        {
            if (players.Count > 0)
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }
        }

        public bool CheckGameOver()
        {
            return players.Any(p => p.Money <= 0);
        }
    }
}