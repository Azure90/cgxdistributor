/*
** pay an invoice online 
 * 
 * */


var Checkout = {
    loadWaiting: false,
    failureUrl: false,

    init: function (failureUrl) {
        this.loadWaiting = false;
        this.failureUrl = failureUrl;

        Accordion.disallowAccessToNextSections = true;
    },

    ajaxFailure: function () {
        location.href = Checkout.failureUrl;
    },

    _disableEnableAll: function (element, isDisabled) {
        var descendants = element.find('*');
        $(descendants).each(function () {
            if (isDisabled) {
                $(this).prop("disabled", true);
            } else {
                $(this).prop("disabled", false);
            }
        });

        if (isDisabled) {
            element.prop("disabled", true);
        } else {
            $(this).prop("disabled", false);
        }
    },

    setLoadWaiting: function (step, keepDisabled) {
        var container;
        if (step) {
            if (this.loadWaiting) {
                this.setLoadWaiting(false);
            }
            container = $('#' + step + '-buttons-container');
            container.addClass('disabled');
            container.css('opacity', '.5');
            this._disableEnableAll(container, true);
            $('#' + step + '-please-wait').show();
        } else {
            if (this.loadWaiting) {
                container = $('#' + this.loadWaiting + '-buttons-container');
                var isDisabled = keepDisabled ? true : false;
                if (!isDisabled) {
                    container.removeClass('disabled');
                    container.css('opacity', '1');
                }
                this._disableEnableAll(container, isDisabled);
                $('#' + this.loadWaiting + '-please-wait').hide();
            }
        }
        this.loadWaiting = step;
    },

    gotoSection: function (section) {
        section = $('#opc-' + section);
        section.addClass('allow');
        Accordion.openSection(section);
    },

    back: function () {
        if (this.loadWaiting) return;
        Accordion.openPrevSection(true, true);
    },

    SetSummaryPage: function () {
        setTransactionInfo();
        this.gotoSection('confirm_order');
    },

    setStepResponse: function (response) {
        if (response.update_section) {
            $('#checkout-' + response.update_section.name + '-load').html(response.update_section.html);
        }
        if (response.allow_sections) {
            response.allow_sections.each(function (e) {
                $('#opc-' + e).addClass('allow');
            });
        }

        if (response.goto_section) {
            Checkout.gotoSection(response.goto_section);
            return true;
        }
        if (response.redirect) {
            location.href = response.redirect;
            return true;
        }
        return false;
    }
};

var PaymentMethod = {
    form: false,
    saveUrl: false,
    localized_data: false,

    init: function (form, saveUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
    },


    validate: function () {
        var methods = document.getElementsByName('paymentmethod');
        if (methods.length === 0) {
            alert(this.localized_data.NotAvailableMethodsError);
            return false;
        }

        for (var i = 0; i < methods.length; i++) {
            if (methods[i].checked) {
                return true;
            }
        }
        alert(this.localized_data.SpecifyMethodError);
        return false;
    },

    save: function () {
        if (Checkout.loadWaiting !== false) return;

        if (this.validate()) {
            Checkout.setLoadWaiting('payment-method');
            $.ajax({
                cache: false,
                url: this.saveUrl,
                data: $(this.form).serialize(),
                type: "POST",
                success: this.nextStep,
                complete: this.resetLoadWaiting,
                error: Checkout.ajaxFailure
            });
        }
    },

    resetLoadWaiting: function () {
        Checkout.setLoadWaiting(false);
    },

    nextStep: function (response) {
        if (response.error) {
            if (typeof response.message === 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }

        Checkout.setStepResponse(response);
    }
};




var PaymentInfo = {
    form: false,
    saveUrl: false,

  init: function (form, saveUrl) {
    
        this.form = form;
        this.saveUrl = saveUrl;
    },

    save: function () {
        if (Checkout.loadWaiting !== false) return;
        Checkout.setLoadWaiting('payment-info');
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            type: "POST",
            success: this.nextStep,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure
        });
    },

    resetLoadWaiting: function () {
        Checkout.setLoadWaiting(false);
    },

    nextStep: function (response) {
        if (response.error) {
            if (typeof response.message === 'string') {
                alert(response.message);
            } else {
                alert(response.message.join("\n"));
            }

            return false;
        }

        Checkout.setStepResponse(response);
    }
};


var ConfirmPayment = {
    form: false,
    saveUrl: false,
    isSuccess: false,

    init: function (form, saveUrl, successUrl) {
        this.form = form;
        this.saveUrl = saveUrl;
        this.successUrl = successUrl;
    },

    save: function () {
        if (Checkout.loadWaiting !== false) return;

        Checkout.setLoadWaiting('confirm-order');
        $.ajax({
            cache: false,
            url: this.saveUrl,
            data: $(this.form).serialize(),
            type: "POST",
            success: this.nextStep,
            complete: this.resetLoadWaiting,
            error: Checkout.ajaxFailure
        });
      //if ($("#isCitagenixbtn").val() == "IsCitagenix")  {
      //  window.location.href = "https://localhost:44369/customer/offlinetransactions";
      //}
    },

    resetLoadWaiting: function (transport) {
        Checkout.setLoadWaiting(false, ConfirmOrder.isSuccess);
    },

    nextStep: function (response) {
        if (response.error) {
            //if (typeof response.message === 'string') {
            //    alert(response.message);
            //} else {
            //    alert(response.message.join("\n"));
            //}
          alert(" There was an error while processing your payment. Please try again or contact us.");
            return false;
        }

        if (response.redirect) {
            ConfirmPayment.isSuccess = true;
            location.href = response.redirect;
            return;
        }
        if (response.success) {
            ConfirmPayment.isSuccess = true;
            window.location = ConfirmPayment.successUrl;
        }

        Checkout.setStepResponse(response);
    }
};
