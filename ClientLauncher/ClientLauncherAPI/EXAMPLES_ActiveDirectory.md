# Active Directory API - Test Examples

## Postman/HTTP Client Examples

### 1. Get All Computers (Basic)

```http
GET https://localhost:7001/api/ActiveDirectory/computers
Authorization: Bearer YOUR_JWT_TOKEN
```

### 2. Get All Enabled Computers with Online Check

```http
GET https://localhost:7001/api/ActiveDirectory/computers?EnabledOnly=true&CheckOnlineStatus=true
Authorization: Bearer YOUR_JWT_TOKEN
```

### 3. Search Computers by Pattern

```http
GET https://localhost:7001/api/ActiveDirectory/computers/search/DESKTOP
Authorization: Bearer YOUR_JWT_TOKEN
```

### 4. Get Specific Computer

```http
GET https://localhost:7001/api/ActiveDirectory/computers/PC-001
Authorization: Bearer YOUR_JWT_TOKEN
```

### 5. Check if Computer is Online

```http
GET https://localhost:7001/api/ActiveDirectory/computers/PC-001/online
Authorization: Bearer YOUR_JWT_TOKEN
```

### 6. Get All Organizational Units

```http
GET https://localhost:7001/api/ActiveDirectory/organizational-units
Authorization: Bearer YOUR_JWT_TOKEN
```

### 7. Get Computers in Specific OU

```http
GET https://localhost:7001/api/ActiveDirectory/organizational-units/OU=IT,DC=company,DC=com/computers
Authorization: Bearer YOUR_JWT_TOKEN
```

### 8. Bulk Deploy to OU (All enabled computers)

```http
POST https://localhost:7001/api/ActiveDirectory/bulk-deploy
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "packageVersionId": 1,
  "organizationalUnit": "OU=IT,DC=company,DC=com",
  "environment": "Production",
  "deploymentType": "Release",
  "requiresApproval": false,
  "deployedBy": "admin@company.com",
  "enabledComputersOnly": true,
  "onlineComputersOnly": false
}
```

### 9. Bulk Deploy to Specific Computers

```http
POST https://localhost:7001/api/ActiveDirectory/bulk-deploy
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "packageVersionId": 1,
  "targetComputerNames": [
    "PC-001",
    "PC-002",
    "LAPTOP-005"
  ],
  "environment": "Production",
  "deploymentType": "Release",
  "requiresApproval": false,
  "deployedBy": "admin@company.com",
  "enabledComputersOnly": true,
  "onlineComputersOnly": true
}
```

### 10. Schedule Deployment for Later

```http
POST https://localhost:7001/api/ActiveDirectory/bulk-deploy
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "packageVersionId": 1,
  "organizationalUnit": "OU=IT,DC=company,DC=com",
  "environment": "Production",
  "deploymentType": "Release",
  "requiresApproval": false,
  "deployedBy": "admin@company.com",
  "scheduledFor": "2024-01-20T02:00:00Z",
  "enabledComputersOnly": true,
  "onlineComputersOnly": true
}
```

### 11. Preview AD Sync

```http
POST https://localhost:7001/api/ActiveDirectory/sync/preview
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "organizationalUnit": "OU=Computers,DC=company,DC=com",
  "autoRegisterNewMachines": true,
  "updateExistingMachines": true,
  "enabledOnly": true
}
```

### 12. Get Computers with OS Filter

```http
GET https://localhost:7001/api/ActiveDirectory/computers?OperatingSystemFilter=Windows 10&EnabledOnly=true
Authorization: Bearer YOUR_JWT_TOKEN
```

## PowerShell Examples

### Get All Computers

```powershell
$token = "YOUR_JWT_TOKEN"
$headers = @{
    "Authorization" = "Bearer $token"
}

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/ActiveDirectory/computers" -Headers $headers -Method Get
$response.computers | Format-Table Name, DnsHostName, OperatingSystem, Enabled
```

### Bulk Deploy to OU

```powershell
$token = "YOUR_JWT_TOKEN"
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$body = @{
    packageVersionId = 1
    organizationalUnit = "OU=IT,DC=company,DC=com"
    environment = "Production"
    deploymentType = "Release"
    requiresApproval = $false
    deployedBy = "admin@company.com"
    enabledComputersOnly = $true
    onlineComputersOnly = $true
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/ActiveDirectory/bulk-deploy" -Headers $headers -Method Post -Body $body
Write-Host "Total Computers: $($response.totalComputers)"
Write-Host "Deployments Created: $($response.deploymentsCreated)"
Write-Host "Deployments Failed: $($response.deploymentsFailed)"
```

### Check Multiple Computers Online Status

```powershell
$token = "YOUR_JWT_TOKEN"
$headers = @{
    "Authorization" = "Bearer $token"
}

$computers = @("PC-001", "PC-002", "LAPTOP-005")

foreach ($computer in $computers) {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/api/ActiveDirectory/computers/$computer/online" -Headers $headers -Method Get
    Write-Host "$($response.computerName): Online = $($response.isOnline)"
}
```

