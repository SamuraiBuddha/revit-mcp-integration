#!/usr/bin/env python3
"""
Revit API HTML to Markdown/JSON Parser
Converts Revit API documentation HTML files to Markdown for RAG and JSON for structured data
"""

import json
import os
import re
import logging
from bs4 import BeautifulSoup
from typing import Dict, List, Any, Optional
from pathlib import Path
import traceback


class RevitAPIParser:
    """Parser for converting Revit API HTML documentation to Markdown and JSON formats"""
    
    def __init__(self, output_dir: str = "processed_data"):
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(exist_ok=True)
        
        # Create subdirectories for different output types
        self.markdown_dir = self.output_dir / "markdown"
        self.json_dir = self.output_dir / "json"
        self.markdown_dir.mkdir(exist_ok=True)
        self.json_dir.mkdir(exist_ok=True)
        
        # Set up logging
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler('parser_log.txt'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)
        
    def parse_html_file(self, file_path: str) -> Optional[Dict[str, Any]]:
        """Parse a single HTML file and extract structured information"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                html_content = f.read()
            
            soup = BeautifulSoup(html_content, 'html.parser')
            
            # Extract the main content
            result = {
                'source_file': os.path.basename(file_path),
                'title': self._extract_title(soup),
                'namespace': self._extract_namespace(soup),
                'assembly': self._extract_assembly(soup),
                'inheritance': self._extract_inheritance(soup),
                'syntax': self._extract_syntax(soup),
                'parameters': self._extract_parameters(soup),
                'return_value': self._extract_return_value(soup),
                'properties': self._extract_properties(soup),
                'methods': self._extract_methods(soup),
                'exceptions': self._extract_exceptions(soup),
                'remarks': self._extract_remarks(soup),
                'examples': self._extract_examples(soup),
                'see_also': self._extract_see_also(soup),
                'overloads': self._extract_overloads(soup),
                'description': self._extract_description(soup)
            }
            
            # Generate Q&A pairs for training
            result['qa_pairs'] = self._generate_qa_pairs(result)
            
            # Generate markdown content
            markdown_content = self._generate_markdown(result)
            
            # Save markdown file
            self._save_markdown(file_path, markdown_content)
            
            return result
            
        except Exception as e:
            self.logger.error(f"Error parsing {file_path}: {str(e)}")
            self.logger.error(traceback.format_exc())
            return None
    
    def _extract_title(self, soup: BeautifulSoup) -> str:
        """Extract the main title/name of the API element"""
        title_elem = soup.find('h1', class_='title') or soup.find('title')
        return title_elem.get_text(strip=True) if title_elem else ""
    
    def _extract_description(self, soup: BeautifulSoup) -> str:
        """Extract the main description/summary"""
        # Look for summary or description sections
        summary = soup.find('div', class_='summary') or soup.find('p', class_='description')
        if summary:
            return summary.get_text(strip=True)
        
        # Sometimes it's the first paragraph after the title
        title = soup.find('h1')
        if title:
            next_p = title.find_next_sibling('p')
            if next_p:
                return next_p.get_text(strip=True)
        
        return ""
    
    def _extract_namespace(self, soup: BeautifulSoup) -> str:
        """Extract namespace information"""
        namespace_elem = soup.find(text=re.compile(r'Namespace:'))
        if namespace_elem:
            parent = namespace_elem.find_parent()
            if parent:
                namespace_link = parent.find('a')
                if namespace_link:
                    return namespace_link.get_text(strip=True)
        return ""
    
    def _extract_assembly(self, soup: BeautifulSoup) -> str:
        """Extract assembly information"""
        assembly_elem = soup.find(text=re.compile(r'Assembly:'))
        if assembly_elem:
            parent = assembly_elem.find_parent()
            if parent:
                return parent.get_text(strip=True).replace('Assembly:', '').strip()
        return ""
    
    def _extract_inheritance(self, soup: BeautifulSoup) -> List[str]:
        """Extract inheritance hierarchy"""
        inheritance = []
        inheritance_section = soup.find('div', class_='inheritance')
        if inheritance_section:
            links = inheritance_section.find_all('a')
            inheritance = [link.get_text(strip=True) for link in links]
        return inheritance
    
    def _extract_syntax(self, soup: BeautifulSoup) -> Dict[str, str]:
        """Extract syntax in multiple languages"""
        syntax = {}
        syntax_section = soup.find('div', class_='codeSnippetContainer')
        if syntax_section:
            # Look for C# syntax
            cs_elem = syntax_section.find('div', {'data-language': 'cs'})
            if cs_elem:
                syntax['csharp'] = cs_elem.get_text(strip=True)
            
            # Look for VB.NET syntax
            vb_elem = syntax_section.find('div', {'data-language': 'vb'})
            if vb_elem:
                syntax['vbnet'] = vb_elem.get_text(strip=True)
                
            # Look for C++ syntax
            cpp_elem = syntax_section.find('div', {'data-language': 'cpp'})
            if cpp_elem:
                syntax['cpp'] = cpp_elem.get_text(strip=True)
        
        return syntax
    
    def _extract_parameters(self, soup: BeautifulSoup) -> List[Dict[str, str]]:
        """Extract method parameters"""
        parameters = []
        params_section = soup.find('div', class_='parameters')
        if params_section:
            param_items = params_section.find_all('dt')
            for param in param_items:
                param_name = param.get_text(strip=True)
                param_desc = param.find_next_sibling('dd')
                if param_desc:
                    parameters.append({
                        'name': param_name,
                        'description': param_desc.get_text(strip=True)
                    })
        return parameters
    
    def _extract_return_value(self, soup: BeautifulSoup) -> Dict[str, str]:
        """Extract return value information"""
        return_section = soup.find('h4', text='Return Value')
        if return_section:
            return_desc = return_section.find_next_sibling()
            if return_desc:
                return {
                    'type': self._extract_type_from_element(return_desc),
                    'description': return_desc.get_text(strip=True)
                }
        return {}
    
    def _extract_properties(self, soup: BeautifulSoup) -> List[Dict[str, Any]]:
        """Extract properties of a class/interface"""
        properties = []
        props_table = soup.find('table', class_='members')
        if props_table:
            rows = props_table.find_all('tr')[1:]  # Skip header
            for row in rows:
                cols = row.find_all('td')
                if len(cols) >= 2:
                    prop_link = cols[0].find('a')
                    if prop_link:
                        properties.append({
                            'name': prop_link.get_text(strip=True),
                            'description': cols[1].get_text(strip=True)
                        })
        return properties
    
    def _extract_methods(self, soup: BeautifulSoup) -> List[Dict[str, Any]]:
        """Extract methods of a class/interface"""
        methods = []
        methods_table = soup.find('table', class_='members')
        if methods_table:
            rows = methods_table.find_all('tr')[1:]  # Skip header
            for row in rows:
                cols = row.find_all('td')
                if len(cols) >= 2:
                    method_link = cols[0].find('a')
                    if method_link:
                        methods.append({
                            'name': method_link.get_text(strip=True),
                            'description': cols[1].get_text(strip=True)
                        })
        return methods
    
    def _extract_exceptions(self, soup: BeautifulSoup) -> List[Dict[str, str]]:
        """Extract exception information"""
        exceptions = []
        exceptions_section = soup.find('h4', text='Exceptions')
        if exceptions_section:
            exc_table = exceptions_section.find_next_sibling('table')
            if exc_table:
                rows = exc_table.find_all('tr')[1:]  # Skip header
                for row in rows:
                    cols = row.find_all('td')
                    if len(cols) >= 2:
                        exceptions.append({
                            'type': cols[0].get_text(strip=True),
                            'condition': cols[1].get_text(strip=True)
                        })
        return exceptions
    
    def _extract_remarks(self, soup: BeautifulSoup) -> str:
        """Extract remarks section"""
        remarks_section = soup.find('h4', text='Remarks')
        if remarks_section:
            remarks_content = remarks_section.find_next_sibling()
            if remarks_content:
                return remarks_content.get_text(strip=True)
        return ""
    
    def _extract_examples(self, soup: BeautifulSoup) -> List[Dict[str, str]]:
        """Extract code examples"""
        examples = []
        examples_section = soup.find('h4', text='Examples')
        if examples_section:
            example_blocks = examples_section.find_next_siblings('div', class_='codeSnippetContainer')
            for block in example_blocks:
                language = block.get('data-language', 'unknown')
                code = block.get_text(strip=True)
                examples.append({
                    'language': language,
                    'code': code
                })
        return examples
    
    def _extract_see_also(self, soup: BeautifulSoup) -> List[str]:
        """Extract see also references"""
        see_also = []
        see_also_section = soup.find('h4', text='See Also')
        if see_also_section:
            links_container = see_also_section.find_next_sibling()
            if links_container:
                links = links_container.find_all('a')
                see_also = [link.get_text(strip=True) for link in links]
        return see_also
    
    def _extract_overloads(self, soup: BeautifulSoup) -> List[Dict[str, Any]]:
        """Extract method overloads"""
        overloads = []
        overload_section = soup.find('h2', text=re.compile(r'Overload'))
        if overload_section:
            overload_table = overload_section.find_next_sibling('table')
            if overload_table:
                rows = overload_table.find_all('tr')[1:]  # Skip header
                for row in rows:
                    cols = row.find_all('td')
                    if len(cols) >= 2:
                        overloads.append({
                            'signature': cols[0].get_text(strip=True),
                            'description': cols[1].get_text(strip=True)
                        })
        return overloads
    
    def _extract_type_from_element(self, element) -> str:
        """Extract type information from an element"""
        type_link = element.find('a')
        if type_link:
            return type_link.get_text(strip=True)
        return element.get_text(strip=True).split()[0] if element else ""
    
    def _generate_markdown(self, data: Dict[str, Any]) -> str:
        """Generate markdown content from parsed data"""
        md_lines = []
        
        # Title
        if data['title']:
            md_lines.append(f"# {data['title']}")
            md_lines.append("")
        
        # Namespace and Assembly
        if data['namespace'] or data['assembly']:
            if data['namespace']:
                md_lines.append(f"**Namespace:** `{data['namespace']}`")
            if data['assembly']:
                md_lines.append(f"**Assembly:** {data['assembly']}")
            md_lines.append("")
        
        # Description
        if data['description']:
            md_lines.append(data['description'])
            md_lines.append("")
        
        # Inheritance
        if data['inheritance']:
            md_lines.append("## Inheritance")
            md_lines.append(" → ".join(data['inheritance']))
            md_lines.append("")
        
        # Syntax
        if data['syntax']:
            md_lines.append("## Syntax")
            for lang, syntax in data['syntax'].items():
                lang_name = {
                    'csharp': 'C#',
                    'vbnet': 'VB.NET',
                    'cpp': 'C++'
                }.get(lang, lang)
                md_lines.append(f"### {lang_name}")
                md_lines.append("```" + lang)
                md_lines.append(syntax)
                md_lines.append("```")
            md_lines.append("")
        
        # Parameters
        if data['parameters']:
            md_lines.append("## Parameters")
            for param in data['parameters']:
                md_lines.append(f"- **{param['name']}**: {param['description']}")
            md_lines.append("")
        
        # Return Value
        if data['return_value']:
            md_lines.append("## Return Value")
            md_lines.append(f"**Type:** `{data['return_value']['type']}`")
            md_lines.append(f"{data['return_value']['description']}")
            md_lines.append("")
        
        # Properties
        if data['properties']:
            md_lines.append("## Properties")
            md_lines.append("| Name | Description |")
            md_lines.append("|------|-------------|")
            for prop in data['properties']:
                md_lines.append(f"| {prop['name']} | {prop['description']} |")
            md_lines.append("")
        
        # Methods
        if data['methods']:
            md_lines.append("## Methods")
            md_lines.append("| Name | Description |")
            md_lines.append("|------|-------------|")
            for method in data['methods']:
                md_lines.append(f"| {method['name']} | {method['description']} |")
            md_lines.append("")
        
        # Exceptions
        if data['exceptions']:
            md_lines.append("## Exceptions")
            md_lines.append("| Exception Type | Condition |")
            md_lines.append("|----------------|-----------|")
            for exc in data['exceptions']:
                md_lines.append(f"| {exc['type']} | {exc['condition']} |")
            md_lines.append("")
        
        # Remarks
        if data['remarks']:
            md_lines.append("## Remarks")
            md_lines.append(data['remarks'])
            md_lines.append("")
        
        # Examples
        if data['examples']:
            md_lines.append("## Examples")
            for example in data['examples']:
                lang_name = {
                    'cs': 'csharp',
                    'vb': 'vbnet',
                    'cpp': 'cpp'
                }.get(example['language'], example['language'])
                md_lines.append(f"```{lang_name}")
                md_lines.append(example['code'])
                md_lines.append("```")
                md_lines.append("")
        
        # See Also
        if data['see_also']:
            md_lines.append("## See Also")
            for ref in data['see_also']:
                md_lines.append(f"- {ref}")
            md_lines.append("")
        
        return "\n".join(md_lines)
    
    def _save_markdown(self, source_path: str, markdown_content: str) -> None:
        """Save markdown content to file"""
        source_file = Path(source_path)
        # Create same directory structure in markdown output
        relative_path = source_file.relative_to(source_file.parent.parent) if source_file.parent.parent in source_file.parents else source_file.name
        
        output_path = self.markdown_dir / relative_path.with_suffix('.md')
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(markdown_content)
        
        self.logger.info(f"Saved markdown: {output_path}")
    
    def _generate_qa_pairs(self, data: Dict[str, Any]) -> List[Dict[str, str]]:
        """Generate Q&A pairs for training from parsed data"""
        qa_pairs = []
        
        # Basic description question
        if data['title']:
            qa_pairs.append({
                'question': f"What is {data['title']}?",
                'answer': self._generate_description_answer(data)
            })
        
        # Syntax question
        if data['syntax']:
            qa_pairs.append({
                'question': f"What is the syntax for {data['title']}?",
                'answer': self._generate_syntax_answer(data)
            })
        
        # Parameters question
        if data['parameters']:
            qa_pairs.append({
                'question': f"What parameters does {data['title']} accept?",
                'answer': self._generate_parameters_answer(data)
            })
        
        # Return value question
        if data['return_value']:
            qa_pairs.append({
                'question': f"What does {data['title']} return?",
                'answer': self._generate_return_answer(data)
            })
        
        # Exception handling question
        if data['exceptions']:
            qa_pairs.append({
                'question': f"What exceptions can {data['title']} throw?",
                'answer': self._generate_exceptions_answer(data)
            })
        
        return qa_pairs
    
    def _generate_description_answer(self, data: Dict[str, Any]) -> str:
        """Generate a comprehensive description answer"""
        parts = []
        
        if data['title']:
            parts.append(f"{data['title']} is a Revit API element")
        
        if data['namespace']:
            parts.append(f"in the {data['namespace']} namespace")
        
        if data['assembly']:
            parts.append(f"from the {data['assembly']} assembly")
        
        if data['description']:
            parts.append(f"\n\n{data['description']}")
        
        if data['inheritance']:
            parts.append(f"\n\nIt inherits from: {' → '.join(data['inheritance'])}")
        
        if data['remarks']:
            parts.append(f"\n\n{data['remarks']}")
        
        return ". ".join(parts)
    
    def _generate_syntax_answer(self, data: Dict[str, Any]) -> str:
        """Generate syntax answer"""
        parts = []
        
        for lang, syntax in data['syntax'].items():
            lang_name = {
                'csharp': 'C#',
                'vbnet': 'VB.NET',
                'cpp': 'C++'
            }.get(lang, lang)
            parts.append(f"{lang_name}:\n{syntax}")
        
        return "\n\n".join(parts)
    
    def _generate_parameters_answer(self, data: Dict[str, Any]) -> str:
        """Generate parameters answer"""
        if not data['parameters']:
            return f"{data['title']} does not accept any parameters."
        
        parts = [f"{data['title']} accepts the following parameters:"]
        
        for param in data['parameters']:
            parts.append(f"- {param['name']}: {param['description']}")
        
        return "\n".join(parts)
    
    def _generate_return_answer(self, data: Dict[str, Any]) -> str:
        """Generate return value answer"""
        if not data['return_value']:
            return f"{data['title']} does not return a value."
        
        return f"{data['title']} returns {data['return_value']['type']}: {data['return_value']['description']}"
    
    def _generate_exceptions_answer(self, data: Dict[str, Any]) -> str:
        """Generate exceptions answer"""
        if not data['exceptions']:
            return f"{data['title']} does not throw any documented exceptions."
        
        parts = [f"{data['title']} can throw the following exceptions:"]
        
        for exc in data['exceptions']:
            parts.append(f"- {exc['type']}: {exc['condition']}")
        
        return "\n".join(parts)
    
    def batch_process(self, input_dir: str) -> None:
        """Process all HTML files in a directory"""
        input_path = Path(input_dir)
        html_files = list(input_path.rglob("*.html"))
        
        self.logger.info(f"Found {len(html_files)} HTML files to process")
        
        all_results = []
        training_data = []
        
        for i, file_path in enumerate(html_files, 1):
            self.logger.info(f"Processing {i}/{len(html_files)}: {file_path.name}")
            
            result = self.parse_html_file(str(file_path))
            if result:
                all_results.append(result)
                
                # Extract Q&A pairs for training
                for qa in result.get('qa_pairs', []):
                    training_data.append(qa)
        
        # Save raw parsed data
        raw_output = self.json_dir / 'revit_api_raw.json'
        with open(raw_output, 'w', encoding='utf-8') as f:
            json.dump(all_results, f, indent=2, ensure_ascii=False)
        
        self.logger.info(f"Saved raw data to {raw_output}")
        
        # Save training data
        training_output = self.json_dir / 'revit_api_training.json'
        with open(training_output, 'w', encoding='utf-8') as f:
            json.dump(training_data, f, indent=2, ensure_ascii=False)
        
        self.logger.info(f"Saved {len(training_data)} Q&A pairs to {training_output}")
        
        # Save summary statistics
        stats = {
            'total_files_processed': len(all_results),
            'total_qa_pairs': len(training_data),
            'files_with_errors': len(html_files) - len(all_results),
            'markdown_files_created': len(all_results),
            'namespaces': list(set(r['namespace'] for r in all_results if r['namespace'])),
            'assemblies': list(set(r['assembly'] for r in all_results if r['assembly']))
        }
        
        stats_output = self.json_dir / 'processing_stats.json'
        with open(stats_output, 'w', encoding='utf-8') as f:
            json.dump(stats, f, indent=2)
        
        self.logger.info(f"Processing complete! Stats saved to {stats_output}")
        self.logger.info(f"Markdown files saved to {self.markdown_dir}")


def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(description='Parse Revit API HTML documentation to Markdown and JSON')
    parser.add_argument('input_dir', help='Directory containing HTML files')
    parser.add_argument('--output-dir', default='processed_data', help='Output directory')
    
    args = parser.parse_args()
    
    parser = RevitAPIParser(output_dir=args.output_dir)
    parser.batch_process(args.input_dir)


if __name__ == '__main__':
    main()
