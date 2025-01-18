using System;
using System.Configuration;
using System.Diagnostics;
using System.Xml.Linq;

using GenerationReportProcessor;
internal class ProcessGeneratorData
{
    // Retrieve the path to the folder where input files will be monitored.
    static string inboundFolderPath = AppConfig.GetAppSettings("InputFolder");

    // Retrieve the path to the folder where output files will be stored.
    static string outboundFolderPath = AppConfig.GetAppSettings("OutputFolder");

    // Retrieve the path to the reference data file that contains static data used for processing.
    static string referenceDataFile = AppConfig.GetAppSettings("ReferenceData");

    /// <summary>
    /// Entry point for the application. Initializes the application, processes existing files, and monitors an inbound folder for new XML files to process.
    /// </summary>
    private static void Main(string[] args)
    {
        try
        {
            //Check if inbound and outbound directories exists
            if (!Directory.Exists(inboundFolderPath) || !Directory.Exists(outboundFolderPath))
            {
                Console.WriteLine("Input or Output folder does not exist.");
                return;
            }

            // Load the reference data XML file into an XDocument object.            
            var referenceData = XDocument.Load(referenceDataFile);

            // Process any files that already exist in the inbound folder before monitoring starts.
            ProcessExistingFiles(inboundFolderPath,referenceData);

            Console.WriteLine("Monitoring input folder for XML files...");

            // Create and configure a FileSystemWatcher to monitor the inbound folder for XML files.
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = inboundFolderPath,
                Filter = "*.xml",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Subscribe to the created event, which is triggered when a new file is added to the folder.
            watcher.Created += (sender, e) =>
            {
                Console.WriteLine($"New file detected: {e.Name}");
                try
                {
                    //Attempt to process the newly detected file
                    ProcessFile(e.FullPath,referenceData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {e.Name}: {ex.Message}");
                }
            };
           
            Console.WriteLine("Watching for new files. Press Enter to exit.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in initialising application: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes the input XML file, generates an output XML file, and saves it to the specified outbound folder.
    /// </summary>
    /// <param name="filePath">The file path of the input XML file to be processed.</param>
    /// <param name="referenceData">An XDocument containing reference data required for processing.</param>
    private static void ProcessFile(string filePath, XDocument referenceData)
    {
        try
        {
            // Load the input XML file from the specified file path.
            var inputXml = XDocument.Load(filePath);

            // Instantiate a ReportProcessor object, passing the reference data as a parameter.
            var processor = new ReportProcessor(referenceData);

            // Call the Process method of the processor to process the input XML and generate the output XML.
            var outputXml = processor.Process(inputXml);

            //Generate output file name along with path
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string outputFilePath = Path.Combine(outboundFolderPath, fileName + "-Result.xml");

            // Save the processed output XML to the specified output file path.
            outputXml.Save(outputFilePath);

            Console.WriteLine($"Output file saved to: {outputFilePath}");            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessFile(): {ex.Message}");
        }

    }

    /// <summary>
    /// Processes all existing files in the specified input folder using the provided reference data.
    /// </summary>
    /// <param name="inputFolder">The path to the folder containing the files to be processed.</param>
    /// <param name="referenceData">An XDocument containing reference data required for processing.</param>
    private static void ProcessExistingFiles(string inputFolder, XDocument referenceData)
    {
        try
        {
            Console.WriteLine("Processing existing files...");

            // Retrieve all files from the specified input folder.
            string[] files = Directory.GetFiles(inputFolder);

            // Iterate through each file in the input folder.
            foreach (string file in files)
            {
                //Process every file in the input folder in the same manner as new files.
                ProcessFile(file, referenceData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing existing files: {ex.Message}");
        }
    }


}