# Test script for RevitMcpServer endpoints
Write-Host "Testing RevitMcpServer endpoints..." -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:7891/api"

# Function to test an endpoint
function Test-Endpoint {
    param (
        [string]$Endpoint,
        [string]$Description
    )
    
    Write-Host "Testing: $Description" -ForegroundColor Yellow
    Write-Host "URL: $baseUrl$Endpoint"
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$Endpoint" -Method Get -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Success!" -ForegroundColor Green
            $content = $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
            Write-Host $content -ForegroundColor Gray
        } else {
            Write-Host "✗ Failed with status: $($response.StatusCode)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
        
        # Check if it's a connection error
        if ($_.Exception.Message -like "*Unable to connect*") {
            Write-Host "Make sure Revit is running and the add-in is loaded." -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
}

# Test health endpoint
Test-Endpoint -Endpoint "/health" -Description "Health Check"

# Test Revit version endpoint
Test-Endpoint -Endpoint "/revit/version" -Description "Revit Version Information"

Write-Host "Testing complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "If tests failed, check:" -ForegroundColor Yellow
Write-Host "1. Is Revit 2024 running?"
Write-Host "2. Did the add-in load successfully?"
Write-Host "3. Check logs at: $env:LOCALAPPDATA\RevitMcpServer\logs"
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
