using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using GemBox.Document;

namespace CoverLetterGenerator.ViewModels;

public partial class GenerateCoverLetterViewModel : ViewModelBase
{
    public ObservableCollection<ChoiceItem> TemplateNames { get; } = new();
    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _errors = new();
    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _warnings = new(); private string _selectedTemplate = string.Empty;
    public string SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
            {
                LoadDocuments();
                GenerateSuccessfulMessage = "";
                OutputWordFilePath = "";
                OutputPDFFilePath = "";
                OutputDirectoryPath = "";
            }
        }
    }
    [ObservableProperty]
    private string _jobSource = string.Empty;
    [ObservableProperty]
    private string _companyName = string.Empty;
    [ObservableProperty]
    private string _jobTitle = string.Empty;
    private string _selectedDocument = string.Empty;
    public string SelectedDocument
    {
        get => _selectedDocument;
        set => SetProperty(ref _selectedDocument, value);
    }
    [ObservableProperty]
    private ObservableCollection<string> _templateDocuments = [];

    [ObservableProperty]
    private string _generateSuccessfulMessage = string.Empty;
    [ObservableProperty]
    private string _pageTitle = "Generate Cover Letter";

    [ObservableProperty]
    private string _outputWordFilePath = string.Empty;
    [ObservableProperty]
    private string _outputPDFFilePath = string.Empty;
    [ObservableProperty]
    private string _outputDirectoryPath = string.Empty;

    private readonly ISettingsService _settingsService;

    private void SettingsService_SettingsChanged(object? sender, EventArgs e)
    {
        LoadTemplates();
    }

    public GenerateCoverLetterViewModel(ISettingsService settingsService)
    {
        Errors.Clear();
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        Errors.Clear();
        var settings = _settingsService.LoadSettings();
        string? templatePath = settings.TemplatesPath;
        if (!string.IsNullOrEmpty(templatePath) && Directory.Exists(templatePath))
        {
            TemplateNames.Clear();
            foreach (var dir in Directory.GetDirectories(templatePath))
            {
                TemplateNames.Add(new ChoiceItem
                {
                    Name = Path.GetFileName(dir),
                    IsSelected = false,
                    ParentViewModel = this
                });
            }
        }
    }

    private void LoadDocuments()
    {
        Errors.Clear();
        TemplateDocuments.Clear();
        var settings = _settingsService.LoadSettings();
        string? templatePath = settings.TemplatesPath;
        string selectedTemplatePath = Path.Combine(templatePath, SelectedTemplate);
        List<string> docs = Directory.GetFiles(selectedTemplatePath).ToList();
        docs = docs.Where(s => s.EndsWith("docx") || s.EndsWith("dotx")).ToList();
        Console.WriteLine("LoadDocuments() ran!");
        Console.WriteLine($"selectedTemplatePath: {selectedTemplatePath}");
        Console.WriteLine($"Directory.GetFiles(selectedTemplatePath).ToList(): {Directory.GetFiles(selectedTemplatePath).ToList()}");
        Console.WriteLine($"Directory.GetFiles(selectedTemplatePath).Count(): {Directory.GetFiles(selectedTemplatePath).Count()}");
        foreach (string document in docs)
        {
            Console.WriteLine(document);
            TemplateDocuments.Add(Path.GetFileName(document));
        }
    }

    [RelayCommand]
    private void GenerateCoverLetter()
    {
        Errors.Clear();
        // Check first to see if any settings are missing. If there are, display an error and don't do anything.
        var settings = _settingsService.LoadSettings();
        var missingProps = settings.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Where(p => string.IsNullOrWhiteSpace((string?)p.GetValue(settings)))
            .Select(p => p.Name)
            .ToList();
        if (missingProps.Any())
        {
            Debug.WriteLine($"The following settings are missing values: {string.Join(", ", missingProps)}");
            Errors.Add(
                new KeyValueItem("Settings", "All settings must be specified before generating a cover letter.")
            );
            return;
        }

        string? templatePath = Path.Combine(settings.TemplatesPath, SelectedTemplate);
        string? templateDocPath = Path.Combine(templatePath, SelectedDocument);

        List<KeyValueItem> inputs = new List<KeyValueItem> {
            new KeyValueItem(SelectedTemplate, "Template"),
            new KeyValueItem(JobSource, "Job Source"),
            new KeyValueItem(CompanyName, "Company Name"),
            new KeyValueItem(JobTitle, "Job Title"),
            new KeyValueItem(SelectedDocument, "Document"),
        };
        List<string> textInputs = new() { "Job Source", "Company Name", "Job Title" };
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        // Validate the inputs.
        foreach (var inp in inputs)
        {
            if (string.IsNullOrWhiteSpace(inp.Key))
            {
                Errors.Add(new KeyValueItem(inp.Key, $"{inp.Value} must be specified."));
            }
            else if (textInputs.Contains(inp.Value))
            {
                string cleaned = inp.Key.Trim();
                cleaned = textInfo.ToTitleCase(cleaned);
                cleaned = Regex.Replace(cleaned, @"\s+", " ");

                var prop = this.GetType().GetProperty(inp.Value.Replace(" ", ""));
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(this, cleaned);
                }
            }
        }

        // If any errors were found, return.
        if (Errors.Count > 0)
        {
            return;
        }

        // Otherwise, the input is valid. Continue process to generate the cover letter
        Debug.WriteLine($"Selected Template: {SelectedTemplate}, Job Source: {JobSource}, Company Name: {CompanyName}, Selected Document: {SelectedDocument}, Document Path: {templateDocPath}");

        try
        {
            using (var wordDoc = WordprocessingDocument.Open(templateDocPath, false))
            {
                // Validate that all parameters are in the template in question.
                var innerText = wordDoc?.MainDocumentPart?.Document?.Body?.InnerText;
                if (innerText == null)
                {
                    Errors.Add(new KeyValueItem("DocumentContents", "Document body is empty or invalid."));
                    return;
                }
                List<string> parameters = new List<string>() { "{JOB SOURCE}", "{COMPANY NAME}", "{FIRST NAME}", "{LAST NAME}" };
                bool allParametersFound = parameters.All(parameter => innerText.Contains(parameter));
                if (!allParametersFound)
                {
                    Errors.Add(new KeyValueItem("DocumentContents", $"Document missing parameters. Required parameters are {string.Join(", ", parameters)}"));
                    return;
                }

                // If the output path already exists, show an error and return.
                string outputDirPath = Path.Combine(settings.OutputPath, CompanyName, JobTitle);
                string outputWordFilePath = Path.Combine(outputDirPath, $"{settings.FirstName} {settings.LastName} Cover Letter.docx");
                if (Directory.Exists(outputDirPath) && File.Exists(outputWordFilePath))
                {
                    Errors.Add(new KeyValueItem("OutputFile", $"Output file {Path.GetFileName(outputWordFilePath)} already exists at {outputDirPath}."));
                    return;
                }
                else if (Directory.Exists(outputDirPath))
                {
                    Warnings.Add(new KeyValueItem("OutputDir", $"Output directory {Path.GetFileName(outputWordFilePath)} already exists at {outputDirPath}."));
                }

                // Attempt to create the output directory in question.
                try
                {
                    Directory.CreateDirectory(outputDirPath);
                }
                catch (Exception exc)
                {
                    Errors.Add(new KeyValueItem("OutputDir", $"Failed to create output directory specified at {outputDirPath}"));
                    Debug.WriteLine($"Failed to create output directory specified at {outputDirPath}: {exc}");
                    return;
                }

                // Copy the template to the output file.
                File.Copy(templateDocPath, outputWordFilePath, overwrite: true);

                List<KeyValueItem> parameterMap = new List<KeyValueItem>
                {
                    new KeyValueItem("{JOB SOURCE}", JobSource),
                    new KeyValueItem("{COMPANY NAME}", CompanyName),
                    new KeyValueItem("{FIRST NAME}", settings.FirstName),
                    new KeyValueItem("{LAST NAME}", settings.LastName),
                };
                using (var outputWordDoc = WordprocessingDocument.Open(outputWordFilePath, true))
                {
                    var mainDocument = outputWordDoc.MainDocumentPart?.Document;
                    if (mainDocument == null)
                    {
                        Errors.Add(new KeyValueItem("DocumentContents", "Main Document part of the word document is invalid."));
                        return;
                    }
                    var paragraphs = mainDocument.Body?.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
                    if (paragraphs == null)
                    {
                        Errors.Add(new KeyValueItem("DocumentContents", "Body of Main Document part of the word document is invalid."));
                        return;
                    }
                    foreach (var paragraph in paragraphs)
                    {
                        string fullText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
                        bool replaced = false;

                        foreach (var pm in parameterMap)
                        {
                            if (fullText.Contains(pm.Key))
                            {
                                fullText = fullText.Replace(pm.Key, pm.Value);
                                replaced = true;
                            }
                        }

                        if (replaced)
                        {
                            var firstRun = paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>().FirstOrDefault();
                            DocumentFormat.OpenXml.Wordprocessing.Run newRun;

                            if (firstRun != null)
                            {
                                newRun = new DocumentFormat.OpenXml.Wordprocessing.Run(firstRun.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties());
                            }
                            else
                            {
                                newRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
                            }

                            newRun.AppendChild(new Text(fullText));
                            paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>().ToList().ForEach(r => r.Remove());
                            paragraph.AppendChild(newRun);
                        }
                    }
                    mainDocument.Save();
                }
                string successMessage = $"Generated letter successfully in {outputDirPath}";
                Debug.WriteLine(successMessage);
                GenerateSuccessfulMessage = successMessage;

                // Generate PDF.
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");
                var pdfDocument = DocumentModel.Load(outputWordFilePath);
                string outputPDFFilePath = outputWordFilePath.Replace(".docx", ".pdf");
                pdfDocument.Save(outputPDFFilePath);

                // Put output paths in vm variables for later handling.
                OutputWordFilePath = outputWordFilePath;
                OutputPDFFilePath = outputPDFFilePath;
                OutputDirectoryPath = outputDirPath;
            }
        }
        catch (Exception exc)
        {
            Errors.Add(new KeyValueItem("DocumentContents", "Failed to read document. Please make sure it is not open in another process."));
            Debug.WriteLine($"Failed to read document: {exc}");
            return;
        }
    }

    [RelayCommand]
    public void OpenWordDocument()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OutputWordFilePath) || !File.Exists(OutputWordFilePath))
            {
                Errors.Add(new KeyValueItem("WordDocument", $"File does not exist: {OutputWordFilePath}"));
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputWordFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception exc)
        {
            string err_msg = $"Failed to open word document at {OutputWordFilePath}";
            Debug.WriteLine($"{err_msg}: {exc}");
            Errors.Add(new KeyValueItem("WordDocument", err_msg));
        }
    }

    [RelayCommand]
    public void OpenPDFDocument()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OutputPDFFilePath) || !File.Exists(OutputPDFFilePath))
            {
                Errors.Add(new KeyValueItem("PDFDocument", $"File does not exist: {OutputPDFFilePath}"));
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputPDFFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception exc)
        {
            string err_msg = $"Failed to open PDF document at {OutputPDFFilePath}";
            Debug.WriteLine($"{err_msg}: {exc}");
            Errors.Add(new KeyValueItem("PDFDocument", err_msg));
        }
    }

    [RelayCommand]
    public void OpenInFileExplorer()
    {
        try
        {
            // if (string.IsNullOrWhiteSpace(OutputDirectoryPath) || !File.Exists(OutputDirectoryPath))
            // {
            //     Errors.Add(new KeyValueItem("OutputDirectory", $"File does not exist: {OutputDirectoryPath}"));
            //     return;
            // }
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputDirectoryPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception exc)
        {
            string err_msg = $"Failed to open output directory at {OutputDirectoryPath}";
            Debug.WriteLine($"{err_msg}: {exc}");
            Errors.Add(new KeyValueItem("OutputDirectory", err_msg));
        }
    }
}