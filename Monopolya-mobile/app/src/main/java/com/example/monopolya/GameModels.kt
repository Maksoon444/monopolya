package com.example.monopolya

import androidx.compose.ui.graphics.Color
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlin.random.Random

enum class CellType { START, PROPERTY, CHANCE, COMMUNITY_CHEST, JAIL, FREE_PARKING, GO_TO_JAIL, TAX, LUCKY, UNLUCKY, GIFT, PENALTY }

data class Cell(val position: Int, val name: String, val type: CellType, var owner: Player? = null)
data class Player(val name: String, var position: Int = 0, var money: Int = 1500, var inJail: Boolean = false, var jailTurns: Int = 0, val properties: MutableList<String> = mutableListOf())

class MonopolyGame(playerCount: Int = 2, startMoney: Int = 1500) {
    private val _players = MutableStateFlow<List<Player>>(emptyList())
    val players: StateFlow<List<Player>> = _players.asStateFlow()
    private val _board = MutableStateFlow<List<Cell>>(emptyList())
    val board: StateFlow<List<Cell>> = _board.asStateFlow()
    private val _currentPlayerIndex = MutableStateFlow(0)
    val currentPlayerIndex: StateFlow<Int> = _currentPlayerIndex.asStateFlow()
    private val _lastDiceRoll = MutableStateFlow(Pair(1, 1))
    val lastDiceRoll: StateFlow<Pair<Int, Int>> = _lastDiceRoll.asStateFlow()
    private val _lastCardEffect = MutableStateFlow("")
    val lastCardEffect: StateFlow<String> = _lastCardEffect.asStateFlow()
    private val _isMoving = MutableStateFlow(false)
    val isMoving: StateFlow<Boolean> = _isMoving.asStateFlow()
    private val dice = Random(System.currentTimeMillis())
    val currentPlayer: Player get() = _players.value[_currentPlayerIndex.value]
    val currentCell: Cell get() = _board.value[currentPlayer.position]

    init {
        _board.value = listOf(
            Cell(0, "СТАРТ", CellType.START), Cell(1, "Бульвар Арбат", CellType.PROPERTY),
            Cell(2, "Шанс", CellType.CHANCE), Cell(3, "Улица Тверская", CellType.PROPERTY),
            Cell(4, "Удача", CellType.LUCKY), Cell(5, "Площадь Революции", CellType.PROPERTY),
            Cell(6, "Казна", CellType.COMMUNITY_CHEST), Cell(7, "Налог", CellType.TAX),
            Cell(8, "ТЮРЬМА", CellType.JAIL), Cell(9, "Улица Пушкинская", CellType.PROPERTY),
            Cell(10, "Подарок", CellType.GIFT), Cell(11, "Проспект Мира", CellType.PROPERTY),
            Cell(12, "Неудача", CellType.UNLUCKY), Cell(13, "Шанс", CellType.CHANCE),
            Cell(14, "Улица Чехова", CellType.PROPERTY), Cell(15, "Бесплатная стоянка", CellType.FREE_PARKING),
            Cell(16, "Штраф", CellType.PENALTY), Cell(17, "Улица Горького", CellType.PROPERTY),
            Cell(18, "Казна", CellType.COMMUNITY_CHEST), Cell(19, "Удача", CellType.LUCKY),
            Cell(20, "Вокзал", CellType.PROPERTY), Cell(21, "Налог на роскошь", CellType.TAX),
            Cell(22, "Подарок", CellType.GIFT), Cell(23, "Отправляйтесь в тюрьму", CellType.GO_TO_JAIL),
            Cell(24, "Улица Ленина", CellType.PROPERTY), Cell(25, "Неудача", CellType.UNLUCKY),
            Cell(26, "Проспект Победы", CellType.PROPERTY), Cell(27, "Штраф", CellType.PENALTY),
            Cell(28, "Шанс", CellType.CHANCE)
        )
        val playersList = mutableListOf<Player>()
        val colors = listOf("Красный", "Синий", "Зеленый", "Желтый")
        for (i in 0 until minOf(playerCount, 4)) playersList.add(Player(colors[i], money = startMoney))
        _players.value = playersList
    }

