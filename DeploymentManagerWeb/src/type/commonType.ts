import type { Permission } from "../constants/roles";

export const WorkFlowService = {
    NewMembership: 1,
    ExistingMembership: 2,
}

export interface NavItem {
    key: string;
    title: string;
    href: string;
    icon: React.ElementType;
    requiredPermission?: Permission;
}

export type ApiEnvelope<T> = {
    status: number;
    data: T;
    success: boolean;
};

export const AVAILABLE_ICONS = [
    { value: "app_default.ico", label: "- Default Icon" },
    { value: "app_cage.ico", label: "- Cage Icon" },
    { value: "app_finance.ico", label: "- Finance Icon" },
    { value: "app_htr.ico", label: "- HTR Icon" },
];