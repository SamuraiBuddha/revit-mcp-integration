import {
	ICredentialType,
	INodeProperties,
} from 'n8n-workflow';

export class RevitMcpApi implements ICredentialType {
	name = 'revitMcpApi';
	displayName = 'Revit MCP API';
	documentationUrl = 'https://github.com/SamuraiBuddha/revit-mcp-integration';
	properties: INodeProperties[] = [
		{
			displayName: 'API URL',
			name: 'apiUrl',
			type: 'string',
			default: 'http://localhost:5000',
			required: true,
			description: 'URL of the Revit MCP server',
		},
		{
			displayName: 'API Key',
			name: 'apiKey',
			type: 'string',
			typeOptions: { password: true },
			default: '',
			description: 'API key for authentication (if enabled)',
		},
	];

	// This method can be used to add headers to the requests
	authenticate = {
		type: 'header',
		properties: {
			header: {
				name: 'X-API-Key',
				value: '={{$credentials.apiKey}}',
			},
		},
	};

	// Optional pre-validation of the credentials
	test = {
		request: {
			baseURL: '={{$credentials.apiUrl}}',
			url: '/api/status',
		},
	};
}
