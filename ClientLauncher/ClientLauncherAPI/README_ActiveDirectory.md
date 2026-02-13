# Active Directory Integration

Tính năng tích hợp Active Directory cho phép bạn:
- Lấy danh sách máy tính từ Active Directory
- Deploy ứng dụng hàng loạt đến các máy tính trong AD
- Quản lý deployment theo Organizational Unit (OU)

## Cấu Hình

### 1. Cài Đặt NuGet Packages

Các package sau đã được thêm vào `ClientLauncher.Implement.csproj`:
```xml
<PackageReference Include="System.DirectoryServices" Version="9.0.0" />
<PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.0" />
```

### 2. Quyền Truy Cập Active Directory

API phải chạy với account có quyền đọc Active Directory:
- **Development**: Chạy Visual Studio với account có quyền domain user
- **Production**: Configure Application Pool identity hoặc Service Account có quyền read AD

### 3. Network Requirements

- Server phải có kết nối đến Domain Controller
- Port 389 (LDAP) hoặc 636 (LDAPS) phải được mở

## API Endpoints

### 1. Lấy Tất Cả Máy Tính

```http
GET /api/ActiveDirectory/computers
GET /api/ActiveDirectory/computers?EnabledOnly=true&CheckOnlineStatus=true
```

**Query Parameters:**
- `OrganizationalUnit`: Đường dẫn OU (optional)
- `SearchPattern`: Pattern tìm kiếm tên máy (optional)
- `EnabledOnly`: Chỉ lấy máy enabled (default: true)
- `CheckOnlineStatus`: Kiểm tra máy online (default: false)
- `OperatingSystemFilter`: Lọc theo OS (optional)

**Response:**
```json
{
  "computers": [
    {
      "name": "DESKTOP-001",
      "dnsHostName": "DESKTOP-001.domain.com",
      "distinguishedName": "CN=DESKTOP-001,OU=Computers,DC=domain,DC=com",
      "operatingSystem": "Windows 10 Enterprise",
      "operatingSystemVersion": "10.0 (19045)",
      "enabled": true,
      "lastLogon": "2024-01-15T10:30:00Z",
      "description": "User workstation",
      "location": "Building A - Floor 2",
      "isOnline": true
    }
  ],
  "totalCount": 150,
  "enabledCount": 145,
  "onlineCount": 120
}
```

### 2. Lấy Máy Tính Theo Tên

```http
GET /api/ActiveDirectory/computers/{computerName}
```

**Example:**
```http
GET /api/ActiveDirectory/computers/DESKTOP-001
```

### 3. Lấy Danh Sách Organizational Units

```http
GET /api/ActiveDirectory/organizational-units
```

**Response:**
```json
[
  {
    "name": "Computers",
    "distinguishedName": "OU=Computers,DC=domain,DC=com",
    "description": "All company computers",
    "computerCount": 150
  },
  {
    "name": "Workstations",
    "distinguishedName": "OU=Workstations,OU=Computers,DC=domain,DC=com",
    "description": "User workstations",
    "computerCount": 120
  }
]
```

### 4. Lấy Máy Tính Trong OU

```http
GET /api/ActiveDirectory/organizational-units/{ouPath}/computers
```

**Example:**
```http
GET /api/ActiveDirectory/organizational-units/OU=Workstations,OU=Computers,DC=domain,DC=com/computers
```

### 5. Tìm Kiếm Máy Tính

```http
GET /api/ActiveDirectory/computers/search/{searchPattern}
```

**Example:**
```http
GET /api/ActiveDirectory/computers/search/DESKTOP
```

### 6. Kiểm Tra Máy Online

```http
GET /api/ActiveDirectory/computers/{computerName}/online
```

**Response:**
```json
{
  "computerName": "DESKTOP-001",
  "isOnline": true,
  "checkedAt": "2024-01-15T10:45:00Z"
}
```

### 7. Deploy Hàng Loạt Đến Máy AD

```http
POST /api/ActiveDirectory/bulk-deploy
```

**Request Body:**
```json
{
  "packageVersionId": 123,
  "organizationalUnit": "OU=Workstations,OU=Computers,DC=domain,DC=com",
  "environment": "Production",
  "deploymentType": "Release",
  "requiresApproval": false,
  "deployedBy": "admin@domain.com",
  "scheduledFor": null,
  "enabledComputersOnly": true,
  "onlineComputersOnly": true
}
```

