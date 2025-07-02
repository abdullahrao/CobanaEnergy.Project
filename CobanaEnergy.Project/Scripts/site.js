// site.js
// Automatically include anti-forgery token in all Ajax requests
$(document).ready(async function () {
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({
            headers: {
                'RequestVerificationToken': token
            }
        });
    }

    window.validateUpliftAgainstSupplierLimit = async function ($upliftInput, $supplierSelect, fuelType) {
        const upliftVal = parseFloat($upliftInput.val());
        const supplierId = $supplierSelect.val();
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !supplierId || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetActiveUpliftForSupplier?supplierId=${supplierId}&fuelType=${fuelType}`,
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            if (res.success && res.Data != null) {
                const maxUplift = parseFloat(res.Data);

                const upliftFixed = Number(upliftVal.toFixed(6));
                const maxFixed = Number(maxUplift.toFixed(6));

                if (upliftFixed > maxFixed) {
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this supplier.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit.");
            return true;
        }
    };

});
