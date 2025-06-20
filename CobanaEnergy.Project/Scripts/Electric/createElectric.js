$(document).ready(function () {

    if (!$('#createElectricForm').length) return;

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    function populateDropdown(id, values) {
        const $select = $('#' + id);
        $select.empty().append(`<option value="">Select ${id}</option>`);
        values.forEach(v => $select.append(`<option value="${v}">${v}</option>`));
    }

    populateDropdown("department", DropdownOptions.department);
    populateDropdown("source", DropdownOptions.source);
    populateDropdown("salesType", DropdownOptions.salesType);
    populateDropdown("salesTypeStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("supplierCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("preSalesStatus", DropdownOptions.preSalesStatus);

    $.get('/Supplier/GetActiveSuppliersForDropdown', function (res) {
        const $supplier = $('#supplierSelect');
        $supplier.empty().append('<option value="">Select Supplier</option>');

        if (res.success && res.Data.length > 0) {
            res.Data.forEach(s => {
                $supplier.append(`<option value="${s.Id}">${s.Name}</option>`);
            });
        } else {
            $supplier.append('<option disabled>No suppliers found</option>');
        }
    });


    $('#supplierSelect').change(function () {
        const supplierId = $(this).val();
        const $product = $('#productSelect');

        // Reset + show loading
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
    });

    restoreDefaultFields();

    $('#createElectricForm').on('submit', function (e) {
        e.preventDefault();

        let hasInvalid = false;

        $(this).find('.form-control, .form-select').each(function () {
            const $field = $(this);

            // Clear previous invalid class
            $field.removeClass('is-invalid');

            // Apply validation
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
            SalesType: $('#salesType').val(),
            SalesTypeStatus: $('#salesTypeStatus').val(),
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
            InitialStartDate: $('#initialStartDate').val(),
            Duration: $('#duration').val(),
            Uplift: $('#uplift').val(),
            InputEAC: $('#inputEAC').val(),
            DayRate: $('#dayRate').val(),
            NightRate: $('#nightRate').val(),
            EveWeekendRate: $('#eveWeekendRate').val(),
            OtherRate: $('#otherRate').val(),
            StandingCharge: $('#standingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            TopLine: $('#topLine').val(),
            MPAN: $('#mpan').val(),
            CurrentSupplier: $('#currentSupplier').val(),
            SupplierId: $('#supplierSelect').val(),
            ProductId: $('#productSelect').val(),
            SupplierCommsType: $('#supplierCommsType').val(),
            PreSalesStatus: $('#preSalesStatus').val(),
            EMProcessor: $('#emProcessor').val(),
            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            ContractNotes: $('#contractNotes').val(),
            //ContractSubmitter: $('#contractSubmitter').val() // Optional
        };

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Submitting...');

        $.ajax({
            url: '/Electric/CreateElectric',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Contract created successfully!");
                    $('#createElectricForm')[0].reset();
                    restoreDefaultFields();
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
                $btn.prop('disabled', false).text('Create Contract');
            }
        });
    });

    $(document).on('input change', '.form-control, .form-select', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();

        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();

            $.get(`/Electric/CheckDuplicateMpan?mpan=${mpan}`, function (res) {
                $('#mpanLoader').hide();

                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateMpanModal tbody').html(`
                    <tr>
                        <td>${d.Agent}</td>
                        <td>${d.BusinessName}</td>
                        <td>${d.CustomerName}</td>
                        <td>${d.InputDate}</td>
                        <td>${d.PreSalesStatus}</td>
                        <td>${d.Duration}</td>
                    </tr>
                `);

                    $('#duplicateMpanModal').modal('show'); // Make sure Bootstrap JS is loaded
                }
            }).fail(function () {
                $('#mpanLoader').hide();
                showToastError("Error checking MPAN.");
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
                    const tbody = $('#duplicateAccountModal tbody');
                    tbody.empty();

                    res.Data.forEach(r => {
                        tbody.append(`
                        <tr>
                            <td>${r.Agent}</td>
                            <td>${r.BusinessName}</td>
                            <td>${r.CustomerName}</td>
                            <td>${r.InputDate}</td>
                            <td>${r.PreSalesStatus}</td>
                            <td>${r.Duration}</td>
                            <td>${r.SortCode}</td>
                            <td>${r.AccountNumber}</td>
                        </tr>
                    `);
                    });

                    $('#duplicateAccountModal').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

    function restoreDefaultFields() {
        const today = new Date().toLocaleDateString('en-GB');
        $('#inputDate').val(today);

        const defaultProcessor = $('#emProcessor').data('default') || 'Presales Team';
        $('#emProcessor').val(defaultProcessor);
    }

});
