using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nop.Web.Infrastructure
{
    public  class PriceChange
    {
        public string getthelowestprice(int productid, int customerId = 0, int Customeroleid = 0)
        {            
            var connection = DataSettingsManager.LoadSettings().ConnectionString;

            using (IDbConnection conn1 = new SqlConnection(connection))
            {

                DynamicParameters ds = new DynamicParameters();

                ds.Add("@ProductId", productid);
                ds.Add("@CustomerId", customerId);
                var result = conn1.Query("GetSpecialPriceByCustomerandPorduct", param: ds, commandType: CommandType.StoredProcedure);
                if (result.Count() > 0)
                {
                    var FinalPrice = result.FirstOrDefault().price;
                    if (FinalPrice > 0)
                    {
                        return string.Format("{0:c}", FinalPrice);
                    }
                }

            }

            using (IDbConnection conn1 = new SqlConnection(connection))
            {

                DynamicParameters ds = new DynamicParameters();

                ds.Add("@prodid", productid);
                ds.Add("@CustomerRoleId", Customeroleid);
                var result = conn1.Query("getpriceasperproductid", param: ds, commandType: CommandType.StoredProcedure);

                return string.Format("{0:c}", result.FirstOrDefault().price != null ? result.FirstOrDefault().price : 0);


            }

            return "";
        }

        public int CheckSpecialPriceDisocunt(int productid, int customerId = 0)
        {
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {
                    DynamicParameters ds = new DynamicParameters();

                    ds.Add("@ProductId", productid);
                    ds.Add("@CustomerId", customerId);
                    var result = conn1.Query("GetSpecialPriceByCustomerandPorduct", param: ds, commandType: CommandType.StoredProcedure);
                    if (result.Any())
                    {
                        var finalPrice = result.FirstOrDefault().price;
                        if (finalPrice > 0)
                        {
                            var price = result.FirstOrDefault().price;
                            if (price > 0)
                            {
                                return 1;
                            }
                        }
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public int GetAssoicateProductid(string AttributesXml)
        {

            int productId = 0;
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@AttributesXml", AttributesXml);

                    var result = conn1.Query("GetchildProductId", param: ds, commandType: CommandType.StoredProcedure);

                    return result.FirstOrDefault().ChildSku;


                }
            }
            catch (Exception ex)
            {
                return productId;
            }
        }

        public string GetPromotionContent(string sku, int productID, string attributeinfo, int actionname)
        {
            string isPromotion = null;
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var dt = new DataTable();
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            if (actionname != 0 && attributeinfo != "")
            {

                var cmd = new SqlCommand("select sku from ProductAttributeCombination where AttributesXml =(select AttributesXml from ShoppingCartItem where id=" + actionname + ")", conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                try
                {
                    da = new SqlDataAdapter(cmd);
                    dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        sku = (dt.Rows[0]["sku"]).ToString();
                    }
                }

                catch (Exception ex)
                {
                    return null;
                }
            }
            if (attributeinfo == "")
            {
                var cmd = new SqlCommand("SELECT PromotionContent FROM Product where IsPromotion=1 and  id =" + productID + "", conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                try
                {
                    da = new SqlDataAdapter(cmd);
                    dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        isPromotion = (dt.Rows[0]["PromotionContent"]).ToString();
                    }
                }

                catch (Exception ex)
                {
                    return null;
                }
            }
            else
            {

                var cmd = new SqlCommand("SELECT PromotionContent FROM Product where IsPromotion=1 and  sku ='" + sku + "'", conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                try
                {
                    da = new SqlDataAdapter(cmd);
                    dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        isPromotion = (dt.Rows[0]["PromotionContent"]).ToString();
                    }
                }

                catch (Exception ex)
                {
                    return null;
                }
            }

            conn.Close();

            //string isPromotion = null;
            //var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            //var dt = new DataTable();
            //try
            //{
            //    var conn = new SqlConnection(connstring);
            //    SqlDataAdapter da = new SqlDataAdapter();
            //    conn.Open();



            //    var Customercmd = new SqlCommand("SELECT * from Customer_CustomerRole_Mapping where CustomerRole_Id in (41,42) and Customer_Id='" + Cusid + "'", conn);
            //    Customercmd.CommandType = CommandType.Text;
            //    Customercmd.CommandTimeout = 0;


            //    da = new SqlDataAdapter(Customercmd);
            //    dt = new DataTable();
            //    da.Fill(dt);

            //    if (dt.Rows.Count > 0)
            //    {
            //        if (sku == null && productID != 0)
            //        {
            //            string Parentsku = null;
            //            var productsku = new SqlCommand("select sku  from ProductAttributeCombination where AttributesXml =( select AttributesXml from ShoppingCartItem where id='" + productID + "')", conn);
            //            productsku.CommandType = CommandType.Text;
            //            productsku.CommandTimeout = 0;
            //            da = new SqlDataAdapter(productsku);
            //            dt = new DataTable();
            //            da.Fill(dt);
            //            if (dt.Rows.Count > 0)
            //            {
            //                Parentsku = (dt.Rows[0]["sku"]).ToString();
            //            }

            //            if (Parentsku == null)
            //            {
            //                var parentsku = new SqlCommand(" select sku  from Product where Id =( select ProductId from ShoppingCartItem where id ='" + productID + "')", conn);
            //                parentsku.CommandType = CommandType.Text;
            //                parentsku.CommandTimeout = 0;
            //                da = new SqlDataAdapter(parentsku);
            //                dt = new DataTable();
            //                da.Fill(dt);
            //                if (dt.Rows.Count > 0)
            //                {
            //                    Parentsku = (dt.Rows[0]["sku"]).ToString();
            //                }
            //            }
            //            sku = Parentsku;

            //        }
            //        if (languageid == 1)
            //        {
            //            var cmd = new SqlCommand("SELECT PromotionContent FROM Product where IsPromotion=1 and  Sku ='" + sku + "'", conn);
            //            cmd.CommandType = CommandType.Text;
            //            cmd.CommandTimeout = 0;
            //            try
            //            {
            //                da = new SqlDataAdapter(cmd);
            //                dt = new DataTable();
            //                da.Fill(dt);

            //                if (dt.Rows.Count > 0)
            //                {
            //                    isPromotion = (dt.Rows[0]["PromotionContent"]).ToString();
            //                }
            //            }

            //            catch (Exception ex)
            //            {
            //                return null;
            //            }
            //        }
            //        else
            //        {
            //            try
            //            {
            //                var cmd = new SqlCommand("SELECT id FROM Product where IsPromotion=1 and  Sku ='" + sku + "'", conn);
            //                cmd.CommandType = CommandType.Text;
            //                cmd.CommandTimeout = 0;

            //                da = new SqlDataAdapter(cmd);
            //                dt = new DataTable();
            //                da.Fill(dt);
            //                if (dt.Rows.Count > 0)
            //                {
            //                    int Mainproductid = Convert.ToInt32(dt.Rows[0]["id"]);
            //                    var LocalizedPropertycmd = new SqlCommand("SELECT LocaleValue FROM LocalizedProperty where LocaleKey='PromotionContentForFrench' and LocaleKeyGroup='Promotion' and  EntityId ='" + Mainproductid + "'", conn);
            //                    LocalizedPropertycmd.CommandType = CommandType.Text;
            //                    LocalizedPropertycmd.CommandTimeout = 0;

            //                    da = new SqlDataAdapter(LocalizedPropertycmd);
            //                    dt = new DataTable();
            //                    da.Fill(dt);

            //                    if (dt.Rows.Count > 0)
            //                    {
            //                        isPromotion = (dt.Rows[0]["LocaleValue"]).ToString();
            //                    }
            //                }

            //            }
            //            catch (Exception ex)
            //            {
            //                return null;
            //            }
            //        }
            //    }
            //    conn.Close();
            //}
            //catch (Exception ex)
            //{
            //    return null;
            //}

            return isPromotion;


        }
        public int PromotionForFrench(string FrenchContent, int Productid)
        {

            int productId = 0;
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@FrenchContent", FrenchContent);
                    ds.Add("@Productid", Productid);
                    var result = conn1.Query("insertpromotionforfrench", param: ds, commandType: CommandType.StoredProcedure);

                    return result.FirstOrDefault().ChildSku;


                }
            }
            catch (Exception ex)
            {
                return productId;
            }
        }
        public string GetPromotionForFrench(int CustomerId, string sku, int Productid)
        {

            string FrenchContent = "";
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@CustomerId", CustomerId);
                    ds.Add("@sku", sku);
                    ds.Add("@Productid", Productid);
                    var result = conn1.Query("GetPromotionForFrench", param: ds, commandType: CommandType.StoredProcedure);

                    return result.First().LocaleValue;


                }
            }
            catch (Exception ex)
            {
                return FrenchContent;
            }
        }

        public class SemiNumericComparer : IComparer<string>
        {
            /// <summary>
            /// Method to determine if a string is a number
            /// </summary>
            /// <param name="value">String to test</param>
            /// <returns>True if numeric</returns>
            public static bool IsNumeric(string value)
            {
                return int.TryParse(value, out _);
            }

            /// <inheritdoc />
            public int Compare(string s1, string s2)
            {
                const int S1GreaterThanS2 = 1;
                const int S2GreaterThanS1 = -1;

                var IsNumeric1 = IsNumeric(s1);
                var IsNumeric2 = IsNumeric(s2);

                if (IsNumeric1 && IsNumeric2)
                {
                    var i1 = Convert.ToInt32(s1);
                    var i2 = Convert.ToInt32(s2);

                    if (i1 > i2)
                    {
                        return S1GreaterThanS2;
                    }

                    if (i1 < i2)
                    {
                        return S2GreaterThanS1;
                    }

                    return 0;
                }

                if (IsNumeric1)
                {
                    return S2GreaterThanS1;
                }

                if (IsNumeric2)
                {
                    return S1GreaterThanS2;
                }

                return string.Compare(s1, s2, true, CultureInfo.InvariantCulture);
            }
        }
        public class StrCmpLogicalComparer : Comparer<string>
        {
            [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
            private static extern int StrCmpLogicalW(string x, string y);

            public override int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }
        public int Getimportedcustomer(int customerid)
        {

            int Content = 0;
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;
                var conn = new SqlConnection(connection);
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@CustomerId", customerid);
                    var result = conn1.Query("Getimportedcustomer", param: ds, commandType: CommandType.StoredProcedure);
                    conn.Close();
                    return result.Count();


                }
            }
            catch (Exception ex)
            {
                return Content;
            }
        }

        public string checkdiscountpromotio(int customerid,string discountcode)
        {

            string Content = "";
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;
                var conn = new SqlConnection(connection);
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@Customerid", customerid);
                    ds.Add("@discountcode", discountcode);
                    var result = conn1.Query<string>("sp_checkpromotiondiscount", param: ds, commandType: CommandType.StoredProcedure);
                    conn.Close();
                    return result.FirstOrDefault();


                }
            }
            catch (Exception ex)
            {
                return Content;
            }
        }

        public int Gettheemailcount(string Name)
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                var Customercmd = new SqlCommand("select count(*) from QueuedEmail where Subject ='" + Name + "' and CreatedOnUtc = GETDATE()", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    int aa = Convert.ToInt32(dt.Rows[0].ItemArray[0]);
                    return aa;

                }
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
            finally
            {
                conn.Close();
            }
        }

        public string Getthefreeproductsetting()
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                var Customercmd = new SqlCommand("select Value from Setting where Name ='customerdiscountasperpricerange'", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var aa = Convert.ToString(dt.Rows[0].ItemArray[0]);
                    return aa;

                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                conn.Close();
            }
        }
        public string forrecurring(string productID)
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                var Customercmd = new SqlCommand("truncate table RecurringProductOrderMapping    Insert into RecurringProductOrderMapping  values('" + productID + "')", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var aa = Convert.ToString(dt.Rows[0].ItemArray[0]);
                    return aa;

                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                conn.Close();
            }
        }
        public string Gettierprice(int productid, int customerroleid)
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                //var Customercmd = new SqlCommand("select count(*) from QueuedEmail where Subject ='" + Name + "' and CreatedOnUtc = GETDATE()", conn);
                var Customercmd = new SqlCommand("select Price from TierPrice where ProductId='" + productid + "' and CustomerRoleId ='" + customerroleid + "'", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var aa = dt.Rows[0].ItemArray[0];
                    return string.Format("{0:c}", aa);


                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                conn.Close();
            }
        }
        public List<string> AdminAssociateBpCode()
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            //var currcustomer = "C80040";
            using (var conn = new SqlConnection(connstring))
            {

                {
                    conn.Open();
                    var query = "select AdminBPCode from AssociatedCustomer_mapping";
                    var result = conn.Query<string>(query);

                    return result.ToList();
                }
            }
        }

        public List<distributnew> distributordetails(string rolename)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            //var currcustomer = "C80040";
            using (var conn = new SqlConnection(connstring))
            {

                {
                    conn.Open();
                    var query = "SELECT * FROM [SS_SL_Shop] WHERE FullDescription LIKE '%" + rolename + "%'";
                    var result = conn.Query<distributordetailsModel>(query);
                    var totalPriceByCategory = result
                                              .GroupBy(cp => cp.Tags)
                                              .Select(group => new distributnew
                                              {
                                                  Tags = group.Key,
                                                  ShortDescriptionWithIds = group
                                                      .Select(cp => new ShortDescriptionWithId
                                                      {
                                                          Id = cp.id,
                                                          ShortDescription = cp.ShortDescription
                                                      }),

                                                  NameWithIds = group
                                                      .Select(cp => new NameWithId
                                                      {
                                                          Id = cp.id,
                                                          Name = cp.Name
                                                      }),
                                              })
                                              .ToList();

                    return totalPriceByCategory;
                }
            }
        }
        public List<string> distributedetaiswithrole(string rolename)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();
                var query = "SELECT id FROM [SS_SL_Shop] WHERE FullDescription LIKE '%" + rolename + "%'";

                // Use Query<string> to ensure the result is a List<string>
                var result = conn.Query<string>(query);

                return result.ToList();
            }
        }

        public List<string> checkproductrole(int roleid, int productid)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();
                var query = "select CustomerRoleId from AclRecord where EntityName ='Product' and EntityId =" + productid + " and CustomerRoleId=" + roleid + "";

                // Use Query<string> to ensure the result is a List<string>
                var result = conn.Query<string>(query);

                return result.ToList();
            }
        }
        public string getdocumentdetail( int productid)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();
                var query = "select DownloadUrl from SS_A_AttachmentDownload where Id in (Select DownloadId from SS_A_Attachment where Visible=1 and id in (select EntityId from SS_MAP_EntityMapping where mappedentityid=" + productid+" and [entitytype]=5))";

                // Use Query<string> to ensure the result is a List<string>
                var result = conn.Query<string>(query);

                return result.FirstOrDefault();
            }
        }
        public partial record distributordetailsModel
        {
            public int id { get; set; }
            public string Name { get; set; }
            public string Latitude { get; set; }

            public string Longitude { get; set; }
            public string ShortDescription { get; set; }
            public string Tags { get; set; }
            public string FullDescription { get; set; }

        }
        public partial record distributnew
        {
            public string Tags { get; set; }

            public IEnumerable<ShortDescriptionWithId> ShortDescriptionWithIds { get; set; }
            public IEnumerable<NameWithId> NameWithIds { get; set; }
            public IEnumerable<int> id { get; set; }


        }
        public class ShortDescriptionWithId
        {
            public int Id { get; set; }
            public string ShortDescription { get; set; }
        }
        public class NameWithId
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

    }

}
