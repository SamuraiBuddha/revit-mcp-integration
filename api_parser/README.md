# Revit API HTML to JSON Parser

This tool converts Revit API documentation from HTML format to structured JSON for AI training purposes.

## Features

- Parses Revit API HTML documentation files
- Extracts structured information including:
  - Class/method names and descriptions
  - Namespaces and assemblies
  - Syntax in multiple languages (C#, VB.NET, C++)
  - Parameters and return values
  - Properties and methods
  - Exceptions and remarks
  - Code examples
  - Inheritance hierarchies
- Generates Q&A training pairs automatically
- Batch processes entire directories
- Comprehensive error handling and logging

## Directory Structure

```
api_parser/
├── revit_parser.py      # Main parser script
├── raw_data/            # Place HTML files here
├── processed_data/      # JSON output will be saved here
│   ├── revit_api_raw.json      # Complete parsed data
│   ├── revit_api_training.json # Q&A pairs for training
│   └── processing_stats.json   # Processing statistics
└── parser_log.txt       # Detailed processing log
```

## Setup

1. Install required dependencies:
```bash
pip install beautifulsoup4
```

2. Create the necessary directories:
```bash
mkdir raw_data processed_data
```

3. Place your Revit API HTML documentation files in the `raw_data` directory.

## Usage

Basic usage:
```bash
python revit_parser.py raw_data
```

Specify custom output directory:
```bash
python revit_parser.py raw_data --output-dir custom_output
```

## Output Format

### Raw JSON Structure
```json
{
  "source_file": "Document.html",
  "title": "Document Class",
  "namespace": "Autodesk.Revit.DB",
  "assembly": "RevitAPI.dll",
  "inheritance": ["Object", "Element", "Document"],
  "syntax": {
    "csharp": "public class Document : Element",
    "vbnet": "Public Class Document Inherits Element"
  },
  "parameters": [...],
  "properties": [...],
  "methods": [...],
  "qa_pairs": [...]
}
```

### Training Data Format
```json
{
  "question": "What is Document Class?",
  "answer": "Document Class is a Revit API element in the Autodesk.Revit.DB namespace..."
}
```

## Example Q&A Pairs Generated

The parser automatically generates various types of questions:

1. **Description Questions**: "What is [ClassName]?"
2. **Syntax Questions**: "What is the syntax for [MethodName]?"
3. **Parameter Questions**: "What parameters does [MethodName] accept?"
4. **Return Value Questions**: "What does [MethodName] return?"
5. **Exception Questions**: "What exceptions can [MethodName] throw?"

## Troubleshooting

- Check `parser_log.txt` for detailed error messages
- Ensure HTML files are valid and contain Revit API documentation
- Verify BeautifulSoup4 is installed correctly
- For large datasets, processing may take several minutes

## Next Steps

After parsing:
1. Review the generated Q&A pairs in `revit_api_training.json`
2. Use the data to fine-tune language models for Revit API assistance
3. Consider filtering or augmenting the training data based on your specific needs
