# Active Directory Troubleshooting Guide

## Lỗi Phổ Biến và Cách Khắc Phục

### 1. Error 0x8007202B - "A referral was returned from the server"

#### Mô Tả
```
System.DirectoryServices.DirectoryServicesCOMException (0x8007202B): 
A referral was returned from the server.
```

#### Nguyên Nhân
- **Multi-Domain Environment**: Domain có child domains hoặc là một phần của forest
- **LDAP Referrals**: Domain Controller trả về referral đến DC khác
- **Global Catalog Query**: Query cross-domain data
- **Authentication Context**: Service account không có quyền follow referrals

#### Giải Pháp Đã Implement

**1. Enable ReferralChasing:**
```csharp
searcher.ReferralChasing = ReferralChasingOption.All;
```

**Options:**
- `ReferralChasingOption.None` - Không follow referrals (default)
- `ReferralChasingOption.Subordinate` - Follow referrals trong cùng domain
- `ReferralChasingOption.External` - Follow referrals ra ngoài domain
- `ReferralChasingOption.All` - Follow tất cả referrals (recommended)

**2. Set SearchScope:**
```csharp
searcher.SearchScope = SearchScope.Subtree;
```

**3. Fallback to PrincipalContext:**
Code đã được update để tự động fallback sang PrincipalContext nếu LDAP query fail:

```csharp
catch (DirectoryServicesCOMException comEx) when (comEx.ErrorCode == -2147016357)
{
    _logger.LogWarning(comEx, "LDAP referral error, falling back to PrincipalContext method");
    
    if (_usePrincipalContextFallback && string.IsNullOrWhiteSpace(request?.OrganizationalUnit))
    {
        return GetComputersUsingPrincipalContext(request);
    }
    throw;
}
```

#### Verification
Sau khi fix, code sẽ:
1. Try sử dụng DirectorySearcher với ReferralChasing enabled
2. Nếu gặp referral error, tự động chuyển sang PrincipalContext
3. Log warning để admin biết có fallback xảy ra

---

### 2. Error 0x8007052E - "Logon failure: unknown user name or bad password"

#### Giải Pháp
```csharp
// Specify credentials explicitly
var entry = new DirectoryEntry(ldapPath, "DOMAIN\\Username", "Password");

// Or use current user context
var entry = new DirectoryEntry(ldapPath);
entry.AuthenticationType = AuthenticationTypes.Secure;
```

---

### 3. Error 0x80072020 - "An operations error occurred"

#### Nguyên Nhân
- Query quá lớn (too many results)
- Timeout

#### Giải Pháp
```csharp
searcher.PageSize = 1000;  // Already implemented
searcher.SizeLimit = 0;     // No limit on results
searcher.ServerTimeLimit = TimeSpan.FromMinutes(5);
```

---

### 4. Error 0x8007203A - "The server is not operational"

#### Nguyên Nhân
- Domain Controller không available
- Network connectivity issues
- DNS resolution failed

#### Kiểm Tra
```powershell
# Test DNS resolution
nslookup domain.com

# Test LDAP connectivity
Test-NetConnection -ComputerName dc.domain.com -Port 389

# Verify domain trust
nltest /domain_trusts
```

---

### 5. Performance Issues với Large Domain

#### Optimization Đã Implement

**1. Paging:**
```csharp
searcher.PageSize = 1000;
```

**2. Property Load Filtering:**
```csharp
searcher.PropertiesToLoad.AddRange(new[]
{
    "name", "dNSHostName", "operatingSystem"
    // Only load needed properties
});
```

**3. Filtered Queries:**
```csharp
searcher.Filter = "(&(objectClass=computer)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
```

---

## Best Practices

### 1. Authentication

**Development:**
```csharp
// Use current user credentials (runs with your AD account)
var entry = new DirectoryEntry(ldapPath);
```

**Production:**
```csharp
// Use service account
var username = Configuration["AD:Username"];
var password = Configuration["AD:Password"];
var entry = new DirectoryEntry(ldapPath, username, password, AuthenticationTypes.Secure);
```

### 2. Connection String Format

**Correct LDAP Paths:**
```
LDAP://DC=domain,DC=com                           ✅ Root domain
LDAP://OU=Computers,DC=domain,DC=com             ✅ Specific OU
LDAP://domain.com                                 ✅ DNS name
LDAP://dc.domain.com/DC=domain,DC=com            ✅ Specific DC
GC://DC=domain,DC=com                            ✅ Global Catalog
```

**Incorrect:**
```
LDAP://domain,com                                 ❌
LDAP:DC=domain,DC=com                            ❌ Missing //
domain.com                                        ❌ Missing protocol
```

