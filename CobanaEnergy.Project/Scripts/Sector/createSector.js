/**
 * Create Sector Page - Minimal JavaScript
 * All dynamic functionality is handled by sectorFormManager.js
 */

$(document).ready(function () {
    // Initialize the consolidated sector form manager
    window.initializeCreateSector();
    
    // Initialize duplicate account checker for bank details
    DuplicateAccountChecker.init({
        accountInputSelector: '#accountNumber',
        modalId: 'duplicateAccountModalSector',
        loaderSelector: null,
        controllerEndpoint: 'CheckDuplicateBankAccount',
        fields: [
            { displayName: 'Bank Name', dataProperty: 'BankName' },
            { displayName: 'Account Name', dataProperty: 'AccountName' },
            { displayName: 'Sort Code', dataProperty: 'AccountSortCode' },
            { displayName: 'Account Number', dataProperty: 'AccountNumber' },
            { displayName: 'IBAN', dataProperty: 'IBAN' },
            { displayName: 'Swift Code', dataProperty: 'SwiftCode' },
            { displayName: 'Bank Branch Address', dataProperty: 'BankBranchAddress' },
            { displayName: 'Receivers Address', dataProperty: 'ReceiversAddress' }
        ],
        showErrorToast: false
    });
    
    // Form submission handling
    $('#createSectorForm').on('submit', function (e) {
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
                    window.sectorFormManager.showToastSuccess(response.message || 'Sector created successfully!');
                    setTimeout(function () {
                        window.location.href = response.redirectUrl || '/Sector/Dashboard';
                    }, 1500);
                } else {
                    window.sectorFormManager.showToastError(response.message || 'Failed to create sector.');
                }
            },
            error: function () {
                window.sectorFormManager.showToastError('An error occurred while creating the sector.');
            }
        });
    });
});
