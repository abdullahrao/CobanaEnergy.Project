$(document).ready(function () {
    const supplierId = window.location.pathname.split("/").pop();
    const $form = $('#editSupplierForm');

    function loadSupplier() {
        $.ajax({
            url: '/Supplier/GetSupplierForEdit',
            type: 'GET',
            data: { id: supplierId },
            success: function (res) {
                if (!res.success) {
                    showToastError(res.message);
                    // return;

                    setTimeout(function () {
                        window.location.href = res.Data.redirectUrl;
                    }, 1000);

                    return;
                }

                const data = res.Data;
                $('#supplierId').val(data.Id);
                $('#supplierName').val(data.Name);
                $('#supplierLink').val(data.Link);
                $('#supplierStatus').prop('checked', data.Status);
                renderContacts(data.Contacts);
                renderProducts(data.Products);
                $('#productContainer .product-start').each(function () {
                    $(this).trigger('change');
                });

                $('#editSupplierLoading').hide();
                $('#editSupplierContainer').removeClass('d-none');
            },
            error: function () {
                showToastError("Failed to load supplier.");
            }
        });
    }

    function renderContacts(contacts) {
        const $container = $('#contactContainer');
        $container.empty();
        contacts.forEach(contact => {
            $container.append(getContactRow(contact));
        });
    }

    function renderProducts(products) {
        const $container = $('#productContainer');
        $container.empty();
        products.forEach(product => {
            $container.append(getProductRow(product));
        });
    }

    function getContactRow(contact = {}) {
        return `
    <div class="row mb-2 contact-row gx-2">
    <input type="hidden" class="contact-id" value="${contact.Id || 0}">
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Name</label>
            <input type="text" class="form-control contact-name" placeholder="Name" value="${contact.ContactName || ''}">
        </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Role</label>
            <input type="text" class="form-control contact-role" placeholder="Role" value="${contact.Role || ''}">
        </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Phone</label>
            <input type="text" class="form-control contact-phone" placeholder="Phone" maxlength="11" inputmode="numeric" pattern="^[0-9]{11}$" title="Enter up to 11 digits" value="${contact.PhoneNumber || ''}">
        </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Email</label>
            <input type="email" class="form-control contact-email" placeholder="Email" value="${contact.Email || ''}">
        </div>
        <div class="col-md-3">
            <label class="d-md-none fw-bold">Notes</label>
            <textarea class="form-control contact-notes" rows="1" placeholder="Notes" style="resize: vertical;">${contact.Notes || ''}</textarea>
        </div>
        <div class="col-md-1 d-flex align-items-center">
            <button type="button" class="btn btn-danger btn-sm remove-contact">X</button>
        </div>
    </div>`;
    }

    function getProductRow(product = {}) {
        return `
    <div class="row mb-2 product-row gx-2">
     <input type="hidden" class="product-id" value="${product.Id || 0}">
        <div class="col-md-3">
            <label class="d-md-none fw-bold">Product Name</label>
            <input type="text" class="form-control product-name" placeholder="Product" value="${product.ProductName || ''}" required>
        </div>
         <div class="col-md-2">
        <label class="d-md-none fw-bold">Comms Type</label>
        <select class="form-control product-comms-type" required>
            <option value="">Select Comms Type</option>
            ${DropdownOptions.supplierCommsType.map(type =>
            `<option value="${type}" ${product.SupplierCommsType === type ? 'selected' : ''}>${type}</option>`
        ).join('')}
        </select>
    </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Start Date</label>
            <input type="date" class="form-control product-start" value="${product.StartDate || ''}" required>
        </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">End Date</label>
            <input type="date" class="form-control product-end" value="${product.EndDate || ''}" required>
        </div>
        <div class="col-md-2">
            <label class="d-md-none fw-bold">Commission</label>
            <input type="text" class="form-control product-commission" placeholder="Commission" value="${product.Commission || ''}" required>
        </div>
        <div class="col-md-1 d-flex align-items-center">
            <button type="button" class="btn btn-danger btn-sm remove-product">X</button>
        </div>
    </div>`;
    }

    $('.add-contact').click(() => $('#contactContainer').append(getContactRow()));
    $('.add-product').click(() => $('#productContainer').append(getProductRow()));

    // Remove contact/product
    $(document).on('click', '.remove-contact', function () {
        $(this).closest('.contact-row').remove();
    });

    $(document).on('click', '.remove-product', function () {
        const $row = $(this).closest('.product-row');
        const productId = parseInt($row.find('.product-id').val()) || 0;

        //if (productId > 0) {
        //    const confirmed = confirm("This product is already used in the contract.\nIf you remove it and submit, it may be deleted permanently (unless it's in use). Are you sure?");
        //    if (!confirmed) return;
        //}

        $row.remove();
        /*        $(this).closest('.product-row').remove();*/
    });

    $form.on('submit', function (e) {
        e.preventDefault();

        const model = {
            Id: $('#supplierId').val(),
            Name: $('#supplierName').val(),
            Link: $('#supplierLink').val(),
            Status: $('#supplierStatus').is(':checked'),
            Contacts: [],
            Products: []
        };

        $('#contactContainer .contact-row').each(function () {
            model.Contacts.push({
                Id: parseInt($(this).find('.contact-id').val()) || 0,
                ContactName: $(this).find('.contact-name').val(),
                Role: $(this).find('.contact-role').val(),
                PhoneNumber: $(this).find('.contact-phone').val(),
                Email: $(this).find('.contact-email').val(),
                Notes: $(this).find('.contact-notes').val()
            });
        });

        $('#productContainer .product-row').each(function () {
            model.Products.push({
                Id: parseInt($(this).find('.product-id').val()) || 0,
                ProductName: $(this).find('.product-name').val(),
                SupplierCommsType: $(this).find('.product-comms-type').val(),
                StartDate: $(this).find('.product-start').val(),
                EndDate: $(this).find('.product-end').val(),
                Commission: $(this).find('.product-commission').val()
            });
        });

        const $submitBtn = $form.find('button[type="submit"]');
        $submitBtn.prop('disabled', true).text('Updating...');

        $.ajax({
            url: '/Supplier/EditSupplier',
            type: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message);

                    setTimeout(function () {
                        window.location.href = res.Data.redirectUrl;
                    }, 1000);
                } else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("An error occurred while updating supplier.");
            },
            complete: function () {
                $submitBtn.prop('disabled', false).text('Update Supplier');
            }
        });
    });

    loadSupplier();

    $(document).on('change', '.product-start', function () {
        const $row = $(this).closest('.product-row');
        const startDate = $(this).val();
        const $endDate = $row.find('.product-end');

        if (startDate) {
            $endDate.prop('disabled', false);
            $endDate.attr('min', startDate);
        } else {
            $endDate.prop('disabled', true).val('').removeAttr('min');
        }
    });

    $(document).on('input', '.contact-phone', function () {
        this.value = this.value.replace(/[^0-9]/g, '').slice(0, 11);
    });

});
