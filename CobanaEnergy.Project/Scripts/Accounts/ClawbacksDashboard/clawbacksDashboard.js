$(document).ready(function () {
    var table = $('#clawbacksTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        dom: 'lfrtip'
    });

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [{
            extend: 'excelHtml5',
            text: 'Export',
            title: 'Clawbacks',
            filename: function () {
                const today = new Date();
                return 'Clawbacks_' + today.toISOString().split('T')[0];
            },
            exportOptions: {
                columns: ':visible'
            }
        }]
    });
    table.buttons(excelButton, false);

    $('#exportExcelBtn').on('click', function () {
        table.button('.buttons-excel').trigger();
    });

    $('#endDateFilter').prop('disabled', true);

    $('#startDateFilter').on('change', function () {
        handleDateValidation();
        loadContracts();
    });

    $('#endDateFilter').on('change', function () {
        if ($(this).val()) loadContracts();
    });

    $('#supplierFilter').on('change', function () {
        loadContracts();
    });

    function handleDateValidation() {
        const startDate = $('#startDateFilter').val();
        const endDate = $('#endDateFilter').val();

        if (startDate) {
            $('#endDateFilter').prop('disabled', false).attr('min', startDate);
            if (endDate && new Date(endDate) < new Date(startDate)) {
                $('#endDateFilter').val('');
                showToastWarning("End Date reset because it was earlier than Start Date.");
            }
        } else {
            $('#endDateFilter').prop('disabled', true).val('');
        }
    }

    function loadContracts() {
        const supplierId = $('#supplierFilter').val();
        const startDate = $('#startDateFilter').val();
        const endDate = $('#endDateFilter').val();

        $.ajax({
            url: '/ClawbacksDashboard/GetClawbackContracts',
            type: 'POST',
            data: {
                supplierId: supplierId,
                startDate: startDate,
                endDate: endDate,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                table.clear();
                if (res.success) {
                    res.Data.Contracts.forEach(contract => {
                        table.row.add([
                            contract.BusinessName,
                            contract.MPAN ?? '',
                            contract.MPRN ?? '',
                            contract.InputEAC,
                            contract.StartDate,
                            contract.CED,
                            contract.CEDCOT,
                            contract.PaymentStatus,
                            contract.CobanaFinalReconciliation
                        ]);
                    });
                } else {
                    showToastError(res.message || "No contracts found.");
                    table.clear().draw();
                }
                table.draw();
            },
            error: function () {
                table.clear().draw();
                showToastError("Error loading contracts.");
            }
        });
    }

    loadContracts();
});
