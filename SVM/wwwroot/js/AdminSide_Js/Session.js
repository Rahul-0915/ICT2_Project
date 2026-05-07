function filterTable() {

    let noData =
        document.getElementById("noDataMessage");

    let searchValue =
        document.getElementById("searchInput")
            .value.toLowerCase().trim();

    let statusValue =
        document.getElementById("statusFilter")
            .value.toLowerCase().trim();

    let rows =
        document.querySelectorAll(".table tbody tr");

    let found = false;

    rows.forEach((row) => {

        let sessionName =
            row.cells[0].innerText.toLowerCase();

        let status =
            row.cells[3].innerText.toLowerCase();

        let matchName =
            searchValue === "" ||
            sessionName.includes(searchValue);

        let matchStatus =
            statusValue === "" ||
            status.includes(statusValue);

        if (matchName && matchStatus) {

            row.style.display = "";
            found = true;

        } else {

            row.style.display = "none";
        }

    });

    if (found) {

        noData.style.display = "none";

    } else {

        noData.style.display = "block";
    }
}

/* RESET */

function resetFilter() {

    document.getElementById("searchInput").value = "";

    document.getElementById("statusFilter").value = "";

    let rows =
        document.querySelectorAll(".table tbody tr");

    rows.forEach((row) => {

        row.style.display = "";

    });
}