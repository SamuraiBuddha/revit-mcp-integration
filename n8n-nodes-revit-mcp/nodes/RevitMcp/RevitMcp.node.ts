import {
	IExecuteFunctions,
	INodeExecutionData,
	INodeType,
	INodeTypeDescription,
	NodeOperationError,
} from 'n8n-workflow';

import axios from 'axios';

export class RevitMcp implements INodeType {
	description: INodeTypeDescription = {
		displayName: 'Revit MCP',
		name: 'revitMcp',
		icon: 'file:revit.svg',
		group: ['transform'],
		version: 1,
		subtitle: '={{$parameter["operation"] + ": " + $parameter["resource"]}}',
		description: 'Interact with Revit through MCP',
		defaults: {
			name: 'Revit MCP',
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
			// Resources
			{
				displayName: 'Resource',
				name: 'resource',
				type: 'options',
				noDataExpression: true,
				options: [
					{
						name: 'Element',
						value: 'element',
					},
					{
						name: 'Dynamo',
						value: 'dynamo',
					},
				],
				default: 'element',
				required: true,
				description: 'Resource to operate on',
			},

			// Element operations
			{
				displayName: 'Operation',
				name: 'operation',
				type: 'options',
				noDataExpression: true,
				displayOptions: {
					show: {
						resource: ['element'],
					},
				},
				options: [
					{
						name: 'Get by Category',
						value: 'getByCategory',
						description: 'Get elements by category',
						action: 'Get elements by category',
					},
					{
						name: 'Get by Type',
						value: 'getByType',
						description: 'Get elements by type',
						action: 'Get elements by type',
					},
					{
						name: 'Get by ID',
						value: 'getById',
						description: 'Get element by ID',
						action: 'Get element by ID',
					},
					{
						name: 'Update Parameter',
						value: 'updateParameter',
						description: 'Update element parameter',
						action: 'Update element parameter',
					},
				],
				default: 'getByCategory',
			},

			// Dynamo operations
			{
				displayName: 'Operation',
				name: 'operation',
				type: 'options',
				noDataExpression: true,
				displayOptions: {
					show: {
						resource: ['dynamo'],
					},
				},
				options: [
					{
						name: 'Run Script',
						value: 'runScript',
						description: 'Run a Dynamo script',
						action: 'Run a Dynamo script',
					},
					{
						name: 'List Scripts',
						value: 'listScripts',
						description: 'List available Dynamo scripts',
						action: 'List available Dynamo scripts',
					},
				],
				default: 'runScript',
			},

			// Element Category field
			{
				displayName: 'Category',
				name: 'category',
				type: 'options',
				displayOptions: {
					show: {
						resource: ['element'],
						operation: ['getByCategory'],
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
				description: 'Category of elements to retrieve',
			},

			// Element Type field
			{
				displayName: 'Type',
				name: 'type',
				type: 'string',
				displayOptions: {
					show: {
						resource: ['element'],
						operation: ['getByType'],
					},
				},
				default: 'Wall',
				required: true,
				description: 'Type of elements to retrieve',
			},

			// Element ID field
			{
				displayName: 'Element ID',
				name: 'elementId',
				type: 'number',
				displayOptions: {
					show: {
						resource: ['element'],
						operation: ['getById', 'updateParameter'],
					},
				},
				default: 0,
				required: true,
				description: 'ID of the element',
			},

			// Parameter fields
			{
				displayName: 'Parameter Name',
				name: 'parameterName',
				type: 'string',
				displayOptions: {
					show: {
						resource: ['element'],
						operation: ['updateParameter'],
					},
				},
				default: '',
				required: true,
				description: 'Name of the parameter to update',
			},
			{
				displayName: 'Parameter Value',
				name: 'parameterValue',
				type: 'string',
				displayOptions: {
					show: {
						resource: ['element'],
						operation: ['updateParameter'],
					},
				},
				default: '',
				required: true,
				description: 'New value for the parameter',
			},

			// Dynamo Script Path
			{
				displayName: 'Script Path',
				name: 'scriptPath',
				type: 'string',
				displayOptions: {
					show: {
						resource: ['dynamo'],
						operation: ['runScript'],
					},
				},
				default: '',
				required: true,
				description: 'Path to the Dynamo script to run',
			},

			// Dynamo Script Parameters
			{
				displayName: 'Script Parameters',
				name: 'scriptParameters',
				type: 'fixedCollection',
				typeOptions: {
					multipleValues: true,
				},
				displayOptions: {
					show: {
						resource: ['dynamo'],
						operation: ['runScript'],
					},
				},
				default: {},
				options: [
					{
						name: 'parameters',
						displayName: 'Parameters',
						values: [
							{
								displayName: 'Parameter Name',
								name: 'name',
								type: 'string',
								default: '',
								description: 'Name of the parameter',
							},
							{
								displayName: 'Parameter Value',
								name: 'value',
								type: 'string',
								default: '',
								description: 'Value of the parameter',
							},
						],
					},
				],
				description: 'Parameters to pass to the Dynamo script',
			},

			// Dynamo Directory
			{
				displayName: 'Directory',
				name: 'directory',
				type: 'string',
				displayOptions: {
					show: {
						resource: ['dynamo'],
						operation: ['listScripts'],
					},
				},
				default: '',
				required: false,
				description: 'Directory containing Dynamo scripts (leave empty for default)',
			},
		],
	};

	async execute(this: IExecuteFunctions): Promise<INodeExecutionData[][]> {
		const items = this.getInputData();
		const returnData: INodeExecutionData[] = [];
		const credentials = await this.getCredentials('revitMcpApi');
		const apiUrl = credentials.apiUrl as string;
		
		// For testing, check if the API URL is accessible
		try {
			await axios.get(`${apiUrl}/api/status`, {
				headers: {
					'X-API-Key': credentials.apiKey,
				},
			});
		} catch (error) {
			throw new NodeOperationError(this.getNode(), `Unable to connect to Revit MCP server at ${apiUrl}: ${error.message}`);
		}

		for (let i = 0; i < items.length; i++) {
			try {
				const resource = this.getNodeParameter('resource', i) as string;
				const operation = this.getNodeParameter('operation', i) as string;

				let responseData;

				// Implement resource-specific operations
				if (resource === 'element') {
					// Element operations
					if (operation === 'getByCategory') {
						const category = this.getNodeParameter('category', i) as string;
						
						// Create MCP request for getting elements by category
						const mcpRequest = {
							action: 'getElementsByCategory',
							parameters: {
								category,
							},
						};
						
						// Send request to Element MCP endpoint
						const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
							headers: {
								'Content-Type': 'application/json',
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data.data;
					} 
					else if (operation === 'getByType') {
						const type = this.getNodeParameter('type', i) as string;
						
						// Create MCP request for getting elements by type
						const mcpRequest = {
							action: 'getElementsByType',
							parameters: {
								type,
							},
						};
						
						// Send request to Element MCP endpoint
						const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
							headers: {
								'Content-Type': 'application/json',
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data.data;
					}
					else if (operation === 'getById') {
						const elementId = this.getNodeParameter('elementId', i) as number;
						
						// Regular API call for getting element by ID
						const response = await axios.get(`${apiUrl}/api/element/${elementId}`, {
							headers: {
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data;
					}
					else if (operation === 'updateParameter') {
						const elementId = this.getNodeParameter('elementId', i) as number;
						const parameterName = this.getNodeParameter('parameterName', i) as string;
						const value = this.getNodeParameter('parameterValue', i) as string;
						
						// Create MCP request for updating parameter
						const mcpRequest = {
							action: 'modifyElementParameter',
							parameters: {
								elementId,
								parameterName,
								value,
							},
						};
						
						// Send request to Element MCP endpoint
						const response = await axios.post(`${apiUrl}/api/element/mcp`, mcpRequest, {
							headers: {
								'Content-Type': 'application/json',
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data.data;
					}
				} 
				else if (resource === 'dynamo') {
					// Dynamo operations
					if (operation === 'runScript') {
						const scriptPath = this.getNodeParameter('scriptPath', i) as string;
						const scriptParametersCollection = this.getNodeParameter('scriptParameters.parameters', i, []) as Array<{ name: string, value: string }>;
						
						// Convert parameters to object
						const scriptParameters: Record<string, any> = {};
						for (const param of scriptParametersCollection) {
							scriptParameters[param.name] = param.value;
						}
						
						// Create MCP request for running Dynamo script
						const mcpRequest = {
							action: 'runDynamoScript',
							parameters: {
								scriptPath,
								scriptParameters,
							},
						};
						
						// Send request to Dynamo MCP endpoint
						const response = await axios.post(`${apiUrl}/api/dynamo/mcp`, mcpRequest, {
							headers: {
								'Content-Type': 'application/json',
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data.data;
					}
					else if (operation === 'listScripts') {
						const directory = this.getNodeParameter('directory', i, '') as string;
						
						// Create MCP request for listing Dynamo scripts
						const mcpRequest = {
							action: 'listDynamoScripts',
							parameters: {
								directory,
							},
						};
						
						// Send request to Dynamo MCP endpoint
						const response = await axios.post(`${apiUrl}/api/dynamo/mcp`, mcpRequest, {
							headers: {
								'Content-Type': 'application/json',
								'X-API-Key': credentials.apiKey,
							},
						});
						
						responseData = response.data.data;
					}
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
