$(document).ready(function () {
    // Initialize DataTable with ReconciliationsDashboard-style configuration
    var sectorTable = $('#sectorTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [[1, 'asc']], // Sort by Name column
        columnDefs: [
            { targets: 6, visible: false } // Hide Sector Type column
        ],
        autoWidth: false,
        dom: 'lfrtip'
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
        console.log('Edit button clicked for sector ID:', sectorId);
        console.log('Type of sectorId:', typeof sectorId);
        
        // Check if sectorId is valid
        if (!sectorId || sectorId === 'null' || sectorId === 'undefined') {
            console.error('Invalid sector ID:', sectorId);
            alert('Invalid sector ID. Please try again.');
            return;
        }
        
        // Redirect to edit page
        var editUrl = '/Sector/Edit/' + sectorId;
        console.log('Redirecting to:', editUrl);
        window.location.href = editUrl;
    };

    // Initialize with "All Sectors" selected
    $('#sectorTypeFilter').val('');
    
    // Apply initial filter
    filterSectorsByType("");
});
