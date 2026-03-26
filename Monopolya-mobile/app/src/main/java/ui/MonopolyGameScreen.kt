package com.example.monopolya.ui

import androidx.compose.foundation.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.*
import androidx.compose.foundation.shape.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.monopolya.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MonopolyGameScreen(viewModel: MonopolyViewModel) {
    val game = viewModel.game
    val players by remember { derivedStateOf { game?.players?.value ?: emptyList() } }
    val board by remember { derivedStateOf { game?.board?.value ?: emptyList() } }
    val currentPlayerIndex by remember { derivedStateOf { game?.currentPlayerIndex?.value ?: 0 } }
    val lastDiceRoll by remember { derivedStateOf { game?.lastDiceRoll?.value ?: Pair(1, 1) } }
    val lastCardEffect by remember { derivedStateOf { game?.lastCardEffect?.value ?: "" } }
    val isMoving by remember { derivedStateOf { game?.isMoving?.value ?: false } }
    var showRules by remember { mutableStateOf(false) }
    var showAbout by remember { mutableStateOf(false) }

    Column(
        Modifier
            .fillMaxSize()
            .background(Color(0xFFF0F0F0))
    ) {
        Surface(Modifier.fillMaxWidth(), tonalElevation = 4.dp, color = Color.LightGray) {
            Row(
                Modifier
                    .fillMaxWidth()
                    .padding(8.dp),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                SettingsMenu(viewModel)
                TextButton({ showRules = true }) {
                    Icon(
                        Icons.Default.Info, null, Modifier.size(16.dp)
                    ); Spacer(Modifier.width(4.dp)); Text(
                    "ПРАВИЛА", fontSize = 12.sp, fontWeight = FontWeight.Bold
                )
                }
                TextButton({ showAbout = true }) {
                    Icon(
                        Icons.Default.Info, null, Modifier.size(16.dp)
                    ); Spacer(Modifier.width(4.dp)); Text(
                    "О ПРОГРАММЕ", fontSize = 12.sp, fontWeight = FontWeight.Bold
                )
                }
            }
        }
        Row(
            Modifier
                .fillMaxSize()
                .padding(8.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Card(
                Modifier
                    .weight(2f)
                    .fillMaxHeight(), shape = RoundedCornerShape(8.dp)
            ) {
                GameBoard(
                    board, players, game
                ) {}
            }
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                PlayersInfoCard(players, game)
                CurrentPlayerCard(
                    game,
                    players.getOrNull(currentPlayerIndex),
                    lastDiceRoll,
                    lastCardEffect,
                    isMoving,
                    { viewModel.rollDice {} },
                    { viewModel.buyProperty() },
                    { viewModel.endTurn() },
                    { viewModel.startNewGame() },
                    { viewModel.toggleLogPopup() })
            }
        }
    }
    if (showRules) RulesDialog { showRules = false }
    if (showAbout) AboutDialog { showAbout = false }
    if (viewModel.showLogPopup) LogPopup(viewModel.logMessages, viewModel::closeLogPopup)
}

@Composable
fun SettingsMenu(vm: MonopolyViewModel) {
    var e by remember { mutableStateOf(false) }; Box {
        TextButton({
            e = true
        }) {
            Icon(Icons.Default.Settings, null, Modifier.size(16.dp)); Text(
            "НАСТРОЙКИ", fontSize = 12.sp, fontWeight = FontWeight.Bold
        )
        }; DropdownMenu(e, { e = false }) {
        listOf(
            2, 3, 4
        ).forEach {
            DropdownMenuItem({ Text("$it игрока") }, { vm.setPlayerCount(it); e = false })
        }; Divider(); listOf(1000, 1500, 2000).forEach {
        DropdownMenuItem({ Text("$$it") }, { vm.setStartMoney(it); e = false })
    }
    }
    }
}

@Composable
fun GameBoard(
    board: List<Cell>, players: List<Player>, game: MonopolyGame?, onCellClick: (Int) -> Unit
) {
    val s = 85.dp; Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Row {
            for (i in 0..7) if (i < board.size) GameCell(
                board[i], players.filter { it.position == i }, s, game
            ) { onCellClick(i) }
        }; Row(
        Modifier.fillMaxWidth(), Arrangement.SpaceBetween
    ) {
        Column {
            for (i in 23 downTo 22) if (i < board.size) GameCell(
                board[i], players.filter { it.position == i }, s, game
            ) { onCellClick(i) }
        }; Box(
        Modifier
            .size(s * 4)
            .padding(4.dp)
    ) { ChatBox() }; Column {
        for (i in 8..14) if (i < board.size) GameCell(
            board[i], players.filter { it.position == i }, s, game
        ) { onCellClick(i) }
    }
    }; Row {
        for (i in 28 downTo 15) if (i < board.size) GameCell(
            board[i], players.filter { it.position == i }, s, game
        ) { onCellClick(i) }
    }
    }
}

