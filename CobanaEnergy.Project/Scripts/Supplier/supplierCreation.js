$(document).ready(function () {

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }

    // Add initial rows
    addContact();
    addProduct();

    $('.add-contact').click(addContact);
    $('.add-product').click(addProduct);

    $('#supplierForm').submit(function (e) {
        e.preventDefault();

        const $saveButton = $(this).find('button[type="submit"]');
        const originalText = $saveButton.html();

        // Disable button + show spinner
        $saveButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        const contacts = [];
        $('#contactContainer .contact-row').each(function () {
            contacts.push({
                ContactName: $(this).find('.contact-name').val(),
                Role: $(this).find('.contact-role').val(),
                PhoneNumber: $(this).find('.contact-phone').val(),
                Email: $(this).find('.contact-email').val(),
                Notes: $(this).find('.contact-notes').val()
            });
        });

        const products = [];
        $('#productContainer .product-row').each(function () {
            products.push({
                ProductName: $(this).find('.product-name').val(),
                StartDate: $(this).find('.product-start').val(),
                EndDate: $(this).find('.product-end').val(),
                Commission: $(this).find('.product-commission').val()
            });
        });

        const data = {
            Name: $('#supplierName').val(),
            Link: $('#supplierLink').val(),
            Contacts: contacts,
            Products: products
        };

        const formToken = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Supplier/SupplierCreation',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': formToken
            },
            success: function (response) {
                if (response.success) {
                    showToastSuccess("Supplier created successfully!");
                    $('#supplierForm')[0].reset();
                    $('#contactContainer').empty();
                    $('#productContainer').empty();
                    addContact();
                    addProduct();
                    setTimeout(function () {
                        window.location.href = response.Data.redirectUrl;
                    }, 1000);
                } else {
                    showToastError(response.message);
                }
            },
            error: function () {
                showToastError("An unexpected error occurred.");
            },
            complete: function () {
                // Re-enable button and restore text
                $saveButton.prop('disabled', false).html(originalText);
            }
        });
    });

    function addContact() {
        const contact = `
        <div class="contact-row row gx-2 mb-3">
            <div class="col-md-2"><input type="text" class="form-control contact-name" placeholder="Contact Name" required></div>
            <div class="col-md-2"><input type="text" class="form-control contact-role" placeholder="Role"></div>
            <div class="col-md-2"><input type="text" class="form-control contact-phone" placeholder="Phone" maxlength="11" inputmode="numeric" pattern="^[0-9]{11}$" title="Enter up to 11 digits"></div>
            <div class="col-md-2"><input type="email" class="form-control contact-email" placeholder="Email"></div>
            <div class="col-md-3">
            <textarea class="form-control contact-notes" placeholder="Notes" rows="1" style="resize: vertical;"></textarea>
            </div>
            <div class="col-md-1"><button type="button" class="btn btn-danger btn-sm remove-row">X</button></div>
        </div>`;
        $('#contactContainer').append(contact);
    }

    function addProduct() {
        const product = `
        <div class="product-row row gx-2 mb-3">
            <div class="col-md-3"><input type="text" class="form-control product-name" placeholder="Product Name" required></div>
            <div class="col-md-2"><input type="date" class="form-control product-start" placeholder="Start Date"  required></div>
            <div class="col-md-2"><input type="date" class="form-control product-end" placeholder="End Date" disabled required></div>
            <div class="col-md-3"><input type="text" class="form-control product-commission" placeholder="Commission"  required></div>
            <div class="col-md-1"><button type="button" class="btn btn-danger btn-sm remove-row">X</button></div>
        </div>`;
        $('#productContainer').append(product);
    }

    $(document).on('click', '.remove-row', function () {
        $(this).closest('.row').slideUp(200, function () {
            $(this).remove();
        });
    });

    $(document).on('change', '.product-start', function () {
        const row = $(this).closest('.product-row');
        const startDate = row.find('.product-start').val();
        const endDateInput = row.find('.product-end');

        if (startDate) {
            endDateInput.prop('disabled', false);  // Enable it
            endDateInput.attr('min', startDate);   // Set min to start date
        } else {
            endDateInput.prop('disabled', true);   // Disable if no start
            endDateInput.val('');                  // Clear value
            endDateInput.removeAttr('min');        // Reset min
        }
    });

    $(document).on('input', '.contact-phone', function () {
        this.value = this.value.replace(/[^0-9]/g, '').slice(0, 11);
    });


});
