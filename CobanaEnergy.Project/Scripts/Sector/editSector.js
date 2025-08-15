/**
 * Edit Sector Page - Minimal JavaScript
 * All dynamic functionality is handled by sectorFormManager.js
 */

$(document).ready(function () {
    // Initialize the consolidated sector form manager
    window.initializeEditSector();
    
    // Form submission handling
    $('#editSectorForm').on('submit', function (e) {
        e.preventDefault();
        
        // Basic validation
        if (!window.sectorFormManager.validateForm()) {
            return false;
        }

        // Submit form
        var formData = new FormData(this);
        
        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    window.sectorFormManager.showToastSuccess(response.message || 'Sector updated successfully!');
                    setTimeout(function () {
                        window.location.href = response.redirectUrl || '/Sector/Dashboard';
                    }, 1500);
                } else {
                    window.sectorFormManager.showToastError(response.message || 'Failed to update sector.');
                }
            },
            error: function () {
                window.sectorFormManager.showToastError('An error occurred while updating the sector.');
            }
        });
    });
});
