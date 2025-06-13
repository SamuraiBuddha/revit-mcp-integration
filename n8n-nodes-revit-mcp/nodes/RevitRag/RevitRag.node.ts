import {
	IExecuteFunctions,
	INodeExecutionData,
	INodeType,
	INodeTypeDescription,
	NodeOperationError,
} from 'n8n-workflow';

import axios from 'axios';

export class RevitRag implements INodeType {
	description: INodeTypeDescription = {
		displayName: 'Revit RAG',
		name: 'revitRag',
		icon: 'file:revit.svg',
		group: ['transform'],
		version: 1,
		subtitle: '={{$parameter["operation"]}}',
		description: 'Perform RAG operations on Revit data',
		defaults: {
			name: 'Revit RAG',
		},
		inputs: ['main'],
		outputs: ['main'],
		credentials: [
			{
				name: 'revitMcpApi',
				required: true,
			},
		],
		properties: [
			{
				displayName: 'Operation',
				name: 'operation',
				type: 'options',
				noDataExpression: true,
				options: [
					{
						name: 'Query BIM Elements',
						value: 'queryElements',
						description: 'Query BIM elements using natural language',
						action: 'Query BIM elements using natural language',
					},
					{
						name: 'Find Similar Elements',
						value: 'findSimilar',
						description: 'Find similar elements to the given one',
						action: 'Find similar elements to the given one',
					},
					{
						name: 'Add to Knowledge Base',
						value: 'addToKnowledgeBase',
						description: 'Add elements to the knowledge base',
						action: 'Add elements to the knowledge base',
					},
				],
				default: 'queryElements',
			},

			// Query Elements fields
			{
				displayName: 'Query',
				name: 'query',
				type: 'string',
				displayOptions: {
					show: {
						operation: ['queryElements'],
					},
				},
				default: '',
				required: true,
				description: 'Natural language query to find elements',
				placeholder: 'Find all doors with fire rating',
			},
			{
				displayName: 'Result Limit',
				name: 'limit',
				type: 'number',
				displayOptions: {
					show: {
						operation: ['queryElements'],
					},
				},
				default: 10,
				description: 'Maximum number of results to return',
			},

			// Find Similar Elements fields
			{
				displayName: 'Element ID',
				name: 'elementId',
				type: 'number',
				displayOptions: {
					show: {
						operation: ['findSimilar'],
					},
				},
				default: 0,
				required: true,
				description: 'ID of the element to find similar ones for',
			},
			{
				displayName: 'Similarity Threshold',
				name: 'threshold',
				type: 'number',
				displayOptions: {
					show: {
						operation: ['findSimilar'],
					},
				},
				default: 0.8,
				typeOptions: {
					minValue: 0,
					maxValue: 1,
				},
				description: 'Minimum similarity score (0-1)',
			},
			{
				displayName: 'Max Results',
				name: 'maxResults',
				type: 'number',
				displayOptions: {
					show: {
						operation: ['findSimilar'],
					},
				},
				default: 5,
				description: 'Maximum number of similar elements to return',
			},

			// Add to Knowledge Base fields
			{
				displayName: 'Category',
				name: 'category',
				type: 'options',
				displayOptions: {
					show: {
						operation: ['addToKnowledgeBase'],
					},
				},
				options: [
					{ name: 'Walls', value: 'Walls' },
					{ name: 'Doors', value: 'Doors' },
					{ name: 'Windows', value: 'Windows' },
					{ name: 'Floors', value: 'Floors' },
					{ name: 'Ceilings', value: 'Ceilings' },
					{ name: 'Rooms', value: 'Rooms' },
					{ name: 'Furniture', value: 'Furniture' },
					{ name: 'Pipes', value: 'Pipes' },
					{ name: 'Ducts', value: 'Ducts' },
					{ name: 'Cable Trays', value: 'CableTrays' },
				],
				default: 'Walls',
				required: true,
				description: 'Category of elements to add to knowledge base',
			},

			// Common options
			{
				displayName: 'Options',
				name: 'options',
				type: 'collection',
				placeholder: 'Add Option',
				default: {},
				options: [
					{
						displayName: 'Include Parameters',
						name: 'includeParameters',
						type: 'boolean',
						default: true,
						description: 'Whether to include element parameters in the result',
					},
					{
						displayName: 'Include Geometry',
						name: 'includeGeometry',
						type: 'boolean',
						default: false,
						description: 'Whether to include element geometry in the result',
					},
					{
						displayName: 'Vector Database',
						name: 'vectorDatabase',
						type: 'options',
						options: [
							{ name: 'Qdrant', value: 'qdrant' },
							{ name: 'Pinecone', value: 'pinecone' },
							{ name: 'Milvus', value: 'milvus' },
						],
						default: 'qdrant',
						description: 'Vector database to use for embeddings',
					},
				],
			},
		],
	};

