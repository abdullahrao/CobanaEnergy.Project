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

    //Invoice Supplier Dashboard Popup
    $(document).on('click', '#openInvoiceSupplierDashboard', function (e) {
        e.preventDefault();

        $.ajax({
            url: '/InvoiceSupplierDashboard/InvoiceSupplierPopup',
            type: 'GET',
            success: function (html) {
                $('body').append(html);
                $('#invoiceUploadModal').modal('show');

                $('#invoiceUploadModal').on('hidden.bs.modal', function () {
                    $(this).remove();
                });
            },
            error: function () {
                showToastError("Failed to load Invoice Supplier popup.");
            }
        });
    });

    //nav
    $(document).ready(function () {
        $(document).on('click', '.dropdown-submenu > a', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var $parentDropdown = $(this).closest('.dropdown-menu');
            var $submenu = $(this).next('.dropdown-menu');

            $parentDropdown.find('.dropdown-submenu .dropdown-menu').not($submenu).removeClass('show').hide();
            $submenu.toggleClass('show').toggle();
        });

        $('.dropdown-submenu').on('mouseenter', function () {
            $(this).children('.dropdown-menu').addClass('show').stop(true, true).fadeIn(150);
        }).on('mouseleave', function () {
            $(this).children('.dropdown-menu').removeClass('show').stop(true, true).fadeOut(150);
        });
        $(document).on('click', '.dropdown-submenu .dropdown-item', function (e) {
            e.stopPropagation();
        });
    });


});
