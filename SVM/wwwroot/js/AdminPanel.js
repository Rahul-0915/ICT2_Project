window.onload = function () {
    document.getElementById("mainFrame").src = "/Admin/AdminDashboard";
};
function toggleMenu(menuId) {
    var menus = document.querySelectorAll(".submenu");
    menus.forEach(m => {
        if (m.id !== menuId) m.style.display = "none";
    });

    var menu = document.getElementById(menuId);
    if (menu.style.display === "block")
        menu.style.display = "none";
    else
        menu.style.display = "block";
}