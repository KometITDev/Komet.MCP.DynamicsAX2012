using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Komet.MCP.DynamicsAX2012.Core.Models;
using Microsoft.Dynamics.BusinessConnectorNet;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Services
{
    /// <summary>
    /// Service for interacting with Dynamics AX 2012 via Business Connector
    /// </summary>
    public class BusinessConnectorService : IDisposable
    {
        private Axapta _axapta;
        private bool _isLoggedIn;
        private readonly object _lock = new object();
        
        // AX Server configuration - can be overridden via environment variables or config
        private static readonly string AosServer = Environment.GetEnvironmentVariable("AX_AOS_SERVER") ?? "IT-TEST-ERP3CU";
        
        // SQL Server connection string for direct database queries
        private static readonly string SqlConnectionString = Environment.GetEnvironmentVariable("AX_SQL_CONNECTION") 
            ?? "Server=IT-TEST-ERP3CU;Database=MicrosoftDynamicsGBLAX;Integrated Security=True;";

        public BusinessConnectorService()
        {
            _axapta = new Axapta();
        }

        /// <summary>
        /// Ensures connection to AX is established
        /// </summary>
        public void EnsureLoggedIn(string company = null)
        {
            lock (_lock)
            {
                if (!_isLoggedIn)
                {
                    // Logon with server name only (like PowerShell AXConnector)
                    // Format: Logon(company, language, aos, configuration)
                    string axCompany = string.IsNullOrEmpty(company) ? "gbl" : company.ToLower();
                    _axapta.Logon(axCompany, "de", AosServer, "");
                    _isLoggedIn = true;
                }
            }
        }

        /// <summary>
        /// Get customer by account number using X++ query
        /// </summary>
        public CustomerInfo GetCustomer(string accountNum, string company, bool includeAddresses = false, bool includeContacts = false)
        {
            EnsureLoggedIn(company);

            var customer = new CustomerInfo
            {
                AccountNum = accountNum,
                Company = company
            };

            try
            {
                // Query CustTable
                using (var custTable = _axapta.CreateAxaptaRecord("CustTable"))
                {
                    custTable.ExecuteStmt($"select * from %1 where %1.AccountNum == '{accountNum}'");

                    if (custTable.Found)
                    {
                        customer.Name = GetFieldValue<string>(custTable, "Name") ?? string.Empty;
                        customer.CustomerGroup = GetFieldValue<string>(custTable, "CustGroup") ?? string.Empty;
                        customer.Currency = GetFieldValue<string>(custTable, "Currency") ?? string.Empty;
                        customer.PaymentTerms = GetFieldValue<string>(custTable, "PaymTermId") ?? string.Empty;
                        customer.PaymentMethod = GetFieldValue<string>(custTable, "PaymMode") ?? string.Empty;
                        customer.DeliveryMode = GetFieldValue<string>(custTable, "DlvMode") ?? string.Empty;
                        customer.DeliveryTerms = GetFieldValue<string>(custTable, "DlvTerm") ?? string.Empty;
                        customer.VatNumber = GetFieldValue<string>(custTable, "VATNum") ?? string.Empty;
                        customer.TaxGroup = GetFieldValue<string>(custTable, "TaxGroup") ?? string.Empty;
                        customer.CreditMax = GetFieldValue<decimal?>(custTable, "CreditMax");

                        var blocked = GetFieldValue<int>(custTable, "Blocked");
                        customer.IsBlocked = blocked != 0;

                        // Get Party reference for DirPartyTable lookup
                        var partyId = GetFieldValue<long>(custTable, "Party");
                        if (partyId > 0)
                        {
                            customer.Party = partyId.ToString();
                            LoadPartyDetails(customer, partyId, includeAddresses, includeContacts);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error querying CustTable: {ex.Message}", ex);
            }

            return customer;
        }

        /// <summary>
        /// Get product by item ID using X++ query
        /// </summary>
        public ProductInfo GetProduct(string itemId, string company, string language = "de", bool includeCategories = false)
        {
            EnsureLoggedIn(company);

            var product = new ProductInfo
            {
                ItemId = itemId,
                Company = company
            };
            
            long productRecId = 0;

            try
            {
                
                using (var inventTable = _axapta.CreateAxaptaRecord("InventTable"))
                {
                    inventTable.ExecuteStmt($"select * from %1 where %1.ItemId == '{itemId}'");

                    if (inventTable.Found)
                    {
                        product.NameAlias = GetFieldValue<string>(inventTable, "NameAlias") ?? string.Empty;
                        product.ItemGroupId = GetFieldValue<string>(inventTable, "ItemGroupId") ?? string.Empty;
                        product.StandardConfigId = GetFieldValue<string>(inventTable, "StandardConfigId") ?? string.Empty;
                        
                        // Get Product RecId for EcoResProductTranslation lookup
                        productRecId = GetFieldValue<long>(inventTable, "Product");
                        
                        // Units are stored in InventTableModule - will be loaded separately
                        // product.PrimaryUnitId loaded from InventTableModule
                        // product.InventUnitId, PurchUnitId, SalesUnitId loaded from InventTableModule
                        
                        // Physical properties
                        product.NetWeight = GetFieldValue<decimal?>(inventTable, "NetWeight");
                        product.GrossWeight = GetFieldValue<decimal?>(inventTable, "GrossWeight");
                        product.Depth = GetFieldValue<decimal?>(inventTable, "Depth");
                        product.Width = GetFieldValue<decimal?>(inventTable, "Width");
                        product.Height = GetFieldValue<decimal?>(inventTable, "Height");
                        
                        // Item type (0=Item, 1=BOM, 2=Service)
                        var itemType = GetFieldValue<int>(inventTable, "ItemType");
                        product.ItemType = MapItemType(itemType);
                        
                        // Status
                        var stopped = GetFieldValue<int>(inventTable, "Stopped");
                        product.Stopped = stopped != 0;
                        
                        // Custom Bras fields
                        product.BrasItemIdBulk = GetFieldValue<string>(inventTable, "BrasItemIdBulk") ?? string.Empty;
                        product.BrasOptNumofRevolutions = GetFieldValue<decimal?>(inventTable, "BrasOptNumofRevolutions");
                        product.BrasMaxNumofRevolutions = GetFieldValue<decimal?>(inventTable, "BrasMaxNumofRevolutions");
                        product.BrasProductTypeId = GetFieldValue<string>(inventTable, "BrasProductTypeId") ?? string.Empty;
                        product.BrasPackageExperts = GetFieldValue<string>(inventTable, "BrasPackageExperts") ?? string.Empty;
                        product.BrasPackingContents = GetFieldValue<string>(inventTable, "BrasPackingContents") ?? string.Empty;
                        product.BrasPackingReleasedId = GetFieldValue<string>(inventTable, "BrasPackingReleasedId") ?? string.Empty;
                        product.BrasSterile = GetFieldValue<string>(inventTable, "BrasSterile") ?? string.Empty;
                        product.BrasFigure = GetFieldValue<string>(inventTable, "BrasFigure") ?? string.Empty;
                        product.BrasShank = GetFieldValue<string>(inventTable, "BrasShank") ?? string.Empty;
                        product.BrasSize = GetFieldValue<string>(inventTable, "BrasSize") ?? string.Empty;
                    }
                }
                
                // Load units from InventTableModule (module-specific units)
                try
                {
                    // Inventory Unit (ModuleType = 0)
                    using (var inventModule = _axapta.CreateAxaptaRecord("InventTableModule"))
                    {
                        inventModule.ExecuteStmt($"select * from %1 where %1.ItemId == '{itemId}' && %1.ModuleType == 0");
                        if (inventModule.Found)
                        {
                            product.InventUnitId = GetFieldValue<string>(inventModule, "UnitId") ?? string.Empty;
                        }
                    }
                    
                    // Purchase Unit (ModuleType = 1)
                    using (var purchModule = _axapta.CreateAxaptaRecord("InventTableModule"))
                    {
                        purchModule.ExecuteStmt($"select * from %1 where %1.ItemId == '{itemId}' && %1.ModuleType == 1");
                        if (purchModule.Found)
                        {
                            product.PurchUnitId = GetFieldValue<string>(purchModule, "UnitId") ?? string.Empty;
                        }
                    }
                    
                    // Sales Unit (ModuleType = 2)
                    using (var salesModule = _axapta.CreateAxaptaRecord("InventTableModule"))
                    {
                        salesModule.ExecuteStmt($"select * from %1 where %1.ItemId == '{itemId}' && %1.ModuleType == 2");
                        if (salesModule.Found)
                        {
                            product.SalesUnitId = GetFieldValue<string>(salesModule, "UnitId") ?? string.Empty;
                        }
                    }
                    
                    // PrimaryUnitId fallback to InventUnitId
                    product.PrimaryUnitId = product.InventUnitId;
                }
                catch
                {
                    // Unit loading failed - continue with empty units
                }
                
                // Load product name and description from EcoResProductTranslation (language-specific)
                if (productRecId > 0)
                {
                    using (var translation = _axapta.CreateAxaptaRecord("EcoResProductTranslation"))
                    {
                        translation.ExecuteStmt($"select * from %1 where %1.Product == {productRecId} && %1.LanguageId == '{language}'");
                        
                        if (translation.Found)
                        {
                            product.ItemName = GetFieldValue<string>(translation, "Name") ?? string.Empty;
                            product.Description = GetFieldValue<string>(translation, "Description") ?? string.Empty;
                        }
                        else
                        {
                            // Fallback to default language if requested language not found
                            translation.ExecuteStmt($"select * from %1 where %1.Product == {productRecId}");
                            if (translation.Found)
                            {
                                product.ItemName = GetFieldValue<string>(translation, "Name") ?? string.Empty;
                                product.Description = GetFieldValue<string>(translation, "Description") ?? string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error querying InventTable: {ex.Message}", ex);
            }
            
            // Load categories if requested
            if (includeCategories && productRecId > 0)
            {
                LoadProductCategories(product, productRecId);
            }

            return product;
        }

        private void LoadProductCategories(ProductInfo product, long productRecId)
        {
            product.Categories = new List<ProductCategory>();
            
            try
            {
                // Get category assignments via EcoResProductCategory
                using (var productCategory = _axapta.CreateAxaptaRecord("EcoResProductCategory"))
                {
                    productCategory.ExecuteStmt($"select * from %1 where %1.Product == {productRecId}");
                    
                    while (productCategory.Found)
                    {
                        var categoryId = GetFieldValue<long>(productCategory, "Category");
                        
                        if (categoryId > 0)
                        {
                            // Get category details
                            using (var category = _axapta.CreateAxaptaRecord("EcoResCategory"))
                            {
                                category.ExecuteStmt($"select * from %1 where %1.RecId == {categoryId}");
                                
                                if (category.Found)
                                {
                                    var categoryName = GetFieldValue<string>(category, "Name") ?? string.Empty;
                                    var categoryHierarchy = GetFieldValue<long>(category, "CategoryHierarchy");
                                    
                                    var hierarchyName = string.Empty;
                                    
                                    // Get hierarchy name
                                    if (categoryHierarchy > 0)
                                    {
                                        using (var hierarchy = _axapta.CreateAxaptaRecord("EcoResCategoryHierarchy"))
                                        {
                                            hierarchy.ExecuteStmt($"select * from %1 where %1.RecId == {categoryHierarchy}");
                                            if (hierarchy.Found)
                                            {
                                                hierarchyName = GetFieldValue<string>(hierarchy, "Name") ?? string.Empty;
                                            }
                                        }
                                    }
                                    
                                    product.Categories.Add(new ProductCategory
                                    {
                                        CategoryId = categoryId,
                                        CategoryName = categoryName,
                                        CategoryHierarchyName = hierarchyName
                                    });
                                }
                            }
                        }
                        
                        productCategory.Next();
                    }
                }
            }
            catch
            {
                // Category loading failed - continue with empty categories
            }
        }

        /// <summary>
        /// Search customers by account number or customer group
        /// Uses direct SQL query for better performance with large datasets
        /// </summary>
        public List<CustomerInfo> SearchCustomers(string accountNum, string customerGroup, string company)
        {
            var customers = new List<CustomerInfo>();

            try
            {
                var conditions = new List<string>();
                
                if (!string.IsNullOrEmpty(accountNum))
                {
                    var pattern = accountNum.Replace("*", "%");
                    conditions.Add(accountNum.Contains("*") 
                        ? "c.ACCOUNTNUM LIKE @accountNum" 
                        : "c.ACCOUNTNUM = @accountNum");
                }
                
                if (!string.IsNullOrEmpty(customerGroup))
                {
                    conditions.Add("c.CUSTGROUP = @customerGroup");
                }

                var whereClause = conditions.Count > 0 
                    ? "AND " + string.Join(" AND ", conditions) 
                    : "";

                var sql = $@"
                    SELECT 
                        c.ACCOUNTNUM, dp.NAME, c.CUSTGROUP, c.CURRENCY, c.PARTY
                    FROM CUSTTABLE c
                    INNER JOIN DIRPARTYTABLE dp ON dp.RECID = c.PARTY
                    WHERE c.DATAAREAID = @company
                    {whereClause}";

                using (var connection = new SqlConnection(SqlConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@company", company.ToLower());
                        
                        if (!string.IsNullOrEmpty(accountNum))
                            command.Parameters.AddWithValue("@accountNum", accountNum.Replace("*", "%"));
                        
                        if (!string.IsNullOrEmpty(customerGroup))
                            command.Parameters.AddWithValue("@customerGroup", customerGroup);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var customer = new CustomerInfo
                                {
                                    AccountNum = reader.GetString(reader.GetOrdinal("ACCOUNTNUM")),
                                    Name = reader.IsDBNull(reader.GetOrdinal("NAME")) ? "" : reader.GetString(reader.GetOrdinal("NAME")),
                                    CustomerGroup = reader.IsDBNull(reader.GetOrdinal("CUSTGROUP")) ? "" : reader.GetString(reader.GetOrdinal("CUSTGROUP")),
                                    Currency = reader.IsDBNull(reader.GetOrdinal("CURRENCY")) ? "" : reader.GetString(reader.GetOrdinal("CURRENCY")),
                                    Company = company,
                                    Party = reader.GetInt64(reader.GetOrdinal("PARTY")).ToString()
                                };

                                customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching customers: {ex.Message}", ex);
            }

            return customers;
        }

        /// <summary>
        /// Search customers by postal address (ZipCode and/or City)
        /// Uses direct SQL query for efficient join across tables
        /// </summary>
        public List<CustomerInfo> SearchCustomersByAddress(string zipCode, string city, string company)
        {
            EnsureLoggedIn(company);
            var customers = new List<CustomerInfo>();

            try
            {
                // Build SQL WHERE clause
                var conditions = new List<string>();
                if (!string.IsNullOrEmpty(zipCode))
                {
                    var pattern = zipCode.Replace("*", "%");
                    conditions.Add(zipCode.Contains("*") 
                        ? $"pa.ZIPCODE LIKE @zipCode" 
                        : $"pa.ZIPCODE = @zipCode");
                }
                if (!string.IsNullOrEmpty(city))
                {
                    var pattern = city.Replace("*", "%");
                    conditions.Add(city.Contains("*") 
                        ? $"pa.CITY LIKE @city" 
                        : $"pa.CITY = @city");
                }

                var whereClause = conditions.Count > 0 
                    ? "WHERE " + string.Join(" AND ", conditions) 
                    : "";

                // SQL query joining CustTable -> DirPartyTable -> DirPartyLocation -> LogisticsPostalAddress
                var sql = $@"
                    SELECT DISTINCT 
                        c.ACCOUNTNUM, dp.NAME, c.CUSTGROUP, c.CURRENCY, c.PARTY,
                        pa.ZIPCODE, pa.CITY, pa.STREET, pa.COUNTRYREGIONID
                    FROM CUSTTABLE c
                    INNER JOIN DIRPARTYTABLE dp ON dp.RECID = c.PARTY
                    INNER JOIN DIRPARTYLOCATION dpl ON dpl.PARTY = c.PARTY
                    INNER JOIN LOGISTICSPOSTALADDRESS pa ON pa.LOCATION = dpl.LOCATION
                    {whereClause}
                    AND c.DATAAREAID = @company";

                using (var connection = new SqlConnection(SqlConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@company", company.ToLower());
                        if (!string.IsNullOrEmpty(zipCode))
                            command.Parameters.AddWithValue("@zipCode", zipCode.Replace("*", "%"));
                        if (!string.IsNullOrEmpty(city))
                            command.Parameters.AddWithValue("@city", city.Replace("*", "%"));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var partyId = reader.GetInt64(reader.GetOrdinal("PARTY"));
                                var customer = new CustomerInfo
                                {
                                    AccountNum = reader.GetString(reader.GetOrdinal("ACCOUNTNUM")),
                                    Name = reader.IsDBNull(reader.GetOrdinal("NAME")) ? "" : reader.GetString(reader.GetOrdinal("NAME")),
                                    CustomerGroup = reader.IsDBNull(reader.GetOrdinal("CUSTGROUP")) ? "" : reader.GetString(reader.GetOrdinal("CUSTGROUP")),
                                    Currency = reader.IsDBNull(reader.GetOrdinal("CURRENCY")) ? "" : reader.GetString(reader.GetOrdinal("CURRENCY")),
                                    Company = company,
                                    Party = partyId.ToString(),
                                    PrimaryAddress = new CustomerAddress
                                    {
                                        ZipCode = reader.IsDBNull(reader.GetOrdinal("ZIPCODE")) ? "" : reader.GetString(reader.GetOrdinal("ZIPCODE")),
                                        City = reader.IsDBNull(reader.GetOrdinal("CITY")) ? "" : reader.GetString(reader.GetOrdinal("CITY")),
                                        Street = reader.IsDBNull(reader.GetOrdinal("STREET")) ? "" : reader.GetString(reader.GetOrdinal("STREET")),
                                        CountryRegionId = reader.IsDBNull(reader.GetOrdinal("COUNTRYREGIONID")) ? "" : reader.GetString(reader.GetOrdinal("COUNTRYREGIONID")),
                                        IsPrimary = true
                                    }
                                };

                                customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching customers by address: {ex.Message}", ex);
            }

            return customers;
        }

        /// <summary>
        /// Get sales order by ID
        /// </summary>
        public SalesOrderInfo GetSalesOrder(string salesId, string company)
        {
            EnsureLoggedIn(company);

            var order = new SalesOrderInfo
            {
                SalesId = salesId,
                Company = company,
                Lines = new List<SalesOrderLineInfo>()
            };

            try
            {
                // Query SalesTable
                using (var salesTable = _axapta.CreateAxaptaRecord("SalesTable"))
                {
                    salesTable.ExecuteStmt($"select * from %1 where %1.SalesId == '{salesId}'");

                    if (salesTable.Found)
                    {
                        order.CustomerAccount = GetFieldValue<string>(salesTable, "CustAccount") ?? string.Empty;
                        order.CustomerName = GetFieldValue<string>(salesTable, "DeliveryName") ?? string.Empty;
                        order.Currency = GetFieldValue<string>(salesTable, "CurrencyCode") ?? string.Empty;
                        order.DeliveryName = GetFieldValue<string>(salesTable, "DeliveryName") ?? string.Empty;
                        order.DeliveryMode = GetFieldValue<string>(salesTable, "DlvMode") ?? string.Empty;
                        order.DeliveryTerms = GetFieldValue<string>(salesTable, "DlvTerm") ?? string.Empty;
                        order.CustomerReference = GetFieldValue<string>(salesTable, "CustomerRef") ?? string.Empty;
                        order.CustomerGroup = GetFieldValue<string>(salesTable, "CustGroup") ?? string.Empty;

                        var status = GetFieldValue<int>(salesTable, "SalesStatus");
                        order.Status = MapSalesStatus(status);

                        order.DeliveryDate = GetFieldValue<DateTime?>(salesTable, "DeliveryDate");
                        order.RequestedShipDate = GetFieldValue<DateTime?>(salesTable, "ShippingDateRequested");
                        order.ConfirmedShipDate = GetFieldValue<DateTime?>(salesTable, "ShippingDateConfirmed");

                        // Load order lines
                        LoadSalesLines(order);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error querying SalesTable: {ex.Message}", ex);
            }

            return order;
        }

        /// <summary>
        /// Search sales orders
        /// Uses direct SQL query for efficient search with wildcards
        /// </summary>
        public List<SalesOrderInfo> SearchSalesOrders(string salesId, string customerAccount, string company, bool includeLines = false)
        {
            var orders = new List<SalesOrderInfo>();

            try
            {
                var conditions = new List<string>();
                
                if (!string.IsNullOrEmpty(salesId))
                {
                    var pattern = salesId.Replace("*", "%");
                    conditions.Add(salesId.Contains("*") 
                        ? "s.SALESID LIKE @salesId" 
                        : "s.SALESID = @salesId");
                }
                
                if (!string.IsNullOrEmpty(customerAccount))
                {
                    conditions.Add("s.CUSTACCOUNT = @customerAccount");
                }

                var whereClause = conditions.Count > 0 
                    ? "AND " + string.Join(" AND ", conditions) 
                    : "";

                var sql = $@"
                    SELECT 
                        s.SALESID, s.CUSTACCOUNT, s.DELIVERYNAME, s.CURRENCYCODE, 
                        s.SALESSTATUS, s.CUSTGROUP
                    FROM SALESTABLE s
                    WHERE s.DATAAREAID = @company
                    {whereClause}
                    ORDER BY s.SALESID DESC";

                using (var connection = new SqlConnection(SqlConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@company", company.ToLower());
                        
                        if (!string.IsNullOrEmpty(salesId))
                            command.Parameters.AddWithValue("@salesId", salesId.Replace("*", "%"));
                        
                        if (!string.IsNullOrEmpty(customerAccount))
                            command.Parameters.AddWithValue("@customerAccount", customerAccount);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var order = new SalesOrderInfo
                                {
                                    SalesId = reader.GetString(reader.GetOrdinal("SALESID")),
                                    CustomerAccount = reader.GetString(reader.GetOrdinal("CUSTACCOUNT")),
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("DELIVERYNAME")) ? "" : reader.GetString(reader.GetOrdinal("DELIVERYNAME")),
                                    Currency = reader.IsDBNull(reader.GetOrdinal("CURRENCYCODE")) ? "" : reader.GetString(reader.GetOrdinal("CURRENCYCODE")),
                                    Status = MapSalesStatus(reader.GetInt32(reader.GetOrdinal("SALESSTATUS"))),
                                    CustomerGroup = reader.IsDBNull(reader.GetOrdinal("CUSTGROUP")) ? "" : reader.GetString(reader.GetOrdinal("CUSTGROUP")),
                                    Company = company,
                                    Lines = new List<SalesOrderLineInfo>()
                                };
                                
                                orders.Add(order);
                            }
                        }
                    }
                }

                // Optionally load lines via Business Connector
                if (includeLines)
                {
                    EnsureLoggedIn(company);
                    foreach (var order in orders)
                    {
                        LoadSalesLines(order);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching sales orders: {ex.Message}", ex);
            }

            return orders;
        }

        /// <summary>
        /// Execute custom X++ static method
        /// </summary>
        public object ExecuteStaticMethod(string className, string methodName, params object[] parameters)
        {
            EnsureLoggedIn();
            return _axapta.CallStaticClassMethod(className, methodName, parameters);
        }

        /// <summary>
        /// Execute custom X++ code (for advanced scenarios)
        /// </summary>
        public object ExecuteStatement(string xppCode)
        {
            EnsureLoggedIn();
            // Note: Direct X++ execution requires careful security consideration
            throw new NotSupportedException("Direct X++ execution is disabled for security reasons. Use specific API methods.");
        }

        #region Private Helper Methods

        private void LoadPartyDetails(CustomerInfo customer, long partyId, bool includeAddresses = false, bool includeContacts = false)
        {
            try
            {
                using (var dirParty = _axapta.CreateAxaptaRecord("DirPartyTable"))
                {
                    dirParty.ExecuteStmt($"select * from %1 where %1.RecId == {partyId}");

                    if (dirParty.Found)
                    {
                        var name = GetFieldValue<string>(dirParty, "Name");
                        if (!string.IsNullOrEmpty(name))
                        {
                            customer.Name = CleanString(name);
                        }
                        customer.NameAlias = CleanString(GetFieldValue<string>(dirParty, "NameAlias"));
                        customer.Language = GetFieldValue<string>(dirParty, "LanguageId") ?? string.Empty;
                    }
                }

                // Optionally load addresses
                if (includeAddresses)
                {
                    LoadAddresses(customer, partyId);
                }

                // Optionally load contacts
                if (includeContacts)
                {
                    LoadContacts(customer, partyId);
                }
            }
            catch
            {
                // Party details are optional, continue without them
            }
        }

        private void LoadAddresses(CustomerInfo customer, long partyId)
        {
            customer.Addresses = new List<CustomerAddress>();

            try
            {
                // Step 1: Get all Location RecIds with metadata for this Party
                var locationInfo = new Dictionary<long, (bool isPrimary, bool isPrivate, string addressType)>();
                using (var partyLocation = _axapta.CreateAxaptaRecord("DirPartyLocation"))
                {
                    partyLocation.ExecuteStmt($"select * from %1 where %1.Party == {partyId}");
                    
                    while (partyLocation.Found)
                    {
                        var locationId = GetFieldValue<long>(partyLocation, "Location");
                        var isPrimary = GetFieldValue<int>(partyLocation, "IsPrimary");
                        var isPrivate = GetFieldValue<int>(partyLocation, "IsPrivate");
                        
                        var isRoleBusiness = GetFieldValue<int>(partyLocation, "IsRoleBusiness") != 0;
                        var isRoleDelivery = GetFieldValue<int>(partyLocation, "IsRoleDelivery") != 0;
                        var isRoleHome = GetFieldValue<int>(partyLocation, "IsRoleHome") != 0;
                        var isRoleInvoice = GetFieldValue<int>(partyLocation, "IsRoleInvoice") != 0;
                        
                        var addressType = BuildAddressType(isRoleBusiness, isRoleDelivery, isRoleHome, isRoleInvoice);
                        
                        if (locationId > 0)
                        {
                            locationInfo[locationId] = (isPrimary != 0, isPrivate != 0, addressType);
                        }
                        partyLocation.Next();
                    }
                }

                // Step 2: Load addresses for each Location
                foreach (var location in locationInfo)
                {
                    using (var postalAddress = _axapta.CreateAxaptaRecord("LogisticsPostalAddress"))
                    {
                        postalAddress.ExecuteStmt($"select * from %1 where %1.Location == {location.Key}");

                        if (postalAddress.Found)
                        {
                            var address = new CustomerAddress
                            {
                                Street = GetFieldValue<string>(postalAddress, "Street") ?? string.Empty,
                                City = GetFieldValue<string>(postalAddress, "City") ?? string.Empty,
                                ZipCode = GetFieldValue<string>(postalAddress, "ZipCode") ?? string.Empty,
                                State = GetFieldValue<string>(postalAddress, "State") ?? string.Empty,
                                CountryRegionId = GetFieldValue<string>(postalAddress, "CountryRegionId") ?? string.Empty,
                                FullAddress = GetFieldValue<string>(postalAddress, "Address") ?? string.Empty,
                                IsPrimary = location.Value.isPrimary,
                                IsPrivate = location.Value.isPrivate,
                                AddressType = location.Value.addressType
                            };

                            customer.Addresses.Add(address);

                            if (location.Value.isPrimary && customer.PrimaryAddress == null)
                            {
                                customer.PrimaryAddress = address;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Address loading is optional
            }
        }

        private string BuildAddressType(bool isRoleBusiness, bool isRoleDelivery, bool isRoleHome, bool isRoleInvoice)
        {
            var roles = new List<string>();
            
            if (isRoleBusiness) roles.Add("Business");
            if (isRoleDelivery) roles.Add("Delivery");
            if (isRoleHome) roles.Add("Home");
            if (isRoleInvoice) roles.Add("Invoice");
            
            return roles.Count > 0 ? string.Join(", ", roles) : string.Empty;
        }

        private void LoadContacts(CustomerInfo customer, long partyId)
        {
            customer.Contacts = new List<CustomerContact>();

            try
            {
                // Step 1: Get all Location RecIds with IsPrimary flag for this Party
                var locations = new Dictionary<long, bool>();
                using (var partyLocation = _axapta.CreateAxaptaRecord("DirPartyLocation"))
                {
                    partyLocation.ExecuteStmt($"select * from %1 where %1.Party == {partyId}");
                    
                    while (partyLocation.Found)
                    {
                        var locationId = GetFieldValue<long>(partyLocation, "Location");
                        var isPrimary = GetFieldValue<int>(partyLocation, "IsPrimary");
                        if (locationId > 0)
                        {
                            locations[locationId] = isPrimary != 0;
                        }
                        partyLocation.Next();
                    }
                }

                // Step 2: Load electronic addresses for each Location
                foreach (var location in locations)
                {
                    using (var contactInfo = _axapta.CreateAxaptaRecord("LogisticsElectronicAddress"))
                    {
                        contactInfo.ExecuteStmt($"select * from %1 where %1.Location == {location.Key}");

                        while (contactInfo.Found)
                        {
                            var contact = new CustomerContact
                            {
                                Value = GetFieldValue<string>(contactInfo, "Locator") ?? string.Empty,
                                Description = GetFieldValue<string>(contactInfo, "Description") ?? string.Empty,
                                IsPrimary = location.Value
                            };

                            var type = GetFieldValue<int>(contactInfo, "Type");
                            contact.Type = MapContactType(type);

                            customer.Contacts.Add(contact);
                            contactInfo.Next();
                        }
                    }
                }
            }
            catch
            {
                // Contact loading is optional
            }
        }

        private void LoadSalesLines(SalesOrderInfo order)
        {
            decimal totalAmount = 0;

            try
            {
                using (var salesLine = _axapta.CreateAxaptaRecord("SalesLine"))
                {
                    salesLine.ExecuteStmt($"select * from %1 where %1.SalesId == '{order.SalesId}'");

                    while (salesLine.Found)
                    {
                        var lineAmount = GetFieldValue<decimal>(salesLine, "LineAmount");
                        totalAmount += lineAmount;

                        var line = new SalesOrderLineInfo
                        {
                            ItemId = GetFieldValue<string>(salesLine, "ItemId") ?? string.Empty,
                            ItemName = GetFieldValue<string>(salesLine, "Name") ?? string.Empty,
                            Quantity = GetFieldValue<decimal>(salesLine, "SalesQty"),
                            UnitPrice = GetFieldValue<decimal>(salesLine, "SalesPrice"),
                            LineAmount = lineAmount,
                            Unit = GetFieldValue<string>(salesLine, "SalesUnit") ?? string.Empty
                        };

                        order.Lines.Add(line);
                        salesLine.Next();
                    }
                }
            }
            catch
            {
                // Line loading error - continue with empty lines
            }

            order.TotalAmount = totalAmount;
        }

        private T GetFieldValue<T>(AxaptaRecord record, string fieldName)
        {
            try
            {
                var value = record.get_Field(fieldName);
                if (value == null || value == DBNull.Value)
                    return default(T);

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        private string MapSalesStatus(int status)
        {
            switch (status)
            {
                case 0: return "None";
                case 1: return "Backorder";
                case 2: return "Delivered";
                case 3: return "Invoiced";
                case 4: return "Canceled";
                default: return status.ToString();
            }
        }

        private string MapContactType(int type)
        {
            switch (type)
            {
                case 1: return "Phone";
                case 2: return "Email";
                case 3: return "Fax";
                case 4: return "URL";
                case 5: return "Telex";
                default: return "Other";
            }
        }

        private string MapItemType(int type)
        {
            switch (type)
            {
                case 0: return "Item";
                case 1: return "BOM";
                case 2: return "Service";
                default: return type.ToString();
            }
        }

        private string CleanString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var lines = value
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var trimmedLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    trimmedLines.Add(trimmed);
            }

            return string.Join("\n", trimmedLines);
        }

        #endregion

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isLoggedIn)
                {
                    try
                    {
                        _axapta.Logoff();
                    }
                    catch { }
                    _isLoggedIn = false;
                }

                _axapta = null;
            }
        }
    }
}
