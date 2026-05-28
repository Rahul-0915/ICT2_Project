let currentIndex = 0;

function openLightbox(index) {

    currentIndex = index;

    updateLightbox();

    document.getElementById("lightbox").style.display = "flex";

    document.body.style.overflow = "hidden";
}

function updateLightbox() {

    let item = galleryItems[currentIndex];

    let img = document.getElementById("lightbox-img");

    let video = document.getElementById("lightbox-video");

    let videoSource = document.getElementById("lightbox-video-source");

    // TEXT

    document.getElementById("lightbox-title").innerText = item.title;

    document.getElementById("lightbox-date").innerText = item.date;

    document.getElementById("lightbox-description").innerText = item.description;

    // VIDEO

    if (item.isVideo) {

        img.style.display = "none";

        video.style.display = "block";

        videoSource.src = item.src;

        video.load();
    }
    else {

        video.pause();

        video.style.display = "none";

        img.style.display = "block";

        img.src = item.src;
    }
}

function closeLightbox() {

    document.getElementById("lightbox").style.display = "none";

    document.getElementById("lightbox-video").pause();

    document.body.style.overflow = "auto";
}

function changeSlide(direction) {

    currentIndex += direction;

    if (currentIndex < 0) {
        currentIndex = galleryItems.length - 1;
    }

    if (currentIndex >= galleryItems.length) {
        currentIndex = 0;
    }

    updateLightbox();
}