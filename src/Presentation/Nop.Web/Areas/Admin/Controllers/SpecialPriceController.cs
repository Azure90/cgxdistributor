using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class SpecialPriceController : BaseAdminController
    {
        [IgnoreAntiforgeryToken]
        public IActionResult Index()
        {
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            SpecialPriceModel model = new SpecialPriceModel();
            using (IDbConnection conn1 = new SqlConnection(connection))
            {
                DynamicParameters ds = new DynamicParameters();
                ds.Add("@searchBy", null);
                var SpecialPriceresult = conn1.Query("GetSpecialPrice", param: ds, commandType: CommandType.StoredProcedure);
                foreach (var data in SpecialPriceresult)
                {
                    SpecialPriceModel Newmodel = new SpecialPriceModel();
                    Newmodel.ProductName = data.ProductName;
                    Newmodel.SKU = data.SKU;
                    Newmodel.CustomerName = data.CustomerName;
                    Newmodel.BPCode = data.BPCode;
                    Newmodel.Price = data.Price;
                    Newmodel.ValidTo = data.ValidTo;
                    Newmodel.ValidFrom = data.ValidFrom;
                    model.ModelList.Add(Newmodel);
                }
            }


            int totalRecord = 100;
            int filterRecord = 0;
            int pageSize = 50;
            int skip = 16000;
            var obj = model.ModelList;
            totalRecord = obj.Count();
            filterRecord = obj.Count();
            var empList = obj.Skip(skip).Take(pageSize).ToList();
            model.recordtotal = totalRecord;
            model.recordsFiltered = filterRecord;
            model.data = empList;

            return View(model);
        }
        [IgnoreAntiforgeryToken]
        public JsonResult CustomServerSideSearchAction(DataTableAjaxPostModel model1)
        {
            // action inside a standard controller
            int filteredResultsCount;
            int totalResultsCount;
            var res = YourCustomSearchFunc(model1, out filteredResultsCount, out totalResultsCount);

            var result = new List<SpecialPriceModel>(res.Count);
            foreach (var s in res)
            {
                // simple remapping adding extra info to found dataset
                result.Add(new SpecialPriceModel
                {
                    CustomerName = s.CustomerName,
                    BPCode = s.BPCode,
                    ProductName = s.ProductName,
                    SKU = s.SKU,
                    Price = s.Price,
                    ValidFrom = s.ValidFrom,
                    ValidTo = s.ValidTo,

                });
            };
            //var connection = DataSettingsManager.LoadSettings().ConnectionString;
            //SpecialPriceModel model = new SpecialPriceModel();
            //using (IDbConnection conn1 = new SqlConnection(connection))
            //{
            //    DynamicParameters ds = new DynamicParameters();
            //    var SpecialPriceresult = conn1.Query("GetSpecialPrice", param: ds, commandType: CommandType.StoredProcedure);
            //    foreach (var data in SpecialPriceresult)
            //    {
            //        SpecialPriceModel Newmodel = new SpecialPriceModel();
            //        Newmodel.ProductName = data.ProductName;
            //        Newmodel.SKU = data.SKU;
            //        Newmodel.CustomerName = data.CustomerName;
            //        Newmodel.BPCode = data.BPCode;
            //        Newmodel.Price = data.Price;
            //        Newmodel.ValidTo = data.ValidTo;
            //        Newmodel.ValidFrom = data.ValidFrom;
            //        model.ModelList.Add(Newmodel);
            //    }
            //}

            return Json(new
            {
                // this is what datatables wants sending back
                draw = model1.draw,
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            });
        }

        public IList<SpecialPriceModel> YourCustomSearchFunc(DataTableAjaxPostModel model, out int filteredResultsCount, out int totalResultsCount)
        {
            var searchBy = (model.search != null) ? model.search.value : null;
            var take = model.length;
            var skip = model.start;

            string sortBy = "";
            bool sortDir = true;

            if (model.order != null)
            {
                // in this example we just default sort on the 1st column
                sortBy = model.columns[model.order[0].column].data;
                sortDir = model.order[0].dir.ToLower() == "asc";
            }

            // search the dbase taking into consideration table sorting and paging
            var result = GetDataFromDbase(searchBy, take, skip, sortBy, sortDir, out filteredResultsCount, out totalResultsCount);
            if (result == null)
            {
                // empty collection...
                return new List<SpecialPriceModel>();
            }
            return result;
        }
        public List<SpecialPriceModel> GetDataFromDbase(string searchBy, int take, int skip, string sortBy, bool sortDir, out int filteredResultsCount, out int totalResultsCount)
        {
            // the example datatable used is not supporting multi column ordering
            // so we only need get the column order from the first column passed to us.        
            //var whereClause = BuildDynamicWhereClause(Db, searchBy);

            if (String.IsNullOrEmpty(searchBy))
            {
                // if we have an empty search then just order the results by Id ascending
                sortBy = "Id";
                sortDir = true;
            }
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            SpecialPriceModel model = new SpecialPriceModel();
            using (IDbConnection conn1 = new SqlConnection(connection))
            {
                DynamicParameters ds = new DynamicParameters();
                ds.Add("@searchBy", searchBy);
                var SpecialPriceresult = conn1.Query("GetSpecialPrice", param: ds, commandType: CommandType.StoredProcedure);

                var result = SpecialPriceresult
               //.Where(x => x.SKU.Contains(searchBy) || x.CustomerName.Contains(searchBy)|| x.BPCode.Contains(searchBy)  || x.ProductName.Contains(searchBy))
               .Select(m => new SpecialPriceModel
               {
                   CustomerId = m.CustomerId,
                   BPCode = m.BPCode,
                   SKU = m.SKU,
                   CustomerName = m.CustomerName,
                   ProductName = m.ProductName,
                   Price = m.Price,
                   ValidFrom = m.ValidFrom,
                   ValidTo = m.ValidTo,

               })
               // .OrderBy(sortBy, sortDir) // have to give a default order when skipping .. so use the PK
               .Skip(skip)
               .Take(take)
               .ToList();


                // now just get the count of items (without the skip and take) - eg how many could be returned with filtering
                filteredResultsCount = SpecialPriceresult.Count();
                totalResultsCount = SpecialPriceresult.Count();
                return result;
            }

        }

        //private Expression<Func<SpecialPriceModel, bool>> BuildDynamicWhereClause(SpecialPriceModel entities, string searchValue)
        //{
        //    simple method to dynamically plugin a where clause
        //    var predicate = PredicateBuilder.New<SpecialPriceModel>(true); // true -where(true) return all
        //    if (String.IsNullOrWhiteSpace(searchValue) == false)
        //    {
        //         as we only have 2 cols allow the user type in name 'firstname lastname' then use the list to search the first and last name of dbase
        //        var searchTerms = searchValue.Split(' ').ToList().ConvertAll(x => x.ToLower());

        //        entities = entities.ModelList.Where(s => searchTerms.Any(srch => s.Firstname.ToLower().Contains(srch)));
        //        entities = entities.entities.Or(s => searchTerms.Any(srch => s.Lastname.ToLower().Contains(srch)));
        //    }
        //    return predicate;
        //}

        //[SpecialPriceid], [ProductId], [CustomerId], [Price], [ValidTo], [ValidFrom]
        public class SpecialPriceModel
        {
            public int SpecialPriceid { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string SKU { get; set; }
            public int CustomerId { get; set; }
            public string BPCode { get; set; }
            public string CustomerName { get; set; }
            public decimal? Price { get; set; }
            public DateTime? ValidTo { get; set; }
            public DateTime? ValidFrom { get; set; }
            public int recordtotal { get; set; }
            public int recordsFiltered { get; set; }
            public List<SpecialPriceModel> data { get; set; }
            public List<SpecialPriceModel> ModelList { get; set; }
            public SpecialPriceModel()
            {
                ModelList = new List<SpecialPriceModel>();
            }

        }
        public class DataTableAjaxPostModel
        {
            // properties are not capital due to json mapping
            public int draw { get; set; }
            public int start { get; set; }
            public int length { get; set; }
            public List<Column> columns { get; set; }
            public Search search { get; set; }
            public List<Order> order { get; set; }
        }
        public class Column
        {
            public string data { get; set; }
            public string name { get; set; }
            public bool searchable { get; set; }
            public bool orderable { get; set; }
            public Search search { get; set; }
        }

        public class Search
        {
            public string value { get; set; }
            public string regex { get; set; }
        }

        public class Order
        {
            public int column { get; set; }
            public string dir { get; set; }
        }
    }
}