    fun startNewGame() {
        _players.value = _players.value.map { it.apply { position = 0; inJail = false; jailTurns = 0; properties.clear() } }
        _board.value = _board.value.map { it.apply { owner = null } }
        _currentPlayerIndex.value = 0
    }

    fun rollDice() { _lastDiceRoll.value = Pair(dice.nextInt(1, 7), dice.nextInt(1, 7)) }

    suspend fun moveCurrentPlayer(onStepComplete: (Int) -> Unit) {
        val total = _lastDiceRoll.value.first + _lastDiceRoll.value.second
        if (currentPlayer.inJail) { handleJailTurn(total); return }
        val oldPosition = currentPlayer.position
        _isMoving.value = true
        for (step in 1..total) {
            val stepPosition = (oldPosition + step) % _board.value.size
            _players.value.toMutableList().apply { this[_currentPlayerIndex.value].position = stepPosition }.let { _players.value = it }
            onStepComplete(stepPosition)
            kotlinx.coroutines.delay(150)
        }
        _isMoving.value = false
        if (oldPosition + total >= _board.value.size) {
            _players.value.toMutableList().apply { this[_currentPlayerIndex.value].money += 200 }.let { _players.value = it }
            _lastCardEffect.value = "Проход через СТАРТ! +$200"
        }
        handleCurrentCellAction()
    }

    private fun handleJailTurn(total: Int) {
        val playersList = _players.value.toMutableList()
        val player = playersList[_currentPlayerIndex.value]
        player.jailTurns--
        if (_lastDiceRoll.value.first == _lastDiceRoll.value.second) {
            player.inJail = false; player.jailTurns = 0
            player.position = (player.position + total) % _board.value.size
            _lastCardEffect.value = "Вы вышли из тюрьмы!"
        } else if (player.jailTurns <= 0) {
            player.money -= 50; player.inJail = false
            player.position = (player.position + total) % _board.value.size
            _lastCardEffect.value = "Оплачено $50 за выход из тюрьмы"
        } else _lastCardEffect.value = "В тюрьме! Осталось ходов: ${player.jailTurns}"
        _players.value = playersList
    }

    private fun handleCurrentCellAction() {
        val cell = currentCell
        val playersList = _players.value.toMutableList()
        val player = playersList[_currentPlayerIndex.value]
        when (cell.type) {
            CellType.CHANCE -> drawCard(true)
            CellType.COMMUNITY_CHEST -> drawCard(false)
            CellType.LUCKY -> { val a = Random.nextInt(50, 201); player.money += a; _lastCardEffect.value = "Удача! +$$a" }
            CellType.UNLUCKY -> { val a = Random.nextInt(30, 151); player.money -= a; _lastCardEffect.value = "Неудача! -$$a" }
            CellType.GIFT -> { val a = Random.nextInt(40, 121); player.money += a; _lastCardEffect.value = "Подарок! +$$a" }
            CellType.PENALTY -> { val a = Random.nextInt(20, 101); player.money -= a; _lastCardEffect.value = "Штраф! -$$a" }
            CellType.GO_TO_JAIL -> { player.position = 8; player.inJail = true; player.jailTurns = 3; _lastCardEffect.value = "Отправляйтесь в тюрьму!" }
            CellType.TAX -> { val tax = if (cell.name.contains("роскошь")) 100 else 50; player.money -= tax; _lastCardEffect.value = "Налог -$$tax" }
            else -> {}
        }
        _players.value = playersList
    }

