$(document).ready(function () {
    if (!$('#createDualForm').length) return;

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    $('.form-control, .form-select').on('input change', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    function populateDropdown(id, values) {
        const $select = $('#' + id);
        $select.empty().append(`<option value="">Select ${id}</option>`);
        values.forEach(v => $select.append(`<option value="${v}">${v}</option>`));
    }

    populateDropdown("department", DropdownOptions.department);
    populateDropdown("source", DropdownOptions.source);
    populateDropdown("electricSalesType", DropdownOptions.salesType);
    populateDropdown("gasSalesType", DropdownOptions.salesType);
    populateDropdown("electricSalesStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("gasSalesStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("electricCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("gasCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("electricPreSalesStatus", DropdownOptions.preSalesStatus);
    populateDropdown("gasPreSalesStatus", DropdownOptions.preSalesStatus);

    $.get('/Supplier/GetActiveSuppliersForDropdown', function (res) {
        const $electricSupplier = $('#electricSupplier');
        const $gasSupplier = $('#gasSupplier');
        $electricSupplier.empty().append('<option value="">Select Supplier</option>');
        $gasSupplier.empty().append('<option value="">Select Supplier</option>');

        if (res.success && res.Data.length > 0) {
            res.Data.forEach(s => {
                $electricSupplier.append(`<option value="${s.Id}">${s.Name}</option>`);
                $gasSupplier.append(`<option value="${s.Id}">${s.Name}</option>`);
            });
        }
    });

    $('#electricSupplier').change(function () {
        loadProducts($(this).val(), '#electricProduct');
    });

    $('#gasSupplier').change(function () {
        loadProducts($(this).val(), '#gasProduct');
    });

    function loadProducts(supplierId, productSelector) {
        const $product = $(productSelector);
        $product.prop('disabled', true).empty().append('<option>Loading...</option>');

        if (!supplierId) {
            $product.empty().append('<option value="">Select Product</option>').prop('disabled', false);
            return;
        }

        $.get(`/Supplier/GetProductsBySupplier?supplierId=${supplierId}`, function (res) {
            $product.empty().append('<option value="">Select Product</option>');

            if (res.success && res.Data.length > 0) {
                res.Data.forEach(p => {
                    $product.append(`<option value="${p.Id}">${p.ProductName}</option>`);
                });
            } else {
                $product.append('<option disabled>No products found</option>');
            }

            $product.prop('disabled', false);
        });
    }

    function restoreDefaults() {
        const today = new Date().toLocaleDateString('en-GB');
        $('#inputDate').val(today);
        const defaultProcessor = $('#emProcessor').data('default') || 'Presales Team';
        $('#emProcessor').val(defaultProcessor);
    }

    restoreDefaults();

    $('#createDualForm').on('submit', function (e) {
        e.preventDefault();

        let hasInvalid = false;
        $(this).find('.form-control, .form-select').each(function () {
            const $field = $(this);
            $field.removeClass('is-invalid');

            if (!this.checkValidity()) {
                $field.addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fill all required fields correctly.");
            return;
        }

        const model = {
            Department: $('#department').val(),
            Agent: $('#agent').val(),
            Source: $('#source').val(),
            Introducer: $('#introducer').val(),
            SubIntroducer: $('#subIntroducer').val(),
            BusinessName: $('#businessName').val(),
            CustomerName: $('#customerName').val(),
            BusinessDoorNumber: $('#businessDoorNumber').val(),
            BusinessHouseName: $('#businessHouseName').val(),
            BusinessStreet: $('#businessStreet').val(),
            BusinessTown: $('#businessTown').val(),
            BusinessCounty: $('#businessCounty').val(),
            PostCode: $('#postCode').val(),
            PhoneNumber1: $('#phoneNumber1').val(),
            PhoneNumber2: $('#phoneNumber2').val(),
            EmailAddress: $('#emailAddress').val(),
            InputDate: $('#inputDate').val(),
            EMProcessor: $('#emProcessor').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),

            MPAN: $('#mpan').val(),
            MPRN: $('#mprn').val(),

            ElectricCurrentSupplier: $('#electricCurrentSupplier').val(),
            // ElectricTopLine: $('#electricTopLine').val(),
            ElectricSalesType: $('#electricSalesType').val(),
            ElectricSalesTypeStatus: $('#electricSalesStatus').val(),
            ElectricDuration: $('#electricDuration').val(),
            ElectricUplift: $('#electricUplift').val(),
            ElectricInputEAC: $('#electricInputEAC').val(),
            ElectricStandingCharge: $('#electricStandingCharge').val(),
            ElectricDayRate: $('#dayRate').val(),
            ElectricNightRate: $('#nightRate').val(),
            ElectricEveWeekendRate: $('#eveWeekendRate').val(),
            ElectricOtherRate: $('#electricOtherRate').val(),
            ElectricSupplierId: $('#electricSupplier').val(),
            ElectricProductId: $('#electricProduct').val(),
            ElectricSupplierCommsType: $('#electricCommsType').val(),
            ElectricPreSalesStatus: $('#electricPreSalesStatus').val(),

            GasCurrentSupplier: $('#gasCurrentSupplier').val(),
            GasSalesType: $('#gasSalesType').val(),
            GasSalesTypeStatus: $('#gasSalesStatus').val(),
            GasDuration: $('#gasDuration').val(),
            GasUplift: $('#gasUplift').val(),
            GasInputAQ: $('#gasInputAQ').val(),
            GasStandingCharge: $('#gasStandingCharge').val(),
            GasUnitRate: $('#gasUnitRate').val(),
            GasSupplierId: $('#gasSupplier').val(),
            GasProductId: $('#gasProduct').val(),
            GasSupplierCommsType: $('#gasCommsType').val(),
            GasPreSalesStatus: $('#gasPreSalesStatus').val(),

            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            TopLine: $('#topLine').val(),
            ElectricInitialStartDate: $('#electricInitialStartDate').val(),
            GasInitialStartDate: $('#gasInitialStartDate').val(),
            GasInputEAC: $('#gasInputEAC').val(),
            GasOtherRate: $('#gasOtherRate').val(),
            ContractNotes: $('#contractNotes').val()
        };

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Submitting...');

        $.ajax({
            url: '/Dual/CreateDual',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Dual contract created successfully!");
                    $('#createDualForm')[0].reset();
                    restoreDefaults();
                    setTimeout(function () {
                        window.location.href = res.Data.redirectUrl;
                    }, 1000);
                } else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("An unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Create Dual Contract');
            }
        });
    });

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();
        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();
            $.get(`/Electric/CheckDuplicateMpan?mpan=${mpan}`, function (res) {
                $('#mpanLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMpanModal tbody').html(`
                        <tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>
                    `);
                    $('#duplicateDualMpanModal').modal('show');
                }
            }).fail(function () {
                $('#mpanLoader').hide();
                showToastError("Error checking MPAN.");
            });
        }
    });

    $('#mprn').on('input', function () {
        const mprn = $(this).val().trim();
        if (/^\d{6,10}$/.test(mprn)) {
            $('#mprnLoader').show();
            $.get(`/Gas/CheckDuplicateMprn?mprn=${mprn}`, function (res) {
                $('#mprnLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMprnModal tbody').html(`
                        <tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>
                    `);
                    $('#duplicateDualMprnModal').modal('show');
                }
            }).fail(function () {
                $('#mprnLoader').hide();
                showToastError("Error checking MPRN.");
            });
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();
        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();
            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();
                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalDual tbody');
                    tbody.empty();
                    res.Data.forEach(r => {
                        tbody.append(`
                            <tr><td>${r.Agent}</td><td>${r.BusinessName}</td><td>${r.CustomerName}</td><td>${r.InputDate}</td><td>${r.PreSalesStatus}</td><td>${r.Duration}</td><td>${r.SortCode}</td><td>${r.AccountNumber}</td></tr>
                        `);
                    });
                    $('#duplicateAccountModalDual').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

});
