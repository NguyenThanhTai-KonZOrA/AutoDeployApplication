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

export const APPLICATION_ICONS = [
    { value: "ClientLauncherApplogo.ico", label: "Client Launcher App" },
    { value: "EntryAutoImage.ico", label: "Entry Auto Image" },
    { value: "HTCasinoEntrylogo.ico", label: "HT Casino Entry" },
    { value: "LevyManualActiveticket.ico", label: "Levy Manual Active Ticket" },
    { value: "LevyNotifyRemainingTimeLogo.ico", label: "Levy Notify Remaining Time" },
    { value: "LEVYPOSlogo.ico", label: "LEVY POS" },
    { value: "LEVYTICKETMONITORlogo.ico", label: "LEVY ticket monitor" },
    { value: "QUEUEDISP.ico", label: "Queue Display" },
    { value: "QUEUkiosklogo.ico", label: "Queue Kiosk" },
]