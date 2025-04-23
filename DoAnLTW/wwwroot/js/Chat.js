"use strict"; // Bật chế độ nghiêm ngặt để tránh lỗi cú pháp

// Tạo kết nối tới Hub SignalR với đường dẫn "/chatHub"
var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Vô hiệu hóa nút gửi tin nhắn cho đến khi kết nối được thiết lập
document.getElementById("sendButton").disabled = true;

// Lắng nghe sự kiện "ReceiveMessage" từ Hub (server gửi tin nhắn tới client)
connection.on("ReceiveMessage", function (user, message) {
    // Tạo một thẻ <li> mới để hiển thị tin nhắn
    var li = document.createElement("li");
    li.textContent = user + " says: " + message; // Định dạng hiển thị tin nhắn
    document.getElementById("messagesList").appendChild(li); // Thêm vào danh sách tin nhắn
});

// Kết nối đến Hub
connection.start().then(function () {
    // Khi kết nối thành công, bật nút gửi tin nhắn
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    // Xử lý lỗi nếu kết nối thất bại
    return console.error(err.toString());
});

// Xử lý sự kiện click của nút gửi tin nhắn
document.getElementById("sendButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value; // Lấy tên người dùng từ input
    var message = document.getElementById("messageInput").value; // Lấy nội dung tin nhắn từ input

    // Gửi tin nhắn đến Hub thông qua phương thức "SendMessage"
    connection.invoke("SendMessage", user, message).catch(function (err) {
        // Xử lý lỗi nếu gửi tin nhắn thất bại
        return console.error(err.toString());
    });

    event.preventDefault(); // Ngăn chặn trình duyệt tải lại trang sau khi gửi tin nhắn
});
