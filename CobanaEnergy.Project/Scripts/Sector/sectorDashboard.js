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

    // Handle edit button clicks using event delegation (more reliable than inline onclick)
    $(document).on('click', '.edit-btn', function(e) {
        e.preventDefault();
        var sectorId = $(this).data('sector-id');
        editSector(sectorId);
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
        if (typeof showToastSuccess === 'function' && typeof showToastError === 'function') {
            if (type === 'success') {
                showToastSuccess(message);
            } else if (type === 'error') {
                showToastError(message);
            } else if (type === 'warning') {
                showToastWarning(message);
            }
        } else {
            // Fallback to console if toast functions not available
            console.log(type === 'success' ? '✓ ' : type === 'error' ? '✗ ' : '⚠ ', message);
        }
    }

    // Handle edit sector button click
    window.editSector = function(sectorId) {
        // Check if sectorId is valid
        if (!sectorId || sectorId === 'null' || sectorId === 'undefined' || sectorId === '') {
            console.error('Invalid sector ID:', sectorId);
            alert('Invalid sector ID. Please try again.');
            return;
        }
        
        // Trim any whitespace
        sectorId = sectorId.toString().trim();
        
        // Redirect to edit page
        var editUrl = '/Sector/Edit/' + encodeURIComponent(sectorId);
        window.location.href = editUrl;
    };

    // Initialize with "All Sectors" selected
    $('#sectorTypeFilter').val('');
    
    // Apply initial filter
    filterSectorsByType("");
});
