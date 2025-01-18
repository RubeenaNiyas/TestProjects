using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GenerationReportProcessor
{
    public class ReportProcessor
    {
        // Dictionary to store value factors, where the key is the factor name and the value is the factor value
        private readonly Dictionary<string, double> ValueFactors;
        // Dictionary to store emission factors, where the key is the factor name and the value is the factor value
        private readonly Dictionary<string, double> EmissionFactors;

        // Constructor to initialize the ReportProcessor with reference data in XML format
        public ReportProcessor(XDocument referenceData)
        {
            // Extract the ValueFactors from the "ValueFactor" element in the XML
            ValueFactors = referenceData
                .Descendants("ValueFactor")
                .First()
                .Elements()
                .ToDictionary(x => x.Name.LocalName, x => (double)x);

            // Extract the EmissionFactors from the "EmissionsFactor" element in the XML
            EmissionFactors = referenceData
                .Descendants("EmissionsFactor")
                .First()
                .Elements()
                .ToDictionary(x => x.Name.LocalName, x => (double)x);
        }
        public XDocument Process(XDocument inputDoc)
        {
            try
            {
                // List to store total generator data
                var totals = new List<GeneratorTotal>();
                // List to store daily emission data
                var dailyEmissions = new List<DailyEmission>();
                // List to store actual heat rates for generators
                var actualHeatRates = new List<ActualHeatRate>();

                // Process all WindGenerators in the input XML
                foreach (var generator in inputDoc.Descendants("WindGenerator"))
                {
                    // Call method to process each WindGenerator and update totals
                    ProcessWindGenerator(generator, totals);
                }

                // Process all GasGenerators in the input XML
                foreach (var generator in inputDoc.Descendants("GasGenerator"))
                {
                    // Call method to process each GasGenerator with "Medium" EmissionFactor
                    ProcessFuelGenerator(generator, "Medium", totals, dailyEmissions);
                }

                // Process all CoalGenerators in the input XML
                foreach (var generator in inputDoc.Descendants("CoalGenerator"))
                {
                    // Call method to process each CoalGenerator with "High" EmissionFactor
                    ProcessFuelGenerator(generator, "High", totals, dailyEmissions);

                    // Extract TotalHeatInput and ActualNetGeneration from each CoalGenerator element
                    double totalHeatInput = (double)generator.Element("TotalHeatInput");
                    double actualNetGeneration = (double)generator.Element("ActualNetGeneration");

                    //Calculate HeatRate
                    double heatRate = totalHeatInput / actualNetGeneration;

                    // Add the calculated heat rate and generator name to the actualHeatRates list
                    actualHeatRates.Add(new ActualHeatRate { Name = (string)generator.Element("Name"), HeatRate = heatRate });
                }

                // Generate and return the output XML document with the processed data
                return GenerateOutputXml(totals, dailyEmissions, actualHeatRates);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Process(): {ex.Message}");
                return null;
            }

        }
        private void ProcessWindGenerator(XElement generator, List<GeneratorTotal> totals)
        {
            try
            {
                // Extract the 'Name' and 'Location' elements from the current WindGenerator
                string name = (string)generator.Element("Name");
                string location = (string)generator.Element("Location");

                // Determine the value factor based on the location of the generator
                // If the location is "Offshore", use the "Low" factor, otherwise use "High"
                double valueFactor = ValueFactors[location == "Offshore" ? "Low" : "High"];

                // Calculate the total generation by summing up the daily energy * price * value factor
                double totalGeneration = generator.Descendants("Day").Sum(day =>
                    (double)day.Element("Energy") *
                    (double)day.Element("Price") *
                    valueFactor);

                // Add the calculated total generation to the 'totals' list with the generator's name
                totals.Add(new GeneratorTotal { Name = name, Total = totalGeneration });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessWindGenerator(): {ex.Message}");
            }
        }

        private void ProcessFuelGenerator(XElement generator, string emissionFactorKey, List<GeneratorTotal> totals, List<DailyEmission> dailyEmissions)
        {
            try
            {
                // Extract the generator's name from the XML element
                string name = (string)generator.Element("Name");

                // Retrieve the value factor for "Medium" fuel type from the ValueFactors dictionary
                double valueFactor = ValueFactors["Medium"];

                // Retrieve the emission factor for the given emissionFactorKey from the EmissionFactors dictionary
                double emissionFactor = EmissionFactors[emissionFactorKey];

                // Extract the emissions rating for the generator
                double emissionRating = (double)generator.Element("EmissionsRating");

                // Calculate the total generation by summing up the energy * price * value factor for each day
                double totalGeneration = generator.Descendants("Day").Sum(day =>
                    (double)day.Element("Energy") *
                    (double)day.Element("Price") *
                    valueFactor);

                // Add the total generation to the 'totals' list for the current generator
                totals.Add(new GeneratorTotal { Name = name, Total = totalGeneration });

                // Iterate through each "Day" element to calculate and track daily emissions
                foreach (var day in generator.Descendants("Day"))
                {
                    // Extract the date and energy value for the day
                    string date = (string)day.Element("Date");
                    double energy = (double)day.Element("Energy");

                    // Calculate daily emissions as energy * emissions rating * emission factor
                    double dailyEmission = energy * emissionRating * emissionFactor;

                    // Add the daily emission data to the 'dailyEmissions' list
                    dailyEmissions.Add(new DailyEmission { Name = name, Date = date, Emission = dailyEmission });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessFuelGenerator(): {ex.Message}");
            }
        }

        private XDocument GenerateOutputXml(List<GeneratorTotal> totals, List<DailyEmission> dailyEmissions, List<ActualHeatRate> actualHeatRates)
        {
            try
            {
                // Create a new XDocument with a root element "GenerationOutput"
                var doc = new XDocument(new XElement("GenerationOutput",
                    // Add XML namespace attributes for schema information
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    // Create the "Totals" element which will contain the generator names and their total generation
                    new XElement("Totals",
                        totals.Select(t => new XElement("Generator", 
                            new XElement("Name", t.Name),
                            new XElement("Total", t.Total.ToString("F9"))) // Add the total generation formatted to 9 decimal places
                        )
                    ),
                    // Create the "MaxEmissionGenerators" element, which will contain the generator with the highest emissions for each day
                    new XElement("MaxEmissionGenerators",
                        // Group the daily emissions by date
                        dailyEmissions.GroupBy(e => e.Date).Select(g =>
                            // For each group (day), select the generator with the highest emissions
                            g.OrderByDescending(e => e.Emission).First()  // Get the generator with the max emission for that day
                            ).Select(e =>
                                new XElement("Day",
                                    new XElement("Name", e.Name),
                                    new XElement("Date", e.Date),                                
                                    new XElement("Emission", e.Emission.ToString("F9")))
                            )
                    ),
                    // Create the "ActualHeatRates" element, which will contain the actual heat rates for each generator
                    new XElement("ActualHeatRates",
                        actualHeatRates.Select(hr => new XElement("ActualHeatRate", 
                            new XElement("Name", hr.Name),
                            new XElement("HeatRate", hr.HeatRate))
                        )
                    )
                ));

                // Return the constructed XML document
                return doc;
            }            
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateOutputXml(): {ex.Message}");
                return null;
            }

        }

        // Class to store total generation data for a generator
        class GeneratorTotal
        {
            public string Name { get; set; }
            public double Total { get; set; }
        }

        // Class to store daily emission data for a generator
        class DailyEmission
        {
            public string Name { get; set; }           
            public string Date { get; set; }
            public double Emission { get; set; }
        }

        // Class to store the actual heat rate data for a generator
        class ActualHeatRate
        {
            public string Name { get; set; }
            public double HeatRate { get; set; }
        }


    }
}
