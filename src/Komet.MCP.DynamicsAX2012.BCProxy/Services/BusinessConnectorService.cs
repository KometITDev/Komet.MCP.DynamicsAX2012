using System;
using System.Collections.Generic;
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
        public CustomerInfo GetCustomer(string accountNum, string company)
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
                            LoadPartyDetails(customer, partyId);
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
        /// Search customers by various criteria
        /// </summary>
        public List<CustomerInfo> SearchCustomers(string accountNum, string customerGroup, string company)
        {
            EnsureLoggedIn(company);
            var customers = new List<CustomerInfo>();

            try
            {
                using (var custTable = _axapta.CreateAxaptaRecord("CustTable"))
                {
                    // Build WHERE clause
                    var whereClause = "1==1";
                    if (!string.IsNullOrEmpty(accountNum))
                    {
                        if (accountNum.Contains("*"))
                            whereClause += $" && %1.AccountNum like '{accountNum.Replace("*", "%")}'";
                        else
                            whereClause += $" && %1.AccountNum == '{accountNum}'";
                    }
                    if (!string.IsNullOrEmpty(customerGroup))
                    {
                        whereClause += $" && %1.CustGroup == '{customerGroup}'";
                    }

                    custTable.ExecuteStmt($"select * from %1 where {whereClause}");

                    while (custTable.Found)
                    {
                        var customer = new CustomerInfo
                        {
                            AccountNum = GetFieldValue<string>(custTable, "AccountNum") ?? string.Empty,
                            Name = GetFieldValue<string>(custTable, "Name") ?? string.Empty,
                            CustomerGroup = GetFieldValue<string>(custTable, "CustGroup") ?? string.Empty,
                            Currency = GetFieldValue<string>(custTable, "Currency") ?? string.Empty,
                            Company = company
                        };
                        customers.Add(customer);
                        custTable.Next();
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
        /// </summary>
        public List<SalesOrderInfo> SearchSalesOrders(string salesId, string customerAccount, string company)
        {
            EnsureLoggedIn(company);
            var orders = new List<SalesOrderInfo>();

            try
            {
                using (var salesTable = _axapta.CreateAxaptaRecord("SalesTable"))
                {
                    var whereClause = "1==1";
                    if (!string.IsNullOrEmpty(salesId))
                    {
                        if (salesId.Contains("*"))
                            whereClause += $" && %1.SalesId like '{salesId.Replace("*", "%")}'";
                        else
                            whereClause += $" && %1.SalesId == '{salesId}'";
                    }
                    if (!string.IsNullOrEmpty(customerAccount))
                    {
                        whereClause += $" && %1.CustAccount == '{customerAccount}'";
                    }

                    salesTable.ExecuteStmt($"select * from %1 where {whereClause}");

                    while (salesTable.Found)
                    {
                        var order = new SalesOrderInfo
                        {
                            SalesId = GetFieldValue<string>(salesTable, "SalesId") ?? string.Empty,
                            CustomerAccount = GetFieldValue<string>(salesTable, "CustAccount") ?? string.Empty,
                            CustomerName = GetFieldValue<string>(salesTable, "DeliveryName") ?? string.Empty,
                            Currency = GetFieldValue<string>(salesTable, "CurrencyCode") ?? string.Empty,
                            Status = MapSalesStatus(GetFieldValue<int>(salesTable, "SalesStatus")),
                            Company = company,
                            Lines = new List<SalesOrderLineInfo>()
                        };
                        orders.Add(order);
                        salesTable.Next();
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

        private void LoadPartyDetails(CustomerInfo customer, long partyId)
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

                // Load addresses
                LoadAddresses(customer, partyId);

                // Load contacts
                LoadContacts(customer, partyId);
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
                using (var postalAddress = _axapta.CreateAxaptaRecord("LogisticsPostalAddress"))
                {
                    // This is simplified - actual query would need to join through DirPartyLocation
                    postalAddress.ExecuteStmt($@"
                        select * from %1 
                        exists join DirPartyLocation 
                        where DirPartyLocation.Location == %1.Location 
                        && DirPartyLocation.Party == {partyId}");

                    while (postalAddress.Found)
                    {
                        var address = new CustomerAddress
                        {
                            Street = GetFieldValue<string>(postalAddress, "Street") ?? string.Empty,
                            City = GetFieldValue<string>(postalAddress, "City") ?? string.Empty,
                            ZipCode = GetFieldValue<string>(postalAddress, "ZipCode") ?? string.Empty,
                            State = GetFieldValue<string>(postalAddress, "State") ?? string.Empty,
                            CountryRegionId = GetFieldValue<string>(postalAddress, "CountryRegionId") ?? string.Empty,
                            FullAddress = GetFieldValue<string>(postalAddress, "Address") ?? string.Empty
                        };

                        customer.Addresses.Add(address);

                        if (customer.PrimaryAddress == null)
                        {
                            customer.PrimaryAddress = address;
                            address.IsPrimary = true;
                        }

                        postalAddress.Next();
                    }
                }
            }
            catch
            {
                // Address loading is optional
            }
        }

        private void LoadContacts(CustomerInfo customer, long partyId)
        {
            customer.Contacts = new List<CustomerContact>();

            try
            {
                using (var contactInfo = _axapta.CreateAxaptaRecord("LogisticsElectronicAddress"))
                {
                    contactInfo.ExecuteStmt($@"
                        select * from %1 
                        exists join DirPartyLocation 
                        where DirPartyLocation.Location == %1.Location 
                        && DirPartyLocation.Party == {partyId}");

                    while (contactInfo.Found)
                    {
                        var contact = new CustomerContact
                        {
                            Value = GetFieldValue<string>(contactInfo, "Locator") ?? string.Empty,
                            Description = GetFieldValue<string>(contactInfo, "Description") ?? string.Empty
                        };

                        var type = GetFieldValue<int>(contactInfo, "Type");
                        contact.Type = MapContactType(type);

                        customer.Contacts.Add(contact);
                        contactInfo.Next();
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
