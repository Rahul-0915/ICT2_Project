// MENU
function toggleMenu() {

    let menu = document.getElementById("menu");

    if (menu) {
        menu.classList.toggle("active");
    }
}

/* PRELOADER + SLIDER */

window.onload = function () {

    // PRELOADER

    let preloader = document.getElementById("preloader");

    if (preloader) {

        setTimeout(function () {

            preloader.style.opacity = "0";

            setTimeout(function () {

                preloader.style.display = "none";

            }, 500);

        }, 1500);
    }

    // SLIDER

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

/*  EVENT IMAGE POPUP */

window.openEventLightbox = function (src) {

    document.getElementById("eventLightbox").style.display = "flex";

    document.getElementById("eventPopupImg").src = src;

    document.body.style.overflow = "hidden";
}

window.closeEventLightbox = function () {

    document.getElementById("eventLightbox").style.display = "none";

    document.body.style.overflow = "auto";
}

window.openEventVideo = function (src) {

    document.getElementById("eventVideoPopup").style.display = "flex";

    document.getElementById("eventPopupVideoSource").src = src;

    document.getElementById("eventPopupVideo").load();

    document.getElementById("eventPopupVideo").play();

    document.body.style.overflow = "hidden";
}

window.closeEventVideo = function () {

    document.getElementById("eventVideoPopup").style.display = "none";

    document.getElementById("eventPopupVideo").pause();

    document.body.style.overflow = "auto";
}
window.openTopperImage = function (src) {

    document.getElementById("topperLightbox").style.display = "flex";

    document.getElementById("topperPopupImg").src = src;

    document.body.style.overflow = "hidden";
}

window.closeTopperImage = function () {

    document.getElementById("topperLightbox").style.display = "none";

    document.body.style.overflow = "auto";
}