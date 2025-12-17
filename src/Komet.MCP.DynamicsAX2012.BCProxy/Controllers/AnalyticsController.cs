using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Http;
using Komet.MCP.DynamicsAX2012.Core.Models;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    /// <summary>
    /// Analytics endpoints using direct SQL queries on AX database
    /// No X++ or AIF development required
    /// </summary>
    [RoutePrefix("api/analytics")]
    public class AnalyticsController : ApiController
    {
        private readonly string _sqlConnectionString;

        public AnalyticsController()
        {
            _sqlConnectionString = System.Configuration.ConfigurationManager.AppSettings["AX_SQL_CONNECTION"] 
                ?? throw new InvalidOperationException("AX_SQL_CONNECTION not configured");
        }

        /// <summary>
        /// Get top customers by sales amount for a given year
        /// </summary>
        [HttpGet]
        [Route("top-customers")]
        public IHttpActionResult GetTopCustomers(
            [FromUri] string company = "GBL",
            [FromUri] int year = 2024,
            [FromUri] int top = 10,
            [FromUri] string? city = null)
        {
            try
            {
                var results = new List<object>();

                using (var connection = new SqlConnection(_sqlConnectionString))
                {
                    connection.Open();

                    var sql = @"
                        WITH CustomerSales AS (
                            SELECT 
                                c.ACCOUNTNUM,
                                c.PARTY,
                                SUM(sl.LINEAMOUNT) as TotalSales,
                                COUNT(DISTINCT st.SALESID) as OrderCount
                            FROM CUSTTABLE c WITH (NOLOCK)
                            INNER JOIN SALESTABLE st WITH (NOLOCK)
                                ON st.CUSTACCOUNT = c.ACCOUNTNUM
                                AND st.DATAAREAID = c.DATAAREAID
                                AND YEAR(st.CREATEDDATETIME) = @Year
                            INNER JOIN SALESLINE sl WITH (NOLOCK)
                                ON sl.SALESID = st.SALESID
                                AND sl.DATAAREAID = st.DATAAREAID
                            WHERE c.DATAAREAID = @Company
                            GROUP BY c.ACCOUNTNUM, c.PARTY
                        ),
                        PrimaryBusinessAddress AS (
                            SELECT 
                                dpl.PARTY,
                                addr.CITY,
                                ROW_NUMBER() OVER (PARTITION BY dpl.PARTY ORDER BY 
                                    CASE WHEN dpl.ISPRIMARY = 1 THEN 0 ELSE 1 END,
                                    CASE WHEN dpl.ISROLEBUSINESS = 1 THEN 0 ELSE 1 END,
                                    dpl.RECID) as RowNum
                            FROM DIRPARTYLOCATION dpl WITH (NOLOCK)
                            LEFT JOIN LOGISTICSLOCATION loc WITH (NOLOCK)
                                ON loc.RECID = dpl.LOCATION
                            LEFT JOIN LOGISTICSPOSTALADDRESS addr WITH (NOLOCK)
                                ON addr.LOCATION = loc.RECID
                            WHERE addr.CITY IS NOT NULL AND addr.CITY <> ''
                        )
                        SELECT TOP (@Top)
                            cs.ACCOUNTNUM as AccountNum,
                            dp.NAME as CustomerName,
                            ISNULL(pba.CITY, '') as City,
                            cs.TotalSales,
                            cs.OrderCount
                        FROM CustomerSales cs
                        INNER JOIN DIRPARTYTABLE dp WITH (NOLOCK)
                            ON dp.RECID = cs.PARTY
                        LEFT JOIN PrimaryBusinessAddress pba
                            ON pba.PARTY = cs.PARTY
                            AND pba.RowNum = 1
                        WHERE (@City IS NULL OR pba.CITY LIKE '%' + @City + '%')
                        ORDER BY cs.TotalSales DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Top", top);
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Company", company);
                        command.Parameters.AddWithValue("@City", city ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new
                                {
                                    accountNum = reader["AccountNum"].ToString(),
                                    customerName = reader["CustomerName"].ToString(),
                                    city = reader["City"].ToString(),
                                    totalSales = Convert.ToDecimal(reader["TotalSales"]),
                                    orderCount = Convert.ToInt32(reader["OrderCount"])
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    year,
                    top,
                    city,
                    company,
                    customerCount = results.Count,
                    customers = results
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    error = "Error executing analytics query",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get customer count grouped by city
        /// </summary>
        [HttpGet]
        [Route("customers-by-city")]
        public IHttpActionResult GetCustomersByCity([FromUri] string company = "GBL")
        {
            try
            {
                var results = new List<object>();

                using (var connection = new SqlConnection(_sqlConnectionString))
                {
                    connection.Open();

                    var sql = @"
                        SELECT 
                            addr.CITY as City,
                            COUNT(DISTINCT c.ACCOUNTNUM) as CustomerCount
                        FROM CUSTTABLE c WITH (NOLOCK)
                        LEFT JOIN DIRPARTYLOCATION dpl WITH (NOLOCK)
                            ON dpl.PARTY = c.PARTY
                            AND dpl.ISPRIMARY = 1
                        LEFT JOIN LOGISTICSLOCATION loc WITH (NOLOCK)
                            ON loc.RECID = dpl.LOCATION
                        LEFT JOIN LOGISTICSPOSTALADDRESS addr WITH (NOLOCK)
                            ON addr.LOCATION = loc.RECID
                        WHERE c.DATAAREAID = @Company
                            AND addr.CITY IS NOT NULL
                            AND addr.CITY <> ''
                        GROUP BY addr.CITY
                        ORDER BY CustomerCount DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Company", company);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new
                                {
                                    city = reader["City"].ToString(),
                                    customerCount = Convert.ToInt32(reader["CustomerCount"])
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    company,
                    cities = results.Count,
                    data = results
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    error = "Error executing analytics query",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get sales statistics for a time period
        /// </summary>
        [HttpGet]
        [Route("sales-stats")]
        public IHttpActionResult GetSalesStats(
            [FromUri] string company = "GBL",
            [FromUri] string fromDate = null,
            [FromUri] string toDate = null)
        {
            try
            {
                DateTime from = string.IsNullOrEmpty(fromDate) 
                    ? new DateTime(DateTime.Now.Year, 1, 1) 
                    : DateTime.Parse(fromDate);
                
                DateTime to = string.IsNullOrEmpty(toDate) 
                    ? DateTime.Now 
                    : DateTime.Parse(toDate);

                using (var connection = new SqlConnection(_sqlConnectionString))
                {
                    connection.Open();

                    var sql = @"
                        SELECT 
                            COUNT(DISTINCT st.SALESID) as OrderCount,
                            SUM(sl.LINEAMOUNT) as TotalRevenue,
                            AVG(sl.LINEAMOUNT) as AvgLineAmount,
                            COUNT(DISTINCT st.CUSTACCOUNT) as UniqueCustomers
                        FROM SALESTABLE st WITH (NOLOCK)
                        INNER JOIN SALESLINE sl WITH (NOLOCK)
                            ON sl.SALESID = st.SALESID
                            AND sl.DATAAREAID = st.DATAAREAID
                        WHERE st.DATAAREAID = @Company
                            AND st.CREATEDDATETIME >= @FromDate
                            AND st.CREATEDDATETIME < DATEADD(day, 1, @ToDate)";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Company", company);
                        command.Parameters.AddWithValue("@FromDate", from);
                        command.Parameters.AddWithValue("@ToDate", to);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var orderCount = Convert.ToInt32(reader["OrderCount"]);
                                var totalRevenue = reader["TotalRevenue"] != DBNull.Value 
                                    ? Convert.ToDecimal(reader["TotalRevenue"]) 
                                    : 0m;
                                var avgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0m;

                                return Ok(new
                                {
                                    company,
                                    period = new
                                    {
                                        from = from.ToString("yyyy-MM-dd"),
                                        to = to.ToString("yyyy-MM-dd")
                                    },
                                    orderCount,
                                    totalRevenue,
                                    avgOrderValue,
                                    avgLineAmount = reader["AvgLineAmount"] != DBNull.Value 
                                        ? Convert.ToDecimal(reader["AvgLineAmount"]) 
                                        : 0m,
                                    uniqueCustomers = Convert.ToInt32(reader["UniqueCustomers"])
                                });
                            }
                        }
                    }
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    error = "Error executing analytics query",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get top products by sales for a given year
        /// </summary>
        [HttpGet]
        [Route("top-products")]
        public IHttpActionResult GetTopProducts(
            [FromUri] string company = "GBL",
            [FromUri] int year = 2024,
            [FromUri] int top = 10)
        {
            try
            {
                var results = new List<object>();

                using (var connection = new SqlConnection(_sqlConnectionString))
                {
                    connection.Open();

                    var sql = @"
                        SELECT TOP (@Top)
                            inv.ITEMID as ItemId,
                            ISNULL(trans.NAME, inv.NAMEALIAS) as ItemName,
                            SUM(sl.LINEAMOUNT) as TotalSales,
                            SUM(sl.SALESQTY) as TotalQuantity,
                            COUNT(DISTINCT sl.SALESID) as OrderCount
                        FROM INVENTTABLE inv WITH (NOLOCK)
                        INNER JOIN SALESLINE sl WITH (NOLOCK)
                            ON sl.ITEMID = inv.ITEMID
                            AND sl.DATAAREAID = inv.DATAAREAID
                        INNER JOIN SALESTABLE st WITH (NOLOCK)
                            ON st.SALESID = sl.SALESID
                            AND st.DATAAREAID = sl.DATAAREAID
                            AND YEAR(st.CREATEDDATETIME) = @Year
                        LEFT JOIN ECORESPRODUCTTRANSLATION trans WITH (NOLOCK)
                            ON trans.PRODUCT = inv.PRODUCT
                            AND trans.LANGUAGEID = 'de'
                        WHERE inv.DATAAREAID = @Company
                        GROUP BY inv.ITEMID, inv.NAMEALIAS, trans.NAME
                        ORDER BY TotalSales DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Top", top);
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Company", company);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new
                                {
                                    itemId = reader["ItemId"].ToString(),
                                    itemName = reader["ItemName"].ToString(),
                                    totalSales = Convert.ToDecimal(reader["TotalSales"]),
                                    totalQuantity = Convert.ToDecimal(reader["TotalQuantity"]),
                                    orderCount = Convert.ToInt32(reader["OrderCount"])
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    year,
                    top,
                    company,
                    productCount = results.Count,
                    products = results
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    error = "Error executing analytics query",
                    message = ex.Message
                });
            }
        }
    }
}
