$(document).ready(function () {
    // Initialize DataTable
    var sectorTable = $('#sectorTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [[1, 'asc'], [2, 'desc']], // Sort by Name, then Active status
        autoWidth: false,
        dom: 'lfrtip',
        columnDefs: [
            { orderable: false, targets: 0 }, // Edit button column
            { orderable: false, targets: 3 }, // Start Date column
            { orderable: false, targets: 4 }, // End Date column
            { visible: false, targets: 6 }    // Hide Sector Type column
        ]
    });

    // Handle sector type filter change
    $('#sectorTypeFilter').on('change', function () {
        var selectedType = $(this).val();
        filterSectorsByType(selectedType);
    });

    // Filter sectors by sector type
    function filterSectorsByType(sectorType) {
        if (sectorType === "") {
            // Show all sectors
            sectorTable.search('').draw();
        } else {
            // Filter by sector type using custom filter function
            $.fn.dataTable.ext.search.push(function(settings, data, dataIndex) {
                var sectorTypeInRow = data[6]; // Sector Type is in the 7th column (index 6) - hidden column
                if (sectorType === "" || sectorTypeInRow === sectorType) {
                    return true;
                }
                return false;
            });
            sectorTable.draw();
            // Remove the custom filter after applying it
            $.fn.dataTable.ext.search.pop();
        }
    }

    // Show toast notification (for future use)
    function showToast(message, type) {
        // You can implement your own toast notification system here
        // For now, we'll use a simple alert
        if (type === "success") {
            alert("✓ " + message);
        } else {
            alert("✗ " + message);
        }
    }

    // Handle edit sector button click
    window.editSector = function(sectorId) {
        // Redirect to edit page
        window.location.href = '/Sector/Edit/' + sectorId;
    };

    // Initialize with "All Sectors" selected
    $('#sectorTypeFilter').val('');
    
    // Apply initial filter
    filterSectorsByType("");
});
