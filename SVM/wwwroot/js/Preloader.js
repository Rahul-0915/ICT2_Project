// MENU

function toggleMenu() {
    let menu = document.getElementById("menu");
    if (menu) {
        menu.classList.toggle("active");
    }
}

// PAGE LOAD
window.onload = function () {

    // 🔥 PRELOADER
    let preloader = document.getElementById("preloader");

    if (preloader) {

        setTimeout(function () {

            preloader.style.opacity = "0";

            setTimeout(function () {
                preloader.style.display = "none";
            }, 500);

        }, 1500);
    }

    // 🔥 SLIDER
    let index = 0;
    let slides = document.getElementsByClassName("slides");

    function showSlides() {

        if (slides.length === 0) return;

        for (let i = 0; i < slides.length; i++) {
            slides[i].classList.remove("active");
        }

        index++;
        if (index > slides.length) {
            index = 1;
        }

        slides[index - 1].classList.add("active");

        setTimeout(showSlides, 3000);
    }

    showSlides();
};