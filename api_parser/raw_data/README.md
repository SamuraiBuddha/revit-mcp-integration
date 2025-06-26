# Raw Data Directory

Place your Revit API HTML documentation files here. The parser will recursively process all HTML files found in this directory and its subdirectories.

## Expected File Structure

The parser can handle HTML files organized in any structure, such as:
- Flat structure with all HTML files in this directory
- Organized by namespace (e.g., `Autodesk.Revit.DB/`, `Autodesk.Revit.UI/`)
- Organized by type (e.g., `Classes/`, `Interfaces/`, `Enums/`)

## Supported HTML Format

The parser expects HTML files generated from Revit API documentation with standard structure including:
- Title/heading elements
- Namespace and assembly information
- Syntax blocks with language tabs
- Parameter and return value sections
- Properties and methods tables
- Exception documentation
- Code examples

## Getting Revit API Documentation

You can obtain the HTML documentation from:
1. The Revit SDK documentation
2. Online Revit API documentation
3. Generated documentation from the Revit assemblies
