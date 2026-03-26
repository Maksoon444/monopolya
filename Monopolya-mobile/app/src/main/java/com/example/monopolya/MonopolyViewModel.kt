package com.example.monopolya

import androidx.compose.runtime.*
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.*

class MonopolyViewModel : ViewModel() {
    private var _game by mutableStateOf<MonopolyGame?>(null)
    val game: MonopolyGame? get() = _game
    var playerCount by mutableStateOf(2); private set
    var startMoney by mutableStateOf(1500); private set
    var logMessages by mutableStateOf(listOf<String>()); private set
    var showLogPopup by mutableStateOf(false); private set

    init { startNewGame() }

    fun startNewGame() {
        _game = MonopolyGame(playerCount, startMoney)
        _game?.startNewGame()
        logMessages = listOf("🔄 Новая игра началась!", "👥 Игроки: ${_game?.players?.value?.joinToString { it.name } ?: ""}", "💰 Начальный капитал: $$startMoney")
    }

    fun rollDice(onMoveComplete: () -> Unit) {
        _game?.let { game ->
            game.rollDice()
            val roll = game.lastDiceRoll.value
            addLogMessage("🎲 ${game.currentPlayer.name} бросил: ${game.getDiceSymbol(roll.first)} + ${game.getDiceSymbol(roll.second)} = ${roll.first + roll.second}")
            viewModelScope.launch { game.moveCurrentPlayer {}; onMoveComplete() }
        }
    }

    fun buyProperty() { _game?.let { if (it.buyCurrentProperty()) addLogMessage("💰 ${it.currentPlayer.name} купил ${it.currentCell.name}") else addLogMessage("❌ Недостаточно денег") } }
    fun endTurn() { _game?.let { it.nextPlayer(); addLogMessage("⏭️ Ход перешел к ${it.currentPlayer.name}") } }
    fun addLogMessage(msg: String) { logMessages = logMessages + "[${SimpleDateFormat("HH:mm:ss", Locale.getDefault()).format(Date())}] $msg" }
    fun setPlayerCount(count: Int) { playerCount = count; startNewGame() }
    fun setStartMoney(money: Int) { startMoney = money; startNewGame() }
    fun toggleLogPopup() { showLogPopup = !showLogPopup }
    fun closeLogPopup() { showLogPopup = false }
}