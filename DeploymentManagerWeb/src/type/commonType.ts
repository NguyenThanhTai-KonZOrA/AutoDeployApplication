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