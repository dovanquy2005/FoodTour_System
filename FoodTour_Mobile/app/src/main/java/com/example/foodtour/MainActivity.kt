package com.example.foodtour

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Face
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController

// --- MAIN ACTIVITY: Cửa chính của App ---
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            FoodTourApp()
        }
    }
}

// --- KHUNG ĐIỀU HƯỚNG TỔNG (QUAN TRỌNG NHẤT) ---
@Composable
fun FoodTourApp() {
    val navController = rememberNavController()

    // Đây là bản đồ chỉ đường cho App
    NavHost(navController = navController, startDestination = "login") {

        // Màn hình 1: Đăng nhập
        composable("login") {
            LoginScreen(navController)
        }

        // Màn hình 2: Trang chủ
        composable("home") {
            HomeScreen(navController)
        }

        // Màn hình 3: Chi tiết món ăn (Nhận tham số id món ăn)
        composable("detail/{monAn}") { backStackEntry ->
            val monAn = backStackEntry.arguments?.getString("monAn") ?: "Món lạ"
            DetailScreen(navController, monAn)
        }
    }
}

// --- MÀN HÌNH 1: ĐĂNG NHẬP ---
@Composable
fun LoginScreen(navController: NavController) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(text = "FoodTour", fontSize = 40.sp, fontWeight = FontWeight.Bold, color = Color(0xFFFF5722))
        Text(text = "Khám phá ẩm thực Việt", fontSize = 16.sp, color = Color.Gray)

        Spacer(modifier = Modifier.height(32.dp))

        OutlinedTextField(
            value = username,
            onValueChange = { username = it },
            label = { Text("Tài khoản") },
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(16.dp))

        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            label = { Text("Mật khẩu") },
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = { navController.navigate("home") }, // Bấm nút thì bay sang Home
            modifier = Modifier.fillMaxWidth().height(50.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFFF5722))
        ) {
            Text("ĐĂNG NHẬP", fontSize = 18.sp)
        }
    }
}

// --- MÀN HÌNH 2: TRANG CHỦ (DANH SÁCH MÓN ĂN) ---
@Composable
fun HomeScreen(navController: NavController) {
    // Dữ liệu giả (Sau này sẽ lấy từ API C#)
    val danhSachMon = listOf("Bún Bò Huế", "Phở Hà Nội", "Cơm Tấm Sài Gòn", "Bánh Mì Hội An", "Mì Quảng")

    Scaffold(
        topBar = {
            @OptIn(ExperimentalMaterial3Api::class)
            TopAppBar(
                title = { Text("Thực đơn hôm nay") },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = Color(0xFFFF5722), titleContentColor = Color.White)
            )
        }
    ) { innerPadding ->
        LazyColumn(contentPadding = innerPadding) {
            items(danhSachMon) { mon ->
                FoodItemCard(monAn = mon, onClick = {
                    // Bấm vào món nào thì chuyển sang trang chi tiết món đó
                    navController.navigate("detail/$mon")
                })
            }
        }
    }
}

// --- COMPONENT CON: THẺ MÓN ĂN (ĐỂ VẼ TRONG LIST) ---
@Composable
fun FoodItemCard(monAn: String, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(10.dp)
            .clickable { onClick() }, // Bắt sự kiện click
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White)
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Icon giả làm ảnh món ăn
            Icon(Icons.Default.ShoppingCart, contentDescription = null, modifier = Modifier.size(50.dp), tint = Color(0xFFFF5722))
            Spacer(modifier = Modifier.width(16.dp))
            Column {
                Text(text = monAn, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                Text(text = "50.000 VNĐ", color = Color.Gray)
            }
        }
    }
}

// --- MÀN HÌNH 3: CHI TIẾT (DETAIL) ---
@Composable
fun DetailScreen(navController: NavController, monAn: String) {
    Column(
        modifier = Modifier.fillMaxSize().padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Nút quay lại
        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Start) {
            IconButton(onClick = { navController.popBackStack() }) { // Lệnh quay lại màn hình trước
                Icon(Icons.Default.ArrowBack, contentDescription = "Back")
            }
        }

        Spacer(modifier = Modifier.height(20.dp))

        Icon(Icons.Default.Face, contentDescription = null, modifier = Modifier.size(100.dp), tint = Color(0xFFFF5722))

        Text(text = monAn, fontSize = 32.sp, fontWeight = FontWeight.Bold)
        Text(text = "Mô tả chi tiết về món $monAn ngon tuyệt vời...", fontSize = 16.sp, color = Color.Gray)

        Spacer(modifier = Modifier.height(20.dp))

        Button(onClick = { /* Chưa làm gì */ }) {
            Text("Đặt món ngay")
        }
    }
}