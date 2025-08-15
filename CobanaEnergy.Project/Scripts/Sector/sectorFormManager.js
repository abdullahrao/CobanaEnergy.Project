/**
 * Sector Form Manager - Consolidated JavaScript for Create and Edit Sector pages
 * This file contains all shared functions to eliminate code duplication
 */

class SectorFormManager {
    constructor(options = {}) {
        this.isEditMode = options.isEditMode || false;
        this.currentSectorType = '';
        this.init();
    }

    init() {
        this.bindEvents();
        this.initializeForm();
    }

    bindEvents() {
        // Sector type change
        $('#sectorType').on('change', this.handleSectorTypeChange.bind(this));
        
        // Global event handlers for dynamically added elements
        this.bindGlobalEventHandlers();
    }

    initializeForm() {
        // If edit mode, load current sector type sections
        if (this.isEditMode) {
            const currentSectorType = $('#sectorType').val();
            if (currentSectorType) {
                this.loadDynamicSections(currentSectorType);
            }
        }
    }

    handleSectorTypeChange(e) {
        const selectedType = $(e.target).val();
        
        if (this.isEditMode) {
            // Edit mode: just load sections
            this.loadDynamicSections(selectedType);
        } else {
            // Create mode: show/hide sections and handle additional logic
            if (selectedType) {
                $('#commonFieldsSection').show();
                $('#bankDetailsSection').show();
                $('#companyTaxSection').show();
                
                // Handle additional fields for Brokerage and Introducers
                if (selectedType === 'Brokerage' || selectedType === 'Introducer') {
                    $('#additionalFieldsSection').show();
                    $('#department').val(selectedType);
                } else {
                    $('#additionalFieldsSection').hide();
                    $('#department').val('');
                    $('#ofgemId').val('');
                }
                
                this.loadDynamicSections(selectedType);
                $('#submitBtn').prop('disabled', false);
            } else {
                // Hide all sections if no type selected
                $('#commonFieldsSection').hide();
                $('#additionalFieldsSection').hide();
                $('#bankDetailsSection').hide();
                $('#companyTaxSection').hide();
                $('#dynamicSectionsContainer').empty();
                $('#submitBtn').prop('disabled', true);
            }
        }
    }

    loadDynamicSections(sectorType) {
        const container = $('#dynamicSectionsContainer');
        container.empty();
        
        switch(sectorType) {
            case 'Brokerage':
                this.loadBrokerageSections();
                break;
            case 'Closer':
                this.loadCloserSections();
                break;
            case 'Leads Generator':
                this.loadLeadGeneratorSections();
                break;
            case 'Referral Partner':
                this.loadReferralPartnerSections();
                break;
            case 'Introducer':
                this.loadIntroducerSections();
                break;
        }
    }

    // Load Brokerage specific sections
    loadBrokerageSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Brokerage Commission Section
        container.append(`
            <div class="form-section brokerage-commission-section">
                <h4>Brokerage Commission & Payment</h4>
                <div id="brokerageCommissionContainer">
                    ${this.generateBrokerageCommissionFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="brokerage">+ Add Commission</button>
            </div>
        `);
        
        // Brokerage Staff Section
        container.append(`
            <div class="form-section brokerage-staff-section">
                <h4>Brokerage Staff</h4>
                <div id="brokerageStaffContainer">
                    ${this.generateBrokerageStaffFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-staff" data-type="brokerage">+ Add Staff Member</button>
            </div>
        `);
        
