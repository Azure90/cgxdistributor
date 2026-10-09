using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Invoices;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common.Pdf;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Nop.Services.Common
{
    /// <summary>
    /// PDF service
    /// </summary>
    public partial class PdfService : IPdfService
    {
        #region Fields

        private readonly AddressSettings _addressSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly IAddressAttributeFormatter _addressAttributeFormatter;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IGiftCardService _giftCardService;
        private readonly IHtmlFormatter _htmlFormatter;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureService _measureService;
        private readonly INopFileProvider _fileProvider;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly IRewardPointService _rewardPointService;
        private readonly ISettingService _settingService;
        private readonly IShipmentService _shipmentService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly IWorkContext _workContext;
        private readonly MeasureSettings _measureSettings;
        private readonly TaxSettings _taxSettings;
        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Ctor

        public PdfService(AddressSettings addressSettings,
            CatalogSettings catalogSettings,
            CurrencySettings currencySettings,
            IAddressAttributeFormatter addressAttributeFormatter,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            IDateTimeHelper dateTimeHelper,
            IGiftCardService giftCardService,
            IHtmlFormatter htmlFormatter,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IMeasureService measureService,
            INopFileProvider fileProvider,
            IOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductService productService,
            IRewardPointService rewardPointService,
            ISettingService settingService,
            IShipmentService shipmentService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            IStoreService storeService,
            IVendorService vendorService,
            IWorkContext workContext,
            MeasureSettings measureSettings,
            TaxSettings taxSettings,
            VendorSettings vendorSettings)
        {
            _addressSettings = addressSettings;
            _addressService = addressService;
            _catalogSettings = catalogSettings;
            _countryService = countryService;
            _currencySettings = currencySettings;
            _addressAttributeFormatter = addressAttributeFormatter;
            _currencyService = currencyService;
            _dateTimeHelper = dateTimeHelper;
            _giftCardService = giftCardService;
            _htmlFormatter = htmlFormatter;
            _languageService = languageService;
            _localizationService = localizationService;
            _measureService = measureService;
            _fileProvider = fileProvider;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _paymentService = paymentService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _productService = productService;
            _rewardPointService = rewardPointService;
            _settingService = settingService;
            _shipmentService = shipmentService;
            _storeContext = storeContext;
            _stateProvinceService = stateProvinceService;
            _storeService = storeService;
            _vendorService = vendorService;
            _workContext = workContext;
            _measureSettings = measureSettings;
            _taxSettings = taxSettings;
            _vendorSettings = vendorSettings;
        }

        #endregion

        #region Utils

        /// <summary>
        /// Get billing address
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <returns>A task that contains address item</returns>
        protected virtual async Task<AddressItem> GetBillingAddressAsync(Vendor vendor, Language lang, Order order)
        {
            var addressResult = new AddressItem();

            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);

            if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(billingAddress.Company))
                addressResult.Company = billingAddress.Company;

            addressResult.Name = $"{billingAddress.FirstName} {billingAddress.LastName}";

            if (_addressSettings.PhoneEnabled)
                addressResult.Phone = billingAddress.PhoneNumber;

            if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(billingAddress.FaxNumber))
                addressResult.Fax = billingAddress.FaxNumber;

            if (_addressSettings.StreetAddressEnabled)
                addressResult.Address = billingAddress.Address1;

            if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(billingAddress.Address2))
                addressResult.Address2 = billingAddress.Address2;

            if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
            {
                addressResult.AddressLine =
                    $"{billingAddress.City}, " +
                    $"{(!string.IsNullOrEmpty(billingAddress.County) ? $"{billingAddress.County}, " : string.Empty)}" +
                    $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                    $"{billingAddress.ZipPostalCode}";
            }

            if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(billingAddress) is Country country)
                addressResult.Country = await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id);

            //VAT number
            if (!string.IsNullOrEmpty(order.VatNumber))
                addressResult.VATNumber = order.VatNumber;

            //custom attributes
            var customBillingAddressAttributes = await _addressAttributeFormatter
                .FormatAttributesAsync(billingAddress.CustomAttributes, "<br />");

            if (!string.IsNullOrEmpty(customBillingAddressAttributes))
            {
                var text = _htmlFormatter.ConvertHtmlToPlainText(customBillingAddressAttributes, true, true);
                addressResult.AddressAttributes = text.Split('\n').ToList();
            }

            //vendors payment details
            if (vendor is null)
            {
                //payment method
                var paymentMethod = await _paymentPluginManager.LoadPluginBySystemNameAsync(order.PaymentMethodSystemName);
                var paymentMethodStr = paymentMethod != null
                    ? await _localizationService.GetLocalizedFriendlyNameAsync(paymentMethod, lang.Id)
                    : order.PaymentMethodSystemName;
                if (!string.IsNullOrEmpty(paymentMethodStr))
                {
                    addressResult.PaymentMethod = paymentMethodStr;
                }

                //custom values
                var customValues = _paymentService.DeserializeCustomValues(order);
                if (customValues != null)
                    addressResult.CustomValues = customValues;
            }

            return addressResult;
        }

        /// <summary>
        /// Get shipping address
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <returns>A task that contains address item</returns>
        protected virtual async Task<AddressItem> GetShippingAddressAsync(Language lang, Order order)
        {
            var addressResult = new AddressItem();

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                if (!order.PickupInStore)
                {
                    if (order.ShippingAddressId == null || await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value) is not Address shippingAddress)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");

                    if (!string.IsNullOrEmpty(shippingAddress.Company))
                        addressResult.Company = shippingAddress.Company;

                    addressResult.Name = $"{shippingAddress.FirstName} {shippingAddress.LastName}";

                    if (_addressSettings.PhoneEnabled)
                        addressResult.Phone = shippingAddress.PhoneNumber;

                    if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(shippingAddress.FaxNumber))
                        addressResult.Fax = shippingAddress.FaxNumber;

                    if (_addressSettings.StreetAddressEnabled)
                        addressResult.Address = shippingAddress.Address1;

                    if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(shippingAddress.Address2))
                        addressResult.Address2 = shippingAddress.Address2;

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
                    {
                        addressResult.AddressLine = $"{shippingAddress.City}, " +
                            $"{(!string.IsNullOrEmpty(shippingAddress.County) ? $"{shippingAddress.County}, " : string.Empty)}" +
                            $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                            $"{shippingAddress.ZipPostalCode}";
                    }

                    if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(shippingAddress) is Country country)
                    {
                        addressResult.Country = await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id);
                    }

                    //custom attributes
                    var customShippingAddressAttributes = await _addressAttributeFormatter
                        .FormatAttributesAsync(shippingAddress.CustomAttributes, "<br />");
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        var text = _htmlFormatter.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true);
                        addressResult.AddressAttributes = text.Split('\n').ToList();
                    }
                }
                else if (order.PickupAddressId.HasValue && await _addressService.GetAddressByIdAsync(order.PickupAddressId.Value) is Address pickupAddress)
                {
                    if (!string.IsNullOrEmpty(pickupAddress.Address1))
                        addressResult.Address = pickupAddress.Address1;

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
                    {
                        addressResult.AddressLine = $"{pickupAddress.City}, " +
                            $"{(!string.IsNullOrEmpty(pickupAddress.County) ? $"{pickupAddress.County}, " : string.Empty)}" +
                            $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(pickupAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                            $"{pickupAddress.ZipPostalCode}";
                    }

                    if (await _countryService.GetCountryByAddressAsync(pickupAddress) is Country country)
                        addressResult.Country = await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id);
                }

                addressResult.ShippingMethod = order.ShippingMethod;
            }

            return addressResult;
        }

        /// <summary>
        /// Get order notes
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="order">Order</param>
        /// <param name="lang">Language</param>
        /// <returns>A task that contains collection of date/note pairs</returns>
        protected virtual async Task<List<(string, string)>> GetOrderNotesAsync(PdfSettings pdfSettingsByStore, Order order, Language lang)
        {
            var notesResult = new List<(string, string)>();

            if (!pdfSettingsByStore.RenderOrderNotes)
                return notesResult;

            var orderNotes = (await _orderService.GetOrderNotesByOrderIdAsync(order.Id, true))
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList();

            if (!orderNotes.Any())
                return notesResult;

            foreach (var orderNote in orderNotes)
            {
                var createdOn = (await _dateTimeHelper.ConvertToUserTimeAsync(orderNote.CreatedOnUtc, DateTimeKind.Utc)).ToString();
                var note = _htmlFormatter.ConvertHtmlToPlainText(_orderService.FormatOrderNoteText(orderNote), true, true);

                notesResult.Add((createdOn, note));

                //should we display a link to downloadable files here?
                //I think, no. Anyway, PDFs are printable documents and links (files) are useful here
            }

            return notesResult;
        }

        /// <summary>
        /// Get product entries for document data source
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderItems">Collection of order items</param>
        /// <param name="language">Language</param>
        /// <param name="shipmentItems">Collection of shipment items; when using to prepare shipment items</param>
        /// <returns>A task that contains collection of product entries</returns>
        protected virtual async Task<List<ProductItem>> GetOrderProductItemsAsync(Order order, IList<OrderItem> orderItems, Language language, IList<ShipmentItem> shipmentItems = null)
        {
            var vendors = _vendorSettings.ShowVendorOnOrderDetailsPage ? await _vendorService.GetVendorsByProductIdsAsync(orderItems.Select(item => item.ProductId).ToArray()) : new List<Vendor>();

            var result = new List<ProductItem>();

            foreach (var oi in orderItems)
            {
                var productItem = new ProductItem();
                var product = await _productService.GetProductByIdAsync(oi.ProductId);

                //product name
                productItem.Name = await _localizationService.GetLocalizedAsync(product, x => x.Name, language.Id);

                //attributes
                if (!string.IsNullOrEmpty(oi.AttributeDescription))
                {
                    var attributes = _htmlFormatter.ConvertHtmlToPlainText(oi.AttributeDescription, true, true);
                    productItem.ProductAttributes = attributes.Split('\n').ToList();
                }

                //SKU
                if (_catalogSettings.ShowSkuOnProductDetailsPage)
                    productItem.Sku = await _productService.FormatSkuAsync(product, oi.AttributesXml);

                //Vendor name
                if (_vendorSettings.ShowVendorOnOrderDetailsPage)
                    productItem.VendorName = vendors.FirstOrDefault(v => v.Id == product.VendorId)?.Name ?? string.Empty;

                //price
                string unitPrice;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var unitPriceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(oi.UnitPriceInclTax, order.CurrencyRate);
                    unitPrice = await _priceFormatter.FormatPriceAsync(unitPriceInclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, language.Id, true);
                }
                else
                {
                    //excluding tax
                    var unitPriceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(oi.UnitPriceExclTax, order.CurrencyRate);
                    unitPrice = await _priceFormatter.FormatPriceAsync(unitPriceExclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, language.Id, false);
                }

                productItem.Price = unitPrice;

                //qty
                productItem.Quantity = shipmentItems is null ?
                    oi.Quantity.ToString() : 
                    shipmentItems.FirstOrDefault(x => x.OrderItemId == oi.Id).Quantity.ToString();

                //total
                string subTotal;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var priceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(oi.PriceInclTax, order.CurrencyRate);
                    subTotal = await _priceFormatter.FormatPriceAsync(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        language.Id, true);
                }
                else
                {
                    //excluding tax
                    var priceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(oi.PriceExclTax, order.CurrencyRate);
                    subTotal = await _priceFormatter.FormatPriceAsync(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        language.Id, false);
                }

                productItem.Total = subTotal;

                result.Add(productItem);
            }

            return result;
        }

        /// <summary>
        /// Get invoice totals
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <returns>A task that contains invoice totals</returns>
        protected virtual async Task<InvoiceTotals> GetTotalsAsync(Language lang, Order order)
        {
            var result = new InvoiceTotals();
            var languageId = lang.Id;

            //order subtotal
            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //including tax
                var orderSubtotalInclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
                result.SubTotal = await _priceFormatter.FormatPriceAsync(orderSubtotalInclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, languageId, true);
            }
            else
            {
                //excluding tax
                var orderSubtotalExclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                result.SubTotal = await _priceFormatter.FormatPriceAsync(orderSubtotalExclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, languageId, false);
            }

            //discount (applied to order subtotal)
            if (order.OrderSubTotalDiscountExclTax > decimal.Zero)
            {
                //order subtotal
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                    !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
                {
                    //including tax
                    var orderSubTotalDiscountInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
                    result.Discount = await _priceFormatter.FormatPriceAsync(
                        -orderSubTotalDiscountInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                }
                else
                {
                    //excluding tax
                    var orderSubTotalDiscountExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
                    result.Discount = await _priceFormatter.FormatPriceAsync(
                        -orderSubTotalDiscountExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                }
            }

            //shipping
            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var orderShippingInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
                    result.Shipping = await _priceFormatter.FormatShippingPriceAsync(
                        orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                }
                else
                {
                    //excluding tax
                    var orderShippingExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                    result.Shipping = await _priceFormatter.FormatShippingPriceAsync(
                        orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                }
            }

            //payment fee
            if (order.PaymentMethodAdditionalFeeExclTax > decimal.Zero)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var paymentMethodAdditionalFeeInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
                    result.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(
                        paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                }
                else
                {
                    //excluding tax
                    var paymentMethodAdditionalFeeExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
                    result.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(
                        paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                }
            }

            //tax
            var taxStr = string.Empty;
            var taxRates = new SortedDictionary<decimal, decimal>();
            bool displayTax;
            var displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
            }
            else
            {
                if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    taxRates = _orderService.ParseTaxRates(order, order.TaxRates);

                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                    displayTax = !displayTaxRates;

                    var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    taxStr = await _priceFormatter.FormatPriceAsync(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        false, languageId);
                }
            }

            if (displayTax)
            {
                result.Tax = taxStr;
            }

            if (displayTaxRates)
            {
                foreach (var item in taxRates)
                {
                    var taxRate = string.Format(await _localizationService.GetResourceAsync("Pdf.TaxRate", languageId),
                        _priceFormatter.FormatTaxRate(item.Key));
                    var taxValue = await _priceFormatter.FormatPriceAsync(
                        _currencyService.ConvertCurrency(item.Value, order.CurrencyRate), true, order.CustomerCurrencyCode,
                        false, languageId);

                    result.TaxRates.Add($"{taxRate} {taxValue}");
                }
            }

            //discount (applied to order total)
            if (order.OrderDiscount > decimal.Zero)
            {
                var orderDiscountInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
                result.Discount = await _priceFormatter.FormatPriceAsync(-orderDiscountInCustomerCurrency,
                    true, order.CustomerCurrencyCode, false, languageId);
            }

            //gift cards
            foreach (var gcuh in await _giftCardService.GetGiftCardUsageHistoryAsync(order))
            {
                var gcTitle = string.Format(await _localizationService.GetResourceAsync("Pdf.GiftCardInfo", languageId),
                    (await _giftCardService.GetGiftCardByIdAsync(gcuh.GiftCardId))?.GiftCardCouponCode);
                var gcAmountStr = await _priceFormatter.FormatPriceAsync(
                    -_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate), true,
                    order.CustomerCurrencyCode, false, languageId);

                result.GiftCards.Add($"{gcTitle} {gcAmountStr}");
            }

            //reward points
            if (order.RedeemedRewardPointsEntryId.HasValue && await _rewardPointService.GetRewardPointsHistoryEntryByIdAsync(order.RedeemedRewardPointsEntryId.Value) is RewardPointsHistory redeemedRewardPointsEntry)
            {
                var rpTitle = string.Format(await _localizationService.GetResourceAsync("Pdf.RewardPoints", languageId),
                    -redeemedRewardPointsEntry.Points);
                var rpAmount = await _priceFormatter.FormatPriceAsync(
                    -_currencyService.ConvertCurrency(redeemedRewardPointsEntry.UsedAmount, order.CurrencyRate),
                    true, order.CustomerCurrencyCode, false, languageId);

                result.RewardPoints = $"{rpTitle} {rpAmount}";
            }

            //order total
            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
            var orderTotalStr = await _priceFormatter.FormatPriceAsync(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);
            result.OrderTotal = $"{await _localizationService.GetResourceAsync("Pdf.OrderTotal", languageId)} {orderTotalStr}";

            return result;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Write PDF invoice to the specified stream
        /// </summary>
        /// <param name="stream">Stream to save PDF</param>
        /// <param name="order">Order</param>
        /// <param name="language">Language; null to use a language used when placing an order</param>
        /// <param name="store">Store</param>
        /// <param name="vendor">Vendor to limit products; null to print all products. If specified, then totals won't be printed</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public virtual async Task PrintOrderToPdfAsync(Stream stream, Order order, Language language = null, Store store = null, Vendor vendor = null)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //store info
            store ??= await _storeContext.GetCurrentStoreAsync();

            var orderStore = order.StoreId == 0 || order.StoreId == store?.Id ?
                store : await _storeService.GetStoreByIdAsync(order.StoreId);

            //language info
            language ??= await _languageService.GetLanguageByIdAsync(order.CustomerLanguageId);

            if (language?.Published != true)
                language = await _workContext.GetWorkingLanguageAsync();

            //by default _pdfSettings contains settings for the current active store
            //and we need PdfSettings for the store which was used to place an order
            //so let's load it based on a store of the current order
            var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(orderStore.Id);

            byte[] logo = null;
            var logoPicture = await _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId);
            if (logoPicture != null)
            {
                //var logoFilePath = await _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false);
                var logoFilePath = "wwwroot\\images\\pdforderinvoicelogo.png";
                if (logoPicture.MimeType == MimeTypes.ImageSvg)
                {
                    logo = await _pictureService.ConvertSvgToPngAsync(logoFilePath);
                }
                else
                {
                    logo = await _fileProvider.ReadAllBytesAsync(logoFilePath);
                }
            }

            var date = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc);

            //a vendor should have access only to products
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: vendor?.Id ?? 0);

            var column1Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn1) ?
                new List<string>()
                : pdfSettingsByStore.InvoiceFooterTextColumn1
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            var column2Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn2) ?
                new List<string>()
                : pdfSettingsByStore.InvoiceFooterTextColumn2
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            var source = new InvoiceSource()
            {
                StoreUrl = orderStore.Url?.Trim('/'),
                Language = language,
                FontFamily = pdfSettingsByStore.FontFamily,
                OrderDateUser = date,
                LogoData = logo,
                OrderNumberText = order.CustomOrderNumber,
                PageSize = pdfSettingsByStore.LetterPageSizeEnabled ? PageSizes.Letter : PageSizes.A4,
                BillingAddress = await GetBillingAddressAsync(vendor, language, order),
                ShippingAddress = await GetShippingAddressAsync(language, order),
                Products = await GetOrderProductItemsAsync(order, orderItems, language),
                ShowSkuInProductList = _catalogSettings.ShowSkuOnProductDetailsPage,
                ShowVendorInProductList = _vendorSettings.ShowVendorOnOrderDetailsPage,
                CheckoutAttributes = vendor is null ? order.CheckoutAttributeDescription : string.Empty, //vendors cannot see checkout attributes
                Totals = vendor is null ? await GetTotalsAsync(language, order) : new(), //vendors cannot see totals
                OrderNotes = await GetOrderNotesAsync(pdfSettingsByStore, order, language),
                FooterTextColumn1 = column1Lines,
                FooterTextColumn2 = column2Lines
            };

            await using var pdfStream = new MemoryStream();
            new InvoiceDocument(source, _localizationService)
                .GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            await pdfStream.CopyToAsync(stream);
        }

        /// <summary>
        /// Write ZIP archive with invoices to the specified stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="language">Language; null to use a language used when placing an order</param>
        /// <param name="vendor">Vendor to limit products; null to print all products. If specified, then totals won't be printed</param>
        /// <returns>A task that represents the asynchronous operation</returns>
       

        /// <summary>
        /// Write ZIP archive with packaging slips to the specified stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="language">Language; null to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task PrintPackagingSlipsToPdfAsync(Stream stream, IList<Shipment> shipments, Language language = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (shipments == null)
                throw new ArgumentNullException(nameof(shipments));

            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

            foreach (var shipment in shipments)
            {
                var entryName = $"{await _localizationService.GetResourceAsync("Pdf.Shipment")}{shipment.Id}";

                await using var fileStreamInZip = archive.CreateEntry($"{entryName}.pdf").Open();
                await using var pdfStream = new MemoryStream();
                await PrintPackagingSlipToPdfAsync(pdfStream, shipment, language);

                pdfStream.Position = 0;
                await pdfStream.CopyToAsync(fileStreamInZip);
            }
        }

        /// <summary>
        /// Write packaging slip to the specified stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipment">Shipment</param>
        /// <param name="language">Language; null to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task PrintPackagingSlipToPdfAsync(Stream stream, Shipment shipment, Language language = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);

            var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(order.StoreId);

            //language info
            language ??= await _languageService.GetLanguageByIdAsync(order.CustomerLanguageId);

            if (language?.Published != true)
                language = await _workContext.GetWorkingLanguageAsync();

            var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);

            if (shipmentItems?.Any() != true)
                return;

            var orderItems = await shipmentItems
                .SelectAwait(async si => await _orderService.GetOrderItemByIdAsync(si.OrderItemId))
                .Where(pi => pi != null)
                .ToListAsync();

            if (orderItems?.Any() != true)
                return;

            var source = new ShipmentSource
            {
                PageSize = pdfSettingsByStore.LetterPageSizeEnabled ? PageSizes.Letter : PageSizes.A4,
                Language = language,
                FontFamily = pdfSettingsByStore.FontFamily,
                ShipmentNumberText = shipment.Id.ToString(),
                OrderNumberText = order.CustomOrderNumber,
                Address = await GetShippingAddressAsync(language, order),
                Products = await GetOrderProductItemsAsync(order, orderItems, language, shipmentItems)
            };

            await using var pdfStream = new MemoryStream();

            new ShipmentDocument(source, _localizationService)
                .GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            await pdfStream.CopyToAsync(stream);
        }

        /// <summary>
        /// Write PDF catalog to the specified stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="products">Products</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task PrintProductsToPdfAsync(Stream stream, IList<Product> products)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var currentStore = await _storeContext.GetCurrentStoreAsync();
            var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(currentStore.Id);
            var lang = await _workContext.GetWorkingLanguageAsync();

            var productItems = new List<CatalogItem>();

            foreach (var product in products)
            {
                var priceStr = $"{product.Price:0.00} {(await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode}";
                if (product.IsRental)
                    priceStr = await _priceFormatter.FormatRentalProductPeriodAsync(product, priceStr);

                var rawDescription = await _localizationService.GetLocalizedAsync(product, x => x.FullDescription, lang.Id);

                var productNumber = products.IndexOf(product) + 1;
                var productName = await _localizationService.GetLocalizedAsync(product, x => x.Name, lang.Id);

                var item = new CatalogItem()
                {
                    Name = $"{productNumber}. {productName}",
                    Description = _htmlFormatter.StripTags(_htmlFormatter.ConvertHtmlToPlainText(rawDescription, decode: true)),
                    Price = priceStr,
                    Sku = product.Sku,
                    Weight = product.IsShipEnabled && product.Weight > decimal.Zero ?
                        $"{product.Weight:0.00} {(await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId)).Name}" :
                        string.Empty,
                    Stock = product.ManageInventoryMethod == ManageInventoryMethod.ManageStock ?
                        $"{await _productService.GetTotalStockQuantityAsync(product)}" :
                        string.Empty
                };

                var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);

                if (pictures.Any())
                {
                    var picturePaths = new HashSet<string>();

                    foreach (var pic in pictures)
                    {
                        var picPath = await _pictureService.GetThumbLocalPathAsync(pic, 200, false);
                        if (!string.IsNullOrEmpty(picPath))
                        {
                            picturePaths.Add(picPath);
                        }
                    }

                    item.PicturePaths = picturePaths;
                }

                productItems.Add(item);
            }

            var source = new CatalogSource
            {
                Language = lang,
                PageSize = pdfSettingsByStore.LetterPageSizeEnabled ? PageSizes.Letter : PageSizes.A4,
                FontFamily = pdfSettingsByStore.FontFamily,
                Products = productItems
            };

            await using var pdfStream = new MemoryStream();

            new CatalogDocument(source, _localizationService)
                .GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            await pdfStream.CopyToAsync(stream);
        }

        /// <summary>
        /// Export an order to PDF and save to disk
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="language">Language identifier; null to use a language used when placing an order</param>
        /// <param name="vendor">Vendor to limit products; null to print all products. If specified, then totals won't be printed</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains a path of generated file
        /// </returns>
        public virtual async Task<string> SaveOrderPdfToDiskAsync(Order order, Language language = null, Vendor vendor = null)
        {
            var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";
            var filePath = _fileProvider.Combine(_fileProvider.MapPath("~/wwwroot/files/exportimport"), fileName);
            await using var fileStream = new FileStream(filePath, FileMode.Create);

            await PrintOrderToPdfAsync(fileStream, order, language, store: null, vendor: vendor);

            return filePath;
        }
        public virtual void PrintInvoiceToPdf(Stream stream, Invoice invoice, List<InvoiceDetails> ObjInvDetails, List<TaxCodeDetails> ObjTaxCodeDetails, List<DownPayments> ObjDownPayments, int languageId = 0, int vendorId = 0)
        {
            if (invoice.DocumentNumber == null)
                throw new ArgumentNullException(nameof(stream));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 10, 10, 25, 160);
            var pdfWriter = PdfWriter.GetInstance(doc, stream);

            // Our custom Header and Footer is done using Page Event Handler
            PdfInvoiceHeaderFooterEvent PageEventHandler = new PdfInvoiceHeaderFooterEvent();
            pdfWriter.PageEvent = PageEventHandler;
            PageEventHandler.objInvoice.DocumentNumber = invoice.DocumentNumber;
            PageEventHandler.objInvoice.DocumentDate = Convert.ToDateTime(invoice.DocumentDate).ToString("dd/MM/yyyy");
            PageEventHandler.objInvoice.DueDate = Convert.ToDateTime(invoice.DueDate).ToString("dd/MM/yyyy");
            PageEventHandler.objInvoice.CustomerNo = invoice.CustomerNo;
            PageEventHandler.objInvoice.ReferencePoNo = invoice.ReferencePoNo;
            PageEventHandler.objInvoice.AccountManager = invoice.AccountManager;
            PageEventHandler.objInvoice.ServiceContractNo = invoice.ServiceContractNo;

            PageEventHandler.objInvoice.BillToName = invoice.BillToName;
            PageEventHandler.objInvoice.BillToAddress = invoice.BillToAddress;

            PageEventHandler.objInvoice.ShipToName = invoice.ShipToName;
            PageEventHandler.objInvoice.ShipToAddress = invoice.ShipToAddress;
            PageEventHandler.objInvoice.Currency = invoice.Currency;

            PageEventHandler.ObjStore.Address = "2830 Argentia Road, Units 5-9, Mississauga, ON Canada L5N 8G4";
            PageEventHandler.ObjStore.LocalPhone = "905-813-8090";
            PageEventHandler.ObjStore.LocalFax = "905-813-8091";
            PageEventHandler.ObjStore.TollFreePhone = "1-800-363-2876";
            PageEventHandler.ObjStore.TollFreeFax = "1-800-863-3213";
            PageEventHandler.ObjStore.Website = "www.hansamed.net";
            PageEventHandler.ObjStore.Email = "info@hansamed.net";
            PageEventHandler.ObjStore.GstHstNo = "889445680RT0001";
            PageEventHandler.ObjStore.QSTNo = "1215192519TQ0001";
            PageEventHandler.ObjStore.MbPstNo = "889445680MT0001";
            PageEventHandler.ObjStore.BcPstNo = "1012-8548";

            PageEventHandler.InvoiceType = "INVOICE";

            // Get Logo Path

            var pdfSettingsByStore = _settingService.LoadSetting<PdfSettings>(invoice.Id);
            var logoPicture = _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId).Result;
            var logoFilePath = _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false).Result;
            PageEventHandler.logoFilePath = logoFilePath;

            Font font = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
            Font fontPaymentOption = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.RED);
            Font fontItemDetail = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.BLACK);
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.BOLD, BaseColor.BLACK);
            Font InvoiceFont = FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD, BaseColor.BLACK);

            PageEventHandler.font = font;
            PageEventHandler.titleFont = titleFont;
            PageEventHandler.InvoiceFont = InvoiceFont;

            var lang = _workContext.GetWorkingLanguageAsync().Result;
            PageEventHandler.lang = lang;

            doc.Open();

            // ********** Details goes here
            // Item's Header

            //header 10 Columns
            string[] cols = { "Item Code", "Description", "Qty Ord", "Qty Ship", "Qty Back Ord", "Unit Price", "Disc. %", "Tax", "Net Price", "Total" };

            float[] HWidths = new float[] { 2, 6, .75f, 1, 1.25f, 1, 1, 1, 1.5f, 1.5f };
            var TableItemHeader = new PdfPTable(HWidths);
            TableItemHeader.SpacingBefore = 5;
            TableItemHeader.WidthPercentage = 100;
            TableItemHeader.HorizontalAlignment = Element.ALIGN_CENTER;

            PdfPCell CellItemHeader;
            foreach (string col in cols)
            {
                CellItemHeader = new PdfPCell();
                CellItemHeader.BorderColor = new BaseColor(33, 150, 243);
                CellItemHeader.BackgroundColor = new BaseColor(214, 234, 248);
                CellItemHeader.BorderWidthLeft = 0;
                CellItemHeader.BorderWidthRight = 0;
                CellItemHeader.PaddingBottom = 10;
                CellItemHeader.Phrase = new Phrase(col, font);
                TableItemHeader.AddCell(CellItemHeader);
            }

            doc.Add(TableItemHeader);

            // INVOICE ITEM'S DETAILS

            var TableItemDetails = new PdfPTable(HWidths);
            TableItemDetails.WidthPercentage = 100;
            TableItemDetails.HorizontalAlignment = Element.ALIGN_CENTER;
            TableItemDetails.SpacingAfter = 10;
            TableItemDetails.SpacingBefore = 5;

            PdfPCell CellItemDetails;

            decimal InvoiceSubTotal = 0;

            foreach (InvoiceDetails item in ObjInvDetails)
            {
                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.ItemCode.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.Description.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyOrd)).ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyShip)).ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyBackOrd)).ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.UnitPrice).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                // Percentage
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.DiscountPrc).ToString("0.#"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.Tax.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.NetPrice).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.Total).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                InvoiceSubTotal = InvoiceSubTotal + Convert.ToDecimal(item.Total);
            }
            doc.Add(TableItemDetails);

            // Draw Line
            iTextSharp.text.pdf.draw.LineSeparator line = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, new BaseColor(33, 150, 243), Element.ALIGN_LEFT, 1);
            doc.Add(new Chunk(line));

            // ********************  INVOICE SUB TOTAL AND TAX Details INFO

            // MAIN TABLE
            float[] MainWidth = new float[] { 2, 1 };
            var TableMainTotal = new PdfPTable(MainWidth);
            TableMainTotal.WidthPercentage = 100;
            TableMainTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            PdfPCell CellMainTotal;
            CellMainTotal = new PdfPCell();
            CellMainTotal.BorderWidth = 0;

            // TAX DETAILS
            // ************************* ADD Nested Table in 1st Column
            // Nested Table Invoice Tax Details
            var TableTaxDetails = new PdfPTable(4);
            TableTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            TableTaxDetails.WidthPercentage = 80;
            PdfPCell CellTaxDetails;

            // 1st Row, 1st Cell Invoice Tax Details
            CellTaxDetails = new PdfPCell();
            CellTaxDetails.BorderWidth = 0;
            CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellTaxDetails.Phrase = new Phrase("Tax Details", font);
            CellTaxDetails.Colspan = 4;
            TableTaxDetails.AddCell(CellTaxDetails);


            // Tax Code Table Header Populate Columns
            string[] colsTaxCode = { "Tax Code", "Tax %", "Net", "Tax" };
            foreach (string item in colsTaxCode)
            {
                // Populate 1,2,3,4 Cell Tax Details
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.PaddingBottom = 5;
                CellTaxDetails.BorderColor = new BaseColor(33, 150, 243);
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellTaxDetails.Phrase = new Phrase(item, font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            // Tax Code Table Details
            foreach (TaxCodeDetails item in ObjTaxCodeDetails)
            {
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(item.TaxCode.ToString(), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                // Percentage
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.TaxPrc).ToString("0.#"), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.NetTax).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.TotalTax).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            // DOWN PAYMENTS
            // Heading
            CellTaxDetails = new PdfPCell();
            CellTaxDetails.PaddingTop = 5;
            CellTaxDetails.BorderWidth = 0;
            CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellTaxDetails.Phrase = new Phrase("Down Payments", font);
            CellTaxDetails.Colspan = 4;
            TableTaxDetails.AddCell(CellTaxDetails);


            // Down Payment Table Header Populate Columns
            string[] colsDownPayments = { "Doc No.", "Doc Date", "Gross", "Payment" };
            foreach (string item in colsDownPayments)
            {
                // Populate 1,2,3,4 
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.PaddingBottom = 5;
                CellTaxDetails.BorderColor = new BaseColor(33, 150, 243);
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellTaxDetails.Phrase = new Phrase(item, font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            // Tax Code Table Details
            foreach (DownPayments item in ObjDownPayments)
            {
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);

                CellTaxDetails.Phrase = new Phrase(item.DocNo.ToString() == "0" ? "" : item.DocNo.ToString(), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(item.DocNo.ToString() == "0" ? "" : Convert.ToDateTime(item.DocDate).ToString("dd/MM/yyyy"), font);

                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(item.DocNo.ToString() == "0" ? "" : Convert.ToDecimal(item.Gross).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(item.DocNo.ToString() == "0" ? "" : Convert.ToDecimal(item.Payment).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            // FREIGHT DETAILS, NEW COLUMN

            CellTaxDetails = new PdfPCell();
            CellTaxDetails.Colspan = 4;
            CellTaxDetails.PaddingTop = 5;
            CellTaxDetails.BorderWidth = 0;
            CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;

            // Nested Table with 7 Columns in TaxDetails CELL

            float[] FreightWidth = new float[] { .25f, 1f, 1f, 1f, 1f, 1f, 1f };
            var TableFreightDetails = new PdfPTable(FreightWidth);
            TableFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            TableFreightDetails.WidthPercentage = 100;
            PdfPCell CellFreightDetails;

            // 1st Row, 1st,2nd,3rd  Col

            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 3;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidth = 0;
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Freight Details", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 1st Row, 4th, 5th Col  // Shipping Type
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 2;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidth = 0;
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellFreightDetails.Phrase = new Phrase("Shipping Type: ", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 1st Row, 6th, 7th Col // Shipping Type Value
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 2;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidth = 0;
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellFreightDetails.Phrase = new Phrase(invoice.ShippingType, font);
            TableFreightDetails.AddCell(CellFreightDetails);


            // 2nd Row, 1st Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("#", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 2nd Row, 2nd Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Description", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 2nd Row, 3rd Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Net", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //2nd Row,  4th Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Tax Code", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //2nd Row,  5th Cell
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Tax %", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //2nd Row,  6th Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Tax", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //2nd Row,  7th Cell // Heading
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Gross", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // ************* VALUES

            // 3rd Row, 1st Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("1", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 3rd Row, 2nd Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase(invoice.DescriptionFreight.ToString(), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 3rd Row, 3rd Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.NetFreight).ToString("F"), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 3rd Row,  4th Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase(invoice.TaxCodeFreight.ToString(), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //3rd Row,  5th Cell Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            // Percentage
            CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.TaxPrcFreight).ToString("0.#"), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //3rd Row,  6th Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.TaxFreight).ToString("F"), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //3rd Row,  7th Cell // Values
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase((Convert.ToDecimal(invoice.NetFreight) + Convert.ToDecimal(invoice.TaxFreight)).ToString("F"), font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // PAYMENT TERM

            //4th Row,  1st Cell 
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 3;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Payment Term:", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            //4th Row,  2nd cell 
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 4;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderWidthTop = 0;
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellFreightDetails.Phrase = new Phrase(invoice.PaymentTerms, font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // ADD In Nested Column
            CellTaxDetails.AddElement(TableFreightDetails);
            TableTaxDetails.AddCell(CellTaxDetails);

            //*******************************

            // Add in Main Table
            CellMainTotal.AddElement(TableTaxDetails);
            TableMainTotal.AddCell(CellMainTotal);

            // ************* MAIN TABLE 1st COLUMN End HERE



            // *************** ADD Nested Table in 2nd Column
            // Nested Table Invoice Sub Total

            CellMainTotal = new PdfPCell();
            CellMainTotal.BorderWidth = 0;

            var TableSubTotal = new PdfPTable(2);
            TableSubTotal.WidthPercentage = 100;
            PdfPCell CellSubTotal;

            // 1st Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Invoice Subtotal:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 1st Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(InvoiceSubTotal.ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 2nd Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Freight/Handling:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 2nd Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.FreightCharge).ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 3rd Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Tax Amount:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 3rd Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.TotalTaxAmount).ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 4th Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Invoice:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 4th Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            // Calculate Total Invoice
            // InvoiceSubTotal + invoice.FeightCharge + invoice.TotalTaxAmount
            Decimal TotalInvoice = InvoiceSubTotal + Convert.ToDecimal(invoice.FreightCharge) + Convert.ToDecimal(invoice.TotalTaxAmount);

            CellSubTotal.Phrase = new Phrase(TotalInvoice.ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 5th Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Down Payment: ", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 5th Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.Downpayment).ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 6th Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(33, 150, 243); // Last Border Line Different Color
            CellSubTotal.PaddingBottom = 5; // Last Line more space on bottom

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Outstanding: ", font);
            TableSubTotal.AddCell(CellSubTotal);

            // 6th Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(33, 150, 243); // Last Border Line Different Color
            CellSubTotal.PaddingBottom = 5; // Last Line more space on bottom

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            // Total OutStanding
            // Decimal Totaloutstanding = TotalInvoice - Convert.ToDecimal(invoice.Downpayment); 
            //CellSubTotal.Phrase = new Phrase(Totaloutstanding.ToString("0.##"), font);
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.TotalAfterDownPayment).ToString("F"), font);
            TableSubTotal.AddCell(CellSubTotal);

            // Add in Main Table
            CellMainTotal.AddElement(TableSubTotal);
            TableMainTotal.AddCell(CellMainTotal);

            doc.Add(TableMainTotal);

            // ********** PAYMENT OPTIONS
            // Add New Table

            var TablePaymentOption = new PdfPTable(1);
            //TablePaymentOption.ExtendLastRow = true;
            TablePaymentOption.WidthPercentage = 100;
            TablePaymentOption.SpacingBefore = 10;
            TablePaymentOption.SpacingAfter = 10;
            PdfPCell CellPaymentOptionl;

            // 1st Row PAYMENT OPTIONS
            CellPaymentOptionl = new PdfPCell();
            CellPaymentOptionl.PaddingBottom = 10;
            CellPaymentOptionl.BorderWidth = 0;
            CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_LEFT;

            var PaymentOPtion = "NEW PAYMENT OPTIONS: 1) e-transfer from your banking institution to accounting@hansamed.net and refer to your " + Environment.NewLine + " customer no./ invoice no. in the message/ remarks. " + Environment.NewLine + " 2) Payment by credit card can be processed by clicking this link: https://hansamed.net/paymentinvoice";
            CellPaymentOptionl.Phrase = new Phrase(PaymentOPtion, fontPaymentOption);
            TablePaymentOption.AddCell(CellPaymentOptionl);

            // 2nd Row Rectangle
            CellPaymentOptionl = new PdfPCell();
            CellPaymentOptionl.PaddingBottom = 10;
            CellPaymentOptionl.BorderWidth = 1;
            CellPaymentOptionl.FixedHeight = 50;
            CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_LEFT;
            CellPaymentOptionl.VerticalAlignment = Element.ALIGN_TOP;
            CellPaymentOptionl.Phrase = new Phrase("Remarks: " + invoice.Remarks, font);

            TablePaymentOption.AddCell(CellPaymentOptionl);

            // 3rd Row POLICY TEXT

            CellPaymentOptionl = new PdfPCell();
            CellPaymentOptionl.PaddingBottom = 5;
            CellPaymentOptionl.BorderWidth = 0;
            CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_LEFT;
            var policyText = "Please ensure the items received are correct. Please contact us for any discrepancies within 5 business days. " + Environment.NewLine + " For Terms and Conditions please visit www.hansamed.net under Policy.Thank you for your attention.";
            CellPaymentOptionl.Phrase = new Phrase(policyText, font);
            TablePaymentOption.AddCell(CellPaymentOptionl);


            // Finally Add Payment OPtion
            doc.Add(TablePaymentOption);


            doc.Close();
            //doc.NewPage();
        }
        public virtual void PrintCreditMemoToPdf(Stream stream, Invoice invoice, List<InvoiceDetails> ObjInvDetails, List<TaxCodeDetails> ObjTaxCodeDetails, List<DownPayments> ObjDownPayments, int languageId = 0, int vendorId = 0)
        {
            if (invoice.DocumentNumber == null)
                throw new ArgumentNullException(nameof(stream));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 10, 10, 25, 160);
            var pdfWriter = PdfWriter.GetInstance(doc, stream);

            // Our custom Header and Footer is done using Page Event Handler
            PdfInvoiceHeaderFooterEvent PageEventHandler = new PdfInvoiceHeaderFooterEvent();
            pdfWriter.PageEvent = PageEventHandler;
            PageEventHandler.objInvoice.DocumentNumber = invoice.DocumentNumber;
            PageEventHandler.objInvoice.DocumentDate = Convert.ToDateTime(invoice.DocumentDate).ToString("dd/MM/yyyy");
            PageEventHandler.objInvoice.DueDate = Convert.ToDateTime(invoice.DueDate).ToString("dd/MM/yyyy");
            PageEventHandler.objInvoice.CustomerNo = invoice.CustomerNo;
            PageEventHandler.objInvoice.ReferencePoNo = invoice.ReferencePoNo;
            PageEventHandler.objInvoice.AccountManager = invoice.AccountManager;
            PageEventHandler.objInvoice.ServiceContractNo = invoice.ServiceContractNo;

            PageEventHandler.objInvoice.BillToName = invoice.BillToName;
            PageEventHandler.objInvoice.BillToAddress = invoice.BillToAddress;

            PageEventHandler.objInvoice.ShipToName = invoice.ShipToName;
            PageEventHandler.objInvoice.ShipToAddress = invoice.ShipToAddress;
            PageEventHandler.objInvoice.Currency = invoice.Currency;

            PageEventHandler.ObjStore.Address = "2830 Argentia Road, Units 5-9, Mississauga, ON Canada L5N 8G4";
            PageEventHandler.ObjStore.LocalPhone = "905-813-8090";
            PageEventHandler.ObjStore.LocalFax = "905-813-8091";
            PageEventHandler.ObjStore.TollFreePhone = "1-800-363-2876";
            PageEventHandler.ObjStore.TollFreeFax = "1-800-863-3213";
            PageEventHandler.ObjStore.Website = "www.hansamed.net";
            PageEventHandler.ObjStore.Email = "info@hansamed.net";
            PageEventHandler.ObjStore.GstHstNo = "889445680RT0001";
            PageEventHandler.ObjStore.QSTNo = "1215192519TQ0001";
            PageEventHandler.ObjStore.MbPstNo = "889445680MT0001";
            PageEventHandler.ObjStore.BcPstNo = "1012-8548";

            PageEventHandler.InvoiceType = "CREDIT MEMO";
            // Get Logo Path

            var pdfSettingsByStore = _settingService.LoadSetting<PdfSettings>(invoice.Id);
            var logoPicture = _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId).Result;
            var logoFilePath = _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false).Result;
            PageEventHandler.logoFilePath = logoFilePath;

            Font font = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
            Font fontPaymentOption = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.RED);
            Font fontItemDetail = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.BLACK);
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.BOLD, BaseColor.BLACK);
            Font InvoiceFont = FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD, BaseColor.BLACK);

            PageEventHandler.font = font;
            PageEventHandler.titleFont = titleFont;
            PageEventHandler.InvoiceFont = InvoiceFont;

            var lang = _workContext.GetWorkingLanguageAsync().Result;
            PageEventHandler.lang = lang;

            doc.Open();

            // ********** Details goes here
            // Item's Header

            //header 10 Columns
            string[] cols = { "Item Code", "Description", "Qty", "Unit Price", "Disc. %", "Tax", "Net Price", "Total" };

            float[] HWidths = new float[] { 2, 6, 1, 1, 1, 1, 1.5f, 1.5f };
            var TableItemHeader = new PdfPTable(HWidths);
            TableItemHeader.SpacingBefore = 5;
            TableItemHeader.WidthPercentage = 100;
            TableItemHeader.HorizontalAlignment = Element.ALIGN_CENTER;

            PdfPCell CellItemHeader;
            foreach (string col in cols)
            {
                CellItemHeader = new PdfPCell();
                CellItemHeader.BorderColor = new BaseColor(33, 150, 243);
                CellItemHeader.BackgroundColor = new BaseColor(214, 234, 248);
                CellItemHeader.BorderWidthLeft = 0;
                CellItemHeader.BorderWidthRight = 0;
                CellItemHeader.PaddingBottom = 10;
                CellItemHeader.Phrase = new Phrase(col, font);
                TableItemHeader.AddCell(CellItemHeader);
            }

            doc.Add(TableItemHeader);

            // INVOICE ITEM'S DETAILS

            var TableItemDetails = new PdfPTable(HWidths);
            TableItemDetails.WidthPercentage = 100;
            TableItemDetails.HorizontalAlignment = Element.ALIGN_CENTER;
            TableItemDetails.SpacingAfter = 10;
            TableItemDetails.SpacingBefore = 5;

            PdfPCell CellItemDetails;

            decimal InvoiceSubTotal = 0;

            foreach (InvoiceDetails item in ObjInvDetails)
            {
                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.ItemCode.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.Description.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyShip)).ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                //CellItemDetails = new PdfPCell();
                //CellItemDetails.BorderWidthLeft = 0;
                //CellItemDetails.BorderWidthRight = 0;
                //CellItemDetails.BorderWidthTop = 0;
                //CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                //CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyShip)).ToString(), fontItemDetail);
                //TableItemDetails.AddCell(CellItemDetails);

                //CellItemDetails = new PdfPCell();
                //CellItemDetails.BorderWidthLeft = 0;
                //CellItemDetails.BorderWidthRight = 0;
                //CellItemDetails.BorderWidthTop = 0;
                //CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                //CellItemDetails.Phrase = new Phrase(Convert.ToInt32(Convert.ToDecimal(item.QtyBackOrd)).ToString(), fontItemDetail);
                //TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.UnitPrice).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                // Percentage
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.DiscountPrc).ToString("0.#"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(item.Tax.ToString(), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.NetPrice).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                CellItemDetails = new PdfPCell();
                CellItemDetails.BorderWidthLeft = 0;
                CellItemDetails.BorderWidthRight = 0;
                CellItemDetails.BorderWidthTop = 0;
                CellItemDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellItemDetails.Phrase = new Phrase(Convert.ToDecimal(item.Total).ToString("F"), fontItemDetail);
                TableItemDetails.AddCell(CellItemDetails);

                InvoiceSubTotal = InvoiceSubTotal + Convert.ToDecimal(item.Total);
            }
            doc.Add(TableItemDetails);

            // Draw Line
            iTextSharp.text.pdf.draw.LineSeparator line = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, new BaseColor(33, 150, 243), Element.ALIGN_LEFT, 1);
            doc.Add(new Chunk(line));

            // ********************  INVOICE SUB TOTAL AND TAX Details INFO

            // MAIN TABLE
            float[] MainWidth = new float[] { 2, 1 };
            var TableMainTotal = new PdfPTable(MainWidth);
            TableMainTotal.WidthPercentage = 100;
            TableMainTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            PdfPCell CellMainTotal;
            CellMainTotal = new PdfPCell();
            CellMainTotal.BorderWidth = 0;

            // TAX DETAILS
            // ************************* ADD Nested Table in 1st Column
            // Nested Table Invoice Tax Details
            var TableTaxDetails = new PdfPTable(4);
            TableTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            TableTaxDetails.WidthPercentage = 80;
            PdfPCell CellTaxDetails;

            // 1st Row, 1st Cell Invoice Tax Details
            CellTaxDetails = new PdfPCell();
            CellTaxDetails.BorderWidth = 0;
            CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellTaxDetails.Phrase = new Phrase("Tax Details", font);
            CellTaxDetails.Colspan = 4;
            TableTaxDetails.AddCell(CellTaxDetails);


            // Tax Code Table Header Populate Columns
            string[] colsTaxCode = { "Tax Code", "Tax %", "Base Amount", "Tax" };
            foreach (string item in colsTaxCode)
            {
                // Populate 1,2,3,4 Cell Tax Details
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.PaddingBottom = 5;
                CellTaxDetails.BorderColor = new BaseColor(33, 150, 243);
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellTaxDetails.Phrase = new Phrase(item, font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            // Tax Code Table Details
            foreach (TaxCodeDetails item in ObjTaxCodeDetails)
            {
                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(item.TaxCode.ToString(), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                // Percentage
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.TaxPrc).ToString("0.#"), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.NetTax).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);

                CellTaxDetails = new PdfPCell();
                CellTaxDetails.BorderWidthLeft = 0;
                CellTaxDetails.BorderWidthRight = 0;
                CellTaxDetails.BorderWidthTop = 0;
                CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
                CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.TotalTax).ToString("F"), font);
                TableTaxDetails.AddCell(CellTaxDetails);
            }

            //// DOWN PAYMENTS
            //// Heading
            //CellTaxDetails = new PdfPCell();
            //CellTaxDetails.PaddingTop = 5;
            //CellTaxDetails.BorderWidth = 0;
            //CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            //CellTaxDetails.Phrase = new Phrase("Down Payments", font);
            //CellTaxDetails.Colspan = 4;
            //TableTaxDetails.AddCell(CellTaxDetails);


            //// Down Payment Table Header Populate Columns
            //string[] colsDownPayments = { "Doc No.", "Doc Date", "Gross", "Payment" };
            //foreach (string item in colsDownPayments)
            //{
            //    // Populate 1,2,3,4 
            //    CellTaxDetails = new PdfPCell();
            //    CellTaxDetails.PaddingBottom = 5;
            //    CellTaxDetails.BorderColor = new BaseColor(33, 150, 243);
            //    CellTaxDetails.BorderWidthLeft = 0;
            //    CellTaxDetails.BorderWidthRight = 0;
            //    CellTaxDetails.BorderWidthTop = 0;
            //    CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            //    CellTaxDetails.Phrase = new Phrase(item, font);
            //    TableTaxDetails.AddCell(CellTaxDetails);
            //}

            //// Tax Code Table Details
            //foreach (DownPayments item in ObjDownPayments)
            //{
            //    CellTaxDetails = new PdfPCell();
            //    CellTaxDetails.BorderWidthLeft = 0;
            //    CellTaxDetails.BorderWidthRight = 0;
            //    CellTaxDetails.BorderWidthTop = 0;
            //    CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
            //    CellTaxDetails.Phrase = new Phrase(item.DocNo.ToString(), font);
            //    TableTaxDetails.AddCell(CellTaxDetails);

            //    CellTaxDetails = new PdfPCell();
            //    CellTaxDetails.BorderWidthLeft = 0;
            //    CellTaxDetails.BorderWidthRight = 0;
            //    CellTaxDetails.BorderWidthTop = 0;
            //    CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
            //    CellTaxDetails.Phrase = new Phrase(Convert.ToDateTime(item.DocDate).ToString("dd/MM/yyyy"), font);
            //    TableTaxDetails.AddCell(CellTaxDetails);

            //    CellTaxDetails = new PdfPCell();
            //    CellTaxDetails.BorderWidthLeft = 0;
            //    CellTaxDetails.BorderWidthRight = 0;
            //    CellTaxDetails.BorderWidthTop = 0;
            //    CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
            //    CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.Gross).ToString("0.##"), font);
            //    TableTaxDetails.AddCell(CellTaxDetails);

            //    CellTaxDetails = new PdfPCell();
            //    CellTaxDetails.BorderWidthLeft = 0;
            //    CellTaxDetails.BorderWidthRight = 0;
            //    CellTaxDetails.BorderWidthTop = 0;
            //    CellTaxDetails.BorderColorBottom = new BaseColor(214, 234, 248);
            //    CellTaxDetails.Phrase = new Phrase(Convert.ToDecimal(item.Payment).ToString("0.##"), font);
            //    TableTaxDetails.AddCell(CellTaxDetails);
            //}


            // FREIGHT DETAILS, NEW COLUMN

            CellTaxDetails = new PdfPCell();
            CellTaxDetails.Colspan = 4;
            CellTaxDetails.PaddingTop = 5;
            CellTaxDetails.BorderWidth = 0;
            CellTaxDetails.HorizontalAlignment = Element.ALIGN_LEFT;

            // Nested Table with 7 Columns in TaxDetails CELL

            float[] FreightWidth = new float[] { .25f, 1f, 1f, 1f, 1f, 1f, 1f };
            var TableFreightDetails = new PdfPTable(FreightWidth);
            TableFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            TableFreightDetails.WidthPercentage = 100;
            PdfPCell CellFreightDetails;

            // 1st Row, 1st,2nd,3rd  Col

            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 3;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderColorTop = new BaseColor(33, 150, 243);
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            CellFreightDetails.Phrase = new Phrase("Freight", font);
            TableFreightDetails.AddCell(CellFreightDetails);


            // 1st Row, 4th, 5th Col  // Shipping Type
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 2;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderColorTop = new BaseColor(33, 150, 243);
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellFreightDetails.Phrase = new Phrase("Shipping Type: ", font);
            TableFreightDetails.AddCell(CellFreightDetails);

            // 1st Row, 6th, 7th Col // Shipping Type Value
            CellFreightDetails = new PdfPCell();
            CellFreightDetails.Colspan = 2;
            CellFreightDetails.PaddingTop = 5;
            CellFreightDetails.BorderWidthLeft = 0;
            CellFreightDetails.BorderWidthRight = 0;
            CellFreightDetails.BorderColorTop = new BaseColor(33, 150, 243);
            CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellFreightDetails.Phrase = new Phrase(invoice.ShippingType, font);
            TableFreightDetails.AddCell(CellFreightDetails);


            // if Net Freight = 0 then dont need to show this section
            if (Convert.ToDecimal(invoice.NetFreight) > 0)
            {
                // 2nd Row, 1st Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("#", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // 2nd Row, 2nd Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Description", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // 2nd Row, 3rd Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Net", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //2nd Row,  4th Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Tax Code", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //2nd Row,  5th Cell
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Tax %", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //2nd Row,  6th Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Tax", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //2nd Row,  7th Cell // Heading
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("Gross", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // ************* VALUES

                // 3rd Row, 1st Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase("1", font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // 3rd Row, 2nd Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase(invoice.DescriptionFreight.ToString(), font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // 3rd Row, 3rd Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.NetFreight).ToString("F"), font);
                TableFreightDetails.AddCell(CellFreightDetails);

                // 3rd Row,  4th Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase(invoice.TaxCodeFreight.ToString(), font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //3rd Row,  5th Cell Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                // Percentage
                CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.TaxPrcFreight).ToString("0.#"), font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //3rd Row,  6th Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase(Convert.ToDecimal(invoice.TaxFreight).ToString("F"), font);
                TableFreightDetails.AddCell(CellFreightDetails);

                //3rd Row,  7th Cell // Values
                CellFreightDetails = new PdfPCell();
                CellFreightDetails.PaddingTop = 5;
                CellFreightDetails.BorderWidthLeft = 0;
                CellFreightDetails.BorderWidthRight = 0;
                CellFreightDetails.BorderWidthTop = 0;
                CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
                CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
                CellFreightDetails.Phrase = new Phrase((Convert.ToDecimal(invoice.NetFreight) + Convert.ToDecimal(invoice.TaxFreight)).ToString("F"), font);
                TableFreightDetails.AddCell(CellFreightDetails);
            }


            // PAYMENT TERM

            ////4th Row,  1st Cell 
            //CellFreightDetails = new PdfPCell();
            //CellFreightDetails.Colspan = 3;
            //CellFreightDetails.PaddingTop = 5;
            //CellFreightDetails.BorderWidthLeft = 0;
            //CellFreightDetails.BorderWidthRight = 0;
            //CellFreightDetails.BorderWidthTop = 0;
            //CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            //CellFreightDetails.HorizontalAlignment = Element.ALIGN_LEFT;
            //CellFreightDetails.Phrase = new Phrase("Payment Term:", font);
            //TableFreightDetails.AddCell(CellFreightDetails);

            ////4th Row,  2nd cell 
            //CellFreightDetails = new PdfPCell();
            //CellFreightDetails.Colspan = 4;
            //CellFreightDetails.PaddingTop = 5;
            //CellFreightDetails.BorderWidthLeft = 0;
            //CellFreightDetails.BorderWidthRight = 0;
            //CellFreightDetails.BorderWidthTop = 0;
            //CellFreightDetails.BorderColorBottom = new BaseColor(33, 150, 243);
            //CellFreightDetails.HorizontalAlignment = Element.ALIGN_RIGHT;
            //CellFreightDetails.Phrase = new Phrase("Net30A", font);
            //TableFreightDetails.AddCell(CellFreightDetails);

            // ADD In Nested Column
            CellTaxDetails.AddElement(TableFreightDetails);
            TableTaxDetails.AddCell(CellTaxDetails);

            //*******************************

            // Add in Main Table
            CellMainTotal.AddElement(TableTaxDetails);
            TableMainTotal.AddCell(CellMainTotal);

            // ************* MAIN TABLE 1st COLUMN End HERE



            // *************** ADD Nested Table in 2nd Column
            // Nested Table Invoice Sub Total

            CellMainTotal = new PdfPCell();
            CellMainTotal.BorderWidth = 0;

            var TableSubTotal = new PdfPTable(2);
            TableSubTotal.WidthPercentage = 100;
            PdfPCell CellSubTotal;

            // 1st Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Invoice Subtotal:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 1st Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(InvoiceSubTotal.ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);


            decimal AdditionalExpence = 0;

            if (Convert.ToDecimal(invoice.FreightCharge) > 0)
            {
                // 2nd Row, 1st Cell Additional Expence
                CellSubTotal = new PdfPCell();
                CellSubTotal.BorderWidthLeft = 0;
                CellSubTotal.BorderWidthRight = 0;
                CellSubTotal.BorderWidthTop = 0;
                CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

                CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
                CellSubTotal.Phrase = new Phrase("Additional Expence:", fontItemDetail);
                TableSubTotal.AddCell(CellSubTotal);

                // 2nd Row, 2nd Cell Additional Expence
                CellSubTotal = new PdfPCell();
                CellSubTotal.BorderWidthLeft = 0;
                CellSubTotal.BorderWidthRight = 0;
                CellSubTotal.BorderWidthTop = 0;
                CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

                CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
                AdditionalExpence = Convert.ToDecimal(invoice.FreightCharge);
                CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(AdditionalExpence).ToString("F"), fontItemDetail);
                TableSubTotal.AddCell(CellSubTotal);
            }

            // 3rd Row, 1st Cell Total Before Tax
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Before Tax:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 3rd Row, 2nd Cell Total Before Tax
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);
            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            decimal TotalBeforeTax = InvoiceSubTotal + AdditionalExpence;
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(TotalBeforeTax).ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 4th Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Tax Amount:", fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            // 4th Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.TotalTaxAmount).ToString("F"), fontItemDetail);
            TableSubTotal.AddCell(CellSubTotal);

            //// 5th Row, 1st Cell Invoice Sub Total
            //CellSubTotal = new PdfPCell();
            //CellSubTotal.BorderWidthLeft = 0;
            //CellSubTotal.BorderWidthRight = 0;
            //CellSubTotal.BorderWidthTop = 0;
            //CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            //CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            //CellSubTotal.Phrase = new Phrase("Down Payment: ", fontItemDetail);
            //TableSubTotal.AddCell(CellSubTotal);

            //// 5th Row, 2nd Cell Invoice Sub Total
            //CellSubTotal = new PdfPCell();
            //CellSubTotal.BorderWidthLeft = 0;
            //CellSubTotal.BorderWidthRight = 0;
            //CellSubTotal.BorderWidthTop = 0;
            //CellSubTotal.BorderColorBottom = new BaseColor(214, 234, 248);

            //CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            //CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.Downpayment).ToString("0.##"), fontItemDetail);
            //TableSubTotal.AddCell(CellSubTotal);

            // 6th Row, 1st Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(33, 150, 243); // Last Border Line Different Color
            CellSubTotal.PaddingBottom = 5; // Last Line more space on bottom

            CellSubTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            CellSubTotal.Phrase = new Phrase("Total Amount: ", font);
            TableSubTotal.AddCell(CellSubTotal);

            // 6th Row, 2nd Cell Invoice Sub Total
            CellSubTotal = new PdfPCell();
            CellSubTotal.BorderWidthLeft = 0;
            CellSubTotal.BorderWidthRight = 0;
            CellSubTotal.BorderWidthTop = 0;
            CellSubTotal.BorderColorBottom = new BaseColor(33, 150, 243); // Last Border Line Different Color
            CellSubTotal.PaddingBottom = 5; // Last Line more space on bottom

            CellSubTotal.HorizontalAlignment = Element.ALIGN_RIGHT;

            CellSubTotal.Phrase = new Phrase(Convert.ToDecimal(invoice.TotalAfterDownPayment).ToString("F"), font);
            TableSubTotal.AddCell(CellSubTotal);

            // Add in Main Table
            CellMainTotal.AddElement(TableSubTotal);
            TableMainTotal.AddCell(CellMainTotal);

            doc.Add(TableMainTotal);

            // ********** PAYMENT OPTIONS
            // Add New Table

            var TablePaymentOption = new PdfPTable(1);
            //TablePaymentOption.ExtendLastRow = true;
            TablePaymentOption.WidthPercentage = 100;
            TablePaymentOption.SpacingBefore = 10;
            TablePaymentOption.SpacingAfter = 10;
            PdfPCell CellPaymentOptionl;

            // 1st Row PAYMENT OPTIONS
            CellPaymentOptionl = new PdfPCell();
            CellPaymentOptionl.PaddingBottom = 10;
            CellPaymentOptionl.BorderWidth = 0;
            CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_CENTER;

            var PaymentOPtion = "THIS IS A CREDIT DEMO, PLEASE DO NOT SEND PAYMENT";
            CellPaymentOptionl.Phrase = new Phrase(PaymentOPtion, fontPaymentOption);
            TablePaymentOption.AddCell(CellPaymentOptionl);


            // 2nd Row Rectangle
            CellPaymentOptionl = new PdfPCell();
            CellPaymentOptionl.PaddingBottom = 10;
            CellPaymentOptionl.BorderWidth = 1;
            CellPaymentOptionl.FixedHeight = 50;
            CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_LEFT;
            CellPaymentOptionl.VerticalAlignment = Element.ALIGN_TOP;
            CellPaymentOptionl.Phrase = new Phrase("Remarks: " + invoice.Remarks, font);

            TablePaymentOption.AddCell(CellPaymentOptionl);

            //// 3rd Row POLICY TEXT

            //CellPaymentOptionl = new PdfPCell();
            //CellPaymentOptionl.PaddingBottom = 5;
            //CellPaymentOptionl.BorderWidth = 0;
            //CellPaymentOptionl.HorizontalAlignment = Element.ALIGN_LEFT;
            //var policyText = "Please ensure the items received are correct. Please contact us for any discrepancies within 5 business days. " + Environment.NewLine + " For Terms and Conditions please visit www.hansamed.net under Policy.Thank you for your attention.";
            //CellPaymentOptionl.Phrase = new Phrase(policyText, font);
            //TablePaymentOption.AddCell(CellPaymentOptionl);


            // Finally Add Payment OPtion
            doc.Add(TablePaymentOption);


            doc.Close();
            //doc.NewPage();
        }

        public async Task PrintOrdersToPdfAsync(Stream stream, Invoice objInv, IList<Order> orders, Language language = null, Vendor vendor = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            var currentStore =  _storeContext.GetCurrentStoreAsync().Result;

            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

            foreach (var order in orders)
            {
                var entryName = string.Format("{0} {1}",  _localizationService.GetResourceAsync("Pdf.Order").Result, order.CustomOrderNumber);

                 using var fileStreamInZip = archive.CreateEntry($"{entryName}.pdf").Open();
                 using var pdfStream = new MemoryStream();
                 PrintOrderToPdfAsync(pdfStream, order, language, currentStore, vendor);
                pdfStream.Position = 0;
                 pdfStream.CopyToAsync(fileStreamInZip);
            }
        }
        #endregion
    }
}