## C# Client Example

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

public class ADClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://localhost:7001";
    
    public ADClient(string jwtToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", jwtToken);
    }
    
    public async Task<ADComputerListResponse> GetAllComputersAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/ActiveDirectory/computers");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ADComputerListResponse>(json);
    }
    
    public async Task<BulkDeploymentResponse> BulkDeployAsync(ADBulkDeploymentRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/ActiveDirectory/bulk-deploy", 
            content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BulkDeploymentResponse>(responseJson);
    }
}

// Usage
var client = new ADClient("YOUR_JWT_TOKEN");
var computers = await client.GetAllComputersAsync();

Console.WriteLine($"Found {computers.TotalCount} computers");
Console.WriteLine($"Enabled: {computers.EnabledCount}");
Console.WriteLine($"Online: {computers.OnlineCount}");

// Deploy to OU
var deployRequest = new ADBulkDeploymentRequest
{
    PackageVersionId = 1,
    OrganizationalUnit = "OU=IT,DC=company,DC=com",
    Environment = "Production",
    DeployedBy = "admin@company.com",
    OnlineComputersOnly = true
};

var result = await client.BulkDeployAsync(deployRequest);
Console.WriteLine($"Deployments Created: {result.DeploymentsCreated}");
```

## cURL Examples

### Get Computers

```bash
curl -X GET "https://localhost:7001/api/ActiveDirectory/computers" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Bulk Deploy

```bash
curl -X POST "https://localhost:7001/api/ActiveDirectory/bulk-deploy" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "packageVersionId": 1,
    "organizationalUnit": "OU=IT,DC=company,DC=com",
    "environment": "Production",
    "deploymentType": "Release",
    "requiresApproval": false,
    "deployedBy": "admin@company.com",
    "enabledComputersOnly": true,
    "onlineComputersOnly": true
  }'
```

## Common Scenarios

### Scenario 1: Deploy to All IT Department Computers (Online Only)

1. Get IT department OU computers
2. Filter by online status
3. Create bulk deployment

```http
POST /api/ActiveDirectory/bulk-deploy
{
  "packageVersionId": 1,
  "organizationalUnit": "OU=IT,OU=Departments,DC=company,DC=com",
  "onlineComputersOnly": true,
  "deployedBy": "it-admin@company.com"
}
```

### Scenario 2: Deploy to Specific Test Machines

```http
POST /api/ActiveDirectory/bulk-deploy
{
  "packageVersionId": 1,
  "targetComputerNames": ["TEST-PC-01", "TEST-PC-02", "TEST-PC-03"],
  "environment": "Testing",
  "deployedBy": "qa-team@company.com"
}
```

### Scenario 3: Schedule Night Deployment

```http
POST /api/ActiveDirectory/bulk-deploy
{
  "packageVersionId": 1,
  "organizationalUnit": "OU=AllComputers,DC=company,DC=com",
  "scheduledFor": "2024-01-20T02:00:00Z",
  "deployedBy": "system-admin@company.com"
}
```

### Scenario 4: Find and Deploy to Windows 10 Machines Only

```http
# Step 1: Search
GET /api/ActiveDirectory/computers?OperatingSystemFilter=Windows 10&EnabledOnly=true

# Step 2: Deploy (use computer names from response)
POST /api/ActiveDirectory/bulk-deploy
{
  "packageVersionId": 1,
  "targetComputerNames": ["PC-001", "PC-002", ...],
  "deployedBy": "admin@company.com"
}
```

## Response Examples

### Successful Bulk Deployment Response

```json
{
  "success": true,
  "totalComputers": 50,
  "deploymentsCreated": 48,
  "deploymentsFailed": 2,
  "results": [
    {
      "computerName": "PC-001",
      "dnsHostName": "PC-001.company.com",
      "deploymentId": 1001,
      "success": true
    },
    {
      "computerName": "PC-002",
      "dnsHostName": "PC-002.company.com",
      "deploymentId": null,
      "success": false,
      "error": "Computer is offline"
    }
  ]
}
```

### Computer List Response

```json
{
  "computers": [
    {
      "name": "PC-001",
      "dnsHostName": "PC-001.company.com",
      "distinguishedName": "CN=PC-001,OU=IT,DC=company,DC=com",
      "operatingSystem": "Windows 10 Enterprise",
      "operatingSystemVersion": "10.0 (19045)",
      "enabled": true,
      "lastLogon": "2024-01-15T08:30:00Z",
      "description": "IT Department Workstation",
      "location": "Building A - Floor 3",
      "isOnline": true
    }
  ],
  "totalCount": 150,
  "enabledCount": 145,
  "onlineCount": 130
}
```
