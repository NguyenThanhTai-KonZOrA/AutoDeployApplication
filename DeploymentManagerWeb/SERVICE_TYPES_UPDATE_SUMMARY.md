# Service Types Management - Update Summary

## Tổng quan
Đã thêm chức năng quản lý Service Types cho Counter trong trang AdminCounterPage.

## Các thay đổi đã thực hiện

### 1. AdminCounterPage.tsx - Thêm chức năng Update Service Types

#### Imports mới:
- `EditIcon` từ @mui/icons-material
- `Checkbox`, `FormControlLabel`, `Divider` từ @mui/material
- `serviceTypeService` từ queueService
- Các types: `SummarryServiceTypeResponse`, `CounterWithServiceTypesResponse`, `UpdateCounterServiceTypesRequest`, `CounterServiceTypeItem`

#### States mới:
```typescript
// Service Types dialog states
const [serviceTypesDialogOpen, setServiceTypesDialogOpen] = useState(false);
const [serviceTypesLoading, setServiceTypesLoading] = useState(false);
const [allServiceTypes, setAllServiceTypes] = useState<SummarryServiceTypeResponse[]>([]);
const [selectedServiceTypes, setSelectedServiceTypes] = useState<number[]>([]);
const [currentEditingCounter, setCurrentEditingCounter] = useState<CountersReportResponse | null>(null);
const [serviceTypesSaving, setServiceTypesSaving] = useState(false);
```

#### Các handler functions mới:

1. **handleOpenServiceTypesDialog(counter)**
   - Mở dialog và load danh sách service types
   - Call API `getSummarryServiceTypes()` để lấy tất cả service types
   - Call API `getCounterServiceTypes(counterId)` để lấy service types đã assign
   - Tự động check các service types đã được assign

2. **handleCloseServiceTypesDialog()**
   - Đóng dialog và reset states

3. **handleToggleServiceType(serviceTypeId)**
   - Toggle checkbox của từng service type
   - Add/remove service type ID từ mảng selectedServiceTypes

4. **handleSelectAllServiceTypes()**
   - Chọn tất cả hoặc bỏ chọn tất cả service types

5. **handleSaveServiceTypes()**
   - Lưu danh sách service types đã chọn
   - Call API `updateCounterServiceTypes()` với payload:
     ```typescript
     {
       counterId: number,
       serviceTypes: [
         { serviceTypeId: number, priority: number }
       ]
     }
     ```
   - Hiển thị thông báo thành công/lỗi
   - Reload danh sách counters

#### UI Updates:

**Cột Service Type trong table:**
- Hiển thị danh sách service types dạng Chips
- Thêm button Edit (icon EditIcon) để mở dialog
- Nếu chưa có service types, hiển thị "No service types"

**Dialog Manage Service Types:**
- Header: "Manage Service Types - Counter {id}"
- Checkbox "Select All" để chọn/bỏ chọn tất cả
- Danh sách service types dạng checkboxes
  - Mỗi item hiển thị: Name (bold) và Description (caption)
  - Tự động check các service types đã được assign
- Footer: 
  - Hiển thị số lượng service types đã chọn
  - Button Cancel và Save
- Loading state khi đang tải dữ liệu
- Alert nếu chưa có service types trong hệ thống

### 2. API Service (queueService.ts)

Các API đã có sẵn và được sử dụng:

```typescript
// Get all service types (summary)
serviceTypeService.getSummarryServiceTypes(): Promise<SummarryServiceTypeResponse[]>

// Get counter's current service types
counterService.getCounterServiceTypes(counterId: number): Promise<CounterWithServiceTypesResponse[]>

// Update counter's service types
counterService.updateCounterServiceTypes(request: UpdateCounterServiceTypesRequest): Promise<boolean>
```

### 3. Types (type.ts)

Các interface đã có sẵn:

```typescript
export interface SummarryServiceTypeResponse {
  id: number;
  name: string;
  description: string;
}

export interface CounterWithServiceTypesResponse {
  id: number;
  name: string;
  description: string;
  hostName: string;
  serviceTypes: ServiceTypeResponse[];
}

export interface UpdateCounterServiceTypesRequest {
  counterId: number;
  serviceTypes: CounterServiceTypeItem[];
}

export interface CounterServiceTypeItem {
  serviceTypeId: number;
  priority: number;
}

export interface CountersReportResponse {
  // ... other fields
  serviceTypes: ServiceTypeInCounterResponse[];
}

export interface ServiceTypeInCounterResponse {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  priority: number;
}
```

## Workflow sử dụng

1. User vào trang Admin Counter Page
2. Ở cột "Service Type", click vào button Edit (icon bút)
3. Dialog mở ra hiển thị:
   - Tất cả service types có trong hệ thống
   - Service types đã được assign cho counter này được tự động check
4. User có thể:
   - Check/uncheck từng service type
   - Click "Select All" để chọn/bỏ chọn tất cả
5. Click "Save" để lưu
6. Hệ thống:
   - Call API update service types
   - Hiển thị thông báo thành công/lỗi
   - Reload danh sách counters để cập nhật UI

## Features

✅ Load danh sách tất cả service types từ API
✅ Load service types hiện tại của counter
✅ Tự động check các service types đã được assign
✅ Chọn/bỏ chọn từng service type
✅ Chọn/bỏ chọn tất cả service types
✅ Update service types cho counter
✅ Loading states
✅ Error handling
✅ Success/Error notifications (Snackbar)
✅ UI/UX nhất quán với các trang khác
✅ Responsive design
✅ TypeScript type safety

## Testing

1. Test load service types khi mở dialog
2. Test checkbox selection (single và select all)
3. Test save service types
4. Test error handling
5. Test loading states
6. Test với counter chưa có service types
7. Test với counter đã có service types

## Notes

- Priority được tự động gán theo thứ tự trong mảng (index + 1)
- Dialog chỉ hiển thị khi có ít nhất 1 service type trong hệ thống
- Nếu chưa có service types, hiển thị Alert khuyến nghị tạo service types trước