@Composable
fun GameCell(
    cell: Cell,
    players: List<Player>,
    size: androidx.compose.ui.unit.Dp,
    game: MonopolyGame?,
    onClick: () -> Unit
) {
    Box(
        Modifier
            .size(size)
            .padding(1.dp)
            .border(1.dp, Color.Black, RoundedCornerShape(4.dp))
            .background(game?.getCellColor(cell.position) ?: Color.White)
            .clickable { onClick() }) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
            Modifier.fillMaxSize()
        ) {
            Text(game?.getCellIcon(cell.type) ?: "⬜", fontSize = 20.sp); Text(
            cell.name.take(10), fontSize = 8.sp, textAlign = TextAlign.Center, maxLines = 2
        ); if (cell.type == CellType.PROPERTY) Text(
            "$${game?.getCellPrice(cell.position) ?: 0}",
            fontSize = 8.sp,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF006400)
        )
        }; players.forEachIndexed { i, p ->
        Box(
            Modifier
                .align(Alignment.TopStart)
                .offset(x = (i * 15).dp, y = (i * 10).dp)
                .size(24.dp)
                .clip(CircleShape)
                .background(game?.getPlayerColor(players.indexOf(p)) ?: Color.Gray)
                .border(2.dp, Color.White, CircleShape), Alignment.Center
        ) {
            Text(
                p.name.first().toString(),
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
        }
    }
    }
}

@Composable
fun ChatBox() {
    var t by remember { mutableStateOf("") }
    var m by remember { mutableStateOf(listOf<Pair<String, String>>()) }; Card(
        shape = RoundedCornerShape(
            12.dp
        ), colors = CardDefaults.cardColors(containerColor = Color(0xFFADD8E6))
    ) {
        Column(
            Modifier
                .padding(8.dp)
                .fillMaxSize()
        ) {
            Text(
                "💬 ЧАТ",
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center,
                Modifier.fillMaxWidth()
            ); LazyColumn(
            Modifier.weight(1f), reverseLayout = true
        ) {
            items(m.reversed()) { (s, msg) ->
                Text(
                    "${s.take(1)}: $msg", fontSize = 9.sp, Modifier.padding(2.dp)
                )
            }
        }; Row {
            OutlinedTextField(
                t,
                { t = it },
                Modifier.weight(1f),
                textStyle = androidx.compose.ui.text.TextStyle(fontSize = 10.sp),
                singleLine = true,
                placeholder = { Text("Введите...", fontSize = 10.sp) }); IconButton({
            if (t.isNotBlank()) {
                m = m + Pair("Игрок", t); t = ""
            }
        }) { Icon(Icons.Default.Send, null, Modifier.size(20.dp)) }
        }
        }
    }
}

@Composable
fun PlayersInfoCard(players: List<Player>, game: MonopolyGame?) {
    Card(colors = CardDefaults.cardColors(containerColor = Color(0xFFFFFFE0))) {
        Column(
            Modifier.padding(
                12.dp
            )
        ) {
            Text(
                "ИГРОКИ",
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center,
                Modifier.fillMaxWidth()
            ); players.forEach { p ->
            Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween) {
                Row {
                    Box(
                        Modifier
                            .size(24.dp)
                            .clip(CircleShape)
                            .background(game?.getPlayerColor(players.indexOf(p)) ?: Color.Gray)
                    ) {
                        Text(
                            p.name.first().toString(),
                            Modifier.align(Alignment.Center),
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold,
                            color = Color.White
                        )
                    }; Spacer(Modifier.width(8.dp)); Text(p.name, fontSize = 12.sp)
                }; Text(
                "$${p.money} ${if (p.inJail) "🔒" else ""}",
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                color = Color(0xFF006400)
            )
            }
        }
        }
    }
}