**Hoặc deploy đến danh sách máy cụ thể:**
```json
{
  "packageVersionId": 123,
  "targetComputerNames": [
    "DESKTOP-001",
    "DESKTOP-002",
    "LAPTOP-005"
  ],
  "environment": "Production",
  "deploymentType": "Release",
  "requiresApproval": false,
  "deployedBy": "admin@domain.com",
  "enabledComputersOnly": true,
  "onlineComputersOnly": true
}
```

**Response:**
```json
{
  "success": true,
  "totalComputers": 120,
  "deploymentsCreated": 115,
  "deploymentsFailed": 5,
  "results": [
    {
      "computerName": "DESKTOP-001",
      "dnsHostName": "DESKTOP-001.domain.com",
      "deploymentId": 456,
      "success": true
    },
    {
      "computerName": "DESKTOP-002",
      "dnsHostName": "DESKTOP-002.domain.com",
      "deploymentId": null,
      "success": false,
      "error": "Computer is offline"
    }
  ]
}
```

### 8. Preview AD Sync

```http
POST /api/ActiveDirectory/sync/preview
```

**Request Body:**
```json
{
  "organizationalUnit": "OU=Computers,DC=domain,DC=com",
  "autoRegisterNewMachines": true,
  "updateExistingMachines": true,
  "enabledOnly": true
}
```

## Ví Dụ Sử Dụng

### Scenario 1: Deploy đến tất cả máy trong một OU

```bash
# Bước 1: Lấy danh sách OU
curl -X GET "https://api.domain.com/api/ActiveDirectory/organizational-units"

# Bước 2: Xem trước máy tính trong OU
curl -X GET "https://api.domain.com/api/ActiveDirectory/organizational-units/OU=IT,DC=domain,DC=com/computers"

# Bước 3: Deploy
curl -X POST "https://api.domain.com/api/ActiveDirectory/bulk-deploy" \
  -H "Content-Type: application/json" \
  -d '{
    "packageVersionId": 123,
    "organizationalUnit": "OU=IT,DC=domain,DC=com",
    "environment": "Production",
    "deployedBy": "admin@domain.com",
    "onlineComputersOnly": true
  }'
```

### Scenario 2: Deploy đến máy cụ thể

```bash
# Bước 1: Tìm kiếm máy
curl -X GET "https://api.domain.com/api/ActiveDirectory/computers/search/DESKTOP"

# Bước 2: Kiểm tra máy online
curl -X GET "https://api.domain.com/api/ActiveDirectory/computers/DESKTOP-001/online"

# Bước 3: Deploy
curl -X POST "https://api.domain.com/api/ActiveDirectory/bulk-deploy" \
  -H "Content-Type: application/json" \
  -d '{
    "packageVersionId": 123,
    "targetComputerNames": ["DESKTOP-001", "DESKTOP-002"],
    "environment": "Production",
    "deployedBy": "admin@domain.com"
  }'
```

## Lưu Ý Quan Trọng

### Performance
- Với domain lớn (>1000 máy), nên sử dụng `OrganizationalUnit` để filter
- `CheckOnlineStatus=true` sẽ làm chậm query do phải ping từng máy
- Nên chạy bulk deployment vào giờ thấp điểm

### Security
- Không expose AD info ra public API
- Sử dụng authentication/authorization cho các endpoint này
- Log tất cả AD queries để audit

### Troubleshooting

**Lỗi: "Unable to contact the server"**
- Kiểm tra network connectivity đến Domain Controller
- Kiểm tra firewall rules (port 389/636)

**Lỗi: "Access is denied"**
- Service account không có quyền read AD
- Grant quyền "Read All Properties" cho service account

**Lỗi: "The server is not operational"**
- Domain Controller không available
- Kiểm tra DNS resolution

## LDAP Query Examples

Để query specific OUs, sử dụng Distinguished Name format:

```
OU=Computers,DC=domain,DC=com
OU=IT,OU=Computers,DC=domain,DC=com
OU=Workstations,OU=IT,OU=Computers,DC=domain,DC=com
```

## Best Practices

1. **Caching**: Cache danh sách computers trong vài phút để tránh query AD liên tục
2. **Filtering**: Luôn filter theo OU hoặc searchPattern với domain lớn
3. **Online Check**: Chỉ enable `CheckOnlineStatus` khi cần thiết
4. **Batch Deployment**: Deploy theo batch nhỏ (50-100 máy/lần)
5. **Scheduling**: Sử dụng `ScheduledFor` để deploy ngoài giờ làm việc
6. **Error Handling**: Luôn check `deploymentsFailed` trong response

## Monitoring

Monitor các metrics sau:
- AD query response time
- Number of computers found
- Online/offline ratio
- Deployment success rate
- Failed deployment reasons
