﻿using Konaju.File.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RaffleDraw
{
    class Program
    {
        static void Help()
        {

        }

        enum TicketMode
        {
            None,
            SPG,
            MSG
        }

        static void Main(string[] args)
        {
            string itemsFilename = null;
            string customersFilename = null;
            string outFilename = null;
            int numberOfDraws = 0;
            string filter = null;
            StreamWriter output = null;
            TicketMode ticketMode = TicketMode.None;

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: <-i filename> [-c filename] <-d count> [-o filename] [-h]");
                Console.WriteLine();
                Console.WriteLine("-i filename | --items filename");
                Console.WriteLine("  The path to the items CSV file.");
                Console.WriteLine();
                Console.WriteLine("-c filename | --customers filename");
                Console.WriteLine("  The path to the customers CSV file.");
                Console.WriteLine();
                Console.WriteLine("-d count | --draws count");
                Console.WriteLine("  The number of winners to be drawn.");
                Console.WriteLine();
                Console.WriteLine("-f string | --filter string");
                Console.WriteLine("  Filter to apply to the item name");
                Console.WriteLine();
                Console.WriteLine("-m mode | --mode mode");
                Console.WriteLine("  The ticket mode. MSG or SPG");
                Console.WriteLine();
                Console.WriteLine("-o filename | --output filename");
                Console.WriteLine("  The path to the file the output will be written to.");
                Console.WriteLine();
                Console.WriteLine("-h | --help | -?");
                Console.WriteLine("  This help information.");
                Console.WriteLine();
                Help();
                Environment.Exit(0);
            }

            // Read parameters from command line
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-i":
                    case "--items":
                        ++i;
                        itemsFilename = i < args.Length ? args[i] : null;
                        break;

                    case "-c":
                    case "--customers":
                        ++i;
                        customersFilename = i < args.Length ? args[i] : null;
                        break;

                    case "-d":
                    case "--draws":
                        ++i;
                        if (i < args.Length)
                        {
                            if (!int.TryParse(args[i], out numberOfDraws))
                            {
                                Console.Error.WriteLine("Invalid number for number of draws");
                                Environment.Exit(1);
                            }
                        }
                        break;

                    case "-f":
                    case "--filter":
                        ++i;
                        filter = i < args.Length ? args[i] : null;
                        break;

                    case "-o":
                    case "--output":
                        ++i;
                        outFilename = i < args.Length ? args[i] : null;
                        break;

                    case "-m":
                    case "--mode":
                        ++i;
                        if (i < args.Length)
                        {
                            if (!Enum.TryParse(args[i], true, out ticketMode))
                            {
                                Console.Error.WriteLine("Invalid mode");
                                Environment.Exit(1);
                            }
                        }
                        break;

                    case "-h":
                    case "--help":
                    case "-?":
                        Help();
                        Environment.Exit(0);
                        break;

                    default:
                        Console.Error.WriteLine("Unknown parameter " + args[i]);
                        Environment.Exit(1);
                        break;
                }
            }

            // Validate parameters passed to application
            if (string.IsNullOrWhiteSpace(itemsFilename))
            {
                Console.Error.WriteLine("Missing items filename");
                Environment.Exit(1);
            }

            if (!File.Exists(itemsFilename))
            {
                Console.Error.WriteLine("Items file not found");
                Environment.Exit(1);
            }

            if (!string.IsNullOrWhiteSpace(customersFilename) && !File.Exists(customersFilename))
            {
                Console.Error.WriteLine("Customers file not found");
                Environment.Exit(1);
            }

            if (numberOfDraws <= 0)
            {
                Console.Error.WriteLine("Number of draws must be greater than zero");
                Environment.Exit(1);
            }

            // Load CSV files
            IList<Item> items = null;
            try
            {
                using (var itemsFile = new FileStream(itemsFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    items = CsvReader.Read<Item>(itemsFile);
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        items = items.Where(i => i.ItemName.Contains(filter)).ToList();
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error reading items file.");
                Console.Error.WriteLine($"{e.GetType().FullName} {e.Message}");
                Environment.Exit(1);
            }

            IList<Customer> customers = null;
            if (!string.IsNullOrWhiteSpace(customersFilename))
            {
                try
                {
                    using (var customersFile = new FileStream(customersFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        customers = CsvReader.Read<Customer>(customersFile);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error reading customers file.");
                    Console.Error.WriteLine($"{e.GetType().FullName} {e.Message}");
                    Environment.Exit(1);
                }
            }

            // Remove items that have been refunded
            var refundPaymentIDs = items.Where(i => i.EventType == EventType.Refund).Select(i => i.PaymentID);
            var validItems = items.Where(i => !refundPaymentIDs.Contains(i.PaymentID));

            List<Item> tickets = new List<Item>();
            switch (ticketMode)
            {
                case TicketMode.None:
                    tickets = validItems.SelectMany(i => Enumerable.Repeat(i, (int)i.Quantity)).ToList();
                    break;
                case TicketMode.SPG:
                    // Expand multiple ticket purchases
                    tickets = validItems.SelectMany(i => Enumerable.Repeat(i, int.Parse(i.ItemName.Substring(i.ItemName.Length - 1, 1)) * (int)i.Quantity)).ToList();
                    break;
                case TicketMode.MSG:
                    // Ticket per dollar spent
                    tickets = validItems.SelectMany(i => Enumerable.Repeat(i, (int)double.Parse(i.ProductSales, NumberStyles.AllowCurrencySymbol | NumberStyles.Currency))).ToList();
                    break;
            }

            // Select the random winners
            var winners = tickets.PickRandom(numberOfDraws);

            // Open output file if requested
            if (!string.IsNullOrWhiteSpace(outFilename))
            {
                try
                {
                    output = new StreamWriter(new FileStream(outFilename, FileMode.Create, FileAccess.Write, FileShare.Write));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error opening output file.");
                    Console.Error.WriteLine($"{e.GetType().FullName} {e.Message}");
                    Environment.Exit(1);
                }
            }

            // List results
            Output(output, $"From {tickets.Count} tickets purchased, selecting {numberOfDraws} winners...");
            for (int w = 0; w < winners.Count; ++w)
            {
                var ticket = winners[w];
                Output(output, string.Empty);
                Output(output, $"{w + 1}. {ticket.CustomerName}");// {ticket.ModifiersApplied.Substring(11)}");
                if (customers != null)
                {
                    var winner = string.IsNullOrWhiteSpace(ticket.CustomerID) ? null : customers.Where(c => c.SquareCustomerID == ticket.CustomerID).FirstOrDefault();
                    if (winner != null)
                    {
                        Output(output, $"    {winner.FirstName} {winner.Surname} {winner.PhoneNumber} {winner.EmailAddress}");
                    }
                    else
                    {
                        Output(output, $"    No customer reference ID in item sale. Check transaction ID link below");
                    }
                }
                //Output(output, $"{ticket.DetailsUrl}");
                Output(output, $"    https://squareup.com/dashboard/orders/overview/{ticket.TransactionID}");
            }

            if (output != null)
            {
                output.Dispose();

                var psi = new ProcessStartInfo(outFilename) { UseShellExecute = true };
                Process.Start(psi);
            }
        }

        static void Output(StreamWriter writer, string line)
        {
            Console.WriteLine(line);
            writer?.WriteLine(line);
        }
    }
}