@Composable
fun CurrentPlayerCard(
    game: MonopolyGame?,
    p: Player?,
    dice: Pair<Int, Int>,
    effect: String,
    moving: Boolean,
    onRoll: () -> Unit,
    onBuy: () -> Unit,
    onEnd: () -> Unit,
    onNew: () -> Unit,
    onLog: () -> Unit
) {
    Card(colors = CardDefaults.cardColors(containerColor = Color.LightGray)) {
        Column(
            Modifier
                .fillMaxWidth()
                .padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                "ТЕКУЩИЙ ИГРОК",
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold,
                color = Color(0xFF8B0000)
            ); Box(
            Modifier
                .size(60.dp)
                .clip(CircleShape)
                .background(
                    game?.getPlayerColor(game?.players?.value?.indexOf(p) ?: 0) ?: Color.Gray
                )
                .border(2.dp, Color.Black, CircleShape), Alignment.Center
        ) {
            Text(
                p?.name?.first()?.toString() ?: "?",
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
        }; Text(
            p?.name ?: "", fontSize = 16.sp, fontWeight = FontWeight.Bold
        ); Text(
            "Деньги: $${p?.money ?: 0}",
            fontSize = 14.sp,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF006400)
        ); Text(
            "Позиция: ${p?.position ?: 0}", fontSize = 12.sp
        ); Text(
            "Недвижимость: ${p?.properties?.size ?: 0}", fontSize = 12.sp
        ); Spacer(Modifier.height(8.dp)); Row {
            DiceBox(dice.first); Spacer(Modifier.width(8.dp)); DiceBox(
            dice.second
        )
        }; if (effect.isNotEmpty()) Text(
            effect, fontSize = 10.sp, color = Color.Magenta, Modifier.padding(4.dp)
        ); Spacer(Modifier.height(8.dp)); Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onRoll,
                enabled = !moving,
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF90EE90))
            ) {
                Icon(Icons.Default.Casino, null); Spacer(Modifier.width(4.dp)); Text(
                "БРОСИТЬ", fontSize = 11.sp
            )
            }; Button(
            onBuy,
            enabled = !moving,
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFADD8E6))
        ) {
            Icon(Icons.Default.House, null); Spacer(Modifier.width(4.dp)); Text(
            "КУПИТЬ", fontSize = 11.sp
        )
        }; Button(
            onEnd,
            enabled = !moving,
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFFAA0A0))
        ) {
            Icon(Icons.Default.SkipNext, null); Spacer(Modifier.width(4.dp)); Text(
            "ХОД", fontSize = 11.sp
        )
        }
        }; Spacer(Modifier.height(4.dp)); Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onNew, colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFF0E68C))
            ) {
                Icon(Icons.Default.Refresh, null); Spacer(Modifier.width(4.dp)); Text(
                "НОВАЯ ИГРА", fontSize = 11.sp
            )
            }; Button(
            onLog, colors = ButtonDefaults.buttonColors(containerColor = Color.LightGray)
        ) {
            Icon(Icons.Default.History, null); Spacer(Modifier.width(4.dp)); Text(
            "ЖУРНАЛ", fontSize = 11.sp
        )
        }
        }
        }
    }
}

@Composable
fun DiceBox(v: Int) {
    Box(
        Modifier
            .size(50.dp)
            .background(Color.White, RoundedCornerShape(8.dp))
            .border(2.dp, Color.Black, RoundedCornerShape(8.dp)), Alignment.Center
    ) {
        Text(
            when (v) {
                1 -> "⚀"; 2 -> "⚁"; 3 -> "⚂"; 4 -> "⚃"; 5 -> "⚄"; 6 -> "⚅"; else -> "⚀"
            }, fontSize = 32.sp
        )
    }
}

@Composable
fun RulesDialog(on: () -> Unit) {
    Dialog(onDismissRequest = on) {
        Card(
            Modifier
                .fillMaxWidth()
                .padding(16.dp), RoundedCornerShape(16.dp)
        ) {
            Column(Modifier.padding(16.dp)) {
                Text(
                    "ПРАВИЛА",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                    Modifier.fillMaxWidth()
                ); Spacer(Modifier.height(8.dp)); Text(
                "1. Бросайте кубики и ходите\n2. Проход СТАРТА +$200\n3. Покупайте недвижимость\n4. Клетки с ? дают бонусы/штрафы\n5. Тюрьма - пропуск 3 ходов\n6. Игра до банкротства",
                fontSize = 12.sp
            ); Spacer(Modifier.height(16.dp)); Button(
                on, Modifier.fillMaxWidth()
            ) { Text("ЗАКРЫТЬ") }
            }
        }
    }
}

@Composable
fun AboutDialog(on: () -> Unit) {
    Dialog(onDismissRequest = on) {
        Card(
            Modifier
                .fillMaxWidth()
                .padding(16.dp), RoundedCornerShape(16.dp)
        ) {
            Column(Modifier.padding(16.dp)) {
                Text(
                    "МОНОПОЛИЯ",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                    Modifier.fillMaxWidth()
                ); Text(
                "Версия 1.0\nKotlin + Jetpack Compose\n© 2024", fontSize = 12.sp
            ); Spacer(Modifier.height(16.dp)); Button(
                on, Modifier.fillMaxWidth()
            ) { Text("ЗАКРЫТЬ") }
            }
        }
    }
}

@Composable
fun LogPopup(messages: List<String>, on: () -> Unit) {
    Dialog(onDismissRequest = on) {
        Card(
            Modifier
                .fillMaxWidth()
                .height(400.dp), RoundedCornerShape(16.dp)
        ) {
            Column(Modifier.padding(16.dp)) {
                Text(
                    "ЖУРНАЛ",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                    Modifier.fillMaxWidth()
                ); Spacer(Modifier.height(8.dp)); LazyColumn(
                Modifier.weight(1f), reverseLayout = true
            ) {
                items(
                    messages.reversed()
                ) { Text(it, fontSize = 11.sp, Modifier.padding(2.dp)) }
            }; Spacer(Modifier.height(8.dp)); Button(
                on, Modifier.fillMaxWidth()
            ) { Text("ЗАКРЫТЬ") }
            }
        }
    }
}