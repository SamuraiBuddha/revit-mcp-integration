# Revit API HTML to Markdown/JSON Parser

This tool converts Revit API documentation from HTML format to:
1. **Markdown files** - Perfect for RAG (Retrieval-Augmented Generation) systems
2. **Structured JSON** - For AI training and structured data processing

## Features

- Parses Revit API HTML documentation files
- Generates clean, formatted Markdown files for each API element
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
├── processed_data/      # Output directory
│   ├── markdown/        # Markdown files for RAG
│   │   └── *.md        # One .md file per HTML file
│   └── json/           # JSON output
│       ├── revit_api_raw.json      # Complete parsed data
│       ├── revit_api_training.json # Q&A pairs for training
│       └── processing_stats.json   # Processing statistics
└── parser_log.txt       # Detailed processing log
```

## Setup

1. Install required dependencies:
```bash
pip install beautifulsoup4 lxml
```

2. Create the necessary directories:
```bash
mkdir raw_data
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

## Output Formats

### Markdown Output (for RAG)
Each HTML file is converted to a clean Markdown file with:
- Proper heading hierarchy
- Code blocks with syntax highlighting
- Tables for properties, methods, and exceptions
- Clear formatting for easy chunking and retrieval

Example markdown structure:
```markdown
# Document Class

**Namespace:** `Autodesk.Revit.DB`
**Assembly:** RevitAPI.dll

Represents an open Revit project or family document.

## Inheritance
Object → Element → Document

## Syntax
### C#
```csharp
public class Document : Element
```

## Properties
| Name | Description |
|------|-------------|
| Title | Gets the title of the document |
| PathName | Gets the full path of the document file |

## Methods
| Name | Description |
|------|-------------|
| Create | Creates a new element in the document |
| Delete | Deletes an element from the document |
```

### JSON Output Structure
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

## Using with RAG Systems

The markdown output is optimized for RAG systems:

1. **Clean Structure**: Each markdown file represents one API element with consistent formatting
2. **Easy Chunking**: Clear section headers make it easy to split documents
3. **Semantic Search**: Well-formatted content improves embedding quality
4. **Cross-References**: Preserves links and relationships between API elements

Example RAG workflow:
```python
# Load markdown files into your RAG system
import os
from pathlib import Path

markdown_dir = Path("processed_data/markdown")
for md_file in markdown_dir.rglob("*.md"):
    with open(md_file, 'r', encoding='utf-8') as f:
        content = f.read()
        # Add to your vector database
        # Create embeddings
        # Index for retrieval
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
1. **For RAG**: Load markdown files into your vector database
2. **For Training**: Use the Q&A pairs in `revit_api_training.json`
3. **For Analysis**: Review statistics in `processing_stats.json`
4. Consider filtering or augmenting the data based on your specific needs
