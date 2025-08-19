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
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageCommissions[${index}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="text" name="BrokerageStaff[${index}].BrokerageStaffName" class="form-control" placeholder="Staff Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-check-label">
                                <input type="checkbox" name="BrokerageStaff[${index}].Active" class="form-check-input" checked />
                                Active
                            </label>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="BrokerageStaff[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="BrokerageStaff[${index}].EndDate" class="form-control" placeholder="End Date" />
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
                            <input type="text" name="BrokerageStaff[${index}].Landline" class="form-control" placeholder="Landline" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].Mobile" class="form-control" placeholder="Mobile" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <textarea name="BrokerageStaff[${index}].Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
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
                            <input type="text" name="SubBrokerages[${index}].OfgemID" class="form-control" placeholder="Ofgem ID" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-check-label">
                                <input type="checkbox" name="SubBrokerages[${index}].Active" class="form-check-input" checked />
                                Active
                            </label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${index}].EndDate" class="form-control" placeholder="End Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubBrokerages[${index}].Email" class="form-control" placeholder="Email" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].Landline" class="form-control" placeholder="Landline" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].Mobile" class="form-control" placeholder="Mobile" />
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
                                <input type="text" name="SubBrokerages[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
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
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubBrokerages[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
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
                            <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageCommissionPercent" class="form-control" placeholder="Sub Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageCommissionPercent" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageStartDate" class="form-control" placeholder="Sub Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageEndDate" class="form-control" placeholder="Sub Brokerage End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageStartDate" class="form-control" placeholder="Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageEndDate" class="form-control" placeholder="Brokerage End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="CloserCommissions[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="CloserCommissions[${index}].EndDate" class="form-control" placeholder="End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="CloserCommissions[${index}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="number" name="LeadGeneratorCommissions[${index}].LeadGeneratorCommission" class="form-control" placeholder="Lead Generator Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="LeadGeneratorCommissions[${index}].CloserCommission" class="form-control" placeholder="Closer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorStartDate" class="form-control" placeholder="Lead Generator Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorEndDate" class="form-control" placeholder="Lead Generator End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="LeadGeneratorCommissions[${index}].CloserStartDate" class="form-control" placeholder="Closer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="LeadGeneratorCommissions[${index}].CloserEndDate" class="form-control" placeholder="Closer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="LeadGeneratorCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="LeadGeneratorCommissions[${index}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="number" name="ReferralPartnerCommissions[${index}].ReferralPartnerCommission" class="form-control" placeholder="Referral Partner Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="ReferralPartnerCommissions[${index}].BrokerageCommission" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerStartDate" class="form-control" placeholder="Referral Partner Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerEndDate" class="form-control" placeholder="Referral Partner End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageStartDate" class="form-control" placeholder="Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageEndDate" class="form-control" placeholder="Brokerage End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="ReferralPartnerCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="ReferralPartnerCommissions[${index}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="text" name="SubReferrals[${index}].SubReferralPartnerName" class="form-control" placeholder="Sub Referral Partner Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-check-label">
                                <input type="checkbox" name="SubReferrals[${index}].Active" class="form-check-input" checked />
                                Active
                            </label>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${index}].EndDate" class="form-control" placeholder="End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubReferrals[${index}].SubReferralPartnerEmail" class="form-control" placeholder="Email" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${index}].SubReferralPartnerLandline" class="form-control" placeholder="Landline" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${index}].SubReferralPartnerMobile" class="form-control" placeholder="Mobile" />
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
                                <input type="text" name="SubReferrals[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
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
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubReferrals[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
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
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommission" class="form-control" placeholder="Sub Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommission" class="form-control" placeholder="Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerStartDate" class="form-control" placeholder="Sub Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerEndDate" class="form-control" placeholder="Sub Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerStartDate" class="form-control" placeholder="Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerEndDate" class="form-control" placeholder="Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="number" name="IntroducerCommissions[${index}].CommissionPercent" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="IntroducerCommissions[${index}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="IntroducerCommissions[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="IntroducerCommissions[${index}].EndDate" class="form-control" placeholder="End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="IntroducerCommissions[${index}].CommissionType" class="form-control" placeholder="Commission Type" />
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
                            <input type="text" name="SubIntroducers[${index}].OfgemID" class="form-control" placeholder="Ofgem ID" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-check-label">
                                <input type="checkbox" name="SubIntroducers[${index}].Active" class="form-check-input" checked />
                                Active
                            </label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${index}].EndDate" class="form-control" placeholder="End Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubIntroducers[${index}].SubIntroducerEmail" class="form-control" placeholder="Email" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].SubIntroducerLandline" class="form-control" placeholder="Landline" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].SubIntroducerMobile" class="form-control" placeholder="Mobile" />
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
                                <input type="text" name="SubIntroducers[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
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
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubIntroducers[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
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
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommission" class="form-control" placeholder="Sub Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommission" class="form-control" placeholder="Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommissionStartDate" class="form-control" placeholder="Sub Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommissionEndDate" class="form-control" placeholder="Sub Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommissionStartDate" class="form-control" placeholder="Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommissionEndDate" class="form-control" placeholder="Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-control" placeholder="Payment Terms" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-control" placeholder="Commission Type" />
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

window.initializeEditSector = function() {
    window.sectorFormManager = new SectorFormManager({ isEditMode: true });
};

