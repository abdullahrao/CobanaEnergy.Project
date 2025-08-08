$(document).ready(function () {
    var table = $('#userTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        dom: 'lfrtip',
        columnDefs: [
            { orderable: false, targets: 0 }
        ]
    });

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [
            {
                extend: 'excelHtml5',
                text: 'Export',
                title: 'UserName',
                filename: function () {
                    const today = new Date();
                    return 'UserList_' + today.toISOString().split('T')[0];
                },
                exportOptions: {
                    columns: ':visible:not(:first-child)'
                }
            }
        ]
    });

    table.buttons(excelButton, false);

    $('#exportExcelBtn').on('click', function () {
        table.button('.buttons-excel').trigger();
    });


    function loadUsers() {

        $.ajax({
            url: '/Account/GetUserList',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                table.clear();

                if (res.success) {
                    const userlist = res.Data || [];

                    userlist.forEach(contract => {

                        // Roles as badges
                        const roles = Array.isArray(contract.Roles) ? contract.Roles : [];
                        const roleLabels = roles
                            .map(role => `<span class="badge bg-primary me-1">${role}</span>`)
                            .join('');

                        // Status badge
                        const statusBadgeClass = contract.Status === "Active" ? "bg-primary" : "bg-danger";
                        const statusLabel = `<span class="badge ${statusBadgeClass} me-1">${contract.Status}</span>`;

                        // Online status dot
                        const isOnline = contract.OnlineStatus === "Online";
                        const onlineDot = `
                            <span class="d-inline-flex align-items-center">
                                <span class="dot ${isOnline ? 'bg-success' : 'bg-secondary'} rounded-circle me-1" 
                                      style="width: 10px; height: 10px; display: inline-block;"></span>
                            </span>`;

                        // Add row
                        const rowNode = table.row.add([
                            `<input type="checkbox" name="SelectedUser" value="${contract.UserId}" />`,
                            contract.UserName,
                            roleLabels,
                            statusLabel,
                            onlineDot
                        ]).draw(false).node();


                        $(rowNode).attr('data-userid', contract.UserId);
                    });

                    
                } else {
                    showToastError(res.message || "No Users found.");
                }
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                table.draw();
            },
            error: function () {
                table.clear().draw();
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                showToastError("Error loading user listing.");
            }
        });
    }


    $('#saveBtn').on('click', function () {
        const selectedUsers = $('input[name="SelectedUser"]:checked').map(function () {
            const row = $(this).closest('tr');
            return {
                UserId: $(this).val(),
            };
        }).get();

        if (selectedUsers.length === 0) {
            showToastWarning("Please select at least one user to update.");
            return;
        }

        $.ajax({
            url: '/Account/UpdateUsers',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                users: selectedUsers,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            }),
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message || "users updated successfully.");
                    loadUsers();
                } else {
                    showToastError(res.message || "Failed to update users.");
                }
            },
            error: function () {
                showToastError("Error updating users.");
            }
        });
    });

    loadUsers();



    $('#checkAll').on('change', function () {
        const checked = this.checked;
        $('input[name="SelectedUser"]').prop('checked', checked).trigger('change');
    });


    $(document).on('change', 'input[name="SelectedUser"]', function () {
        const total = $('input[name="SelectedUser"]').length;
        const checked = $('input[name="SelectedUser"]:checked').length;
        $('#saveBtn').prop('disabled', checked === 0);
        $('#checkAll').prop('checked', total === checked);
    });

});