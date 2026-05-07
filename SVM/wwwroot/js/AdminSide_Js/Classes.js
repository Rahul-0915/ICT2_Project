function filterClasses() {

    let search =
        document.getElementById("searchInput")
            .value.toLowerCase();

    let medium =
        document.getElementById("mediumFilter")
            .value.toLowerCase();

    let cards =
        document.querySelectorAll(".class-card");

    let found = false;

    cards.forEach(card => {

        let className =
            card.querySelector(".class-name")
                .innerText.toLowerCase();

        let cardMedium =
            card.dataset.medium.toLowerCase();

        let matchSearch =
            className.includes(search);

        let matchMedium =
            medium === "all" ||
            cardMedium === medium;

        if (matchSearch && matchMedium) {

            card.style.display = "block";

            found = true;

        }

        else {

            card.style.display = "none";

        }

    });

    document.getElementById("noDataMessage")
        .style.display = found ? "none" : "block";

}

function resetFilter() {

    document.getElementById("searchInput").value = "";

    document.getElementById("mediumFilter").value = "all";

    filterClasses();

}