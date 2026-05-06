function filterTable() {
    let title = document.getElementById("searchTitle").value.toLowerCase();
    let category = document.getElementById("searchCategory").value.toLowerCase();
    let date = document.getElementById("searchDate").value;

    let rows = document.querySelectorAll(".table tbody tr");

    rows.forEach(row => {

        let rowTitle = row.cells[0].innerText.toLowerCase();
        let rowCategory = row.cells[2].innerText.toLowerCase();

        // raw date from table
        let rowDateRaw = row.cells[5].innerText.trim();

        // 🔥 IMPORTANT: take only date part
        // works for: "06/05/2026", "06-05-2026", "2026-05-06 10:30"
        let rowDateOnly = rowDateRaw.split(" ")[0];

        // normalize slashes to match input format
        rowDateOnly = rowDateOnly.replace(/\//g, "-");

        let inputDate = date.replace(/\//g, "-");

        let matchTitle = rowTitle.includes(title) || title === "";
        let matchCategory = rowCategory.includes(category) || category === "";
        let matchDate = (rowDateOnly === inputDate || date === "");

        if (matchTitle && matchCategory && matchDate) {
            row.style.display = "";
        } else {
            row.style.display = "none";
        }
    });
}
function resetFilter() {
    document.getElementById("searchTitle").value = "";
    document.getElementById("searchCategory").value = "";
    document.getElementById("searchDate").value = "";

    let rows = document.querySelectorAll(".table tbody tr");
    rows.forEach(row => row.style.display = "");
}


/*file uploads */
function previewFile(event) {
    const file = event.target.files[0];
    const previewImg = document.getElementById('imagePreview');
    const container = document.getElementById('imagePreviewContainer');
    const pdfLabel = document.getElementById('pdfLabel');

    if (file) {
        const fileType = file.type;
        const isImage = fileType.startsWith('image/');
        const isPdf = fileType === 'application/pdf';

        if (isImage) {
            const reader = new FileReader();
            reader.onload = function (e) {
                previewImg.src = e.target.result;
                previewImg.style.display = 'block';
                pdfLabel.textContent = '';
                container.style.display = 'block';
            };
            reader.readAsDataURL(file);
        } else if (isPdf) {
            previewImg.style.display = 'none';
            pdfLabel.textContent = '📄 PDF file selected: ' + file.name;
            container.style.display = 'block';
        } else {
            container.style.display = 'none';
            pdfLabel.textContent = '';
        }
    } else {
        container.style.display = 'none';
        pdfLabel.textContent = '';
    }
}