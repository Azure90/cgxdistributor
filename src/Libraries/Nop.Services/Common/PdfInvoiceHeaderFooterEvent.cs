using iTextSharp.text.pdf;
using System;
using iTextSharp.text;
using Nop.Core.Domain.Invoices;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Localization;
using System.IO;
using System.Text.RegularExpressions;

namespace Nop.Services.Common
{
    public class PdfInvoiceHeaderFooterEvent : PdfPageEventHelper
    {
        #region Fields
        // This is the contentbyte object of the writer
        PdfContentByte cb;
        // we will put the final number of pages in a template
        PdfTemplate PageNumberTemplate;

        // this is the BaseFont we are going to use for the header / footer
        //BaseFont bf = null;

        public Invoice objInvoice { get; set; }
        public PdfStoreAddress ObjStore { get; set; }
        public string logoFilePath { get; set; }

        public Font font { get; set; }
        public Font titleFont { get; set; }
        public Font InvoiceFont { get; set; }
        public Language lang { get; set; }
        public string InvoiceType { get; set; }

        // This keeps track of the creation time
        DateTime PrintTime = DateTime.Now;

        #endregion
        #region Ctor
        public PdfInvoiceHeaderFooterEvent()
        {
            objInvoice = new Invoice();
            ObjStore = new PdfStoreAddress();
        }
        #endregion
        #region Override the method
        // we override the onOpenDocument method
        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            base.OnOpenDocument(writer, document);
            try
            {
                cb = writer.DirectContent;
                // Page Number Area, where page Number will be display in Particular Area, like 1/3
                // Area will be cover in particular size
                PageNumberTemplate = cb.CreateTemplate(10, 10);
            }
            catch (DocumentException)
            {
                throw;
            }
            catch (System.IO.IOException)
            {
                throw;
            }
        }
        public override void OnStartPage(PdfWriter writer, Document document)
        {
            base.OnStartPage(writer, document);

            //logo
            var logoPicture = logoFilePath;
            var logoExists = logoPicture != null;

            //header
            float[] widths = new float[] { 2.5f, .75f, .75f };

            var headerTable = new PdfPTable(widths);
            var cellHeader = new PdfPCell();
            cellHeader.BorderWidth = 0;

            //logo Left To Right
            //*********1st ROW, 1st Column LOGO

            var subTable = new PdfPTable(1);
            var subCell = new PdfPCell();
            if (logoExists)
            {
                var logo = Image.GetInstance(logoFilePath);

                logo.Alignment = GetAlignment(lang, false);
                logo.ScaleToFit(150f, 150f);
                logo.BorderWidth = 0;

                subTable.WidthPercentage = 100;
                subCell.BorderWidth = 0;

                // Add logo
                subCell.AddElement(logo);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.PaddingTop = 10;  // Space b/w Address and Logo
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Address, font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase("Local: T " + ObjStore.LocalPhone + " | F " + ObjStore.LocalFax + "", font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase("Toll Free: T " + ObjStore.TollFreePhone + " | F " + ObjStore.TollFreeFax + "", font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Email, font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Website, font);
                subTable.AddCell(subCell);

                cellHeader.AddElement(subTable);
                headerTable.AddCell(cellHeader);
            }
            else
            {
                subTable.AddCell(Environment.NewLine);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Address, font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase("Local: T " + ObjStore.LocalPhone + " | F " + ObjStore.LocalFax + "", font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase("Toll Free: T " + ObjStore.TollFreePhone + " | F " + ObjStore.TollFreeFax + "", font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Email, font);
                subTable.AddCell(subCell);

                subCell = new PdfPCell();
                subCell.BorderWidth = 0;
                subCell.HorizontalAlignment = Element.ALIGN_LEFT;
                subCell.Phrase = new Phrase(ObjStore.Website, font);
                subTable.AddCell(subCell);

                cellHeader.AddElement(subTable);
                headerTable.AddCell(cellHeader);
            }


            //********* 1st ROW, 2nd Column Nested Table Orginal

            cellHeader = new PdfPCell();
            headerTable.WidthPercentage = 100f;
            cellHeader.BorderWidth = 0;
            cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;

            // Nested Table Original
            var OriginalTable = new PdfPTable(1);
            OriginalTable.WidthPercentage = 100;
            var OriginalCell = new PdfPCell();

            // 1st Cell Heading Original
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_LEFT;
            OriginalCell.Phrase = new Phrase("Original", titleFont);
            OriginalTable.AddCell(OriginalCell);

            // 2nd Cell Blank
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_LEFT;
            OriginalCell.Phrase = new Phrase(" ", font);
            OriginalTable.AddCell(OriginalCell);

            // 3rd Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Document Number", font);
            OriginalTable.AddCell(OriginalCell);

            // 4th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Document Date", font);
            OriginalTable.AddCell(OriginalCell);

            // 5th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Due Date", font);
            OriginalTable.AddCell(OriginalCell);

            // 6th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Customer No.", font);
            OriginalTable.AddCell(OriginalCell);

            // 7th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Reference/PO No.", font);
            OriginalTable.AddCell(OriginalCell);

            // 8th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Account Manager", font);
            OriginalTable.AddCell(OriginalCell);

            // 9th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Service Contract No.", font);
            OriginalTable.AddCell(OriginalCell);

            // 10th Cell
            OriginalCell = new PdfPCell();
            OriginalCell.BorderWidth = 0;
            OriginalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            OriginalCell.Phrase = new Phrase("Page", font);
            OriginalTable.AddCell(OriginalCell);

            cellHeader.AddElement(OriginalTable);
            headerTable.AddCell(cellHeader);

            //********* 1st ROW, 3rd Column Nested Table INVOICE
            cellHeader = new PdfPCell();
            headerTable.WidthPercentage = 100f;
            cellHeader.BorderWidth = 0;
            cellHeader.HorizontalAlignment = Element.ALIGN_RIGHT;

            // Nested Table INVOICE
            var InvoiceTable = new PdfPTable(1);
            InvoiceTable.WidthPercentage = 100;
            var InvoiceCell = new PdfPCell();

            // 1st Cell Heading Invoice
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(InvoiceType, titleFont);
            InvoiceTable.AddCell(InvoiceCell);

            // 2nd Cell Blank
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(" ", font);
            InvoiceTable.AddCell(InvoiceCell);

            // 3rd Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.DocumentNumber, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 4th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.DocumentDate, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 5th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.DueDate, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 6th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.CustomerNo, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 7th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.ReferencePoNo == "" ? " " : objInvoice.ReferencePoNo, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 8th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.AccountManager == "" ? " " : objInvoice.AccountManager, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 9th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase(objInvoice.ServiceContractNo == "" ? " " : objInvoice.ServiceContractNo, font);
            InvoiceTable.AddCell(InvoiceCell);

            // 10th Cell
            InvoiceCell = new PdfPCell();
            InvoiceCell.BorderWidth = 0;
            InvoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            InvoiceCell.Phrase = new Phrase("", font); // Epty Page Number, Writing Page Number Through Template
            InvoiceTable.AddCell(InvoiceCell);

            cellHeader.AddElement(InvoiceTable);
            headerTable.AddCell(cellHeader);

            document.Add(headerTable);


            //********** BILL TO , SHIP TO
            Font BilToTitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.BOLD, BaseColor.BLACK);

            float[] BillWidths = new float[] { 2f, 2f, 2f };

            //1st Row 1st Col
            var TableBillTo = new PdfPTable(BillWidths);
            TableBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            TableBillTo.WidthPercentage = 100f;

            TableBillTo.SpacingBefore = 10f;

            var CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("Bill To:", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //1st Row 2nd Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("Delivery Address", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //1st Row 3rd Col
            // Currency Col is Empty
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //2nd Row 1st Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase(objInvoice.BillToName, BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //2nd Row 2nd Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase(objInvoice.ShipToName, BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //2nd Row 3rd Col
            // Currency Col is Empty
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            //3rd Row 1st Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase(objInvoice.BillToAddress, font);
            TableBillTo.AddCell(CellBillTo);

            //3rd Row 2nd Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase(objInvoice.ShipToAddress, font);
            TableBillTo.AddCell(CellBillTo);

            //3rd Row 3rd Col
            // Currency Col is Empty
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            // 4th Row ist Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            // 4th Row 2nd Col
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_LEFT;
            CellBillTo.Phrase = new Phrase("", BilToTitleFont);
            TableBillTo.AddCell(CellBillTo);

            // 4th Row 3rd Col
            // Currency
            CellBillTo = new PdfPCell();
            CellBillTo.BorderWidth = 0;
            CellBillTo.HorizontalAlignment = Element.ALIGN_RIGHT;
            CellBillTo.Phrase = new Phrase("Currency: " + objInvoice.Currency, font);
            TableBillTo.AddCell(CellBillTo);

            document.Add(TableBillTo);
        }
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

            //// *********** Draw Line
            //// Line Move (X Position, Y Position) 
            //cb.MoveTo(10, document.PageSize.GetBottom(90));

            //// Line With (With , Height)
            //cb.LineTo(document.PageSize.Width - 15, document.PageSize.GetBottom(90));
            //cb.SetColorStroke(new BaseColor(33, 150, 243)); // Line Color Code
            //cb.Stroke();
            //// **************** Draw Line


            // ******************** BOTTOM IMAGES
            // ******* Logo Image saved in Azure Container by Manually
            // ******* Showing in Report from Azure Container

            Uri uri = new Uri(logoFilePath); // getting the Container Path
            var fi = new FileInfo(logoFilePath);

            // this one has error, if space in string, not changing footerImage.png
            //var FooterImgPath = uri.AbsoluteUri.Replace(fi.Name, "footerImage.png");
            var FooterImgPath = uri.ToString().Replace(fi.Name, "footerImage.png");
            var FooterImg = Image.GetInstance(FooterImgPath);

            FooterImg.Alignment = GetAlignment(lang, false);
            //FooterImg.ScaleToFit(150f, 150f);
            FooterImg.BorderWidth = 0;

            //float[] Imgwidths = new float[] {1};
            //var imgTable = new PdfPTable(Imgwidths);
            //imgTable.WidthPercentage = 100;
            //imgTable.TotalWidth = document.PageSize.Width - 20;

            //var imgCell = new PdfPCell();
            //imgCell.BorderWidth = 0;

            //// Add logo
            //imgCell.AddElement(FooterImg);
            //imgTable.AddCell(imgCell);
            //imgTable.WriteSelectedRows(0, -1, 10, document.Bottom + 100, writer.DirectContent);
            //// ******************** BOTTOM IMAGES


            float[] widths = new float[] { .5f, .75f, 2.25f, 1f };
            var footerTable = new PdfPTable(widths);

            footerTable.WidthPercentage = 100f;
            footerTable.TotalWidth = document.PageSize.Width - 20;
            PdfPCell footerCell;

            // Before 1st Cell Add Bottom Image

            footerCell = new PdfPCell();
            footerCell.Colspan = 4;
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerCell.AddElement(FooterImg);
            footerTable.AddCell(footerCell);

            // Add Line

            footerCell = new PdfPCell();
            footerCell.Colspan = 4;
            footerCell.BorderWidthTop = 0;
            footerCell.BorderWidthRight = 0;
            footerCell.BorderWidthLeft = 0;
            footerCell.BorderWidthBottom = 1f;
            footerCell.BorderColorBottom = new BaseColor(33, 150, 243);
            footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 1st Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("GST/HST No:", font);
            footerTable.AddCell(footerCell);

            // 2nd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase(ObjStore.GstHstNo, font);
            footerTable.AddCell(footerCell);

            // 3rd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerCell.Phrase = new Phrase("We thank you for your continued trust in our services.", font);
            footerTable.AddCell(footerCell);

            // 4th Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            footerCell.Phrase = new Phrase("Printed by: www.hansamed.net", font);
            footerTable.AddCell(footerCell);

            // 1st Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("QST No:", font);
            footerTable.AddCell(footerCell);

            // 2nd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase(ObjStore.QSTNo, font);
            footerTable.AddCell(footerCell);

            //3rd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 4th Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 1st Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("MB-PST No:", font);
            footerTable.AddCell(footerCell);

            // 2nd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase(ObjStore.MbPstNo, font);
            footerTable.AddCell(footerCell);

            // 3rd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 4th Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 1st Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("BC-PST No:", font);
            footerTable.AddCell(footerCell);

            // 2nd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase(ObjStore.BcPstNo, font);
            footerTable.AddCell(footerCell);

            // 3rd Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            // 4th Col
            footerCell = new PdfPCell();
            footerCell.BorderWidth = 0;
            footerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell.Phrase = new Phrase("", font);
            footerTable.AddCell(footerCell);

            footerTable.WriteSelectedRows(0, -1, 10, document.Bottom, writer.DirectContent);

            // Create Page Number
            CreatePageNumber(writer, document);

        }
        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);

