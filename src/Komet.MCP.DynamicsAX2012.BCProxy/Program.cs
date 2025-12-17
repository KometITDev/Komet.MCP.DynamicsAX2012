using System;
using System.Reflection;
using Microsoft.Owin.Hosting;

namespace Komet.MCP.DynamicsAX2012.BCProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:5100/";

            // Parse command line args for custom port
            if (args.Length > 0 && int.TryParse(args[0], out int port))
            {
                baseAddress = $"http://localhost:{port}/";
            }

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

            Console.WriteLine("===========================================");
            Console.WriteLine("  Dynamics AX 2012 Business Connector Proxy");
            Console.WriteLine($"  Version {fileVersion ?? version.ToString()}");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            try
            {
                using (WebApp.Start<Startup>(url: baseAddress))
                {
                    Console.WriteLine($"BC Proxy running at {baseAddress}");
                    Console.WriteLine();
                    Console.WriteLine("Available endpoints:");
                    Console.WriteLine();
                    Console.WriteLine("CRUD:");
                    Console.WriteLine("  GET  /api/health");
                    Console.WriteLine("  GET  /api/customer/{accountNum}?company=GBL");
                    Console.WriteLine("  GET  /api/customer/search?accountNum=...&company=GBL");
                    Console.WriteLine("  GET  /api/product/{itemId}?company=GBL");
                    Console.WriteLine("  GET  /api/salesorder?salesId=...&company=GBL");
                    Console.WriteLine("  GET  /api/salesorder/search?customerAccount=...&company=GBL");
                    Console.WriteLine();
                    Console.WriteLine("Analytics (SQL-based):");
                    Console.WriteLine("  GET  /api/analytics/top-customers?year=2024&top=10&company=GBL");
                    Console.WriteLine("  GET  /api/analytics/customers-by-city?company=GBL");
                    Console.WriteLine("  GET  /api/analytics/sales-stats?fromDate=2024-01-01&toDate=2024-12-31&company=GBL");
                    Console.WriteLine("  GET  /api/analytics/top-products?year=2024&top=10&company=GBL");
                    Console.WriteLine();
                    Console.WriteLine("X++ Execution:");
                    Console.WriteLine("  POST /api/ax/execute");
                    Console.WriteLine();
                    Console.WriteLine("Press Enter to stop...");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting BC Proxy: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Make sure:");
                Console.WriteLine("  1. AX Client components are installed");
                Console.WriteLine("  2. Business Connector is configured");
                Console.WriteLine("  3. Port is not in use");
                Console.WriteLine();
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }
}