        // Sub Brokerage Section
        container.append(`
            <div class="form-section sub-brokerage-section">
                <h4>Sub Brokerage</h4>
                <div id="subBrokerageContainer">
                    ${this.generateSubBrokerageFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subBrokerage">+ Add Sub Brokerage</button>
            </div>
        `);
    }

    // Load Closer specific sections
    loadCloserSections() {
        const container = $('#dynamicSectionsContainer');
        
        container.append(`
            <div class="form-section closer-commission-section">
                <h4>Closer Commission & Payment</h4>
                <div id="closerCommissionContainer">
                    ${this.generateCloserCommissionFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="closer">+ Add Commission</button>
            </div>
        `);
    }

    // Load Lead Generator specific sections
    loadLeadGeneratorSections() {
        const container = $('#dynamicSectionsContainer');
        
        container.append(`
            <div class="form-section lead-generator-commission-section">
                <h4>Lead Generator Commission & Payment</h4>
                <div id="leadGeneratorCommissionContainer">
                    ${this.generateLeadGeneratorCommissionFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="leadGenerator">+ Add Commission</button>
            </div>
        `);
    }

    // Load Referral Partner specific sections
    loadReferralPartnerSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Referral Partner Commission Section
        container.append(`
            <div class="form-section referral-partner-commission-section">
                <h4>Referral Partner Commission & Payment</h4>
                <div id="referralPartnerCommissionContainer">
                    ${this.generateReferralPartnerCommissionFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="referralPartner">+ Add Commission</button>
            </div>
        `);
        
        // Sub Referral Section
        container.append(`
            <div class="form-section sub-referral-section">
                <h4>Sub Referral</h4>
                <div id="subReferralContainer">
                    ${this.generateSubReferralFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subReferral">+ Add Sub Referral</button>
            </div>
        `);
    }

    // Load Introducer specific sections
    loadIntroducerSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Introducer Commission Section
        container.append(`
            <div class="form-section introducer-commission-section">
                <h4>Introducer Commission & Payment</h4>
                <div id="introducerCommissionContainer">
                    ${this.generateIntroducerCommissionFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="introducer">+ Add Commission</button>
            </div>
        `);
        
        // Sub Introducer Section
        container.append(`
            <div class="form-section sub-introducer-section">
                <h4>Sub Introducer</h4>
                <div id="subIntroducerContainer">
                    ${this.generateSubIntroducerFields(0)}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subIntroducer">+ Add Sub Introducer</button>
            </div>
        `);
    }

    // Generate field functions
    generateBrokerageCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="BrokerageCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="BrokerageCommissions[${index}].StartDate" class="form-control" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="BrokerageCommissions[${index}].EndDate" class="form-control" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateBrokerageStaffFields(index) {
        return `
            <div class="staff-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].StaffName" class="form-control" placeholder="Staff Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].Role" class="form-control" placeholder="Role" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="BrokerageStaff[${index}].Email" class="form-control" placeholder="Email Address" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].Phone" class="form-control" placeholder="Phone Number" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-staff"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubBrokerageFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Brokerage ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].SubBrokerageName" class="form-control" placeholder="Sub Brokerage Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].ContactPerson" class="form-control" placeholder="Contact Person" />
                        </div>
                    </div>
                </div>
                
                <!-- Sub Brokerage Commission & Payment -->
                <div class="sub-commission-section">
                    <h6>Commission & Payment</h6>
                    <div class="sub-commission-container">
                        ${this.generateSubBrokerageCommissionFields(index, 0)}
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        <i class="fas fa-plus me-1"></i>Add Commission
                    </button>
                </div>

                <!-- Sub Brokerage Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Brokerage Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                </div>
                
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubBrokerageCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateCloserCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="CloserCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="CloserCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateLeadGeneratorCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="LeadGeneratorCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="LeadGeneratorCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateReferralPartnerCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="ReferralPartnerCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="ReferralPartnerCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubReferralFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Referral ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${index}].SubReferralName" class="form-control" placeholder="Sub Referral Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${index}].ContactPerson" class="form-control" placeholder="Contact Person" />
                        </div>
                    </div>
                </div>
                
                <!-- Sub Referral Commission & Payment -->
                <div class="sub-commission-section">
                    <h6>Commission & Payment</h6>
                    <div class="sub-commission-container">
                        ${this.generateSubReferralCommissionFields(index, 0)}
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        <i class="fas fa-plus me-1"></i>Add Commission
                    </button>
                </div>

                <!-- Sub Referral Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Referral Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                </div>
                
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubReferralCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateIntroducerCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="IntroducerCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="IntroducerCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubIntroducerFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Introducer ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].SubIntroducerName" class="form-control" placeholder="Sub Introducer Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].ContactPerson" class="form-control" placeholder="Contact Person" />
                        </div>
                    </div>
                </div>
                
                <!-- Sub Introducer Commission & Payment -->
                <div class="sub-commission-section">
                    <h6>Commission & Payment</h6>
                    <div class="sub-commission-container">
                        ${this.generateSubIntroducerCommissionFields(index, 0)}
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        <i class="fas fa-plus me-1"></i>Add Commission
                    </button>
                </div>

                <!-- Sub Introducer Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Introducer Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                </div>
                
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubIntroducerCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    // Bind global event handlers for dynamically added elements
    bindGlobalEventHandlers() {
        const self = this; // Store reference to the class instance
        
        // Add Commission buttons
        $(document).on('click', '.add-commission', function () {
            const type = $(this).data('type');
            const container = self.getCommissionContainer(type);
            const newIndex = container.children().length;
            
            switch (type) {
                case 'brokerage':
                    container.append(self.generateBrokerageCommissionFields(newIndex));
                    break;
                case 'closer':
                    container.append(self.generateCloserCommissionFields(newIndex));
                    break;
                case 'leadGenerator':
                    container.append(self.generateLeadGeneratorCommissionFields(newIndex));
                    break;
                case 'referralPartner':
                    container.append(self.generateReferralPartnerCommissionFields(newIndex));
                    break;
                case 'introducer':
                    container.append(self.generateIntroducerCommissionFields(newIndex));
                    break;
            }
        });

        // Add Staff buttons
        $(document).on('click', '.add-staff', function () {
            const type = $(this).data('type');
            const container = self.getStaffContainer(type);
            const newIndex = container.children().length;
            
            if (type === 'brokerage') {
                container.append(self.generateBrokerageStaffFields(newIndex));
            }
        });

        // Add Subsection buttons
        $(document).on('click', '.add-subsection', function () {
            const type = $(this).data('type');
            const container = self.getSubsectionContainer(type);
            const newIndex = container.children().length;
            
            switch (type) {
                case 'subBrokerage':
                    container.append(self.generateSubBrokerageFields(newIndex));
                    break;
                case 'subReferral':
                    container.append(self.generateSubReferralFields(newIndex));
                    break;
                case 'subIntroducer':
                    container.append(self.generateSubIntroducerFields(newIndex));
                    break;
            }
        });

        // Add Sub Commission buttons
        $(document).on('click', '.add-sub-commission', function () {
            const subsectionIndex = $(this).data('subsection-index');
            const container = $(this).siblings('.sub-commission-container');
            const newIndex = container.children().length;
            
            // Determine the type based on the parent section
            const parentSection = $(this).closest('.subsection-item');
            const sectionType = parentSection.find('h5').text().toLowerCase();
            
            if (sectionType.includes('brokerage')) {
                container.append(self.generateSubBrokerageCommissionFields(subsectionIndex, newIndex));
            } else if (sectionType.includes('referral')) {
                container.append(self.generateSubReferralCommissionFields(subsectionIndex, newIndex));
            } else if (sectionType.includes('introducer')) {
                container.append(self.generateSubIntroducerCommissionFields(subsectionIndex, newIndex));
            }
        });

        // Remove buttons
        $(document).on('click', '.remove-commission', function () {
            $(this).closest('.commission-item').remove();
        });

        $(document).on('click', '.remove-staff', function () {
            $(this).closest('.staff-item').remove();
        });

        $(document).on('click', '.remove-subsection', function () {
            $(this).closest('.subsection-item').remove();
        });

        $(document).on('click', '.remove-sub-commission', function () {
            $(this).closest('.sub-commission-item').remove();
        });
    }

    // Helper functions to get containers
    getCommissionContainer(type) {
        switch (type) {
            case 'brokerage': return $('#brokerageCommissionContainer');
            case 'closer': return $('#closerCommissionContainer');
            case 'leadGenerator': return $('#leadGeneratorCommissionContainer');
            case 'referralPartner': return $('#referralPartnerCommissionContainer');
            case 'introducer': return $('#introducerCommissionContainer');
            default: return null;
        }
    }

    getStaffContainer(type) {
        switch (type) {
            case 'brokerage': return $('#brokerageStaffContainer');
            default: return null;
        }
    }

    getSubsectionContainer(type) {
        switch (type) {
            case 'subBrokerage': return $('#subBrokerageContainer');
            case 'subReferral': return $('#subReferralContainer');
            case 'subIntroducer': return $('#subIntroducerContainer');
            default: return null;
        }
    }

    // Form validation
    validateForm() {
        let isValid = true;
        
        // Check required fields
        if (!$('#sectorType').val()) {
            this.showToastError('Please select a sector type.');
            isValid = false;
        }
        
        if (!$('input[name="Name"]').val().trim()) {
            this.showToastError('Please enter a sector name.');
            isValid = false;
        }
        
        return isValid;
    }

    // Toast notification functions
    showToastSuccess(message) {
        // You can implement your own toast notification system here
        alert('✓ ' + message);
    }

    showToastError(message) {
        // You can implement your own toast notification system here
        alert('✗ ' + message);
    }
}

// Global instance for backward compatibility
window.sectorFormManager = null;

// Initialize function for Create page
window.initializeCreateSector = function() {
    window.sectorFormManager = new SectorFormManager({ isEditMode: false });
};

// Initialize function for Edit page
window.initializeEditSector = function() {
    window.sectorFormManager = new SectorFormManager({ isEditMode: true });
};