            // Define Page Number Template
            PageNumberTemplate.BeginText();
            PageNumberTemplate.SetFontAndSize(font.BaseFont, 8);
            PageNumberTemplate.SetTextMatrix(0, 0);
            PageNumberTemplate.ShowText("" + (writer.PageNumber - 1).ToString());
            PageNumberTemplate.EndText();
        }
        private PdfPCell GetPdfCell(object text, Font font)
        {
            return new PdfPCell(new Phrase(text.ToString(), font));
        }
        private int GetAlignment(Language lang, bool isOpposite = false)
        {
            //if we need the element to be opposite, like logo etc`.
            if (!isOpposite)
                return lang.Rtl ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT;

            return lang.Rtl ? Element.ALIGN_LEFT : Element.ALIGN_RIGHT;
        }
        private void CreatePageNumber(PdfWriter writer, Document document)
        {
            // Page Number showing as Template on Particular Position on PDF
            int pageN = writer.PageNumber;
            string text = pageN + "/";
            float len = font.BaseFont.GetWidthPoint(text, 8);  // font Total Page Number
            Rectangle pageSize = document.PageSize;
            //cb.SetRgbColorFill(40, 53, 147);  // Set Color of page Number like 1/2
            cb.BeginText();
            cb.SetFontAndSize(font.BaseFont, 8); // Set font of each Page Number

            // Each Page Number Position on Pdf
            cb.SetTextMatrix(pageSize.GetRight(60), pageSize.GetTop(150));
            cb.ShowText(text);
            cb.EndText();

            // Total Page Number Position on Pdf, Total Page Number is the Template
            cb.AddTemplate(PageNumberTemplate, pageSize.GetRight(60) + len, pageSize.GetTop(150));
        }

        #endregion
    }
}
