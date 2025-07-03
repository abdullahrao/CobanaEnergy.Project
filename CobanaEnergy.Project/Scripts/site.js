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

    $(document).ajaxError(function (e, xhr) {
        if (xhr.status === 401) {
            showToastWarning("Your session has expired. Redirecting to login...");
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 1500);
        }
    });

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

    window.validateUpliftAgainstSupplierLimitElectric = async function ($upliftInput, $supplierSelect, eid) {
        const upliftVal = parseFloat($upliftInput.val());
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !eid || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetSnapshotMaxUpliftElectric?eid=${eid}`,
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
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this contract snapshot.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit from snapshot.");
            return true;
        }
    };

    window.validateUpliftAgainstSupplierLimitGas = async function ($upliftInput, $supplierSelect, eid) {
        const upliftVal = parseFloat($upliftInput.val());
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !eid || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetSnapshotMaxUpliftGas?eid=${eid}`,
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
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this contract snapshot.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit from snapshot.");
            return true;
        }
    };

});