### 3. Error Handling

```csharp
try
{
    // AD query
}
catch (DirectoryServicesCOMException comEx)
{
    switch (comEx.ErrorCode)
    {
        case -2147016657: // 0x8007052E - Bad credentials
            _logger.LogError("Authentication failed");
            break;
        case -2147016357: // 0x8007202B - Referral
            _logger.LogWarning("Referral returned, retrying...");
            break;
        case -2147023570: // 0x80072020 - Operations error
            _logger.LogError("Operation error, check query size");
            break;
        default:
            _logger.LogError(comEx, "AD error: {ErrorCode}", comEx.ErrorCode);
            break;
    }
}
```

---

## Configuration Settings

### appsettings.json

```json
{
  "ActiveDirectory": {
    "Domain": "company.com",
    "Username": "svc_deployment@company.com",
    "Password": "encrypted_password",
    "Container": "OU=Computers,DC=company,DC=com",
    "EnableReferralChasing": true,
    "PageSize": 1000,
    "Timeout": 300,
    "UsePrincipalContextFallback": true
  }
}
```

### Inject Configuration

```csharp
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ADSettings _settings;
    
    public ActiveDirectoryService(IOptions<ADSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

---

## Testing

### Unit Test with Mock

```csharp
[Fact]
public async Task GetComputers_WithReferral_ShouldFallbackToPrincipalContext()
{
    // Arrange
    var service = new ActiveDirectoryService(_logger);
    
    // Act
    var result = await service.GetAllComputersAsync();
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.TotalCount >= 0);
}
```

### Integration Test

```powershell
# Test API endpoint
Invoke-RestMethod -Uri "https://localhost:7001/api/ActiveDirectory/computers" `
    -Headers @{ "Authorization" = "Bearer $token" } `
    -Method Get
```

---

## Monitoring & Logging

### Log Levels

```csharp
_logger.LogTrace("Querying AD with filter: {Filter}", filter);
_logger.LogDebug("Found {Count} computers", count);
_logger.LogInformation("Retrieved computers successfully");
_logger.LogWarning("Referral error, using fallback method");
_logger.LogError(ex, "Failed to query AD");
```

### Performance Metrics

```csharp
var sw = Stopwatch.StartNew();
var computers = await _adService.GetAllComputersAsync();
sw.Stop();

_logger.LogInformation(
    "AD query completed in {Elapsed}ms. Found {Count} computers",
    sw.ElapsedMilliseconds, 
    computers.TotalCount);
```

---

## Network Requirements

### Required Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 389  | LDAP     | Standard LDAP queries |
| 636  | LDAPS    | Secure LDAP (SSL) |
| 3268 | GC       | Global Catalog |
| 3269 | GC SSL   | Secure Global Catalog |
| 88   | Kerberos | Authentication |
| 53   | DNS      | Name resolution |

### Firewall Rules

```powershell
# Allow LDAP outbound
New-NetFirewallRule -DisplayName "Allow LDAP" -Direction Outbound -Protocol TCP -RemotePort 389

# Allow LDAPS outbound
New-NetFirewallRule -DisplayName "Allow LDAPS" -Direction Outbound -Protocol TCP -RemotePort 636
```

---

## FAQ

### Q: Tại sao GetAllComputers() chậm?
**A:** 
- Domain có nhiều computers (>1000)
- Không dùng paging
- CheckOnlineStatus = true (ping từng máy)

**Fix:**
- Enable paging (đã có)
- Filter theo OU cụ thể
- Tắt CheckOnlineStatus nếu không cần

### Q: Làm sao query specific OU?
**A:**
```http
GET /api/ActiveDirectory/computers?OrganizationalUnit=OU=IT,DC=company,DC=com
```

### Q: Service account cần quyền gì?
**A:**
- Read all properties on Computer objects
- List contents of OUs
- Thường thì Domain Users là đủ cho read-only operations

### Q: Có thể cache kết quả không?
**A:** Có, nên cache 5-10 phút:
```csharp
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetComputers()
{
    // ...
}
```

---

## Summary of Fixes

✅ **Added ReferralChasing = All** - Handle multi-domain environments  
✅ **Added SearchScope = Subtree** - Search all levels  
✅ **Implemented PrincipalContext Fallback** - Alternative method if LDAP fails  
✅ **Improved Error Handling** - Catch and handle specific COM exceptions  
✅ **Enhanced Logging** - Better diagnostics  
✅ **Fixed GetDomainPath()** - Better domain path resolution  

Code hiện tại đã robust và handle được hầu hết các AD environments phổ biến!
