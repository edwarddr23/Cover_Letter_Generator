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
using Avalonia.Input;

namespace CoverLetterGenerator.ViewModels;

public partial class GenerateCoverLetterViewModel : ViewModelBase
{
    public ObservableCollection<ChoiceItem> TemplateNames { get; } = new();
    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _errors = new();
    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _warnings = new();private string _selectedTemplate = string.Empty;
    public string SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
            {
                LoadDocuments();
                GenerateSuccessfulMessage = "";
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
            Errors.Add(new KeyValueItem
            {
                Key = "Settings",
                Value = "All settings must be specified before generating a cover letter."
            });
            return;
        }

        string? templatePath = Path.Combine(settings.TemplatesPath, SelectedTemplate);
        string? templateDocPath = Path.Combine(templatePath, SelectedDocument);

        // Template Name Validation.
        if (string.IsNullOrWhiteSpace(SelectedTemplate))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "SelectedTemplate",
                Value = "Template must be specified."
            });
        }
        // Job Source Validation.
        if (string.IsNullOrWhiteSpace(JobSource))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "JobSource",
                Value = "Job Source must be specified."
            });
        }
        // Company Name Validation.
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "CompanyName",
                Value = "Company Name must be specified."
            });
        }
        // Job Title Validation.
        if (string.IsNullOrWhiteSpace(JobTitle))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "JobTitle",
                Value = "Job Title must be specified."
            });
        }
        // Document Name Validation.
        if (string.IsNullOrWhiteSpace(SelectedDocument))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "SelectedDocument",
                Value = "Document must be specified."
            });
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
                List<string> parameters = new List<string>() { "{JOB SOURCE}", "{COMPANY NAME}", "{FIRST NAME}", "{LAST NAME}" };
                bool allParametersFound = parameters.All(parameter => innerText.Contains(parameter));
                if (!allParametersFound)
                {
                    Errors.Add(new KeyValueItem
                    {
                        Key = "DocumentContents",
                        Value = $"Document missing parameters. Required parameters are {string.Join(", ", parameters)}"
                    });
                    return;
                }

                // If the output path already exists, show an error and return.
                string outputDirPath = Path.Combine(settings.OutputPath, CompanyName, JobTitle);
                string outputFilePath = Path.Combine(outputDirPath, $"{settings.FirstName} {settings.LastName} Cover Letter.docx");
                if (Directory.Exists(outputDirPath) && File.Exists(outputFilePath))
                {
                    Errors.Add(new KeyValueItem
                    {
                        Key = "OutputFile",
                        Value = $"Output file {Path.GetFileName(outputFilePath)} already exists at {outputDirPath}."
                    });
                    return;
                }
                else if (Directory.Exists(outputDirPath))
                {
                    Warnings.Add(new KeyValueItem
                    {
                        Key = "OutputDir",
                        Value = $"Output directory {Path.GetFileName(outputFilePath)} already exists at {outputDirPath}."
                    });
                }

                // Attempt to create the output directory in question.
                try
                {
                    Directory.CreateDirectory(outputDirPath);
                }
                catch (Exception exc)
                {
                    Errors.Add(new KeyValueItem
                    {
                        Key = "OutputDir",
                        Value = $"Failed to create output directory specified at {outputDirPath}"
                    });
                    Debug.WriteLine($"Failed to create output directory specified at {outputDirPath}: {exc}");
                    return;
                }

                // Copy the template to the output file.
                File.Copy(templateDocPath, outputFilePath, overwrite: true);

                Dictionary<string, string> parameterMap = new Dictionary<string, string>
                {
                    { "{JOB SOURCE}", JobSource },
                    { "{COMPANY NAME}", CompanyName },
                    { "{FIRST NAME}", settings.FirstName },
                    { "{LAST NAME}", settings.LastName },
                };
                using (var outputWordDoc = WordprocessingDocument.Open(outputFilePath, true))
                {
                    foreach (var paragraph in outputWordDoc.MainDocumentPart.Document.Body.Descendants<Paragraph>())
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
                            var firstRun = paragraph.Descendants<Run>().FirstOrDefault();
                            Run newRun;

                            if (firstRun != null)
                            {
                                newRun = new Run(firstRun.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties());
                            }
                            else
                            {
                                newRun = new Run();
                            }

                            newRun.AppendChild(new Text(fullText));
                            paragraph.Descendants<Run>().ToList().ForEach(r => r.Remove());
                            paragraph.AppendChild(newRun);
                        }
                    }
                    outputWordDoc.MainDocumentPart.Document.Save();
                }
                string successMessage = $"Generated letter successfuly in {outputFilePath}";
                Debug.WriteLine(successMessage);
                GenerateSuccessfulMessage = successMessage;
                // foreach (var text in wordDoc.MainDocumentPart.Document.Body.Descendants<Text>())
                // {
                //     foreach (var kvp in parameterMap)
                //     {
                //         if (text.Text.Contains(kvp.Key))
                //         {
                //             text.Text = text.Text.Replace(kvp.Key, kvp.Value);
                //         }
                //     }
                // }
            }
        }
        catch (Exception exc)
        {
            Errors.Add(new KeyValueItem
            {
                Key = "DocumentContents",
                Value = "Failed to read document. Please make sure it is not open in another process."
            });
            Debug.WriteLine($"Failed to read document: {exc}");
            return;
        }
    }
}