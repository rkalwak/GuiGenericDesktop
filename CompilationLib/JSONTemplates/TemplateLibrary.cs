using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SuplaTemplateBoard
{
    /// <summary>
    /// Manages a library of board templates from template_boards.json
    /// </summary>
    public class TemplateLibrary
    {
        private List<Models.BoardTemplate> _templates;
        private readonly string _templatesFilePath;

        public TemplateLibrary(string templatesFilePath = "template_boards.json")
        {
            _templatesFilePath = templatesFilePath;
            _templates = new List<Models.BoardTemplate>();
        }

        /// <summary>
        /// Load all templates from the JSON file
        /// </summary>
        public void LoadTemplates()
        {
            if (!File.Exists(_templatesFilePath))
            {
                throw new FileNotFoundException($"Template file not found: {_templatesFilePath}");
            }

            string jsonContent = File.ReadAllText(_templatesFilePath);
            _templates = JsonSerializer.Deserialize<List<Models.BoardTemplate>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            }) ?? new List<Models.BoardTemplate>();
        }

        /// <summary>
        /// Get all available template names
        /// </summary>
        public List<string> GetTemplateNames()
        {
            return _templates.Select(t => t.Name).ToList();
        }

        /// <summary>
        /// Get a template by name
        /// </summary>
        public Models.BoardTemplate GetTemplateByName(string name)
        {
            return _templates.FirstOrDefault(t => 
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get a template by index
        /// </summary>
        public Models.BoardTemplate GetTemplateByIndex(int index)
        {
            if (index < 0 || index >= _templates.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"Index must be between 0 and {_templates.Count - 1}");
            }

            return _templates[index];
        }

        /// <summary>
        /// Get template as JSON string
        /// </summary>
        public string GetTemplateJson(string name)
        {
            var template = GetTemplateByName(name);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template '{name}' not found");
            }

            return JsonSerializer.Serialize(template, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Get template as JSON string by index
        /// </summary>
        public string GetTemplateJson(int index)
        {
            var template = GetTemplateByIndex(index);
            return JsonSerializer.Serialize(template, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Search templates by keyword
        /// </summary>
        public List<Models.BoardTemplate> SearchTemplates(string keyword)
        {
            return _templates.Where(t => 
                t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Get templates by manufacturer
        /// </summary>
        public List<Models.BoardTemplate> GetTemplatesByManufacturer(string manufacturer)
        {
            return _templates.Where(t => 
                t.Name.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Get total number of templates
        /// </summary>
        public int Count => _templates.Count;
    }
}