    private fun drawCard(isChance: Boolean) {
        val effects = if (isChance) listOf(
            "Аванс! +$100" to { currentPlayer.money += 100 },
            "Штраф! -$50" to { currentPlayer.money -= 50 },
            "Лотерея! +$150" to { currentPlayer.money += 150 },
            "Ремонт! -$80" to { currentPlayer.money -= 80 },
            "На СТАРТ! +$200" to { currentPlayer.position = 0; currentPlayer.money += 200 },
            "В тюрьму!" to { currentPlayer.position = 8; currentPlayer.inJail = true; currentPlayer.jailTurns = 3 }
        ) else listOf(
            "Наследство! +$100" to { currentPlayer.money += 100 },
            "Лечение! -$50" to { currentPlayer.money -= 50 },
            "День рождения! +$10 от всех" to { _players.value.filter { it != currentPlayer && it.money >= 10 }.forEach { it.money -= 10; currentPlayer.money += 10 } },
            "Налоги! -$75" to { currentPlayer.money -= 75 },
            "Возврат! +$20" to { currentPlayer.money += 20 },
            "Благотворительность! -$30" to { currentPlayer.money -= 30 }
        )
        val (effect, action) = effects.random()
        action()
        _lastCardEffect.value = effect
    }

    fun buyCurrentProperty(): Boolean {
        val cell = currentCell
        if (cell.type == CellType.PROPERTY && cell.owner == null) {
            val price = 100 + cell.position * 15
            if (currentPlayer.money >= price) {
                _players.value.toMutableList().apply { this[_currentPlayerIndex.value].money -= price; this[_currentPlayerIndex.value].properties.add(cell.name) }.let { _players.value = it }
                _board.value.toMutableList().apply { this[cell.position].owner = currentPlayer }.let { _board.value = it }
                _lastCardEffect.value = "Куплено: ${cell.name} за $$price"
                return true
            }
        }
        return false
    }

    fun nextPlayer() { if (_players.value.isNotEmpty()) _currentPlayerIndex.value = (_currentPlayerIndex.value + 1) % _players.value.size }
    fun checkGameOver(): Boolean = _players.value.any { it.money <= 0 }
    fun getCellPrice(position: Int): Int = 100 + position * 15
    fun getCellIcon(type: CellType) = when (type) {
        CellType.START -> "🏁"; CellType.PROPERTY -> "🏘️"; CellType.CHANCE -> "❓"
        CellType.COMMUNITY_CHEST -> "📦"; CellType.JAIL -> "⛓️"; CellType.FREE_PARKING -> "🅿️"
        CellType.GO_TO_JAIL -> "🚓"; CellType.TAX -> "💰"; CellType.LUCKY -> "🍀"
        CellType.UNLUCKY -> "💢"; CellType.GIFT -> "🎁"; CellType.PENALTY -> "⚠️"
        else -> "⬜"
    }
    fun getPlayerColor(index: Int) = when (index % 4) { 0 -> Color(0xFFFF5050); 1 -> Color(0xFF5050FF); 2 -> Color(0xFF50FF50); 3 -> Color(0xFFFFFF50); else -> Color.Gray }
    fun getCellColor(position: Int): Color {
        val cell = _board.value.getOrNull(position) ?: return Color.Gray
        if (cell.owner != null) return getPlayerColor(_players.value.indexOf(cell.owner))
        return when (cell.type) {
            CellType.START -> Color(0xFFC8FFC8); CellType.PROPERTY -> Color(0xFFC8DCFF); CellType.CHANCE -> Color(0xFFFFC896)
            CellType.COMMUNITY_CHEST -> Color(0xFFFFFFC8); CellType.JAIL -> Color(0xFFFF9696); CellType.FREE_PARKING -> Color(0xFFF0F0F0)
            CellType.GO_TO_JAIL -> Color(0xFFFF6464); CellType.TAX -> Color(0xFFFFC8DC); CellType.LUCKY -> Color(0xFFFFD700)
            CellType.UNLUCKY -> Color(0xFFA9A9A9); CellType.GIFT -> Color(0xFFEE82EE); CellType.PENALTY -> Color(0xFFFF8C00)
            else -> Color.White
        }
    }
    fun getDiceSymbol(value: Int) = when (value) { 1 -> "⚀"; 2 -> "⚁"; 3 -> "⚂"; 4 -> "⚃"; 5 -> "⚄"; 6 -> "⚅"; else -> "⚀" }
}