	async execute(this: IExecuteFunctions): Promise<INodeExecutionData[][]> {
		const items = this.getInputData();
		const returnData: INodeExecutionData[] = [];
		const credentials = await this.getCredentials('revitMcpApi');
		const apiUrl = credentials.apiUrl as string;

		// For this example, we'll simulate the RAG functionality
		// In a real implementation, this would connect to vector databases and LLMs

		for (let i = 0; i < items.length; i++) {
			try {
				const operation = this.getNodeParameter('operation', i) as string;
				const options = this.getNodeParameter('options', i, {}) as {
					includeParameters: boolean;
					includeGeometry: boolean;
					vectorDatabase: string;
				};

				let responseData;

				if (operation === 'queryElements') {
					const query = this.getNodeParameter('query', i) as string;
					const limit = this.getNodeParameter('limit', i) as number;

					// In a real implementation, this would:
					// 1. Convert the query to embeddings
					// 2. Search the vector database for similar elements
					// 3. Retrieve the actual elements from Revit
					
					// For the proof of concept, we'll call our Revit MCP API and filter the results
					// First get walls as a demonstration
					const mcpRequest = {
						action: 'getElementsByCategory',
						parameters: {
							category: 'Walls',
						},
					};
					
					// Get elements from Revit
					const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
						headers: {
							'Content-Type': 'application/json',
							'X-API-Key': credentials.apiKey,
						},
					});
					
					// Simulate RAG by filtering elements based on the query
					// In a real implementation, this would use vector similarity search
					const elements = response.data.data;
					
					// Simple keyword filtering for demonstration
					const filteredElements = elements.filter((element: any) => {
						// Look for keywords in parameters
						const paramValues = Object.values(element.parameters || {}).join(' ').toLowerCase();
						const keywords = query.toLowerCase().split(' ');
						return keywords.some(keyword => 
							paramValues.includes(keyword) || 
							(element.name && element.name.toLowerCase().includes(keyword))
						);
					}).slice(0, limit);
					
					responseData = {
						query,
						results: filteredElements,
						totalFound: filteredElements.length,
					};
				} 
				else if (operation === 'findSimilar') {
					const elementId = this.getNodeParameter('elementId', i) as number;
					const threshold = this.getNodeParameter('threshold', i) as number;
					const maxResults = this.getNodeParameter('maxResults', i) as number;
					
					// Get the source element first
					const sourceResponse = await axios.get(`${apiUrl}/api/element/${elementId}`, {
						headers: {
							'X-API-Key': credentials.apiKey,
						},
					});
					
					// For demonstration, we'll get elements of the same category
					const sourceElement = sourceResponse.data;
					const category = sourceElement.category;
					
					const mcpRequest = {
						action: 'getElementsByCategory',
						parameters: {
							category,
						},
					};
					
					// Get elements from Revit
					const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
						headers: {
							'Content-Type': 'application/json',
							'X-API-Key': credentials.apiKey,
						},
					});
					
					// Simulate similarity calculation
					// In a real implementation, this would use vector similarity
					const elements = response.data.data;
					const similarElements = elements
						.filter((element: any) => element.id !== elementId) // Exclude the source element
						.map((element: any) => {
							// Very simple similarity calculation for demonstration
							// Count matching parameters
							const sourceParams = Object.entries(sourceElement.parameters || {});
							const targetParams = Object.entries(element.parameters || {});
							
							let matchCount = 0;
							for (const [key, value] of sourceParams) {
								if (element.parameters[key] === value) {
									matchCount++;
								}
							}
							
							const similarity = sourceParams.length > 0 ? 
								matchCount / sourceParams.length : 0;
								
							return {
								...element,
								similarityScore: similarity,
							};
						})
						.filter((element: any) => element.similarityScore >= threshold)
						.sort((a: any, b: any) => b.similarityScore - a.similarityScore)
						.slice(0, maxResults);
					
					responseData = {
						sourceElement,
						similarElements,
						totalFound: similarElements.length,
					};
				}
				else if (operation === 'addToKnowledgeBase') {
					const category = this.getNodeParameter('category', i) as string;
					
					// Get elements to add to the knowledge base
					const mcpRequest = {
						action: 'getElementsByCategory',
						parameters: {
							category,
						},
					};
					
					// Get elements from Revit
					const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
						headers: {
							'Content-Type': 'application/json',
							'X-API-Key': credentials.apiKey,
						},
					});
					
					const elements = response.data.data;
					
					// In a real implementation, this would:
					// 1. Convert elements to embeddings
					// 2. Store them in the vector database
					// 3. Update the knowledge graph
					
					// For this demo, we'll simulate success
					responseData = {
						success: true,
						elementsAdded: elements.length,
						category,
						vectorDatabase: options.vectorDatabase || 'qdrant',
					};
				}

				// Add the response data to the output
				returnData.push({
					json: responseData,
					pairedItem: {
						item: i,
					},
				});
			} catch (error) {
				if (this.continueOnFail()) {
					returnData.push({
						json: {
							error: error.message,
						},
						pairedItem: {
							item: i,
						},
					});
					continue;
				}
				throw error;
			}
		}

		return [returnData];
	}
}
